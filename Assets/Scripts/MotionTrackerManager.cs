using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.XR.PXR;

public class MotionTrackerManager : MonoBehaviour
{
    [SerializeField]
    private GameObject motionTrackerPrefab;

    private List<string> connectedList = new List<string>();
    private List<string> disconnectedList = new List<string>();
    private Dictionary<string, GameObject> objTrackerMap = new Dictionary<string, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void Awake()
    {
        CheckForObjectTrackingMode();
    }


    private void OnEnable()
    {
        PXR_MotionTracking.MotionTrackerNumberOfConnections += CheckMotionTrackerConnections;
    }

    private void OnDisable()
    {
        PXR_MotionTracking.MotionTrackerNumberOfConnections -= CheckMotionTrackerConnections;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void CheckForObjectTrackingMode()
    {
        //MotionTracking == Object Tracking
        //Launch Motion Tracker App if needed automatically
        PXR_MotionTracking.CheckMotionTrackerModeAndNumber(MotionTrackerMode.MotionTracking, 0);
    }

    private void CheckMotionTrackerConnections(int state, int value)
    {
        //state 1 is connected, 0 disconnected
        //value is number of trackers connected
        Debug.Log($"Motion Trackers State: {state}\nConnected: {value}");

        //Update/Creation/Disconnect Logic
        UpdateObjectTracker();
    }

    private void UpdateObjectTracker()
    {
        MotionTrackerConnectState motionTrackerConnectState = new MotionTrackerConnectState();
        var result = PXR_MotionTracking.GetMotionTrackerConnectStateWithSN(ref motionTrackerConnectState);
        if(result == 0) // 0 is Success
        {
            if (motionTrackerConnectState.trackersSN != null)
            {
                //Clear list each time
                connectedList.Clear();
                foreach (var trackerSN in motionTrackerConnectState.trackersSN)
                {
                    if(!string.IsNullOrEmpty(trackerSN.value))
                    {
                        //check if we have a new tracker connected
                        if (!objTrackerMap.ContainsKey(trackerSN.value))
                            CreateObjectTracker(trackerSN);
                        //Add to our list of currently connected motion trackers
                        connectedList.Add(trackerSN.value);
                    }
                }

                //After getting a list of active trackers, find disconnected
                if(objTrackerMap.Count > motionTrackerConnectState.trackerSum)
                {
                    //our tracker map has more entries then currently active trackers
                    FindDisconnectedTrackers();
                }
            }
        }
    }

    private void CreateObjectTracker(TrackerSN trackerSN)
    {
        //Create new motion tracker and add info to our object tracker class
        var newObjTracker = Instantiate(motionTrackerPrefab);
        //Add to our Object Tracker Map
        objTrackerMap[trackerSN.value] = newObjTracker;

        //update info on tracker
        MotionObjTracker motionObjTracker = newObjTracker.GetComponent<MotionObjTracker>();
        motionObjTracker.trackerSN = trackerSN;
        motionObjTracker.serialNumber = trackerSN.value;
    }

    private void FindDisconnectedTrackers()
    {
        disconnectedList.Clear();
        foreach(var item in objTrackerMap)
        {
            //check in our connected list for same in our obj tracker map
            if (connectedList.Contains(item.Key))
                continue;
            //previous entry in obj tracker map is not part of currently connected
            disconnectedList.Add(item.Key);
        }

        //after list, now we can destroy and remove from map/dictionary
        foreach(var discTrackerSN in disconnectedList)
        {
            Destroy(objTrackerMap[discTrackerSN]);
            objTrackerMap.Remove(discTrackerSN);
        }
    }
}
