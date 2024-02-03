using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RotaryHeart.Lib.PhysicsExtension;

public class LedgeGrabbing : MonoBehaviour
{

    [Header("References")]
    public PlayerMovement pm;
    public Transform orientation;
    public Transform cam;
    public Rigidbody rb;


    [Header("Ledge Grabbing")]
    public float moveToLedgeSpeed;
    public float maxLedgeGrabDistance;

    public float minTimeOnLedge;
    private float timeOnLedge; //current time on ledge

    public bool holding; //check if currently holding to a ledge


    [Header("Ledge Jumping")]
    public KeyCode jumpKey = KeyCode.Space;
    public float ledgeJumpForwardForce;
    public float ledgeJumpUpwardForce;


    
    [Header("Ledge Detection")]
    public float ledgeDetectionLength; //Distance to which the ledge is accessible
    public float ledgeSphereCastRadius;
    public LayerMask whatIsLedge;

    private Transform lastLedge;
    private Transform currLedge;

    private RaycastHit ledgeHit; //To store information of the ledge hit


    [Header("Exiting")]
    public bool exitingLedge;
    public float exitLedgeTime;
    private float exitLedgeTimer;



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LedgeDetection();
        SubStateMachine();
    }


    private void SubStateMachine()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        bool anyInputKeyPressed = horizontalInput != 0 || verticalInput != 0; //if any of input key is pressed

        // SubState 1 - Holding onto ledge
        if (holding)
        {
            FreezeRigidbodyOnLedge();

            timeOnLedge += Time.deltaTime;

            //If times of hold up or key pressed, exit ledged hold
            if (timeOnLedge > minTimeOnLedge && anyInputKeyPressed) ExitLedgeHold();

            if (Input.GetKeyDown(jumpKey)) LedgeJump();
        }

        // Substate 2 - Exiting Ledge
        else if (exitingLedge)
        {
            if (exitLedgeTimer > 0) exitLedgeTimer -= Time.deltaTime;
            else exitingLedge = false;
        }
    }


    //
    private void LedgeDetection(){
        bool ledgeDetected = UnityEngine.Physics.SphereCast(transform.position, ledgeSphereCastRadius, cam.forward, out ledgeHit, ledgeDetectionLength, whatIsLedge);
        //Sphere Cast  RotaryHeart.Lib.PhysicsExtension.Physics.SphereCast(transform.position, ledgeSphereCastRadius, cam.forward, ledgeDetectionLength, PreviewCondition.Editor, 0, Color.green, Color.red);

        if (!ledgeDetected) return; //If no ledged detected, stop function

        //Else calcul of the distance to the ledge
        float distanceToLedge = Vector3.Distance(transform.position, ledgeHit.transform.position);
        
        if (ledgeHit.transform == lastLedge) return; //So only new ledges are detected 

        //If ledge accessible and not holding, we can enter ledge hold
        if (distanceToLedge < maxLedgeGrabDistance && !holding) EnterLedgeHold();
    
    }


    private void LedgeJump()
    {
        ExitLedgeHold();
        Invoke(nameof(DelayedJumpForce), 0.05f);
    }


    //Jump from ledge call with delay for better feeling
    private void DelayedJumpForce(){
        Vector3 forceToAdd = cam.forward * ledgeJumpForwardForce + orientation.up * ledgeJumpUpwardForce; 
        rb.velocity = Vector3.zero;
        rb.AddForce(forceToAdd, ForceMode.Impulse);
    }




    private void EnterLedgeHold(){
        holding = true;

        pm.unlimited = true;
        pm.restricted = true;

        currLedge = ledgeHit.transform;
        lastLedge = ledgeHit.transform;

        rb.useGravity = false;
        rb.velocity = Vector3.zero;
    }


    private void FreezeRigidbodyOnLedge()
    {
        rb.useGravity = false;

        Vector3 directionToLedge = currLedge.position - transform.position;
        float distanceToLedge = Vector3.Distance(transform.position, currLedge.position);

        // Move player towards ledge
        if(distanceToLedge > 1f){
            if(rb.velocity.magnitude < moveToLedgeSpeed) //if rigidbody not moving faster than moveToLedgeSpeed
                rb.AddForce(directionToLedge.normalized * moveToLedgeSpeed * 1000f * Time.deltaTime); //add force in direction calculated
        }

        // Hold onto ledge
        else { //if distance below, it means playze has reaches the ledge and want to freeze position
            if (!pm.freeze) pm.freeze = true;
            if (pm.unlimited) pm.unlimited = false;
        }

        // Exiting if something goes wrong
        if (distanceToLedge > maxLedgeGrabDistance) ExitLedgeHold();
    }

    private void ExitLedgeHold()
    {
        exitingLedge = true;
        exitLedgeTimer = exitLedgeTime;

        holding = false;
        timeOnLedge = 0f; //reser time on ledge*/

        pm.restricted = false;
        pm.freeze = false; //not freeze animore

        rb.useGravity = true; //reactivate gravity

        //Allow to not grab a ledge 1 second after leaving it
        StopAllCoroutines();
        Invoke(nameof(ResetLastLedge), 1f); 
    }


    private void ResetLastLedge(){
        lastLedge = null;
    }


}
