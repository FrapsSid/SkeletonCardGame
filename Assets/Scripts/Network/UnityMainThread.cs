using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UnityMainThread : MonoBehaviour
{
    private static UnityMainThread _instance;
    private static readonly Queue<Action> _pendingActions = new();

    public static void Execute(Action action)
    {
        if (action == null) return;
        lock (_pendingActions)
        {
            _pendingActions.Enqueue(action);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        int maxPerFrame = 16;
        while (maxPerFrame-- > 0)
        {
            Action action;
            lock (_pendingActions)
            {
                if (_pendingActions.Count == 0) return;
                action = _pendingActions.Dequeue();
            }
            try { action?.Invoke(); }
            catch (Exception e) { Debug.LogError($"[MainThread] {e}"); }
        }
    }
}
