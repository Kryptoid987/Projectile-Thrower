using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgnosticProjectileThrowerInputHandle : MonoBehaviour
{
    public event Action<Touch> OnTouch;
    bool isDragging = false;
    Vector2 lastPos;

    private void Update()
    {
#if UNITY_EDITOR
        Touch touch = new Touch();
        touch.position = Input.mousePosition;
        touch.phase = TouchPhase.Canceled;

        if (Input.GetMouseButtonDown(0))
        {
            touch.phase = TouchPhase.Began;
            lastPos = touch.position;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            touch.phase = TouchPhase.Ended;
            isDragging = false;
        }
        else if (isDragging)
        {
            var delta = (touch.position - lastPos);
            touch.deltaPosition = delta;

            if (delta.sqrMagnitude > 2.5f)
                touch.phase = TouchPhase.Moved;
            else touch.phase = TouchPhase.Stationary;

            lastPos = touch.position;
        }

        if (touch.phase != TouchPhase.Canceled)
            OnTouch?.Invoke(touch);
#else
        if(Input.touchCount > 0)
        {
            OnTouch?.Invoke(Input.touches[0]);
        }
#endif
    }
}
