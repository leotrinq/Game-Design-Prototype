using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RotaryHeart.Lib.PhysicsExtension;

public class Climbing : MonoBehaviour
{

    [Header("References")]
    public Transform orientation;
    public Rigidbody rb;
    public PlayerMovement pm;
    private LedgeGrabbing lg;
    public LayerMask whatIsWall;


    [Header("Climbing")]
    public float climbSpeed;
    public float maxClimbTime; //Time during wich player can climb
    private float climbTimer;
    private bool climbing; //Indicate if plyaer is currently climbing


    [Header("ClimbJumping")]
    public float climbJumpUpForce;
    public float climbJumpBackForce;

    public KeyCode jumpKey = KeyCode.Space;
    public int climbJumps;
    private int climbJumpsLeft;


    private Transform lastWall;
    private Vector3 lastWallNormal;
    public float minWallNormalAngleChange;


    [Header("Detection")]
    public float detectionLength;
    public float sphereCastRadius; // radius of the sphere cast 
    public float maxWallLookAngle;
    private float wallLookAngle;

    private RaycastHit frontWallHit; //Information of the wall hit by raycast
    public bool wallFront; //Indicate if wall in front of player


    [Header("Exiting")]
    public bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;


    // Start is called before the first frame update
    void Start()
    {
        lg = GetComponent<LedgeGrabbing>();
    }

    // Update is called once per frame
    void Update()
    {
        WallCheck();
        StateMachine(); 

        if(climbing /*&& !exitingWall*/) ClimbingMovement();
    }

    
    private void StateMachine()
    {

        //State 0 - Ledge Grabbing
        if(lg.holding){
            if(climbing) StopClimbing();

            //everything else get handled by the SubStateMachine() in the ledge grabbing script
        }

        // State 1 - Climbing
        //climb when wall in front, z key pressed, in angle of climbing
        else if (wallFront && Input.GetKey(KeyCode.W) && wallLookAngle < maxWallLookAngle /*&& !exitingWall*/)
        {
            if (!climbing && climbTimer > 0){ 
                StartClimbing();
            } 

            // timer
            if (climbTimer > 0) climbTimer -= Time.deltaTime;
            if (climbTimer < 0) StopClimbing();
        }

        // State 2 - Exiting
        /*else if (exitingWall)
        {
            if (climbing) {
                StopClimbing();
            }
            if (exitWallTimer > 0) exitWallTimer -= Time.deltaTime;
            if (exitWallTimer < 0) exitingWall = false;
        }*/

        // State 3 - None
        else
        {
            if (climbing) StopClimbing();
        }
    
        //Condition to call climJump
        if (wallFront && Input.GetKeyDown(jumpKey) && climbJumpsLeft > 0) {
            ClimbJump();
        }
    
    }



    private void WallCheck()
    {
        wallFront = UnityEngine.Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsWall);
        //Display the sphere cast for debugging purpose
        //RotaryHeart.Lib.PhysicsExtension.Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, detectionLength, PreviewCondition.Editor, 0, Color.green, Color.red);
        
        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal); //angle of vision for the wallclimbing to activate

        if(pm.grounded){
            climbTimer = maxClimbTime;
        }

        //Two possible condition when a new wall is hit
        //When the transform is different of when the normal of te wall has changed
        bool newWall = frontWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;
        
        if ((wallFront && newWall) || pm.grounded)
        {
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
        }
    }



    private void StartClimbing(){
        climbing = true;

        //Variables used to reset climb jump 
        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
        //camera fov change
        
    }


    private void ClimbingMovement(){
         rb.velocity = new Vector3(rb.velocity.x, climbSpeed, rb.velocity.z);

        /// idea - sound effect
    }


    private void StopClimbing(){
        climbing = false;

        //particle effect

    }


    private void ClimbJump() {

        if(pm.grounded) return;
        if(lg.holding || lg.exitingLedge) return;

        //exitingWall = true;
        //exitWallTimer = exitWallTime;

        Vector3 forceToApply = transform.up * climbJumpUpForce + frontWallHit.normal * climbJumpBackForce;

        //rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); //Reset y velocity, so force 
    
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(forceToApply, ForceMode.Impulse);

        climbJumpsLeft--;
    }




}
