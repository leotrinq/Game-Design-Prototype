using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Dashing : MonoBehaviour
{
    

    [Header("References")]
    public Transform orientation;
    public Transform playerCam;
    private Rigidbody rb;
    private PlayerMovement pm;

    
    [Header("Dashing")]
    public float dashForce;
    public float dashUpwardForce;
    //public float maxDashYSpeed;
    public float dashDuration; //To limit dash in y
    public float maxDashYSpeed;


    [Header("CameraEffects")]
    public PlayerCam cam;
    public float dashFov; //66 is a good value
    //public bool useCameraEffectOnDash; 



    
    [Header("Settings")]
    public bool useCameraForward = true; //Using or not camera direction to dash
    public bool allowAllDirections = true; //To dash using zqsd or not
    public bool disableGravity = false; //To change dash feeling
    public bool resetVel = true;

    
    [Header("Cooldown")]
    public float dashCd; //Dash cooltime
    private float dashCdTimer;


    [Header("Input")]
    public KeyCode dashKey = KeyCode.E;



    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        
    }

    // Update is called once per frame
    private void Update()
    {
        if(Input.GetKeyDown(dashKey)){
            Dash();    
        }

        if (dashCdTimer > 0){ //Cooldown counting down
            dashCdTimer -= Time.deltaTime;
        }  
            

        
    }


    private Vector3 delayedForceToApply;
    private void Dash()
    {
        //Cooldown stil active so you can't dash
        if (dashCdTimer > 0){
            return;
        }    
        else {
            dashCdTimer = dashCd;
        }

        pm.dashing = true;
        pm.maxYSpeed = maxDashYSpeed;

        cam.DoFov(dashFov); //Camera effect for dash

        Transform forwardT;

        if (useCameraForward)
            forwardT = playerCam; // where you're looking
        else
            forwardT = orientation; // where you're facing (no up or down)

    
        

    
        Vector3 direction = GetDirection(forwardT);
        
        Vector3 forceToApply = direction * dashForce + orientation.up * dashUpwardForce; //Force of the dash
        
        if (disableGravity){ //To have better feeling
            rb.useGravity = false;
        }
            

        delayedForceToApply = forceToApply;
        Invoke(nameof(DelayedDashForce), 0.025f);
        Invoke(nameof(ResetDash), dashDuration); //Stop dash after a certain duration

    }

    
    //Sometimes the force gets added before the movement script switches to dashing mode
    //So we wait before adding dash force
    private void DelayedDashForce()
    {
        //We reset velocity before dashing
        if (resetVel)
            rb.velocity = Vector3.zero;

        rb.AddForce(delayedForceToApply, ForceMode.Impulse); //Force application
    }

    private void ResetDash()
    {
        
        pm.dashing = false;
        pm.maxYSpeed = 0;

        cam.DoFov(60f); //Reset to default field of view

        if (disableGravity)
            rb.useGravity = true;
    
    }

    //Allow to dash in direction using zqsd,
    private Vector3 GetDirection(Transform forwardT)
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3();

        //If we allow or not to dash in any direction with keys
        //Dash in all direction depending of the key pressed
        if (allowAllDirections)
            direction = forwardT.forward * verticalInput + forwardT.right * horizontalInput;
        else
            direction = forwardT.forward;

        //If no key pressed, forward direction
        if (verticalInput == 0 && horizontalInput == 0)
            direction = forwardT.forward;

        return direction.normalized;
    }




}
