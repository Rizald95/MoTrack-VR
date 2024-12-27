using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneCtrl : MonoBehaviour
{
    [Header("Hover Components")]
    [SerializeField]
    private float hoverAmount = 0.15f; //The range up and down movement
    [SerializeField]
    private float hoverSpeed = 1f; //Speed of the hover movement

    [Header("Laser Fire Components")]
    [SerializeField]
    private GameObject laserPrefab;
    [SerializeField]
    private float fireInterval = 2.5f; //Time Delay between shots
    [SerializeField]
    private float laserSpeed = 1.5f; //Speed of laser
    [SerializeField]
    private float laserErrorMargin = .1f;// Error of margin for laser fire
    [SerializeField]
    private Transform firePoint;
    [SerializeField]
    private AudioSource audioSource;// laser fire audio

    private Transform player;
    private Vector3 startPosition;
    private float hoverTime;//Tracks hover oscillation
    private float fireTimer;//Tracks time between shots

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        player = Camera.main.transform; //For player look
        startPosition = transform.position; // Store the initial position for hover
        hoverTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        DroneHover();
        LookAtPlayer();
        FireLaser();
    }

    private void DroneHover()
    {
        //Calculate new position based on a sine wave for smooth hovering
        hoverTime += Time.deltaTime * hoverSpeed;
        float hoverOffset = Mathf.Sin(hoverTime) * hoverAmount;
        transform.position = startPosition + new Vector3(0, hoverOffset, 0);
    }

    private void LookAtPlayer()
    {
        if(player)
        {
            //make drone look at player
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    private void FireLaser()
    {
        fireTimer += Time.deltaTime;
        if(fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            if(laserPrefab && firePoint)
            {
                //Instantiate new laser
                GameObject newLaser = Instantiate(laserPrefab, firePoint.position, firePoint.rotation);

                //Add error to laser direction
                Vector3 randomError = new Vector3(
                    Random.Range(-laserErrorMargin, laserErrorMargin),
                    Random.Range(-laserErrorMargin, laserErrorMargin),
                    Random.Range(-laserErrorMargin, laserErrorMargin));

                newLaser.transform.forward = firePoint.forward + randomError;

                //Add force to laser
                Rigidbody rb = newLaser.GetComponent<Rigidbody>();
                if(rb)
                {
                    rb.AddForce(newLaser.transform.forward * laserSpeed, ForceMode.Impulse);
                }

                //play laser audio
                audioSource.Play();
            }
        }
    }
    
}
