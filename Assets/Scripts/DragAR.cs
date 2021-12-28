using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ARRaycastManager))]

public class DragAR : MonoBehaviour
{
    //Objekt, das auf AR-Plane platziert werden soll
    public GameObject spawnedObject;

    //Kopie des Objektes beim ersten platzieren auf AR Plane
    //private GameObject arObject;

    private ARRaycastManager _arRayCastManager;

    static List<ARRaycastHit> hits = new List<ARRaycastHit>();


    private void Awake()
    {
        _arRayCastManager = GetComponent<ARRaycastManager>();
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
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null || !eventSystem.IsPointerOverGameObject())
        {
            if (!TryGetTouchPosition(out Vector2 touchPosition))
                return;
            if (_arRayCastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                var hitPose = hits[0].pose;

                spawnedObject.transform.position = hitPose.position;

            }
        }
    }
}
