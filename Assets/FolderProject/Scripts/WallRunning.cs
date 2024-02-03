using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    /*Attempt to handle looking away from wall being runned on  
    public float angle ;
    public bool lookingWallBeingRunnedOn;
    public float rayLength;
    public bool stop;*/

    [Header("Wallrunning")]
    public LayerMask whatIsWall; //to identify wall
    public LayerMask whatIsGround; //to identify ground
    public float wallRunForce;
    public float wallJumpUpForce; //5
    public float wallJumpSideForce;  //15
    //public float maxWallRunTime; //Time during which we can wall run
    //private float wallRunTimer;
    public float wallClimbSpeed; //Move speed when going up/down while wallrunning


    [Header("Input")]
    private float horizontalInput;
    private float verticalInput;
    public KeyCode jumpKey = KeyCode.Space; //To jump off wall
    public KeyCode upwardsRunKey = KeyCode.LeftShift; //Go up during wallrun
    public KeyCode downwardsRunKey = KeyCode.LeftControl; //Go down  during wallrun
    private bool upwardsRunning; //To dectect wallrun up
    private bool downwardsRunning; //To dectect wallrun down


    [Header("Detection")]
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallhit; //Wall to the left hit. Allow acces to its normal
    private RaycastHit rightWallhit; //Wall to the right hit. Allow acces to its normal
    private bool wallLeft; //true if a wall to the left is hit
    private bool wallRight; //true if a wall to the right is hit

    [Header("Exiting")]
    private bool exitingWall; //Bool indicating that player leave the wall
    public float exitWallTime; //Time during which player is in state to leave the wall 
    private float exitWallTimer; //Timer for the defined time

    /*
    //To use gravity during wall run : Not used
    [Header("Gravity")]
    public bool useGravity; //Using gravity or not is wallrunning
    public float gravityCounterForce;
    */
    


    [Header("References")]
    public Transform orientation;
    public PlayerCam cam; //Reference to camera script
    private PlayerMovement pm;
    private LedgeGrabbing lg;
    private Rigidbody rb;



    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        lg = GetComponent<LedgeGrabbing>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckForWall();
        StateMachine();


    }


    private void FixedUpdate(){
        if(pm.wallrunning){
            WallRunningMovement();
        }
    }

    //Check if there is nearby walls
    private void CheckForWall()
    {
        //out rightWallhit : allow to store the info of the object hit in rightWallhit
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallhit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallhit, wallCheckDistance, whatIsWall);
    }


    //Check if the player is high enough in the air to wallrun
    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }



    private void StateMachine()
    {
        // Getting Inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        upwardsRunning = Input.GetKey(upwardsRunKey);
        downwardsRunning = Input.GetKey(downwardsRunKey);


        // State 1 - Wallrunning
        //if there is wall, going forward and above ground
        if((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && !exitingWall){
            //start wall run here
            if (!pm.wallrunning){
                StartWallRun(); //set boll wallRunning to true
            }

            // wall jump
            if (Input.GetKeyDown(jumpKey)) WallJump();
            
        }

        // State 2 - Exiting
        else if (exitingWall)
        {
            if (pm.wallrunning)
                StopWallRun();

            //in this state for a short amount of time
            if (exitWallTimer > 0)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0)
                exitingWall = false;
        }

        // State 3 - None
        else {
            if (pm.wallrunning){
                StopWallRun();
            }
            
        }
    }


    //
    private void WallRunningMovement()
    {
        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        
        //We need the forward direction of the wall.
        //To find it, we need 2 vectors perpendicular to it
        //With rightWallhit and leftWallhit we have the wall normal
        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal; 

        
        //Vector3.cross return a vector perpendicular 2 vectors
        //Here, the normal of the wall and the vector going up
        //It results in the forward direction of the walls
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        //Identify which direction is closer to where the player is facing
        //Prevent from going backward
        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude){
            wallForward = -wallForward;
        }
            
        // forward force to add force
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);


        //Going up/down while wallrunning
        // upwards/downwards force
        if (upwardsRunning)
            rb.velocity = new Vector3(rb.velocity.x, wallClimbSpeed, rb.velocity.z);
        if (downwardsRunning)
            rb.velocity = new Vector3(rb.velocity.x, -wallClimbSpeed, rb.velocity.z);
    

        // push to wall (stick player to wall while the player not trying to get away of if)
        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0)){
            rb.AddForce(-wallNormal * 100, ForceMode.Force);
        }

        /*
        angle = Vector3.Angle(orientation.forward, wallForward);
        lookingWallBeingRunnedOn = Physics.Raycast(transform.position, orientation.forward, rayLength, whatIsWall); //Raycast to define the collision of player while running on wall
        //Debug.DrawRay(transform.position, orientation.forward*rayLength, Color.red); //Line to visualize raycast

        if(angle > 30 && lookingWallBeingRunnedOn ){
            stop = true;
            //StopWallRun();
        }
        else{
            stop = false;
        }
        */
       

        
    }


    private void StartWallRun(){
        pm.wallrunning = true;

        //apply camera effects
        if(wallLeft) cam.DoTilt(-5f);
        if(wallRight) cam.DoTilt(5f);


    }


      private void StopWallRun(){
        pm.wallrunning = false;

        //reset camera effects
        cam.DoFov(80f);
        cam.DoTilt(0f);


    }


    private void WallJump()
    {
        if(lg.holding || lg.exitingLedge) return;

        // enter exiting wall state
        exitingWall = true;
        exitWallTimer = exitWallTime;

        //Identify current wall normal
        Vector3 wallNormal = wallRight ? rightWallhit.normal : leftWallhit.normal;

        //Force to jump of wall
        Vector3 forceToApply = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        // reset y velocity and add force
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); //reset player velocity for better feeling
        rb.AddForce(forceToApply, ForceMode.Impulse);
    }



}
