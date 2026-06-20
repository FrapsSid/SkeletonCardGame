using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public static class InputKeyUtils {
    public static bool WasPressedThisFrame(KeyCode keyCode) {
#if ENABLE_INPUT_SYSTEM
        if (TryReadInputSystemKey(keyCode, out bool pressed)) {
            return pressed;
        }
#endif

        try {
            return Input.GetKeyDown(keyCode);
        }
        catch (InvalidOperationException) {
            return false;
        }
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryReadInputSystemKey(KeyCode keyCode, out bool pressed) {
        pressed = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !TryConvertKeyCode(keyCode, out Key key)) {
            return false;
        }

        KeyControl control = keyboard[key];
        if (control == null) {
            return false;
        }

        pressed = control.wasPressedThisFrame;
        return true;
    }

    private static bool TryConvertKeyCode(KeyCode keyCode, out Key key) {
        switch (keyCode) {
            case KeyCode.Alpha0:
                key = Key.Digit0;
                return true;
            case KeyCode.Alpha1:
                key = Key.Digit1;
                return true;
            case KeyCode.Alpha2:
                key = Key.Digit2;
                return true;
            case KeyCode.Alpha3:
                key = Key.Digit3;
                return true;
            case KeyCode.Alpha4:
                key = Key.Digit4;
                return true;
            case KeyCode.Alpha5:
                key = Key.Digit5;
                return true;
            case KeyCode.Alpha6:
                key = Key.Digit6;
                return true;
            case KeyCode.Alpha7:
                key = Key.Digit7;
                return true;
            case KeyCode.Alpha8:
                key = Key.Digit8;
                return true;
            case KeyCode.Alpha9:
                key = Key.Digit9;
                return true;
            case KeyCode.Return:
                key = Key.Enter;
                return true;
            case KeyCode.Escape:
                key = Key.Escape;
                return true;
            default:
                return Enum.TryParse(keyCode.ToString(), out key);
        }
    }
#endif
}
