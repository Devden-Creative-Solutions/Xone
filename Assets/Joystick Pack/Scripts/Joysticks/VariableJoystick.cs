using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class VariableJoystick : Joystick
{
    public Action<bool> OnDoubleTap;
    public Action<bool> OnJoystickClicked;

    public float MoveThreshold { get { return moveThreshold; } set { moveThreshold = Mathf.Abs(value); } }

    [SerializeField] private float moveThreshold = 1;
    [SerializeField] private JoystickType joystickType = JoystickType.Fixed;

    private Vector2 fixedPosition = Vector2.zero;

    int tapCount;

    float tappedTime;

    public int currentPointerId;

    public void SetMode(JoystickType joystickType)
    {
        this.joystickType = joystickType;
        if (joystickType == JoystickType.Fixed)
        {
            background.anchoredPosition = fixedPosition;
            background.gameObject.SetActive(true);
        }
        else
            background.gameObject.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        fixedPosition = background.anchoredPosition;
        SetMode(joystickType);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        tapCount++;
        OnJoystickClicked?.Invoke(true);
        currentPointerId = eventData.pointerId;
        Debug.Log("Pointer ID : " + eventData.pointerId);
        if (joystickType != JoystickType.Fixed)
        {
            background.anchoredPosition = ScreenPointToAnchoredPosition(eventData.position);
            background.gameObject.SetActive(true);
        }
        base.OnPointerDown(eventData);
    }

    private void Update()
    {
        if (tapCount > 0)
        {
            tappedTime += Time.deltaTime;

            if (tappedTime > 0.35f)
            {
                OnDoubleTap?.Invoke(false);
                tappedTime = 0;
                tapCount = 0;
            }
            else
            {
                if (tapCount == 2)
                {
                    OnDoubleTap?.Invoke(true);
                    tappedTime = 0;
                    tapCount = 0;
                }
            }
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        OnJoystickClicked?.Invoke(false);

        if (joystickType != JoystickType.Fixed)
            background.gameObject.SetActive(false);

        base.OnPointerUp(eventData);
    }

    protected override void HandleInput(float magnitude, Vector2 normalised, Vector2 radius, Camera cam)
    {
        if (joystickType == JoystickType.Dynamic && magnitude > moveThreshold)
        {
            Vector2 difference = normalised * (magnitude - moveThreshold) * radius;
            background.anchoredPosition += difference;
        }
        base.HandleInput(magnitude, normalised, radius, cam);
    }
}

public enum JoystickType { Fixed, Floating, Dynamic }