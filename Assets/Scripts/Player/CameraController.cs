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

    [Header("Pivots")]
    public Transform thirdPersonPivot;
    public Transform firstPersonPivot;
    [Header("Cinemachine")]
    public CinemachineCamera thirdPersonCam;
    public CinemachineCamera firstPersonCam;
    public CinemachineBrain brain;
    [Header("First Person Look Limits")]
    public float firstPersonMaxAngle = 30f;

    [Header("Player Control")]
    public PlayerController playerController;

    private void Awake()
    {
        _input = GetComponentInParent<InputReader>();
        _yaw = thirdPersonPivot.eulerAngles.y;
        _pitch = thirdPersonPivot.eulerAngles.x;
        SetFirstPerson(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            SetFirstPerson(!_isFirstPerson);
        }
    }

    private void LateUpdate()
    {
        if (_isFirstPerson)
            UpdateFirstPerson();
        else
            UpdateThirdPerson();
    }

    private void UpdateThirdPerson()
    {
        Vector2 look = _input.LookInput;
        bool isMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;
        float sens = isMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

        _yaw += look.x * sens;
        _pitch -= look.y * sens;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _yaw, ref _yawVelocity, smoothTime);
        _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _pitch, ref _pitchVelocity, smoothTime);

        thirdPersonPivot.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);
    }

    private void UpdateFirstPerson()
    {
        Vector2 look = _input.LookInput;
        bool isMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;
        float sens = isMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

        _fpYaw += look.x * sens;
        _fpPitch -= look.y * sens;

        _fpYaw = Mathf.Clamp(_fpYaw, -firstPersonMaxAngle, firstPersonMaxAngle);
        _fpPitch = Mathf.Clamp(_fpPitch, -firstPersonMaxAngle, firstPersonMaxAngle);

        _fpCurrentYaw = Mathf.SmoothDampAngle(_fpCurrentYaw, _fpYaw, ref _fpYawVelocity, smoothTime);
        _fpCurrentPitch = Mathf.SmoothDampAngle(_fpCurrentPitch, _fpPitch, ref _fpPitchVelocity, smoothTime);

        firstPersonPivot.localRotation = Quaternion.Euler(_fpCurrentPitch, _fpCurrentYaw, 0f);
    }

    private void SetFirstPerson(bool enable)
    {
        _isFirstPerson = enable;

        if (thirdPersonCam != null) thirdPersonCam.Priority = enable ? 0 : 10;
        if (firstPersonCam != null) firstPersonCam.Priority = enable ? 10 : 0;

        if (playerController != null) playerController.SetFirstPersonLock(enable);

        if (enable)
        {
            _fpYaw = 0f;
            _fpPitch = 0f;
            _fpCurrentYaw = 0f;
            _fpCurrentPitch = 0f;
            _fpYawVelocity = 0f;
            _fpPitchVelocity = 0f;
            firstPersonPivot.localRotation = Quaternion.identity;
        }
    }
}