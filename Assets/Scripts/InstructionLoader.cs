using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class InstructionLoader : MonoBehaviour
{
    [Tooltip("Filename of instructions to be used.")]
    public string Filename;
    [Tooltip("Smoothing for animating the rotation.")]
    public float Smoothing = 2.0f;
    [Tooltip("Epsilon for angle, that is required finish the rotation Coroutine.")]
    public float RotationEpsilon = 0.05f;
    [Tooltip("Color for the Next and Last Step Buttons when they are active.")]
    public Color ColorActive = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Color for the Next and Last Step Buttons when they are locked.")]
    public Color ColorInactive = new Color(.5f, .5f, .5f, .5f);
    [HideInInspector]
    public string[][] Instructions { get; set; }
    [HideInInspector]
    public int StepNumber { get; set; }

    private List<string[]> StepStack = new List<string[]>();
    private Dictionary<string, Dictionary<string, Tuple<Vector3, Quaternion, Material>>> ComponentStack = new Dictionary<string, Dictionary<string, Tuple<Vector3, Quaternion, Material>>>();
    private List<GameObject> LastSteps = new List<GameObject>();
    private List<GameObject> Inventory = new List<GameObject>();
    private bool ButtonLocked = false;
    private Button ButtonNext;
    private Button ButtonLast;


    // Start is called before the first frame update
    void Start()
    {
        Instructions = InstructionParser.ParseInstructions($"Instructions/{Filename}");
        ButtonNext = GameObject.Find("ButtonNext").GetComponent<Button>();
        ButtonLast = GameObject.Find("ButtonLast").GetComponent<Button>();
        ButtonNext.image.color = ColorActive;
        ButtonLast.image.color = ColorActive;
        StepNumber = 0;
        NextStep();
    }

    // Update is called once per frame
    void Update()
    {
       
    }


    public void NextStep()
    {
        GameObject InsLoader = transform.gameObject;
        string[] Step = Instructions[StepNumber];
        StepStack.Add(Step);
        string CompName = Step[1];
        InsLoader.transform.position = Vector3.zero;
        if (!(Step[0] == "R") && !(Step[0] == "C"))
            InsLoader.transform.rotation = Quaternion.Euler(Vector3.zero);
        if (Step[0] == "P")
        {
            Vector3 Pos = new Vector3(
                float.Parse(Step[2], CultureInfo.InvariantCulture),
                float.Parse(Step[3], CultureInfo.InvariantCulture),
                float.Parse(Step[4], CultureInfo.InvariantCulture)
                );
            Vector3 Rot = new Vector3(
                float.Parse(Step[5], CultureInfo.InvariantCulture),
                float.Parse(Step[6], CultureInfo.InvariantCulture),
                float.Parse(Step[7], CultureInfo.InvariantCulture)
                );
            string PartId = Step[1];
            string Color = Step[8];
            GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
            Material Mat = Resources.Load($"Materials/{Color}", typeof(Material)) as Material;
            Part.transform.parent = InsLoader.transform;
            Part.GetComponent<MeshRenderer>().material = Mat;
            Part.transform.localPosition = Pos;
            Part.transform.rotation = Quaternion.Euler(Rot);
            // new
            // end new
            LastSteps.Add(Part);
        }
        else if (Step[0] == "I")
        {
            int PartCounter = 0;
            foreach (GameObject Part in LastSteps)
            {
                string PartName = $"{Part.name}.{PartCounter}";
                Vector3 PartPos = Part.transform.position;
                Quaternion PartRot = Part.transform.rotation;
                Material Mat = Part.GetComponent<MeshRenderer>().material;
                Dictionary<string, Tuple<Vector3, Quaternion, Material>> PartData = new Dictionary<string, Tuple<Vector3, Quaternion, Material>>();
                if (ComponentStack.ContainsKey(CompName))
                {
                    ComponentStack[CompName].Add(PartName, new Tuple<Vector3, Quaternion, Material>(PartPos, PartRot, Mat));
                }
                else
                {
                    PartData.Add(PartName, new Tuple<Vector3, Quaternion, Material>(PartPos, PartRot, Mat));
                    ComponentStack.Add(CompName, PartData);
                }
                Destroy(Part);
                PartCounter++;
            }
            Inventory.Add(GameObject.Find(CompName));
            LastSteps.Clear();
        }
        else if (Step[0] == "C")
        {
            GameObject Comp = GameObject.Find($"{CompName}(Clone)");
            if (Comp == null)
                Comp = Instantiate(Resources.Load($"Models/Components/{CompName}", typeof(GameObject))) as GameObject;
            Vector3 PosComp = new Vector3(
                float.Parse(Step[2], CultureInfo.InvariantCulture),
                float.Parse(Step[3], CultureInfo.InvariantCulture),
                float.Parse(Step[4], CultureInfo.InvariantCulture)
                );
            Vector3 RotComp = new Vector3(
                float.Parse(Step[5], CultureInfo.InvariantCulture),
                float.Parse(Step[6], CultureInfo.InvariantCulture),
                float.Parse(Step[7], CultureInfo.InvariantCulture)
                );
            Vector3 RotView = new Vector3(
                float.Parse(Step[8], CultureInfo.InvariantCulture),
                float.Parse(Step[9], CultureInfo.InvariantCulture),
                float.Parse(Step[10], CultureInfo.InvariantCulture)
                );
            Comp.transform.parent = InsLoader.transform;
            Comp.transform.localPosition = PosComp;
            Comp.transform.localRotation = Quaternion.Euler(RotComp);
            InsLoader.transform.rotation = Quaternion.Euler(RotView);
            ToggleVisibility(Comp, true);
        }
        else if (Step[0] == "R")
        {
            GameObject Comp = InsLoader;
            Vector3 RotTo = new Vector3(
                float.Parse(Step[5], CultureInfo.InvariantCulture),
                float.Parse(Step[6], CultureInfo.InvariantCulture),
                float.Parse(Step[7], CultureInfo.InvariantCulture)
                );
            StartCoroutine(RotateTo(Comp.transform, Quaternion.Euler(RotTo)));
        }
        else if (Step[0] == "L")
        {
            ClearPanel();
            string ListStr = String.Join(' ', Step, 1, Step.Length-1);
            string[] ListSplit = ListStr.Split(":");
            foreach (string ListItem in ListSplit)
            {
                string[] ListItemSplit = ListItem.Split(" ");
                if (ListItemSplit[0] == "P")
                {
                    GameObject InvPartParent = Instantiate(Resources.Load($"UIElements/InvPartParent", typeof(GameObject))) as GameObject;
                    GameObject Panel = GameObject.Find("Panel");
                    InvPartParent.transform.parent = Panel.transform;
                    InvPartParent.layer = 5;
                    string PartId = ListItemSplit[1];
                    string Color = ListItemSplit[2];
                    string ItemCount = ListItemSplit[3];
                    GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
                    Material Mat = Resources.Load($"Materials/{Color}", typeof(Material)) as Material;
                    Part.GetComponent<MeshRenderer>().material = Mat;
                    Part.transform.parent = InvPartParent.transform;
                    Part.layer = 5;
                    Part.transform.localPosition = new Vector3(0, 0, 6.53f);
                    Part.transform.localRotation = Quaternion.Euler(-197.3f, 36.3f, 5.8f);
                    int ScaleFactor = 40;
                    Part.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);
                }
            }
        }
    }

    public void LastStep()
    {
        GameObject InsLoader = transform.gameObject;
        string[] Step = StepStack[StepStack.Count - 1];
        InsLoader.transform.position = Vector3.zero;
        if (!(Step[0] == "C") && !(Step[0] == "R"))
            InsLoader.transform.rotation = Quaternion.Euler(Vector3.zero);
        string CompName = Step[1];
        if (Step[0] == "P")
        {
            if (LastSteps.Count > 0)
            {
                GameObject last_step = LastSteps[LastSteps.Count - 1];
                LastSteps.Remove(last_step);
                Destroy(last_step);
            }
        }
        else if (Step[0] == "I")
        {
            LastSteps.Clear();
            foreach (string PartName in ComponentStack[CompName].Keys)
            {
                string PartId = PartName.Split(".")[0].Replace("(Clone)", "");
                GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
                Part.transform.position = ComponentStack[CompName][PartName].Item1;
                Part.transform.rotation = ComponentStack[CompName][PartName].Item2;
                Part.GetComponent<MeshRenderer>().material = ComponentStack[CompName][PartName].Item3;
                Part.transform.parent = InsLoader.transform;
                LastSteps.Add(Part);
            }
            ComponentStack.Remove(CompName);
            Inventory.Remove(GameObject.Find(CompName));
        }
        else if (Step[0] == "C")
        {
            CompName = $"{Step[1]}(Clone)";
            GameObject Comp = GameObject.Find(CompName);
            RotateView(Comp);
            if (Comp != null)
                Destroy(Comp);
        }
        else if (Step[0] == "R")
        {
            GameObject Comp = InsLoader;
            RotateView(Comp);
        }
        StepStack.Remove(Step);
    }

    private void RotateView(GameObject Comp)
    {
        Vector3 RotView = Vector3.zero;
        int TmpStepNumber = StepNumber - 1;
        if (TmpStepNumber > 0)
        {
            string[] TempStep = Instructions[TmpStepNumber];
            RotView = Vector3.zero;
            if (TempStep[0] == "R")
            {
                RotView = new Vector3(
                float.Parse(TempStep[5], CultureInfo.InvariantCulture),
                float.Parse(TempStep[6], CultureInfo.InvariantCulture),
                float.Parse(TempStep[7], CultureInfo.InvariantCulture)
                );
            }
            else if (TempStep[0] == "C")
            {
                RotView = new Vector3(
                float.Parse(TempStep[8], CultureInfo.InvariantCulture),
                float.Parse(TempStep[9], CultureInfo.InvariantCulture),
                float.Parse(TempStep[10], CultureInfo.InvariantCulture)
                );
            }
        }
        StartCoroutine(RotateTo(Comp.transform, Quaternion.Euler(RotView)));
    }

    IEnumerator RotateTo(Transform ObjTransform, Quaternion RotEnd)
    {
        ToggleStepButtons(true);
        // float angle = Quaternion.Angle(transform.rotation, target.rotation)
        while (ObjTransform != null && Quaternion.Angle(ObjTransform.rotation, RotEnd) > RotationEpsilon)
        {
            ObjTransform.rotation = Quaternion.Slerp(ObjTransform.rotation, RotEnd, Smoothing * Time.deltaTime);

            yield return null;
        }

        ToggleStepButtons(false);
        yield return new WaitForSeconds(0.1f);

    }
    private void ToggleVisibility(GameObject Obj, bool Visibility)
    {
        foreach (MeshRenderer Child in Obj.GetComponentsInChildren<MeshRenderer>())
            Child.enabled = Visibility;
    }
    private void ClearPanel()
    {
        GameObject Panel = GameObject.Find("Panel");
        for (int i = 0; i < Panel.transform.childCount; i++)
        {
            Destroy(Panel.transform.GetChild(i).gameObject);
        }
    }

    private void ToggleStepButtons(bool Locked)
    {
        ButtonLocked = Locked;
        if (ButtonLocked)
        {
            ButtonNext.image.color = ColorInactive;
            ButtonLast.image.color = ColorInactive;
        }
        else
        {
            ButtonNext.image.color = ColorActive;
            ButtonLast.image.color = ColorActive;
        }
    }

    public void GoToNextStep()
    {
        if (!ButtonLocked && StepNumber + 1 < Instructions.Length)
        {
            StepNumber += 1;
            NextStep();
        }
    }

    public void GoToLastStep()
    {
        if (!ButtonLocked && StepNumber - 1 >= 0)
        {
            StepNumber -= 1;
            LastStep();
        }
    }
}
