using UnityEngine;
using System;

/// <summary>
/// Attach to the player body (next to PlayerController / SkeletonBody).
/// After pot resolution, GameManager calls Refresh().
/// If the bound Skeleton has lost its Soul, ghost activates.
/// </summary>
public sealed class GhostController : MonoBehaviour
{
    [SerializeField] private float flySpeed = 5f;

    private Skeleton _skeleton;
    private CharacterController _cc;
    private Collider[] _colliders;
    private bool _isGhostActive;
    private bool _hasUsedReveal;

    public bool IsGhostActive => _isGhostActive;
    public bool HasUsedReveal => _hasUsedReveal;

    public event Action OnGhostActivated;

    public void Bind(Skeleton skeleton)
    {
        _skeleton = skeleton;
        _cc = GetComponent<CharacterController>();
        _colliders = GetComponentsInChildren<Collider>();
    }

    /// <summary>Call after every pot resolution or asset transfer.</summary>
    public void Refresh()
    {
        if (_skeleton == null || _isGhostActive) return;
        if (_skeleton.IsGhost)
            ActivateGhost();
    }

    private void ActivateGhost()
    {
        _isGhostActive = true;

        if (_cc != null) _cc.enabled = false;
        foreach (var c in _colliders)
            if (c != null) c.isTrigger = true;

        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            var m = r.material;
            var col = m.color;
            col.a = 0.4f;
            m.color = col;
        }

        var interactor = GetComponent<PlayerInteractor>();
        if (interactor != null) interactor.enabled = false;

        OnGhostActivated?.Invoke();
    }

    public void HandleGhostMovement(Vector2 moveInput, bool up, bool down)
    {
        if (!_isGhostActive) return;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        var cam = Camera.main;
        if (cam != null)
            move = cam.transform.TransformDirection(move);

        move.y = up ? 1f : down ? -1f : 0f;
        transform.position += move.normalized * flySpeed * Time.deltaTime;
    }

    public bool TryRevealCardStack(CardStack stack)
    {
        if (!_isGhostActive || _hasUsedReveal || stack == null)
            return false;
        _hasUsedReveal = true;
        return true;
    }
}
