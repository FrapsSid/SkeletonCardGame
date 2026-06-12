using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Awake()
    {
        _input = GetComponentInParent<InputReader>();
        _yaw = transform.eulerAngles.y;
        _pitch = transform.eulerAngles.x;
    }

    private void LateUpdate()
    {
        Vector2 look = _input.LookInput;

        bool isMouse = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

        float sens = isMouse ? mouseSensitivity : gamepadSensitivity * Time.deltaTime;

        _yaw += look.x * sens;
        _pitch -= look.y * sens;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _yaw, ref _yawVelocity, smoothTime);
        _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _pitch, ref _pitchVelocity, smoothTime);

        transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0);
    }
}