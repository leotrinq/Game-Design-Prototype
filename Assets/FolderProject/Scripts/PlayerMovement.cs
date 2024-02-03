using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

//using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;




public class PlayerMovement : MonoBehaviour
{
    public RaycastHit groundHit;

    public bool contact;


    private Vector3 hookShotPosition;


    [Header("Movement")]  //Create "Movement" section in the inspector 
    private float moveSpeed; //Player movement speed varies depending the action 
    //General/original  player movement speed
    //This variable replace moveSpeed because now there is momentum
    //We still use moveSpeed for movement  
    private float desiredMoveSpeed; //The speed the player tend to 
    private float lastDesiredMoveSpeed;
    public float walkSpeed;
    public float sprintSpeed; 
    public float slideSpeed;
    public float wallRunSpeed;
    //public float climbSpeed;

    public float dashSpeed;
    public float dashSpeedChangeFactor;

    public float maxYSpeed; //TO limit the dash when going up with the mouse

    //Variables to go faster according to the slope
    public float speedIncreaseMultiplier;
    public float slopeIncreaseMultiplier; //Determine the acceleration on slope


    [Header("Jumping")]
    //Handle jump
    public float jumpForce;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.25f; //How much we multiply gravit in the air. airMultiplier = 1/4 du ground
    public float fallMultiplier = 2.5f; //Fall speed
    public float lowJumpMultiplier = 2f; //If we stop pressing on jump mid this gravity is applied 
    bool readyToJump;
    

    [Header("Crouching")]
    public float crouchSpeed; //Speed when crouching
    public float crouchYScale; //Axis in relation to which it is reduced
    private float startYScale; //Initial height of the player


    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space; //To jump
    public KeyCode sprintKey = KeyCode.LeftShift; //To run
    public KeyCode crouchKey = KeyCode.LeftControl; //To crouch

    [Header("Ground Check")]  
    public float playerHeight; //To have a height for the raycast
    public LayerMask whatIsGround;
    public bool grounded;
    public bool underSomething;

    
    [Header("Slope Handling")]  
    public float maxSlopeAngle; //Maximum angle of ground that can be considered as a slope
    public RaycastHit slopeHit; //Raycast : Structure used to get information back from a raycast.
    //RaycastHit.normal : The normal of the surface the ray hit.
    private bool exitingSlope; //To jump on slope

    [Header("References")] 
    public Climbing climbingScript;


    public Transform orientation;
    
    public float groundDrag = 5 ; //Manage adhesion to the ground

    //Hotizontal and vertial keyboard input 
    float horizontalInput;
    float verticalInput;

    
    // Adjust the transition speed by changing the interpolation factor
    public float lerpSpeed = 3.0f; // Increase this value for a faster transition
    
    Vector3 moveDirection; //Direction the player is moving to

    Rigidbody rb; //Reference to player rigidbody


    [Header("Grappling")] 
    public float grapplingPropulsionForce; 
    public float gravityEffect;//If < 1 reduce gravity effect



    
    private static System.Timers.Timer aTimer;



    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>(); //Access to yout rb
        rb.freezeRotation = true; //If not freezed, the player fall
        
        readyToJump = true;

        startYScale = transform.localScale.y; //Default size of the player

    }

    // Update is called once per frame
    void Update()
    {
        //rigidbodyVelocity = rb.velocity;
        //Casts ray from  origin in direction of length maxDistance
        //Return true if collide with object
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.1f, whatIsGround);
        
        underSomething = Physics.Raycast(transform.position, Vector3.up, startYScale * 0.5f + 0.1f, whatIsGround);
        
        MyInput(); 
        SpeedControl();
        StateHandler();
        //KeepMomentumAfterSlide();

        //Handle drag
        //Drag only when on ground and not dashing
        if(state == MovementState.walking || state == MovementState.sprinting || state == MovementState.crouching){
            rb.drag = groundDrag;
        }
        else{
            rb.drag = 0;
        }

    }

    private void FixedUpdate(){
        MovePlayer();
        JumpHandler();
    }


    public MovementState state; //store the current state the player is in
    public MovementState previousStateForSliding; //Variable that I added for momentum
    public MovementState previousState ;//
    

    private MovementState lastState;
    private bool keepMomentum;


    public enum MovementState {
        freeze,
        unlimited,
        walking,
        sprinting,
        wallrunning,
        //climbing,
        crouching,
        sliding,
        dashing,
        air,
        onSlopeSlidingDownward
    }

    public bool sliding;
    public bool wallrunning;
    public bool freeze;
    public bool activeGrapple;
    public bool unlimited;
    public bool dashing;


    public bool restricted; //Allow to stop normal player movement. Used for ledge
    //public bool climbing;
    


    //Handle the different possible state of the player
    public void StateHandler(){
        MovementState currentState = state;
        //Mode - Climbing
        /*if(climbing){
            state = MovementState.climbing;
            desiredMoveSpeed = climbSpeed;
        }*/
    

        //Mode - Dashing
        if(dashing){
            state = MovementState.dashing;
            desiredMoveSpeed = dashSpeed;
            speedChangeFactor = dashSpeedChangeFactor;
        }

        //Mode - Freeze
        else if(freeze){
            state = MovementState.freeze;
            moveSpeed = 0;
            rb.velocity = Vector3.zero;
        }

        //Mode - Uunlimited
        else if (unlimited){
            state = MovementState.unlimited;
            moveSpeed = 999f;
            return;
        }

        //Mode - wallRunning
        else if (wallrunning){
            state = MovementState.wallrunning;
            desiredMoveSpeed = wallRunSpeed;
        }
        /*
        else if (sliding && OnSlope() && rb.velocity.y < 0.1f ){
            state = MovementState.onSlopeSlidingDownward;
        }*/

        //Mode - Sliding
        else if(sliding){
            state = MovementState.sliding;
            //if on slope and moving downward
            if(OnSlope() && rb.velocity.y < 0.1f){
                desiredMoveSpeed = slideSpeed;
                state = MovementState.onSlopeSlidingDownward;
                Debug.Log("1ere condition");
            }
            /*else{
                desiredMoveSpeed = sprintSpeed;
                Debug.Log("2eme condition");
            }*/
        }

        //Mode - Crouching
        else if(Input.GetKey(crouchKey)){
            state = MovementState.crouching;
            desiredMoveSpeed = crouchSpeed;
        }

        //Mode - Sprinting
        else if(grounded && Input.GetKey(sprintKey)){
            state = MovementState.sprinting;
            desiredMoveSpeed = sprintSpeed;
        }
        //Mode - Walking
        else if(grounded){
            state = MovementState.walking;
            desiredMoveSpeed = walkSpeed;
        }

        //Mode - Air
        else {
            state = MovementState.air;

            //To keepmomentum after dash
            if(desiredMoveSpeed < sprintSpeed){
                desiredMoveSpeed = walkSpeed;
            } 
            else{
                desiredMoveSpeed = sprintSpeed;
            }
        }

        //I added it to better handle momentum on slope
        if(previousState == MovementState.onSlopeSlidingDownward) {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeedAfterSlide());
        }
        else{
            moveSpeed = desiredMoveSpeed; //IMPORTANT LINE
        }
        

        
        bool desiredMoveSpeedHasChanged = desiredMoveSpeed != lastDesiredMoveSpeed;
        if(lastState == MovementState.dashing) keepMomentum = true;

        if(desiredMoveSpeedHasChanged){
            if (keepMomentum){
                StopAllCoroutines();
                StartCoroutine(SmoothlyLerpMoveSpeedAfterDash());
            }
            else{
                StopAllCoroutines();
                moveSpeed = desiredMoveSpeed ;
            }

        }

        lastDesiredMoveSpeed = desiredMoveSpeed;
        lastState = state;

        if (currentState != state) {
            previousState = currentState;
    }
    }







    //change the moveSpeed to the desiredMoveSpeed over time (smoothly)
    /*private IEnumerator SmoothlyLerpMoveSpeed(){
        //smoothly lerp movement speed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed); //difference between the speed wanted and the current speed
        float startValue = moveSpeed;

        while(time < difference){
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference * lerpSpeed);
            time += Time.deltaTime;
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;

    }*/

    private IEnumerator SmoothlyLerpMoveSpeedAfterSlide(){
        //smoothly lerp movement speed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed); //difference between the speed wanted and the current speed
        Debug.Log("Beginning of the function : difference " + difference);
        float startValue = moveSpeed;
        Debug.Log("Beginning of the function " + startValue);
        while(time < difference){
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference);
            
            if(OnSlope()){
                float slopeAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else 
                time += Time.deltaTime * speedIncreaseMultiplier;
            
            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
        Debug.Log("End of the function"+moveSpeed);
    }


    private float speedChangeFactor;
    private IEnumerator SmoothlyLerpMoveSpeedAfterDash(){

        //smoothly lerp movement speed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed); //difference between the speed wanted and the current speed
        float startValue = moveSpeed;

        float boostFactor = speedChangeFactor; //Higher the boost factor is, the higher the speed change

        while(time < difference){
            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, time / difference * lerpSpeed);

            time += Time.deltaTime * boostFactor;

            yield return null;
        }

        moveSpeed = desiredMoveSpeed;
        speedChangeFactor = 1f;
        keepMomentum = false;
    }


    

/*

    //If on slope, sliding and going downward activate momentum
    //In this state, we activate lerp
    private void KeepMomentumAfterSlide(){
        if (OnSlope() && sliding && rb.velocity.y < 0.1f){
            previousStateForSliding = MovementState.onSlopeSlidingDownward;
        }
        else{
            previousStateForSliding = state; //To not always have the same state or the speed never decrease
        }
    }*/


    //Get input on x and y axis. 
    private void MyInput(){
        ////GetAxis return from -1 to 1 depending the tilt on axis
        horizontalInput = Input.GetAxisRaw("Horizontal"); 
        verticalInput = Input.GetAxisRaw("Vertical");
        //Debug.Log("horizontalInput : "+horizontalInput) ;
        //Debug.Log("verticalInput : "+verticalInput) ;
        

         // when to jump
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        //start crouch
        if(Input.GetKeyDown(crouchKey)){
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z); //shrink player on y axis
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse); //Quick push the player on ground
        }

        //stop crouch
        if(Input.GetKeyUp(crouchKey)){
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z); //expandink player on y axis
        }

    }

    private void MovePlayer(){

        //if (activeGrapple) return; //To not move while grappling

        //On slope we turn off gravity which collide with the
        //use gravity in dash. So if state of dashing we
        //disable player movement
        if(state == MovementState.dashing) return;

        if (restricted) return;
        
        //mean while exiting a wall, forward key has no effect
        //if(climbingScript.exitingWall) return; 

        //calculate movement direction
        //Moving forward * 1 = Moving forward 
        //Moving forwa rd * -1 = Moving backward
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if(OnSlope()){
            rb.AddForce(GetSlopeMoveDirection(moveDirection) * moveSpeed * 20, ForceMode.Force);

            if(rb.velocity.y >0){
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);//prevent player from bumping when going up slope
            }
        }

        else if(grounded){
            //Apply a force in a direction indicated by vector
            //Moving rb *10 to go faster
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force) ;
        }
        else if (!grounded){
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        //turn gravity off while on slope to note glide
        rb.useGravity = !OnSlope();

    }

    //Better jump gravity
    //Varies according to the jump button used or not
    private void JumpHandler(){
        if(rb.velocity.y < 0){//if we're falling 
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime ; //Physics.gravity.y = -9.81
        }
        else if (rb.velocity.y > 0 && !Input.GetKey(jumpKey) ){//if we're jumping
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime ;
        }
    }


    //Limit max speed of player
    private void SpeedControl(){
        
        //To not limit speed while grappling
        if(activeGrapple) return;

        //limting speed on slope
        if (OnSlope() && !exitingSlope){
            if(rb.velocity.magnitude > moveSpeed){
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }

        //limitting speed on ground or in air
        else{
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            //limit velocity if needed
            if(flatVel.magnitude > moveSpeed){
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

        }

        //limit y velocity for dash
        if(maxYSpeed != 0 && rb.velocity.y > maxYSpeed){
            rb.velocity = new Vector3(rb.velocity.x, maxYSpeed, rb.velocity.z);
        }


        
    }

    private void Jump(){

        exitingSlope = true;

        //reset y velocity (to always jump the same height)
        //We stopped all vertical movement
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        //maxJump = transform.position.y * 10 ;
        //Apply vertical force
        //while(transform.position.y < maxJump){
        rb.AddForce(transform.up*jumpForce, ForceMode.Impulse);
        //}
        
    }

    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }



    //Identify if the player is on slope or not
    public bool OnSlope(){
        // out slopeHit : allow to store the info of the object hit in slopeHit
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.1f)){
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);//calculate how steep the ground is. Angle between the slope an the vector up
            return angle < maxSlopeAngle && angle != 0;
        }

        else{
            return false;
        }

    }

    //Define the correction direction relative to slope
    public Vector3 GetSlopeMoveDirection(Vector3 direction){
        //normalized cause it's a direction
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }


    public bool enableMovementOnNextTouch;
    public void JumpToPosition(Vector3 targetPosition, float trajectoryHeight){
        activeGrapple = true ;
        
        velocityToSet = CalculateJumpVelocity(transform.position, targetPosition, trajectoryHeight);
        Invoke(nameof(SetVelocity), 0.1f); //Called with delay to not overstep with speed control
    }


    private Vector3 velocityToSet;
    private void SetVelocity(){
        rb.velocity = velocityToSet;
        enableMovementOnNextTouch = true;
    }


    public void ResetRestrictions(){
        activeGrapple = false;
    }
    private void OnCollisionEnter(Collision collision){
        if(enableMovementOnNextTouch){
            enableMovementOnNextTouch = false;
            ResetRestrictions();

            GetComponent<Grappling>().StopGrapple();
        }
    }

    //To be projected toward grappling
    public Vector3 CalculateJumpVelocity (Vector3 startPoint, Vector3 endPoint, float trajectoryHeight){
        //Vector3 hookShotDir = (hookShotPosition - transform.position).normalized;
        float gravity = Physics.gravity.y * gravityEffect ;
        float displacementY = endPoint.y - startPoint.y;
        Vector3 displacementXZ = new Vector3(endPoint.x - startPoint.x, 0f, endPoint.z - startPoint.z); 

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * trajectoryHeight);
        Vector3 velocityXZ = displacementXZ / (Mathf.Sqrt(-2 * trajectoryHeight / gravity) 
            + Mathf.Sqrt(2 * (displacementY - trajectoryHeight) /  gravity)) * grapplingPropulsionForce;

        return velocityXZ + velocityY;
    }


    
}
