using DotsNav.Collections;
using Unity.Collections;
using UnityEngine;

class DequeTest : MonoBehaviour
{
    Deque<int> _deque;
    int _i;

    void Start()
    {
        _deque = new Deque<int>(2, Allocator.Persistent);
        Log();
    }

    void OnDestroy()
    {
        _deque.Dispose();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Push back"))
        {
            _deque.PushBack(_i++);
            Log();
        }
        if (GUILayout.Button("Push front"))
        {
            _deque.PushFront(_i++);
            Log();
        }
        if (GUILayout.Button("Pop back"))
        {
            Debug.Log($"Pop back:{_deque.PopBack()}");
            Log();
        }
        if (GUILayout.Button("Pop front"))
        {
            Debug.Log($"Pop front:{_deque.PopFront()}");
            Log();
        }
    }

    void Log()
    {
        if (_deque.Count == 0)
            Debug.Log($"Count: {_deque.Count}");
        else
            Debug.Log($"Count: {_deque.Count}, Front {_deque.Front}, Back {_deque.Back}");
    }
}