using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ToggleAR : MonoBehaviour
{
    [Tooltip("Sprite used for the Toggle Button when it's active.")]
    public Sprite ArIsActive;
    [Tooltip("Sprite used for the Toggle Button when it's inactive.")]
    public Sprite ArIsInactive;

    private bool IsActive = false;
    private Button ButtonToggle;
    public GameObject cameraNoAR;
    public GameObject cameraAR;
    // Start is called before the first frame update

    void Start()
    {
        ButtonToggle = GameObject.Find("ButtonToggleAR").GetComponent<Button>();
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
            cameraNoAR.SetActive(false);
            cameraAR.SetActive(true);
            ButtonToggle.image.sprite = ArIsActive;
        }
        else
        {
            cameraAR.SetActive(false);
            cameraNoAR.SetActive(true);
            ButtonToggle.image.sprite = ArIsInactive;
        }
    }
}
