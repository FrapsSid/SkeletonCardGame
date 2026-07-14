using UnityEngine;

public class AuthBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (AuthManager.Instance == null)
        {
            var go = new GameObject("AuthManager");
            go.AddComponent<AuthManager>();
        }
    }
}
