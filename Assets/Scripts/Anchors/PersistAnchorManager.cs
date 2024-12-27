using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.PXR;
using UnityEngine.InputSystem;
using System;

public class PersistAnchorManager : MonoBehaviour
{
    [Header("Game Components")]
    [SerializeField]
    private GameObject roadPreview;//rightGrip
    [SerializeField]
    private GameObject ballPreview;//rightPrimary
    [SerializeField]
    private GameObject anchorPreview;//leftGrip
    [SerializeField]
    private GameObject roadPrefab;//rightGrip
    [SerializeField]
    private GameObject ballPrefab;//rightPrimary
    [SerializeField]
    private GameObject anchorPrefab;//leftGrip
    [SerializeField]
    private GameObject menuObj;//rightSecondary

    [Header("Controller")]
    [SerializeField]
    private InputActionReference rightGrip;//Road
    [SerializeField]
    private InputActionReference leftGrip;//Anchor
    [SerializeField]
    private InputActionReference rightPrimary;//Ball
    [SerializeField]
    private InputActionReference leftPrimary;//Delete Last
    [SerializeField]
    private InputActionReference rightSecondary;//MenuToggle

    private bool anchorProviderState = false;
    private Dictionary<ulong, Transform> anchorMap = new Dictionary<ulong, Transform>();
    private Stack<(GameObject, ulong)> objectStack = new Stack<(GameObject, ulong)>();
    private bool menuActiveState = false;
    private List<ulong> currPersistList = new List<ulong>();
    private List<ulong> allPersistList = new List<ulong>();
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
        leftGrip.action.started += OnLeftGripPressed;
        leftGrip.action.canceled += OnLeftGripReleased;
        rightPrimary.action.started += OnRightPrimaryPressed;
        rightPrimary.action.canceled += OnRightPrimaryReleased;
        leftPrimary.action.started += OnLeftPrimaryPressed;
        rightSecondary.action.started += OnRightSecondaryPressed;
    }

    private void OnDisable()
    {
        rightGrip.action.started -= OnRightGripPressed;
        rightGrip.action.canceled -= OnRightGripReleased;
        leftGrip.action.started -= OnLeftGripPressed;
        leftGrip.action.canceled -= OnLeftGripReleased;
        rightPrimary.action.started -= OnRightPrimaryPressed;
        rightPrimary.action.canceled -= OnRightPrimaryReleased;
        leftPrimary.action.started -= OnLeftPrimaryPressed;
        rightSecondary.action.started -= OnRightSecondaryPressed;
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

    private void OnLeftGripPressed(InputAction.CallbackContext cb)
    {
        //Show preview object
        ShowPreview(anchorPreview, true);
    }

    private void OnLeftGripReleased(InputAction.CallbackContext cb)
    {
        //Make Anchor then instantiate
        CreateSpatialAnchor(anchorPreview, anchorPrefab);
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

    private void OnRightSecondaryPressed(InputAction.CallbackContext cb)
    {
        //Menu Toggle
        menuActiveState = !menuActiveState;
        menuObj.SetActive(menuActiveState);
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

    //Persist Current List
    public async void PersistCurrList()
    {
        if(anchorMap.Count > 0)
        {
            var anchorList = new List<KeyValuePair<ulong, Transform>>(anchorMap);
            //use the for loop to wait
            for(int i = 0; i < anchorList.Count; i++)
            {
                var item = anchorList[i];

                if(!currPersistList.Contains(item.Key))
                {
                    currPersistList.Add(item.Key);
                    var result = await PXR_MixedReality.PersistSpatialAnchorAsync(item.Key);

                    if(result == PxrResult.SUCCESS)
                    {
                        //ulong is not constant, only the uuid
                        Debug.Log($"PersistAnchor[{item.Key}] Success");
                    }
                }
            }
        }
    }

    //UnPersist Current List
    public async void UnPersistCurrList()
    {
        if (currPersistList.Count > 0)
        {
            //use the for loop to wait
            for (int i = 0; i < currPersistList.Count; i++)
            {
                var result = await PXR_MixedReality.UnPersistSpatialAnchorAsync(currPersistList[i]);
                if (result == PxrResult.SUCCESS)
                {
                    //removed success
                    Debug.Log($"UnPersistedAnchor {currPersistList[i]}");
                }
            }
            currPersistList.Clear();
            DeleteAllObjects();
        }
    }

    //Delete the last instantiated object
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
            anchorMap.Remove(lastObject.Item2);
        }
    }

    //Delete only in Memory and Scene, Not Anchors in Local Disk
    public void DeleteAllObjects()
    {
        while(objectStack.Count > 0)
        {
            DeleteLastObject();
        }
    }

    public async void LoadAllAnchors()
    {
        //if empty input, Query will return all pesistent in local disk and memory
        var result = await PXR_MixedReality.QuerySpatialAnchorAsync();
        if(result.result == PxrResult.SUCCESS)
        {
            foreach(var anchorHandle in result.anchorHandleList)
            {
                //check if its an anchor already in the scene
                if(!anchorMap.ContainsKey(anchorHandle))
                {
                    //locate anchor
                    PXR_MixedReality.LocateAnchor(anchorHandle, out var position, out var orientation);

                    GameObject anchorObj = Instantiate(roadPrefab, position, orientation);

                    //Keep track
                    anchorMap.Add(anchorHandle, anchorObj.transform);
                    objectStack.Push((anchorObj, anchorHandle));
                }

                if(!allPersistList.Contains(anchorHandle))
                    allPersistList.Add(anchorHandle);
            }
        }
    }

    public async void UnPersistAndDestroyAll()
    {
        //UnPersist first before you Destroy the anchor
        if(allPersistList.Count > 0)
        {
            //use for loop to wait
            for(int i = 0; i < allPersistList.Count; i++)
            {
                var anchorHandle = allPersistList[i];
                var result = await PXR_MixedReality.UnPersistSpatialAnchorAsync(anchorHandle);
                if(result == PxrResult.SUCCESS)
                {
                    Debug.Log($"Anchor {anchorHandle} - UnPersistSuccess");
                }
            }

            DeleteAllObjects();
        }
           
    }

}
