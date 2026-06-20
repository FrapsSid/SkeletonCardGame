using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

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

    private void Awake()
    {
        _input = GetComponentInParent<InputReader>();

        if (thirdPersonPivot != null)
        {
            _yaw = thirdPersonPivot.eulerAngles.y;
            _pitch = thirdPersonPivot.eulerAngles.x;
        }

        SetFirstPerson(false);
    }

    private void Update()
    {
        if (InventoryUI.IsAnyInventoryOpen)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            SetFirstPerson(!_isFirstPerson);
        }
    }

    private void LateUpdate()
    {
        if (InventoryUI.IsAnyInventoryOpen || _input == null)
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
        if (firstPersonPivot == null)
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

        firstPersonPivot.localRotation = Quaternion.Euler(_fpCurrentPitch, 0f, 0f);
    }

    private void SetFirstPerson(bool enable)
    {
        _isFirstPerson = enable;

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

        if (enable)
        {
            _fpYaw = playerController != null ? playerController.transform.eulerAngles.y : transform.eulerAngles.y;
            _fpPitch = 0f;
            _fpCurrentYaw = _fpYaw;
            _fpCurrentPitch = 0f;
            _fpYawVelocity = 0f;
            _fpPitchVelocity = 0f;

            if (firstPersonPivot != null)
            {
                firstPersonPivot.localRotation = Quaternion.identity;
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
}