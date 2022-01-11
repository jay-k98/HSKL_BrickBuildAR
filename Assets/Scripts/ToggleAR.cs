using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ToggleAR : MonoBehaviour
{
    [Tooltip("Sprite used for the Toggle Button when it's active.")]
    public Sprite ArIsActive;
    [Tooltip("Sprite used for the Toggle Button when it's inactive.")]
    public Sprite ArIsInactive;

    [HideInInspector]
    public bool IsActive = false;
    private Button ButtonToggle;
    public GameObject cameraNoAR;
    public GameObject cameraAR;
    public GameObject InstructionLoader;
    public Vector3 ScaleAR;
    public Vector3 ScaleBlueprint;
    private Vector3 ResetPos;

    private GameObject ArSession;
    private GameObject ArSessionOrigin;

    private Vector3 BackgroundPos;
    private Vector3 BackgroundRot;
    private Vector3 BackgroundScale;

    private Vector3 ResetNormale;
    // Start is called before the first frame update

    void Start()
    {
        ButtonToggle = GameObject.Find("ButtonToggleAR").GetComponent<Button>();
        try
        {
            GameObject Background = GameObject.Find("Background");
            BackgroundPos = Background.transform.position;
            BackgroundRot = Background.transform.rotation.eulerAngles;
            BackgroundScale = Background.transform.localScale;
        }
        catch
        {
            Debug.Log("DEBUG: Can't Retrieve GameObject \"Background\"!");
        }

        ArSessionOrigin = GameObject.Find("AR Session Origin");
        ArSession = GameObject.Find("AR Session");
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ToggleActivityAR()
    {
        IsActive = !IsActive;
        if (IsActive)
        {
            try
            {
                // GameObject Background = GameObject.Find("Background");
                foreach (var bg in GameObject.FindGameObjectsWithTag("BackgroundTag"))
                    Destroy(bg);
            }
            catch
            {
                Debug.Log("DEBUG: Can't destroy Background!");
            }
            InstructionLoader.GetComponent<Lean.Touch.LeanDragTranslate>().enabled = false;
            ResetPos = InstructionLoader.transform.position;
            ResetNormale = InstructionLoader.transform.up;
            InstructionLoader.transform.localScale = ScaleAR;

            Vector3 Pos = InstructionLoader.transform.position;
            Pos.y = 0.0f;
            InstructionLoader.transform.position = Pos;
            try
            {
                cameraNoAR.SetActive(false);
                cameraAR.SetActive(true);
            }
            catch
            {
                Debug.Log("DEBUG: Cant switch Cameras");
            }
            try
            {
                ArSession.SetActive(true);
                ArSession.GetComponent<ARSession>().enabled = true;
                ArSession.GetComponent<ARInputManager>().enabled = true;
            }
            catch
            {
                Debug.Log("DEBUG: Cant Activate AR Session OR added Scripts");
            }
            try
            {
                ArSessionOrigin.SetActive(true);
                ArSessionOrigin.GetComponent<ARSessionOrigin>().enabled = true;
                ArSessionOrigin.GetComponent<ARPlaneManager>().enabled = true;
                ArSessionOrigin.GetComponent<ARRaycastManager>().enabled = true;
                ArSessionOrigin.GetComponent<DragAR>().enabled = true;
            }
            catch
            {
                Debug.Log("DEBUG: Cant Activate AR Session Origin OR added Scripts");
            }


            ButtonToggle.image.sprite = ArIsActive;
        }
        else
        {
            try
            {

                GameObject Background = Instantiate(Resources.Load("Models/Components/Background", typeof(GameObject))) as GameObject;
                Background.name = "Background";
                Background.tag = "BackgroundTag";
                Background.transform.position = BackgroundPos;
                Background.transform.rotation = Quaternion.Euler(BackgroundRot);
                Background.transform.localScale = BackgroundScale;
                Material Mat = Resources.Load("Materials/BackgroundMat", typeof(Material)) as Material;
                Background.GetComponent<MeshRenderer>().material = Mat;
            }
            catch
            {
                Debug.Log("DEBUG: Can't instantiate Background!");
            }
            DragAR DragAr = ArSessionOrigin.GetComponent<DragAR>();
            DragAr.ActiveTrackableId = null;
            DragAr.enabled = false;
            InstructionLoader.GetComponent<Lean.Touch.LeanDragTranslate>().enabled = true;
            InstructionLoader.transform.localScale = ScaleBlueprint;
            InstructionLoader.transform.position = ResetPos;
            InstructionLoader.transform.up = ResetNormale;
            cameraAR.SetActive(false);
            ArSession.GetComponent<ARSession>().Reset();
            ArSession.SetActive(false);
            ArSessionOrigin.SetActive(false);
            cameraNoAR.SetActive(true);
            ButtonToggle.image.sprite = ArIsInactive;
        }
    }
}