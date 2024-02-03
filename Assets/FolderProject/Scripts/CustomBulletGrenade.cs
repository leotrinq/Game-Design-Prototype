using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomBulletGrenade : MonoBehaviour
{

    //Assignables
    public Rigidbody rb;
    public GameObject explosion;
    public LayerMask whatIsEnemies;

    //Stats
    [Range(0f,1f)] //Attribute for range
    public float bounciness; //between 0 and 1
    public bool useGravity;

    //Damage
    public int explosionDamage;
    public float explosionRange;
    public float explosionForce;

    //Lifetime
    //3 way the bullet can explode
    public int maxCollisions; //reached max collision
    public float maxLifetime; //life time runs out
    public bool explodeOnTouch = true; //touch with enemy

    int collisions;
    PhysicMaterial physics_mat;


    // Start is called before the first frame update
    void Start()
    {
        Setup();
    }

    // Update is called once per frame
    void Update()
    {
        //When to explode:
        if (collisions > maxCollisions) Explode();

        //Count down lifetime
        maxLifetime -= Time.deltaTime;
        if (maxLifetime <= 0) Explode();
    }


    private void Explode()
    {
        //Instantiate explosion
        //Quaternion.identity for no rotation
        if (explosion != null) Instantiate(explosion, transform.position, Quaternion.identity);

        //Check for enemies 
        //OverlapSphere create a sphere. Colliders touching or inside the sphere are stored
        Collider[] enemies = Physics.OverlapSphere(transform.position, explosionRange, whatIsEnemies);
        //Get all enemies and damage them
        for (int i = 0; i < enemies.Length; i++)
        {
            
            //Get component of enemy and call Take Damage
            //Just an example!
            enemies[i].GetComponent<EnemyAi>().TakeDamage(explosionDamage);

            //Add explosion force (if enemy has a rigidbody)
            if (enemies[i].GetComponent<Rigidbody>())
                enemies[i].GetComponent<Rigidbody>().AddExplosionForce(explosionForce, transform.position, explosionRange);
        }
        //Destroy(gameObject);
        //Add a little delay, just to make sure everything works fine
        Invoke("Delay", 0.05f);
    }

    //Function to destroy bullets with delay
    private void Delay()
    {
        Destroy(gameObject);
    }

    //funciton called when object collid with any other object 
    private void OnCollisionEnter(Collision collision)
    {
        //Count up collisions
        collisions++;

        //Explode if bullet hits directly an object tagged as enemy and explodeOnTouch is activated
        if (collision.collider.CompareTag("Enemy") && explodeOnTouch) Explode();
    }

    //Setup the physic of the bullet
    private void Setup()
    {
        //Create a new Physic material
        physics_mat = new PhysicMaterial();
        physics_mat.bounciness = bounciness;

        //To be sure it bounces on everything
        physics_mat.frictionCombine = PhysicMaterialCombine.Minimum; 
        physics_mat.bounceCombine = PhysicMaterialCombine.Maximum;

        //Assign material to collider
        GetComponent<SphereCollider>().material = physics_mat; //Associat a personal material

        //Set gravity
        rb.useGravity = useGravity;

        //Initialize bullet position

    }

    //Just to visualize the explosion range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
    }

}



/*
Function TakeDamage for enemy AI
public void TakeDamage(int damage) {

    health -= damage;

    if(health < 0){
        idDead = true;
    }

}




*/