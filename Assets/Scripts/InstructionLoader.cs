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
    private List<string[]> InventoryStack = new List<string[]>();
    private Dictionary<string, Dictionary<string, Tuple<Vector3, Quaternion, Material>>> ComponentStack = new Dictionary<string, Dictionary<string, Tuple<Vector3, Quaternion, Material>>>();
    private List<GameObject> LastSteps = new List<GameObject>();
    private List<GameObject> Inventory = new List<GameObject>();
    private List<Vector3> RotationStack = new List<Vector3>();
    private bool ButtonLocked = false;
    private Button ButtonNext;
    private Button ButtonLast;


    // Start is called before the first frame update
    void Start()
    {
        Instructions = InstructionParser.ParseInstructions($"Instructions/{Filename}");
        ButtonNext = GameObject.Find("Canvas/ButtonNext").GetComponent<Button>();
        ButtonLast = GameObject.Find("Canvas/ButtonLast").GetComponent<Button>();
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
        //Debug.Log($"Step: {Step[0]} {Step[1]} StepStackCount: {StepStack.Count} StepNumber: {StepNumber}");
        string CompName = Step[1];
        InsLoader.transform.position = Vector3.zero;
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
            Inventory.Add(GameObject.Find($"InstructionLoader/{CompName}"));
            LastSteps.Clear();
        }
        else if (Step[0] == "C")
        {
            GameObject Comp = GameObject.Find($"InstructionLoader/{CompName}(Clone)");
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
            Comp.transform.parent = InsLoader.transform;
            Comp.transform.localPosition = PosComp;
            Comp.transform.localRotation = Quaternion.Euler(RotComp);
            ToggleVisibility(Comp, true);
        }
        else if (Step[0].StartsWith("R"))
        {
            Vector3 RotateTo = new Vector3(
                float.Parse(Step[1], CultureInfo.InvariantCulture),
                float.Parse(Step[2], CultureInfo.InvariantCulture),
                float.Parse(Step[3], CultureInfo.InvariantCulture)
                );
            if (Step[0] == "RS")
                InsLoader.transform.rotation = Quaternion.Euler(RotateTo);
            else
                RotateView(InsLoader, RotateTo);
            RotationStack.Add(RotateTo);
        }
        else if (Step[0] == "L")
        {
            ClearPanel();
            FillInventory(Step);
            InventoryStack.Add(Step);
        }
        if (Step[0] == "L" || Step[0] == "I" || Step[0] == "RS")
        {
            GoToNextStep();
        }
    }

    public void LastStep()
    {
        GameObject InsLoader = transform.gameObject;
        string[] Step = StepStack[StepStack.Count - 1];
        StepStack.RemoveAt(StepStack.Count - 1);
        //Debug.Log($"Step: {Step[0]} {Step[1]} StepStackCount: {StepStack.Count} StepNumber: {StepNumber}");
        InsLoader.transform.position = Vector3.zero;
        string CompName = Step[1];
        if (Instructions.Length > StepNumber + 1 && Instructions[StepNumber + 1][0] == "L")
        {
            ClearPanel();
            InventoryStack.RemoveAt(InventoryStack.Count - 1);
            FillInventory(InventoryStack[InventoryStack.Count - 1]);
        }
        if (Step[0] == "P")
        {
            if (LastSteps.Count > 0)
            {
                GameObject LastStep = LastSteps[LastSteps.Count - 1];
                LastSteps.RemoveAt(LastSteps.Count - 1);
                Destroy(LastStep);
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
            Inventory.Remove(GameObject.Find($"InstructionLoader/{CompName}"));
        }
        else if (Step[0] == "C")
        {
            CompName = $"{Step[1]}(Clone)";
            GameObject Comp = GameObject.Find($"InstructionLoader/{CompName}");
            if (Comp != null)
                Destroy(Comp);
        }
        else if (Step[0].StartsWith("R"))
        {
            if (Step[0].EndsWith("S"))
            {
                InsLoader.transform.rotation = Quaternion.Euler(Vector3.zero);
            }
            else
            {
                Vector3 RotateTo = Vector3.zero;
                RotationStack.RemoveAt(RotationStack.Count - 1);
                if (RotationStack.Count > 0)
                    RotateTo = RotationStack[RotationStack.Count - 1];
                RotateView(InsLoader, RotateTo);
            }
            
        }
        if (Step[0] == "L" || Step[0] == "RS")
        {
            GoToLastStep();
        }
    }
    private void FillInventory(string[] Step)
    {
        string ListStr = string.Join(' ', Step, 1, Step.Length - 1);
        string[] ListSplit = ListStr.Split(":");
        GameObject Panel = GameObject.Find("Canvas/Panel");
        foreach (string ListItem in ListSplit)
        {
            string[] ListItemSplit = ListItem.Split(" ");
            GameObject InvPartParent = Instantiate(Resources.Load("UIElements/InvPartParent", typeof(GameObject))) as GameObject;
            InvPartParent.transform.parent = Panel.transform;
            InvPartParent.layer = 5;
            InvPartParent.transform.localPosition = Vector3.zero;
            if (ListItemSplit[0] == "P")
            {
                //GameObject InvPartParent = Instantiate(Resources.Load("UIElements/InvPartParent", typeof(GameObject))) as GameObject;
                //InvPartParent.transform.parent = Panel.transform;
                //InvPartParent.layer = 5;
                //InvPartParent.transform.localPosition = Vector3.zero;

                string PartId = ListItemSplit[1];
                string Color = ListItemSplit[2];
                string ItemCount = ListItemSplit[3];
                GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
                Material Mat = Resources.Load($"Materials/{Color}", typeof(Material)) as Material;
                Part.GetComponent<MeshRenderer>().material = Mat;
                Part.layer = 5;
                Part.transform.parent = InvPartParent.transform;
                Part.transform.localPosition = new Vector3(0, 0, -1.25f);
                Part.transform.localRotation = Quaternion.Euler(-197.3f, 36.3f, 5.8f);
                int ScaleFactor = 40;
                Part.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);
            }
            else if (ListItemSplit[0] == "C")
            {
                //string CompName = ListItemSplit[1];
                //GameObject InvComp = Instantiate(Resources.Load($"UIElements/Inv{CompName}", typeof(GameObject))) as GameObject;
                //InvComp.transform.parent = Panel.transform;
                //InvComp.transform.localPosition = Vector3.zero;
                //InvComp.transform.GetChild(0).transform.localPosition = new Vector3(0, 0, -1.25f);
                //int ScaleFactor = 40;
                //InvComp.transform.GetChild(0).transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);

                string CompName = ListItemSplit[1];
                GameObject Comp = Instantiate(Resources.Load($"Models/Components/{CompName}", typeof(GameObject))) as GameObject;
                Comp.layer = 5;
                for (int i = 0; i < Comp.transform.childCount; i++)
                {
                    Comp.transform.GetChild(i).gameObject.layer = 5;
                }
                Comp.transform.parent = InvPartParent.transform;
                Comp.transform.localPosition = new Vector3(0, 0, -1.25f);
                Comp.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                int ScaleFactor = 40;
                Comp.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);
            }
        }
    }
    private void RotateView(GameObject Comp, Vector3 RotateTo)
    {
        StartCoroutine(RotateViewAnimation(Comp.transform, Quaternion.Euler(RotateTo)));
    }

    IEnumerator RotateViewAnimation(Transform CompTransform, Quaternion RotateTo)
    {
        ToggleStepButtons(true);
        while (CompTransform != null && Quaternion.Angle(CompTransform.rotation, RotateTo) > RotationEpsilon)
        {
            CompTransform.rotation = Quaternion.Slerp(CompTransform.rotation, RotateTo, Smoothing * Time.deltaTime);

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
        GameObject Panel = GameObject.Find("Canvas/Panel");
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
            StepNumber++;
            NextStep();
        }
    }

    public void GoToLastStep()
    {
        if (!ButtonLocked && StepNumber - 1 > 0)
        {
            StepNumber--;
            LastStep();
        }
    }
}