using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	public enum State
    {
        WAIT,
        NORMAL,
        WON,
        STATES
    }
	
	public State currentState;
	public Rigidbody rb;
    public GameObject cam;
	public LayerMask terrain;
	public float terrainRadiusCheck;
	public Vector3 normal;
	public float moveForce;
    public OSC osc;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startCamPosition;
    private Quaternion startCamRotation;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = rb.position;
        startRotation = rb.rotation;
        startCamPosition = cam.transform.position;
        startCamRotation = cam.transform.rotation;
        currentState = State.WAIT;

        // Initialize speed at 0
        OscMessage msg = new OscMessage();
        msg.address = "/speed";
        msg.values.Add(0);
        osc.Send(msg);
    }

    void OnCollisionEnter()
    {
        // Send message to trigger a bounce sound effect in Pd
        OscMessage msg = new OscMessage();
        msg.address = "/hit";
        msg.values.Add("bang"); // Actually have rolling sound only if not paused and on land
        osc.Send(msg);
    }

    void Update()
    {
        // Send updated speed every frame
        OscMessage msg = new OscMessage();
        msg.address = "/speed";
        msg.values.Add(onLand() && Time.timeScale > 0.1f ? (rb.velocity.magnitude / 2) : 0.0f); // Actually have rolling sound only if not paused and on land
        osc.Send(msg);

        // Reset player to starting state if fall out of map
        if (rb.position.y < -20.0f)
        {
            cam.transform.position = startCamPosition;
            cam.transform.rotation = startCamRotation;
            rb.position = startPosition;
            rb.rotation = startRotation;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
            
    }
	
    public bool onLand()
    {
        return Physics.CheckSphere(transform.position - (Vector3.up * 0.5f), terrainRadiusCheck, terrain);
    }

    public void Move(float verticalTilt, float horizontalTilt, Vector3 right)
    {
        // Allow user movement only when landed
        if (onLand())
        {
            RaycastHit hit;
			
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, terrain))
            {
                normal = hit.normal;
            }

            // Slowdown the ball when no input from user
            if (horizontalTilt == 0.0f && verticalTilt == 0.0f && rb.velocity.magnitude > 0.0f)
            {
                rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, moveForce * 0.1f * Time.deltaTime);
            }
            else
            {
                Vector3 forward = Vector3.Cross(right, normal);
                Vector3 forwardForce = (verticalTilt > 0.0f ? 1.0f : 0.8f) * moveForce * verticalTilt * forward;
                Vector3 rightForce = moveForce * horizontalTilt * right;
                Vector3 forceVector = forwardForce + rightForce;
                rb.AddForce(forceVector);
            }
        }
    }
}
