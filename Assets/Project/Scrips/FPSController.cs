using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FPSController : MonoBehaviour
{
    public Camera fpsCamera;
    public float minFOV, maxFOV;
    public float zoomSpeed = 0.1f;
    public float cameraSensitivity;
    public float playerWalkSpeed;
    public float playerRunSpeed;
    public VariableJoystick joystick;
    public CharacterController playerCharacterController;

    float playerSpeed;
    int fingerIndex = -1;
    Vector2 lookInput;
    float cameraPitch;
    bool isZooming, isLooking;
    int uiTouchIndex = -1;
    bool clickedJoystick;

    // Start is called before the first frame update
    void Start()
    {
        playerSpeed = playerWalkSpeed;
        joystick.OnDoubleTap += JoystickDoubleTapped;
        joystick.OnJoystickClicked += JoystickClicked;
    }

    private void OnDestroy()
    {
        joystick.OnDoubleTap -= JoystickDoubleTapped;
        joystick.OnJoystickClicked -= JoystickClicked;
    }

    void JoystickDoubleTapped(bool val)
    {
        playerSpeed = val ? playerRunSpeed : playerWalkSpeed;
    }

    void JoystickClicked(bool val)
    {
        clickedJoystick = val;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount <= 0)
        {
            return;
        }

        GetTouchInput();
    }

    void FixedUpdate()
    {
        Vector3 direction = transform.forward * joystick.Vertical + transform.right * joystick.Horizontal;
        playerCharacterController.Move(direction * playerSpeed * Time.deltaTime);
    }


    void GetTouchInput()
    {
        var isOverUI = IsPointerOverUIObject();

        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);

            int index = i;
            var isThisTouchOverUI = IsPointerOverUIObject(index);

            fingerIndex = !isThisTouchOverUI ? index : -1;
            uiTouchIndex = isThisTouchOverUI ? index : -1;

            if (Input.touchCount == 2 && !isOverUI && !clickedJoystick)
            {
                Touch tZero = Input.GetTouch(0);
                Touch tOne = Input.GetTouch(1);

                Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
                Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;

                float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
                float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);

                float deltaDistance = oldTouchDistance - currentTouchDistance;

                Zoom(deltaDistance, zoomSpeed);
                continue;
            }

            if (fingerIndex != -1)
            {
                switch (t.phase)
                {

                    case TouchPhase.Moved:
                        lookInput = Input.GetTouch(fingerIndex).deltaPosition * cameraSensitivity * Time.deltaTime;
                        break;

                    case TouchPhase.Stationary:
                        lookInput = Vector2.zero;
                        break;

                }

                LookAround();
            }
        }
    }

    void LookAround()
    {
        //vertical rotation
        cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
        fpsCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);

        //horizontal rotation
        transform.Rotate(transform.up, lookInput.x);
    }

    void Zoom(float deltaMagnitudeDiff, float speed)
    {
        isZooming = true;
        fpsCamera.fieldOfView += deltaMagnitudeDiff * speed;
        // set min and max value of Clamp function upon your requirement
        fpsCamera.fieldOfView = Mathf.Clamp(fpsCamera.fieldOfView, minFOV, maxFOV);
    }


    public bool IsPointerOverUIObject()
    {
        bool overUI = false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);

            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(t.position.x, t.position.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            if (results.Count > 0)
                overUI = true;
        }

        return overUI;
    }

    public bool IsPointerOverUIObject(int index)
    {
        var t = Input.GetTouch(index);

        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(t.position.x, t.position.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
