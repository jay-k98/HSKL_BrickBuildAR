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

    private Vector3 ResetNormale;
    private MeshRenderer BackgroundMeshRenderer;
    // Start is called before the first frame update

    void Start()
    {
        ButtonToggle = GameObject.Find("ButtonToggleAR").GetComponent<Button>();
        try
        {
            BackgroundMeshRenderer = GameObject.Find("Background").GetComponent<MeshRenderer>();
        }
        catch
        {
            Debug.Log("DEBUG: Can't Retrieve MeshRenderer of Background!");
        }
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
                BackgroundMeshRenderer.enabled = false;
            }
            catch
            {
                Debug.Log("DEBUG: Can't disable MeshRenderer!");
            }
            GameObject.Find("AR Session Origin").GetComponent<DragAR>().enabled = true;
            InstructionLoader.GetComponent<Lean.Touch.LeanDragTranslate>().enabled = false;
            ResetPos = InstructionLoader.transform.position;
            ResetNormale = InstructionLoader.transform.up;
            InstructionLoader.transform.localScale = ScaleAR;
            cameraNoAR.SetActive(false);
            cameraAR.SetActive(true);
            ButtonToggle.image.sprite = ArIsActive;
        }
        else
        {
            try
            {
                BackgroundMeshRenderer.enabled = true;
            }
            catch
            {
                Debug.Log("DEBUG: Can't enable MeshRenderer!");
            }
            GameObject.Find("AR Session Origin").GetComponent<DragAR>().enabled = false;
            InstructionLoader.GetComponent<Lean.Touch.LeanDragTranslate>().enabled = true;
            InstructionLoader.transform.localScale = ScaleBlueprint;
            InstructionLoader.transform.position = ResetPos;
            InstructionLoader.transform.up = ResetNormale;
            cameraAR.SetActive(false);
            cameraNoAR.SetActive(true);
            ButtonToggle.image.sprite = ArIsInactive;
        }
    }
}