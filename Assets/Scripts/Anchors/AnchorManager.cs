using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.PXR;
using UnityEngine.InputSystem;
using System;

public class AnchorManager : MonoBehaviour
{
    [Header("Game Components")]
    [SerializeField]
    private GameObject roadPreview;//rightGrip
    [SerializeField]
    private GameObject ballPreview;//rightPrimary
    [SerializeField]
    private GameObject roadPrefab;//rightGrip
    [SerializeField]
    private GameObject ballPrefab;//rightPrimary

    [Header("Controller")]
    [SerializeField]
    private InputActionReference rightGrip;//Road
    [SerializeField]
    private InputActionReference rightPrimary;//Ball
    [SerializeField]
    private InputActionReference leftPrimary;//Delete Last

    private bool anchorProviderState = false;
    private Dictionary<ulong, Transform> anchorMap = new Dictionary<ulong, Transform>();
    private Stack<(GameObject, ulong)> objectStack = new Stack<(GameObject, ulong)>();

    // Start is called before the first frame update
    void Start()
    {
        PXR_Manager.EnableVideoSeeThrough = true;
        StartAnchorProvider();
    }

    private void OnApplicationPause(bool pause)
    {
        //if not paused, enable seethrough
        PXR_Manager.EnableVideoSeeThrough = !pause;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        rightGrip.action.started += OnRightGripPressed;
        rightGrip.action.canceled += OnRightGripReleased;
        rightPrimary.action.started += OnRightPrimaryPressed;
        rightPrimary.action.canceled += OnRightPrimaryReleased;
        leftPrimary.action.started += OnLeftPrimaryPressed;
    }

    private void OnDisable()
    {
        rightGrip.action.started -= OnRightGripPressed;
        rightGrip.action.canceled -= OnRightGripReleased;
        rightPrimary.action.started -= OnRightPrimaryPressed;
        rightPrimary.action.canceled -= OnRightPrimaryReleased;
        leftPrimary.action.started -= OnLeftPrimaryPressed;

    }

    private async void StartAnchorProvider()
    {
        var result = await PXR_MixedReality.StartSenseDataProvider(PxrSenseDataProviderType.SpatialAnchor);
        if(result == PxrResult.SUCCESS)
        {
            anchorProviderState = true;
        }
    }

    private void OnRightGripPressed(InputAction.CallbackContext cb)
    {
        //Show preview object
        ShowPreview(roadPreview, true);
    }

    private void OnRightGripReleased(InputAction.CallbackContext cb)
    {
        //Make Anchor then instantiate
        CreateSpatialAnchor(roadPreview, roadPrefab);
    }

    private void OnRightPrimaryPressed(InputAction.CallbackContext cb)
    {
        //Show preview object
        ShowPreview(ballPreview, true);
    }

    private void OnRightPrimaryReleased(InputAction.CallbackContext cb)
    {
        //Make Ball
        CreateObject(ballPreview, ballPrefab);
    }

    private void OnLeftPrimaryPressed(InputAction.CallbackContext cb)
    {
        //Delete last object
        DeleteLastObject();
    }

    private void ShowPreview(GameObject previewObj, bool state)
    {
        previewObj.SetActive(state);
    }

    private async void CreateSpatialAnchor(GameObject previewObj, GameObject anchorPrefab)
    {
        if(!anchorProviderState)
        {
            Debug.Log("Anchor Provider is not active");
            return;
        }

        Transform point = previewObj.transform;
        var result = await PXR_MixedReality.CreateSpatialAnchorAsync(point.position, point.rotation);
        Debug.Log($"CreateSpatialAnchor: {result.ToString()}");
        if(result.result == PxrResult.SUCCESS)
        {
            //Add prefab to new spatial anchor location
            GameObject anchorObj = Instantiate(anchorPrefab, point.position, point.rotation);
            //keep track of Spatial Anchors
            anchorMap.Add(result.anchorHandle, anchorObj.transform);
            objectStack.Push((anchorObj, result.anchorHandle));
        }
        ShowPreview(previewObj, false);
    }

    private void CreateObject(GameObject previewObj, GameObject previewPrefab)
    {
        GameObject newObject = Instantiate(previewPrefab, previewObj.transform.position, previewObj.transform.rotation);
        objectStack.Push((newObject, UInt64.MinValue));
        ShowPreview(previewObj, false);
    }

    //Deleate the last instantiated object
    private void DeleteLastObject()
    {
        //Item1 is GameObject
        //Item2 will hold the anchor handle if gameobject is a spatial anchor
        if(objectStack.Count > 0)
        {
            var lastObject = objectStack.Pop();
            if (lastObject.Item2 != UInt64.MinValue)
            {
                //if the ulong anchor handle does not == 0, then its a valid spatial anchor
                var result = PXR_MixedReality.DestroyAnchor(lastObject.Item2);
                Debug.Log($"Destroying Spatial Anchor - {result.ToString()}");
            }
            GameObject.Destroy(lastObject.Item1);
        }
    }

}
