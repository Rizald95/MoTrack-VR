using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.PXR;
using UnityEngine.InputSystem;

public class MotionObjTracker : MonoBehaviour
{
    public TrackerSN trackerSN { get; set; }
    public string serialNumber { get; set; }

    [SerializeField]
    private Renderer renderer;
    [SerializeField]
    private Material goodMat;
    [SerializeField]
    private Material badMat;
    [SerializeField]
    private MeshRenderer swordMesh;
    [SerializeField]
    private InputActionReference rightPrimary;//show mesh
    [SerializeField]
    private AudioSource audi;
    [SerializeField]
    private AudioClip swordOnAudi;

    private bool showMesh = false;
    private MotionTrackerLocations locations;
    private MotionTrackerConfidence confidence;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        if(!renderer)
            renderer = GetComponentInChildren<Renderer>();
    }

    private void OnEnable()
    {
        rightPrimary.action.started += OnRightPrimaryPressed;
        audi.PlayOneShot(swordOnAudi);
    }

    private void OnDisable()
    {
        rightPrimary.action.started -= OnRightPrimaryPressed;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (string.IsNullOrEmpty(serialNumber))
            return;

        UpdateTrackingData();
    }


    /// <summary>
    /// Unity Defaults to left-handed coordinate system
    /// GetMotionTrackerLocations gives right-handed coordinate, conversion is required
    /// </summary>
    private void UpdateTrackingData()
    {
        //Update position
        PXR_MotionTracking.GetMotionTrackerLocations(trackerSN, ref locations, ref confidence);
        
        transform.localPosition = locations.localLocation.pose.Position.ToVector3();
        transform.localRotation = locations.localLocation.pose.Orientation.ToQuat();

        //how confident it is correct, maybe lag, low light, positioning
        if (confidence == MotionTrackerConfidence.PXR_STATIC_ACCURATE || confidence == MotionTrackerConfidence.PXR_6DOF_ACCURATE)
        {
            //we are confident of position
            renderer.material = goodMat;
        }
        else
        {
            //we are not confident of position
            renderer.material = badMat;
        }

    }

    private void OnRightPrimaryPressed(InputAction.CallbackContext cb)
    {
        //toggle mesh
        showMesh = !showMesh;
        swordMesh.enabled = showMesh;
    }
}
