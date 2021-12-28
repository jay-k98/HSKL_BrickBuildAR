using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]

public class DragAR : MonoBehaviour
{
    //Objekt, das auf AR-Plane platziert werden soll
    public GameObject spawnedObject;

    //Kopie des Objektes beim ersten platzieren auf AR Plane
    //private GameObject arObject;

    private ARRaycastManager _arRayCastManager;

    //Touchposition auf Screen
    private Vector2 touchPosition;

    //Größenänderung im Raum
    private Vector3 scaleChange;


    private Vector3 versatz;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();


    private void Awake()
    {
        _arRayCastManager = GetComponent<ARRaycastManager>();
        versatz = new Vector3(0.0f, 0.0f, 0.0f);
    }

    bool TryGetTouchPosition(out Vector2 touchPosition)
    {
        if (Input.touchCount > 0)
        {
            touchPosition = Input.GetTouch(0).position;
            return true;
        }

        touchPosition = default;
        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!TryGetTouchPosition(out Vector2 touchPosition))
            return;
        if (_arRayCastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            var hitPose = hits[0].pose;

            //if (arObject == null)
            //{
            //    arObject = Instantiate(spawnedObject, hitPose.position, hitPose.rotation);
            //}

            spawnedObject.transform.position = hitPose.position + versatz;
            spawnedObject.transform.localScale = new Vector3(4, 4, 4);
        }
    }
}
