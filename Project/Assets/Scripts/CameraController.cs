using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class DS
{
    public static Gamepad controller = null;
    public static ButtonControl accelX = null;
    public static ButtonControl accelY = null;
    public static ButtonControl accelZ = null;
    public static ButtonControl accelX2 = null;
    public static ButtonControl accelY2 = null;
    public static ButtonControl accelZ2 = null;

    public static Gamepad getController(string layoutFile = null)
    {
        // Read layout from JSON file
        string layout = File.ReadAllText(layoutFile);

        // Overwrite the default layout
        InputSystem.RegisterLayoutOverride(layout, "DualSenseGamepadHIDExt");

        var ds = Gamepad.current;
        DS.controller = ds;
        accelX = ds.GetChildControl<ButtonControl>("accl X 0");
        accelY = ds.GetChildControl<ButtonControl>("accl Y 0");
        accelZ = ds.GetChildControl<ButtonControl>("accl Z 0");
        accelX2 = ds.GetChildControl<ButtonControl>("accl X 1");
        accelY2 = ds.GetChildControl<ButtonControl>("accl Y 1");
        accelZ2 = ds.GetChildControl<ButtonControl>("accl Z 1");
        return ds;
    }

    private static int processAccelData(float accel0, float accel1)
    {
        byte[] accelRaw = { 0, 0 };
        accelRaw[0] = (byte)(int)(accel0 * 0xFF);
        accelRaw[1] = (byte)(int)(accel1 * 0xFF);
        int accel = System.BitConverter.ToInt16(accelRaw, 0);
        return accel;
    }

    public static float[] getRotation()
    {
        float[] res = new float[2];
        res[0] = ((float)(processAccelData(accelX.ReadValue(), accelX2.ReadValue())) / 9000.0f) * -1.0f;
        res[1] = (float)(processAccelData(accelZ.ReadValue(), accelZ2.ReadValue())) / 9000.0f;

        // Deadzone
        if (res[0] > -0.18f && res[0] < 0.18f) res[0] = 0.0f;
        if (res[1] > -0.18f && res[1] < 0.18f) res[1] = 0.0f;

        res[0] *= 2.0f;
        res[1] *= 2.0f;

        Debug.Log("X: " + res[0]);
        Debug.Log("Y: " + res[1]);

        // X is for left/right (range: 8000-ish left, -8000-ish right
        // Z is for up/down (range: 8000-ish up, 8000-ish down

        return res;
    }
}

public class CameraController : MonoBehaviour
{
    private Transform mCamera;
    private PlayerController player;

    private float horizontalTilt;
    private float verticalTilt;

    private float initialXRotation;

    public float spiralTimer;
    public float spiralSpeed;
    public float spiralSpeedMultiplier;
    public float maxVerticalAngle;
    public float maxHorizontalAngle;
    public float tiltSpeed;
    public float offset;

    private Gamepad controller;

    void Start()
    {
        this.controller = DS.getController("Assets/ControllerLayout/DualsenseMotion.json");
        mCamera = transform.GetChild(0);
        player = FindObjectOfType<PlayerController>();
        initialXRotation = transform.eulerAngles.x; // Store initial x rotation
    }

    void FixedUpdate()
    {
        if (player.currentState == PlayerController.State.NORMAL)
        {
            float[] rot = DS.getRotation();
            verticalTilt = rot[1];
            horizontalTilt = rot[0];

            Debug.Log("Vertical " + Input.GetAxis("Vertical"));
            Debug.Log("Hori " + Input.GetAxis("Horizontal"));

            player.Move(verticalTilt, horizontalTilt, transform.right);
        }
    }

    void Update()
    {
        if (player.currentState == PlayerController.State.NORMAL)
        {
            CameraTilt();
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        switch (player.currentState) {
            case PlayerController.State.WAIT:
                SpiralProcedure();
                break;
            case PlayerController.State.NORMAL:
                // Follow the player
                FollowTarget();
                break;
        }
    }

    void SpiralProcedure()
    {
        // Decrement timer
        if (spiralTimer > 0.0f)
                spiralTimer -= spiralSpeed * Time.deltaTime;
        else
        {
            // Start the stage (Follow the player)
            spiralTimer = 0.0f;
            StartStage();
        }

        // This moves the camera in a spiral patttern that gets smaller the more the spiral decrements
        float x = Mathf.Sin(spiralTimer) * Mathf.Pow(1.0f + spiralTimer, 2.0f);
        float z = -Mathf.Cos(spiralTimer) * Mathf.Pow(1.0f + spiralTimer, 2.0f);

        transform.position = new Vector3(x, spiralTimer, z) + player.transform.position;

        transform.LookAt(player.transform.position);                                                                // Face the player
        transform.eulerAngles = new Vector3(initialXRotation, transform.eulerAngles.y, transform.eulerAngles.z);    // Adjust X angle
        transform.position = transform.position - (transform.forward * offset) + Vector3.forward;                   // Move back by a distance of offset + move it forward a distance of forward (1.0f)
    }

    void StartStage()
    {
        player.currentState = PlayerController.State.NORMAL;
    }

    void CameraTilt()
    {
        // Rotate camera container along the x axis when tilting the joystick up or down to give a forward and back tilt effect.
        // The further up the joystick is the higher the angle for target rotation will be and vice versa.
        float scaledVerticalTilt = initialXRotation - (verticalTilt * maxVerticalAngle);
        float angleBetweenFloorNormal = Vector3.SignedAngle(Vector3.up, player.normal, transform.right);
        Quaternion targetXRotation = Quaternion.Euler(scaledVerticalTilt + angleBetweenFloorNormal, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetXRotation, tiltSpeed * Time.deltaTime);

        // Rotate camera along the z axis when tilting the joystick left or right to give a left and right tilt effect.
        // The further right the joystick is the higher the angle for target rotation will be and vice versa.
        float scaledHorizontalTilt = Input.GetAxis("Horizontal") * maxHorizontalAngle;

        Quaternion targetZRotation = Quaternion.Euler(mCamera.rotation.eulerAngles.x, mCamera.rotation.eulerAngles.y, scaledHorizontalTilt);

        mCamera.rotation = Quaternion.RotateTowards(mCamera.rotation, targetZRotation, tiltSpeed * Time.deltaTime);
    }

    void FollowTarget()
    {
        // Get forward vector minus the y component
        Vector3 vectorA = new Vector3(transform.forward.x, 0.0f, transform.forward.z);

        // Get target's velocity vector minus the y component
        Vector3 vectorB = new Vector3(player.rb.velocity.x, 0.0f, player.rb.velocity.z);

        // Find the angle between vectorA and vectorB
        float rotateAngle = Vector3.SignedAngle(vectorA.normalized, vectorB.normalized, Vector3.up);

        // Get the target's speed (maginitude) without the y component
        // Only set speed factor when vector A and B are almost facing the same direction
        float speedFactor = Vector3.Dot(vectorA, vectorB) > 0.0f ? vectorB.magnitude : 1.0f;

        // Rotate towards the angle between vectorA and vectorB
        // Use speedFactor so camera doesn't rotatate at a constant speed
        // Limit speedFactor to be between 1 and 2
        transform.Rotate(Vector3.up, rotateAngle * Mathf.Clamp(speedFactor, 1.0f, 2.0f) * Time.deltaTime);

        // Position the camera behind target at a distance of offset
        transform.position = player.transform.position - (transform.forward * offset);
        transform.LookAt(player.transform.position);
    }
}
