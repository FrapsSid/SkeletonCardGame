using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    [Header("Sensitivity")]
    public float mouseSensitivity = 0.15f;
    public float gamepadSensitivity = 120f;

    [Header("Pitch Clamp")]
    public float minPitch = -30f;
    public float maxPitch = 70f;

    [Header("Smoothing")]
    public float smoothTime = 0.03f;

    [Header("Pivots")]
    public Transform thirdPersonPivot;
    public Transform firstPersonPivot;

    [Header("Cinemachine")]
    public CinemachineCamera thirdPersonCam;
    public CinemachineCamera firstPersonCam;
    public CinemachineBrain brain;

    [Header("First Person Look Limits")]
    public float firstPersonMinPitch = -80f;
    public float firstPersonMaxPitch = 80f;

    [Header("Player Control")]
    public PlayerController playerController;

    [Header("UI State")]
    [SerializeField] private UIStateController uiStateController;

    [Header("Player Body")]
    public SkeletonBody _skeletonBody;
    public Renderer skullRenderer;
    [Header("Rendering")]
    [SerializeField] private ScriptableRendererFeature thirdPersonFilterFeature;

    private InputReader _input;
    private float _yaw;
    private float _pitch;
    private float _currentYaw;
    private float _currentPitch;
    private float _yawVelocity;
    private float _pitchVelocity;
    private float _fpYaw;
    private float _fpPitch;
    private float _fpCurrentYaw;
    private float _fpCurrentPitch;
    private float _fpYawVelocity;
    private float _fpPitchVelocity;
    private bool _isFirstPerson;
    private readonly List<Renderer> _hiddenBodyRenderers = new();
    private PlayerHand _playerHand;
    private BodyPartItem _firstPersonBodyPartItem;
    public bool IsFirstPerson => _isFirstPerson;
    public static event Action<bool> PerspectiveChanged;
    public static bool IsFirstPersonActive { get; private set; }

    private void Awake()
    {
        if (uiStateController == null)
        {
            uiStateController = FindFirstObjectByType<UIStateController>();
        }

        _input = GetComponentInParent<InputReader>();
        _playerHand = GetComponentInParent<PlayerHand>();
        if (_skeletonBody == null)
        {
            _skeletonBody = GetComponentInParent<SkeletonBody>();
        }

        if (_skeletonBody != null)
        {
            _skeletonBody.OnBodyChanged += HandleBodyChanged;
        }

        if (thirdPersonPivot != null)
        {
            _yaw = thirdPersonPivot.eulerAngles.y;
            _pitch = thirdPersonPivot.eulerAngles.x;
        }

        SetFirstPerson(false);
    }

    private void Start()
    {
        CacheFirstPersonBodyPartItem();
        UpdateFirstPersonViewpoint();
    }

    private void OnDisable()
    {
        SetLocalBodyVisible(true);
        if (thirdPersonFilterFeature != null)
        {
            thirdPersonFilterFeature.SetActive(false); 
        }
    }

    private void OnDestroy()
    {
        if (_skeletonBody != null)
        {
            _skeletonBody.OnBodyChanged -= HandleBodyChanged;
        }
        if (thirdPersonFilterFeature != null)
        {
            thirdPersonFilterFeature.SetActive(false); 
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            if (!_isFirstPerson && !HasFirstPersonViewpoint())
            {
                return;
            }
            SetFirstPerson(!_isFirstPerson);
        }
        if (_isFirstPerson && !HasFirstPersonViewpoint())
        {
            SetFirstPerson(false);
        }
    }

    private void LateUpdate()
    {
        if (IsAnyUiOpen())
        {
            return;
        }

        if (_input == null)
        {
            return;
        }

        if (_isFirstPerson)
        {
            UpdateFirstPerson();
        }
        else
        {
            UpdateThirdPerson();
        }
    }

    private void UpdateThirdPerson()
    {
        if (thirdPersonPivot == null)
        {
            return;
        }

        Vector2 look = _input.LookInput;
        bool isMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;
        float sens = isMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

        _yaw += look.x * sens;
        _pitch -= look.y * sens;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _yaw, ref _yawVelocity, smoothTime);
        _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _pitch, ref _pitchVelocity, smoothTime);

        thirdPersonPivot.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
    }

    private void UpdateFirstPerson()
    {
        Transform viewpoint = UpdateFirstPersonViewpoint();
        if (viewpoint == null)
        {
            return;
        }

        Vector2 look = _input.LookInput;
        bool isMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;
        float sens = isMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

        _fpYaw += look.x * sens;
        _fpPitch -= look.y * sens;
        _fpPitch = Mathf.Clamp(_fpPitch, firstPersonMinPitch, firstPersonMaxPitch);

        _fpCurrentYaw = Mathf.SmoothDampAngle(_fpCurrentYaw, _fpYaw, ref _fpYawVelocity, smoothTime);
        _fpCurrentPitch = Mathf.SmoothDampAngle(_fpCurrentPitch, _fpPitch, ref _fpPitchVelocity, smoothTime);

        if (playerController != null)
        {
            playerController.transform.rotation = Quaternion.Euler(0f, _fpCurrentYaw, 0f);
        }

        if (IsDetachedSkullViewpoint(viewpoint))
        {
            viewpoint.rotation = Quaternion.Euler(_fpCurrentPitch, _fpCurrentYaw, 0f);
        }
        else
        {
            viewpoint.localRotation = Quaternion.Euler(_fpCurrentPitch, 0f, 0f);
        }
    }

    private void SetFirstPerson(bool enable)
    {
        _isFirstPerson = enable;
        IsFirstPersonActive = enable;
        PerspectiveChanged?.Invoke(enable);

        if (thirdPersonCam != null)
        {
            thirdPersonCam.Priority = enable ? 0 : 10;
        }

        if (firstPersonCam != null)
        {
            firstPersonCam.Priority = enable ? 10 : 0;
        }

        if (playerController != null)
        {
            playerController.SetFirstPersonLock(enable);
        }

        if (thirdPersonFilterFeature != null)
        {
            thirdPersonFilterFeature.SetActive(!enable);
        }

        SetLocalBodyVisible(!enable);

        if (enable)
        {
            Transform viewpoint = UpdateFirstPersonViewpoint();
            _fpYaw = playerController != null ? playerController.transform.eulerAngles.y : transform.eulerAngles.y;
            _fpPitch = 0f;
            _fpCurrentYaw = _fpYaw;
            _fpCurrentPitch = 0f;
            _fpYawVelocity = 0f;
            _fpPitchVelocity = 0f;

            if (viewpoint != null)
            {
                viewpoint.localRotation = Quaternion.identity;
            }
        }
        else
        {
            _yaw = playerController != null ? playerController.transform.eulerAngles.y : _yaw;
            _currentYaw = _yaw;
            _fpYawVelocity = 0f;
            _fpPitchVelocity = 0f;
        }
    }

    private bool IsAnyUiOpen()
    {
        return uiStateController.AnyUiOpen;
    }

    private bool HasFirstPersonViewpoint()
    {
        if (_skeletonBody == null)
        {
            return false;
        }

        return _skeletonBody.HasSkull() || UpdateFirstPersonViewpoint() != null;
    }

    private bool IsDetachedSkullViewpoint(Transform viewpoint)
    {
        BodyPart skull = viewpoint.GetComponent<BodyPart>();
        BodyPartItem skullItem = skull != null ? skull.Item : null;
        if (skullItem == null)
        {
            return false;
        }

        return skullItem.Type == BodyPartType.Head && skull.State == BodyPartState.Detached;
    }

    private Transform UpdateFirstPersonViewpoint()
    {
        Transform viewpoint = GetFirstPersonViewpoint();
        if (viewpoint == null)
        {
            return null;
        }

        firstPersonPivot = viewpoint;
        if (firstPersonCam != null)
        {
            firstPersonCam.Follow = viewpoint;
        }

        return viewpoint;
    }

    private Transform GetFirstPersonViewpoint()
    {
        CacheFirstPersonBodyPartItem();
        if (_firstPersonBodyPartItem != null && _firstPersonBodyPartItem.CurrentBodyPart != null)
        {
            return _firstPersonBodyPartItem.CurrentBodyPart.transform;
        }

        return firstPersonPivot;
    }

    private void CacheFirstPersonBodyPartItem()
    {
        if (_firstPersonBodyPartItem != null || firstPersonPivot == null)
        {
            return;
        }

        BodyPart skull = firstPersonPivot.GetComponent<BodyPart>();
        BodyPartItem skullItem = skull != null ? skull.Item : null;
        if (skullItem != null && skullItem.Type == BodyPartType.Head)
        {
            _firstPersonBodyPartItem = skullItem;
        }
    }

    private void HandleBodyChanged()
    {
        if (_isFirstPerson)
        {
            SetLocalBodyVisible(false);
        }
    }

    private void SetLocalBodyVisible(bool visible)
    {
        for (int i = 0; i < _hiddenBodyRenderers.Count; i++)
        {
            Renderer renderer = _hiddenBodyRenderers[i];
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        _hiddenBodyRenderers.Clear();

        if (visible || _skeletonBody == null)
        {
            return;
        }

        Transform viewpoint = UpdateFirstPersonViewpoint();
        if (viewpoint == null || !IsDetachedSkullViewpoint(viewpoint))
        {
            HideRenderers(_skeletonBody.GetComponentsInChildren<Renderer>(true));
        }

        if (!_skeletonBody.HasSkull() && viewpoint != null)
        {
            HideRenderers(viewpoint.GetComponentsInChildren<Renderer>(true), true);
        }
    }

    private void HideRenderers(Renderer[] renderers, bool includeHeldItems = false)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer.enabled || (!includeHeldItems && _playerHand != null && _playerHand.ContainsHeldItemRenderer(renderer)))
            {
                continue;
            }

            renderer.enabled = false;
            _hiddenBodyRenderers.Add(renderer);
        }
    }
}
