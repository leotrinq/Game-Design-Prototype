using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerCam : MonoBehaviour
{

    //Sensibility on x and y axis of the mouse
    public float sensX ;
    public float sensY ;

    //For player orientation 
    public Transform orientation ;
    public Transform camHolder;

    //Rota of the camera on x and y axis
    float xRotation ;
    float yRotation ;



    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked ; //Cursor locked on middle of screen
        Cursor.visible = false ; //Make cursor invisible
    }

    // Update is called once per frame
    void Update()
    {
        //get mouse input
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX; //We turn on y when we go from left to right (x axis)
        
        xRotation -= mouseY; //We turn on x when we go from down to up (y axis)
        xRotation = Mathf.Clamp(xRotation, -90f, 90); //We can't look up or down more than 90°

        //rotate camera and position
        camHolder.rotation = Quaternion.Euler(xRotation, yRotation, 0); //rotate camera on x and y
        orientation.rotation = Quaternion.Euler(0, yRotation, 0); //rotate player on y axis

    }


    public void DoFov(float endValue){
        GetComponent<Camera>().DOFieldOfView(endValue, 0.25f);
    }


    public void DoTilt(float zTilt){
        transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.25f);
    }


}
