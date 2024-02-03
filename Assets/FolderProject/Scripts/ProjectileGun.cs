using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class ProjectileGun : MonoBehaviour
{

    //bullet 
    public GameObject bullet;

    //bullet force
    public float shootForce, upwardForce;


    [Header("Gun stats")]
    //timeBetweenShooting == time to wait to fire again after firing 
    //timeBetweenShots = time between bullets
    //magazineSize = number of bullet the gun can contain
    //bulletsPerTap = number of bullet per shot
    public float timeBetweenShooting, spread, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap;
    public bool allowButtonHold;
    
    int bulletsLeft, bulletsShot; //number of bullets shot and not shot yet

    //bools
    //To check the sate of the gun
    bool shooting, readyToShoot, reloading;

    /*Recoil
    public Rigidbody playerRb;
    public float recoilForce;*/

    [Header("Reference")]
    public Camera fpsCam;
    public Transform attackPoint;
    public Vector3 bulletOrientation; 


    [Header("Graphics")]
    public GameObject muzzleFlash;
    public TextMeshProUGUI ammunitionDisplay;


    //bug fixing :D
    public bool allowInvoke = true;



    private void Awake()
    {
        //make sure magazine is full
        bulletsLeft = magazineSize;
        readyToShoot = true;
    }




    // Start is called before the first frame update
    void Start()
    {
        //bullet.transform.SetParent(this.transform);
    }

    // Update is called once per frame
    void Update()
    {
         MyInput();

        //Set ammo display, if it exists
        if (ammunitionDisplay != null)
            ammunitionDisplay.SetText(bulletsLeft / bulletsPerTap + " / " + magazineSize / bulletsPerTap);
            //If we shoot 8 bullet per tap and the ammo is 16, we don't display 16/16 but 2/2
    
    }


    private void MyInput()
    {
        //Check if allowed to hold down button and take corresponding input
        if (allowButtonHold) shooting = Input.GetKey(KeyCode.Mouse0);
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        //Reloading 
        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading) Reload();

        
        //Reload automatically when trying to shoot without ammo
        if (readyToShoot && shooting && !reloading && bulletsLeft <= 0) Reload();

        //Condition for Shooting
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            //Set bullets shot to 0
            bulletsShot = 0;

            Shoot();
        }
    }

    private void Shoot()
    {
        readyToShoot = false;

        //Find the exact hit position using a raycast
        Ray ray = fpsCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); //Just a ray through the middle of your current view
        RaycastHit hit;

        //check if ray hits something
        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
            targetPoint = hit.point;
        else
            targetPoint = ray.GetPoint(75); //Just a point far away from the player

        //Calculate direction from attackPoint to targetPoint
        Vector3 directionWithoutSpread = targetPoint - attackPoint.position;

        //Calculate spread
        //If we want to spread bullet
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        //Calculate new direction with spread
        Vector3 directionWithSpread = directionWithoutSpread + new Vector3(x, y, 0); //Just add spread to last direction

        //Instantiate bullet/projectile
        //Quaternion.identity disable rotation so the object is aligned with the world or parent axe
        GameObject currentBullet = Instantiate(bullet, attackPoint.position, /*Quaternion.Euler(bulletOrientation)*/Quaternion.identity); //store instantiated bullet in currentBullet
        //Rotate bullet to shoot direction
        currentBullet.transform.forward = transform.forward; //directionWithSpread.normalized;
        //currentBullet.transform.Rotate(bulletOrientation);
        //currentBullet.transform.localRotation = Quaternion.Euler(bulletOrientation);
        //currentBullet.transform.Rotate(90.0f, -15.0f, 0.0f, Space.Self); //Roate the bullet to be in the right direction

        //Add forces to bullet
        currentBullet.GetComponent<Rigidbody>().AddForce(directionWithSpread.normalized * shootForce, ForceMode.Impulse); //Forward
        currentBullet.GetComponent<Rigidbody>().AddForce(fpsCam.transform.up * upwardForce, ForceMode.Impulse); //Upward used if special munition like grenade

        //Instantiate muzzle flash, if you have one
        if (muzzleFlash != null)
            Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        //Count of bullets
        bulletsLeft--; 
        bulletsShot++;

        //Invoke resetShot function (if not already invoked), with your timeBetweenShooting
        //After every shooting we wait a bit before shooting again
        if (allowInvoke)
        {
            Invoke("ResetShot", timeBetweenShooting); //timeBetweenShooting in second
            allowInvoke = false;

            //Add recoil to player (should only be called once)
            //playerRb.AddForce(-directionWithSpread.normalized * recoilForce, ForceMode.Impulse); //Force in opposite direction
        }

        //if more than one bulletsPerTap make sure to repeat shoot function
        if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
            Invoke("Shoot", timeBetweenShots);
            
    }
    
    private void ResetShot()
    {
        //Allow shooting and invoking again
        readyToShoot = true;
        allowInvoke = true;
    }

    private void Reload()
    {
        reloading = true;
        Invoke("ReloadFinished", reloadTime); //Invoke ReloadFinished function with your reloadTime as delay
    }

    private void ReloadFinished()
    {
        //Fill magazine
        bulletsLeft = magazineSize;
        reloading = false;
    }
}
