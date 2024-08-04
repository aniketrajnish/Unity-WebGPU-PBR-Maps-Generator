using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance()
    {
        if (!_instance)
        {
            _instance = FindAnyObjectByType<UnityMainThreadDispatcher>();
            if (!_instance)
            {
                var obj = new GameObject("MainThreadDispatcher");
                _instance = obj.AddComponent<UnityMainThreadDispatcher>();
            }
        }
        return _instance;
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    public void EnqueueCoroutine(IEnumerator routine)
    {
        Enqueue(() => StartCoroutine(routine));
    }
}