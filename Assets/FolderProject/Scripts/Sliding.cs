using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Sliding : MonoBehaviour
{

    [Header("References")]
    public Transform orientation; //Empty object tracking where the pkayer is looking
    public Transform playerObj;
    private Rigidbody rb;
    private PlayerMovement pm; //Script created

    [Header("Sliding")]
    public float maxSlideTime; 
    public float slideForce; 
    private float slideTimer; //To handle duration of the slide 

    public float slideYScale; //While sliding the player will shrink
    private float startYScale; //Original player scale

    [Header("Input")]
    public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;

    private bool sliding;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>(); //Get the PlayerMovement script

        startYScale = playerObj.localScale.y;
    }


    // Update is called once per frame
    void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal"); //A and D 
        verticalInput = Input.GetAxis("Vertical"); //Z and S

        if(Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0)){
            StartSlide();
        }

        if(Input.GetKeyUp(slideKey) && pm.sliding){
            StopSlide();
        }
        
    }


    private void FixedUpdate(){
        if(pm.sliding){
            SlidingMovement();
        }
    }



    private void StartSlide(){
        pm.sliding = true;

        playerObj.localScale = new Vector3(playerObj.localScale.x, slideYScale, playerObj.localScale.z); //shrinking player
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse); //To stay on ground
        
        slideTimer = maxSlideTime; //slide timer reset
    }


    //To  handle movement and apply the sliding force
     private void SlidingMovement(){
        //To slide in any direcion depending of keyd pressed
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        //if not on slope or going upward
        //sliding normal : give more speed while sliding
        if(!pm.OnSlope() || rb.velocity.y > -0.1f){
            //To apply force in the calculted direction
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime; //Count slide timer while sliding
        }
        
        //sliding down a slope
        else {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
            //No timer we slide as long as there is a slope
        }
  
        if(slideTimer <= 0){
            StopSlide();

        }
    }



     private void StopSlide(){
        pm.sliding = false;
        //player scale back to normal
        playerObj.localScale = new Vector3(playerObj.localScale.x, startYScale, playerObj.localScale.z); //shrinking player
        
    }







}
