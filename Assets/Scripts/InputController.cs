using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InputController : Controller
{
    public float XSensivity = 360; //Degres par seconde par valeur d input.
    public float YSensivity = 360; //Degres par seconde par valeur d input.
    public string MouseXName = "Mouse X";
    public string MouseYName = "Mouse Y";

    public float MaxUpDownAngle = 70; //Angle max de rot verticale depuis l horizontal

    // Update is called once per frame
    void FixedUpdate()
    {
        //Lecture des inputs
        Vector2 axisLook = new Vector2(Input.GetAxis(MouseXName), Input.GetAxis(MouseYName));

        //Rotation de la direction de look si input sur les axes
        //Deadzone deja gérée par les axis
        if (axisLook.x != 0 || axisLook.y != 0)
        {
            Vector3 WantedDirectionLookRightTargetSmooth = Vector3.Cross(transform.up, WantedDirectionLookTargetSmooth).normalized;
            Quaternion rotateHorizontal = Quaternion.AngleAxis(axisLook.x * XSensivity * Time.deltaTime, Vector3.up);
            Quaternion rotateVertical = Quaternion.AngleAxis(-axisLook.y * YSensivity * Time.deltaTime, WantedDirectionLookRightTargetSmooth);

            //On teste la rotation verticale par rapport au max
            Vector3 afterVertRot = rotateVertical * WantedDirectionLookTargetSmooth;
            float angleWithUp = Vector3.Angle(afterVertRot, Vector3.up);
            float angleMinWithUp = 90 - MaxUpDownAngle;
            if (angleWithUp < angleMinWithUp)
                rotateVertical *= Quaternion.AngleAxis(angleMinWithUp - angleWithUp, WantedDirectionLookRightTargetSmooth);
            float angleMaxWithUp = 90 + MaxUpDownAngle;
            if (angleWithUp > angleMaxWithUp)
                rotateVertical *= Quaternion.AngleAxis(angleMaxWithUp - angleWithUp, WantedDirectionLookRightTargetSmooth);

            WantedDirectionLookTargetSmooth = rotateHorizontal * rotateVertical * WantedDirectionLookTargetSmooth;
        }

        //On change la direction de marche au clavier
        Vector2 axisMove = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        Vector3 WantedDirectionRight = Vector3.Cross(Vector3.up, WantedDirectionLook);
        WantedDirectionMove = WantedDirectionLook * axisMove.y + WantedDirectionRight * axisMove.x;
        WantedSpeed = Mathf.Max(Mathf.Abs(axisMove.x), Mathf.Abs(axisMove.y));
        
        //On gère le shoot
        WantsToShoot = Input.GetButton("Fire1");
        WantsToShoot1 = Input.GetButton("Fire2");


        //On gère le jump
        WantsToJump = Input.GetButton("Jump");

        //On applique doucement
        SmoothWantedDirectionLook(Time.deltaTime);
    }

    private void Update()
    {
        //On affiche le debug
        DrawDebug();

        UpdateCursorLock();
    }

    public bool LockCursor = true;
    private bool hasFocus = false;
    private void UpdateCursorLock()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            hasFocus = false;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            hasFocus = true;
        }

        if (hasFocus && LockCursor)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
        else if (!hasFocus)
        {
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
    }

}
