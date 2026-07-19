#nullable enable

using System;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public sealed class WorldCardView : MonoBehaviour
{
    private const float MinimumSize = 0.01f;
    private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    private static readonly int CullId = Shader.PropertyToID("_Cull");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int AlphaClipId = Shader.PropertyToID("_AlphaClip");
    private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");

    private static readonly int[] FrontTriangles = { 0, 1, 2, 0, 2, 3 };
    private static readonly int[] BackTriangles = { 4, 5, 6, 4, 6, 7 };

    [Header("Card")]
    [SerializeField] private CardAtlas? cardAtlas;
    [SerializeField] private CardSuit suit = CardSuit.Hearts;
    [SerializeField] private CardValue value = CardValue.Ace;

    [Header("World Render")]
    [SerializeField] private Vector2 size = new Vector2(0.7f, 1f);
    [SerializeField, Min(0f)] private float thickness = 0.01f;
    [SerializeField] private Material? materialTemplate;
    [SerializeField] private GameObject glitch;

    private GameManager? _gameManager;
    private CameraController? _localCameraController;
    private bool _isHidden;

    private MeshFilter? _meshFilter;
    private MeshRenderer? _meshRenderer;
    private Mesh? _mesh;
    private Material? _frontMaterial;
    private Material? _backMaterial;

    public CardAtlas? CardAtlas => cardAtlas;
    public CardSuit Suit => suit;
    public CardValue Value => value;
    public Vector2 Size => size;
    public float Thickness => thickness;

    private void Awake()
    {
        _gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Reset()
    {
        Refresh();
    }

    private void OnEnable()
    {
        _isHidden = Application.isPlaying && ShouldHideCard();
        Refresh();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        bool shouldHide = ShouldHideCard();
        if (_isHidden == shouldHide)
        {
            return;
        }

        _isHidden = shouldHide;
        Refresh();
    }

    private void OnValidate()
    {
        size = new Vector2(Mathf.Max(MinimumSize, size.x), Mathf.Max(MinimumSize, size.y));
        thickness = Mathf.Max(0f, thickness);
        Refresh();
    }

    private void OnDestroy()
    {
        DestroyGeneratedObject(_frontMaterial);
        DestroyGeneratedObject(_backMaterial);
        DestroyGeneratedObject(_mesh);
    }

    public void Initialize(CardAtlas atlas, CardData card)
    {
        if (atlas == null)
        {
            throw new ArgumentNullException(nameof(atlas));
        }

        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        cardAtlas = atlas;
        SetCard(card);
    }

    public void SetAtlas(CardAtlas atlas)
    {
        if (atlas == null)
        {
            throw new ArgumentNullException(nameof(atlas));
        }

        cardAtlas = atlas;
        Refresh();
    }

    public void SetCard(CardData card)
    {
        if (card == null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        SetCard(card.Suit, card.Value);
    }

    public void SetCard(CardSuit cardSuit, CardValue cardValue)
    {
        suit = cardSuit;
        value = cardValue;
        Refresh();
    }

    public void Refresh()
    {
        MeshRenderer meshRenderer = GetRequiredMeshRenderer();

        if (cardAtlas == null)
        {
            meshRenderer.enabled = false;
            return;
        }

        Sprite faceSprite = GetActiveFrontSprite();
        Sprite backSprite = cardAtlas.GetBackSprite();

        if (faceSprite == null || backSprite == null)
        {
            meshRenderer.enabled = false;
            return;
        }

        MeshFilter meshFilter = GetRequiredMeshFilter();
        EnsureMesh(meshFilter);
        RebuildMesh(faceSprite, backSprite);
        ApplyMaterials(meshRenderer, faceSprite, backSprite);
        meshRenderer.enabled = true;
        if (glitch != null)
            glitch.SetActive(_isHidden);
    }

    private Sprite? GetActiveFrontSprite()
    {
        if (cardAtlas == null) return null;

        return _isHidden && cardAtlas.GetCensoredSprite() != null
            ? cardAtlas.GetCensoredSprite()
            : cardAtlas.GetFaceSprite(suit, value);
    }

    private Skeleton? GetCardOwner()
    {
        PlayerInventoryOwner? inventoryOwner = GetComponentInParent<PlayerInventoryOwner>();
        if (inventoryOwner != null)
            return inventoryOwner.OwnerSkeleton;
        
        CardStack? stack = GetComponentInParent<CardStack>();
        if (stack != null)
            return stack.Owner;
        
        return null;
    }
    private bool ShouldHideCard()
    {
        if (cardAtlas == null || cardAtlas.GetCensoredSprite() == null)
        {
            return false;
        }

        SkeletonBody? localBody = _gameManager?.LocalPlayer?.Body;
        if (localBody == null)
        {
            return true;
        }

        GhostMode? ghost = localBody.GetComponent<GhostMode>();
        if (ghost != null && ghost.IsGhost)
        {
            return true;
        }

        if (_localCameraController == null)
        {
            _localCameraController = localBody.GetComponent<CameraController>();
        }

        if (_localCameraController == null)
        {
            return true;
        }

        Skeleton? owner = GetCardOwner();
        Skeleton? localPlayer = _gameManager?.LocalPlayer;
        if (owner != null && localPlayer != null)
        {
            if (owner == localPlayer)
            {
                if (IsHeldWithSkullInOtherHand(localBody, _localCameraController))
                    return false;
                return !IsVisibleFromSkull(_localCameraController);
            }

            if (owner.team == localPlayer.team)
                return false;

            return true;
        }

        if (IsHeldWithSkullInOtherHand(localBody, _localCameraController))
        {
            return false;
        }

        return !IsVisibleFromSkull(_localCameraController);
    }

    private bool IsHeldWithSkullInOtherHand(SkeletonBody localBody, CameraController cameraController)
    {
        PlayerInventoryOwner? inventoryOwner = localBody.GetComponent<PlayerInventoryOwner>();
        Transform skullViewpoint = cameraController.SkullViewpoint;
        BodyPart? skull = skullViewpoint != null ? skullViewpoint.GetComponent<BodyPart>() : null;
        if (inventoryOwner == null || skull == null)
        {
            return false;
        }

        MeshRenderer cardRenderer = GetRequiredMeshRenderer();
        return IsHeldCardsAndSkull(inventoryOwner.leftHand, inventoryOwner.rightHand, cardRenderer, skull.Item) ||
               IsHeldCardsAndSkull(inventoryOwner.rightHand, inventoryOwner.leftHand, cardRenderer, skull.Item);
    }

    private static bool IsHeldCardsAndSkull(
        PlayerHand? cardsHand,
        PlayerHand? skullHand,
        Renderer cardRenderer,
        BodyPartItem skullItem)
    {
        return cardsHand != null && cardsHand.Item is CardsItem &&
               cardsHand.ContainsHeldItemRenderer(cardRenderer) &&
               skullHand != null && ReferenceEquals(skullHand.Item, skullItem);
    }

    private bool IsVisibleFromSkull(CameraController cameraController)
    {
        Transform skull = cameraController.SkullViewpoint;
        if (skull == null || cameraController.firstPersonCam == null ||
            cameraController.brain == null || cameraController.brain.OutputCamera == null)
        {
            return false;
        }

        Vector3 skullToCard = transform.position - skull.position;
        float forwardDistance = Vector3.Dot(skullToCard, skull.forward);
        if (forwardDistance <= 0f)
        {
            return false;
        }

        float verticalHalfAngle = 80 * Mathf.Deg2Rad;
        float pyramidHalfHeight = forwardDistance * Mathf.Tan(verticalHalfAngle);
        float pyramidHalfWidth = pyramidHalfHeight;
        float horizontalDistance = Mathf.Abs(Vector3.Dot(skullToCard, skull.right));
        float verticalDistance = Mathf.Abs(Vector3.Dot(skullToCard, skull.up));

        bool isInsideViewPyramid = horizontalDistance <= pyramidHalfWidth &&
                                   verticalDistance <= pyramidHalfHeight;
        return isInsideViewPyramid;
    }

    private MeshFilter GetRequiredMeshFilter()
    {
        if (_meshFilter == null)
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        return _meshFilter;
    }

    private MeshRenderer GetRequiredMeshRenderer()
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        return _meshRenderer;
    }

    private void EnsureMesh(MeshFilter meshFilter)
    {
        if (_mesh == null)
        {
            _mesh = new Mesh
            {
                name = "Generated World Card Mesh",
                hideFlags = HideFlags.DontSave
            };
            _mesh.MarkDynamic();
        }

        if (meshFilter.sharedMesh != _mesh)
        {
            meshFilter.sharedMesh = _mesh;
        }
    }

    private void RebuildMesh(Sprite faceSprite, Sprite backSprite)
    {
        if (_mesh == null)
        {
            return;
        }

        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        float halfThickness = thickness * 0.5f;

        Vector3[] vertices =
        {
            new Vector3(-halfWidth, -halfHeight, -halfThickness),
            new Vector3(-halfWidth, halfHeight, -halfThickness),
            new Vector3(halfWidth, halfHeight, -halfThickness),
            new Vector3(halfWidth, -halfHeight, -halfThickness),
            new Vector3(-halfWidth, -halfHeight, halfThickness),
            new Vector3(halfWidth, -halfHeight, halfThickness),
            new Vector3(halfWidth, halfHeight, halfThickness),
            new Vector3(-halfWidth, halfHeight, halfThickness)
        };

        Vector3[] normals =
        {
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.back,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        Vector2[] uvs = new Vector2[8];
        SpriteUvRect faceUv = GetSpriteUvRect(faceSprite);
        SpriteUvRect backUv = GetSpriteUvRect(backSprite);

        uvs[0] = faceUv.BottomLeft;
        uvs[1] = faceUv.TopLeft;
        uvs[2] = faceUv.TopRight;
        uvs[3] = faceUv.BottomRight;
        uvs[4] = backUv.BottomRight;
        uvs[5] = backUv.BottomLeft;
        uvs[6] = backUv.TopLeft;
        uvs[7] = backUv.TopRight;

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.normals = normals;
        _mesh.uv = uvs;
        _mesh.subMeshCount = 2;
        _mesh.SetTriangles(FrontTriangles, 0);
        _mesh.SetTriangles(BackTriangles, 1);
        _mesh.RecalculateBounds();
    }

    private void ApplyMaterials(MeshRenderer meshRenderer, Sprite faceSprite, Sprite backSprite)
    {
        _frontMaterial = CreateOrUpdateMaterial(_frontMaterial, faceSprite, "Generated Card Face Material");
        _backMaterial = CreateOrUpdateMaterial(_backMaterial, backSprite, "Generated Card Back Material");

        if (_frontMaterial == null || _backMaterial == null)
        {
            meshRenderer.enabled = false;
            return;
        }

        meshRenderer.sharedMaterials = new[] { _frontMaterial, _backMaterial };
    }

    private Material? CreateOrUpdateMaterial(Material? currentMaterial, Sprite sprite, string materialName)
    {
        Shader shader = materialTemplate != null ? materialTemplate.shader : FindDefaultShader();
        if (shader == null)
        {
            Debug.LogError("WorldCardView could not find a compatible shader for card rendering.", this);
            return currentMaterial;
        }

        if (currentMaterial == null || currentMaterial.shader != shader)
        {
            DestroyGeneratedObject(currentMaterial);
            currentMaterial = materialTemplate != null ? new Material(materialTemplate) : new Material(shader);
            currentMaterial.hideFlags = HideFlags.DontSave;
        }

        currentMaterial.name = materialName;
        ConfigureMaterial(currentMaterial, sprite.texture);
        return currentMaterial;
    }

    private static Shader FindDefaultShader()
    {
        return Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Transparent")
            ?? Shader.Find("Unlit/Texture")
            ?? Shader.Find("Standard");
    }

    private static void ConfigureMaterial(Material material, Texture texture)
    {
        if (material.HasProperty(MainTextureId))
        {
            material.SetTexture(MainTextureId, texture);
        }

        if (material.HasProperty(BaseMapId))
        {
            material.SetTexture(BaseMapId, texture);
        }

        if (material.HasProperty(BaseColorId))
        {
            material.SetColor(BaseColorId, Color.white);
        }

        if (material.HasProperty(ColorId))
        {
            material.SetColor(ColorId, Color.white);
        }

        if (material.HasProperty(SurfaceId))
        {
            material.SetFloat(SurfaceId, 1f);
        }

        if (material.HasProperty(BlendId))
        {
            material.SetFloat(BlendId, 0f);
        }

        if (material.HasProperty(CullId))
        {
            material.SetFloat(CullId, 2f);
        }

        if (material.HasProperty(SrcBlendId))
        {
            material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
        }

        if (material.HasProperty(DstBlendId))
        {
            material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty(ZWriteId))
        {
            material.SetFloat(ZWriteId, 0f);
        }

        if (material.HasProperty(AlphaClipId))
        {
            material.SetFloat(AlphaClipId, 1f);
        }

        if (material.HasProperty(CutoffId))
        {
            material.SetFloat(CutoffId, 0.5f);
        }

        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "Opaque");
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Geometry;
    }

    private static SpriteUvRect GetSpriteUvRect(Sprite sprite)
    {
        Rect rect = sprite.textureRect;
        Texture texture = sprite.texture;

        float xMin = rect.xMin / texture.width;
        float xMax = rect.xMax / texture.width;
        float yMin = rect.yMin / texture.height;
        float yMax = rect.yMax / texture.height;

        return new SpriteUvRect(
            new Vector2(xMin, yMin),
            new Vector2(xMin, yMax),
            new Vector2(xMax, yMax),
            new Vector2(xMax, yMin));
    }

    private static void DestroyGeneratedObject(UnityEngine.Object? target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private readonly struct SpriteUvRect
    {
        public SpriteUvRect(Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight)
        {
            BottomLeft = bottomLeft;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
        }

        public Vector2 BottomLeft { get; }
        public Vector2 TopLeft { get; }
        public Vector2 TopRight { get; }
        public Vector2 BottomRight { get; }
    }
}
