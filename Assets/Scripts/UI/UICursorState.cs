#nullable enable

using UnityEngine;

public static class UICursorState
{
    public static void Apply(bool isUiOpen)
    {
        Cursor.lockState = isUiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isUiOpen;
    }
}
