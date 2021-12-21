using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Lean;

public class InputManager : MonoBehaviour
{

    public GameObject arObj;
    public Camera arCam;
    public ARRaycastManager _raycastManager;

    List<ARRaycastHit> _hits = new List<ARRaycastHit>();



    // Start is called before the first frame update
    void Awake()
    {
        Debug.Log("Input Manager gestartet");
    }

    // Update is called once per frame
    void Update()
    {
        var fingers = Lean.Touch.LeanTouch.Fingers;
        var _fingers = new Lean.Touch.LeanFinger();

        Debug.Log("There are currently " + fingers.Count + " fingers touching the screen.");



        if (fingers.Count > 0)
        {
            Ray ray = arCam.ScreenPointToRay(_fingers.ScreenPosition);
            if(_raycastManager.Raycast(ray, _hits))
            {
                Pose pose = _hits[0].pose;
                Instantiate(arObj, pose.position, pose.rotation);
            }

        }
    }
}
