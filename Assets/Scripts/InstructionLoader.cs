using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

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

    //private List<string[]> StepStack = new List<string[]>();
    //private List<string[]> InventoryStack = new List<string[]>();
    private Dictionary<string, string[]> InventoryDict = new Dictionary<string, string[]>();
    private Dictionary<string, Dictionary<string, Tuple<Vector3, Quaternion, Material>>> PartStack = new Dictionary<string, Dictionary<string, Tuple<Vector3, Quaternion, Material>>>();
    //private List<GameObject> Inventory = new List<GameObject>();
    //private List<Vector3> RotationStack = new List<Vector3>();
    private bool ButtonLocked = false;
    private Button ButtonNext;
    private Button ButtonLast;

    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> Instr = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

    //private List<GameObject> LastParts = new List<GameObject>();

    //private string ActiveInventoryKey = "";
    //private int PartCounter = 0;

    private GameObject DebugText;
    private GameObject InsLoader;

    private int StepNumber = 0;
    private int StepCount = 0;

    private bool GoingBackwards = false;


    // Start is called before the first frame update
    void Start()
    {
        //Instructions = InstructionParser.ParseInstructions($"Instructions/TXT/{Filename}");
        //InventoryDict = InstructionParser.ParseInventory($"Instructions/TXT/{Filename}");

        ButtonNext = GameObject.Find("Canvas/ButtonNext").GetComponent<Button>();
        ButtonLast = GameObject.Find("Canvas/ButtonLast").GetComponent<Button>();
        ButtonNext.image.color = ColorActive;
        ButtonLast.image.color = ColorActive;


        //StepNumber = 0;
        //NextStep();

        DebugText = GameObject.Find("Canvas/DebugText");

        Instr = InstructionParser.ParseInstructionsYaml($"Instructions/YAML/{Filename}");
        InsLoader = GameObject.Find("InstructionLoader");
        StepCount = Instr["steps"].Keys.Count;
        StepNumber = 0;
        RenderStep();
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    private string[] PartsExcept (string[] Base, string[] Diff)
    {
        List<string> Except = new List<string>();
        foreach (string Value in Base)
        {
            if (!Diff.Contains(Value))
                Except.Add(Value);
        }
        return Except.ToArray();
    }

    private void RenderStep()
    {
        // TO DO INVENTORY:
        // implemenent inventory like before. edit yaml to have correct stepnumber and inventory_id relation

        DebugText.GetComponent<TMPro.TextMeshProUGUI>().text = $"StepNumber: {StepNumber}";

        Dictionary<string, string>  PartList = new Dictionary<string, string>();
        List<string> PartsAdded = new List<string>();

        Dictionary<string, Dictionary<string, string>> Steps = Instr["steps"];
        string StepId = $"&id{StepNumber:D3}";
        Dictionary<string, string> Step = Steps[StepId];

        if (Step.ContainsKey("rot_view_x"))
        {
            Vector3 RotView = new Vector3(
                float.Parse(Step["rot_view_x"], CultureInfo.InvariantCulture),
                float.Parse(Step["rot_view_y"], CultureInfo.InvariantCulture),
                float.Parse(Step["rot_view_z"], CultureInfo.InvariantCulture)
                );
            if (!(Vector3.SqrMagnitude(InsLoader.transform.rotation.eulerAngles - RotView) < 0.001))
            {
                if ((Step.ContainsKey("smoothing") && (Step["smoothing"] == "0")) || (GoingBackwards && Step.ContainsKey("part")))
                    InsLoader.transform.rotation = Quaternion.Euler(RotView);
                else
                    RotateView(InsLoader.transform.rotation.eulerAngles, RotView, 1.0f);
            }
        }

        if (Step.ContainsKey("part"))
            PartList.Add(Step["part"], StepId);
        if (Step.ContainsKey("comp"))
            PartList.Add(Step["comp"], StepId);

        Dictionary<string, string> TempStep = Step;
        while (TempStep.ContainsKey("step_ref"))
        {
            string TempStepId = TempStep["step_ref"].Replace("*", "&");
            TempStep = Steps[TempStepId];
            if (TempStep.ContainsKey("part"))
                PartList.Add(TempStep["part"], TempStepId);
            if (TempStep.ContainsKey("comp"))
                PartList.Add(TempStep["comp"], TempStepId);
        }

        for (int i = 0; i < InsLoader.transform.childCount; i++)
            PartsAdded.Add(InsLoader.transform.GetChild(i).name);

        if (PartList.Count > 0)
        {
            string[] PartsAddedArr = PartsAdded.ToArray();
            string[] PartListArr = PartList.Keys.ToArray();

            string[] PartsToDel = PartsExcept(PartsAddedArr, PartListArr);
            foreach (var PartName in PartsToDel)
            {
                GameObject Part = GameObject.Find($"InstructionLoader/{PartName}");
                Destroy(Part);
            }

            string[] PartsToAdd = PartsExcept(PartListArr, PartsAddedArr);
            foreach (var PartName in PartsToAdd)
            {
                string PartId = PartName.ToString().Split(".")[0];
                StepId = PartList[PartName];
                Step = Steps[StepId];

                GameObject Part = null;
                if (Step.ContainsKey("part"))
                    Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
                if (Step.ContainsKey("comp"))
                    Part = Instantiate(Resources.Load($"Models/Components/{PartId}", typeof(GameObject))) as GameObject;

                Part.name = PartName;
                Part.transform.SetParent(InsLoader.transform);

                Vector3 Pos = new Vector3(
                    float.Parse(Step["pos_x"], CultureInfo.InvariantCulture),
                    float.Parse(Step["pos_y"], CultureInfo.InvariantCulture),
                    float.Parse(Step["pos_z"], CultureInfo.InvariantCulture)
                    );
                Part.transform.localPosition = Pos;

                Vector3 Rot = Vector3.zero;
                if (Step.ContainsKey("part"))
                {
                    Rot = new Vector3(
                    float.Parse(Step["rot_x"], CultureInfo.InvariantCulture),
                    float.Parse(Step["rot_y"], CultureInfo.InvariantCulture),
                    float.Parse(Step["rot_z"], CultureInfo.InvariantCulture)
                    );
                    Part.transform.localRotation = Quaternion.Euler(Rot);

                    string Color = Step["color"];
                    Material Mat = Resources.Load($"Materials/{Color}", typeof(Material)) as Material;
                    Part.GetComponent<MeshRenderer>().material = Mat;
                }
                if (Step.ContainsKey("comp"))
                {
                    Part.transform.localRotation = Quaternion.Euler(Rot);
                }
            }
        }
    }

    //public void NextStep()
    //{
    //    GameObject InsLoader = transform.gameObject;
    //    string[] Step = Instructions[StepNumber];
        
    //    DebugText.GetComponent<TMPro.TextMeshProUGUI>().text = $"StepNumber: {StepNumber} Step[0]: {Step[0]}";

    //    string InventoryKey = Step[1];
    //    if (InventoryKey != ActiveInventoryKey)
    //    {
    //        ClearPanel();
    //        FillInventory(InventoryKey);
    //        PartCounter = 0;
    //        if (PartStack.ContainsKey(ActiveInventoryKey))
    //        {
    //            foreach (string PartName in PartStack[ActiveInventoryKey].Keys)
    //            {
    //                GameObject Part = GameObject.Find($"InstructionLoader/{PartName}");
    //                Destroy(Part);
    //                //PartStack[ActiveInventoryKey].Remove(PartName);
    //            }
    //        }
    //        //PartStack.Remove(ActiveInventoryKey);
    //        ActiveInventoryKey = InventoryKey;
    //    }

    //    StepStack.Add(Step);
    //    //Debug.Log($"Step: {Step[0]} {Step[1]} StepStackCount: {StepStack.Count} StepNumber: {StepNumber}");
    //    InsLoader.transform.position = Vector3.zero;
    //    if (Step[0] == "P")
    //    {
    //        string PartId = Step[2];
    //        Vector3 Pos = new Vector3(
    //            float.Parse(Step[3], CultureInfo.InvariantCulture),
    //            float.Parse(Step[4], CultureInfo.InvariantCulture),
    //            float.Parse(Step[5], CultureInfo.InvariantCulture)
    //            );
    //        Vector3 Rot = new Vector3(
    //            float.Parse(Step[6], CultureInfo.InvariantCulture),
    //            float.Parse(Step[7], CultureInfo.InvariantCulture),
    //            float.Parse(Step[8], CultureInfo.InvariantCulture)
    //            );
    //        string Color = Step[9];
    //        GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
    //        Material Mat = Resources.Load($"Materials/{Color}", typeof(Material)) as Material;
    //        Part.transform.parent = InsLoader.transform;
    //        Part.GetComponent<MeshRenderer>().material = Mat;
    //        Part.transform.localPosition = Pos;
    //        Part.transform.rotation = Quaternion.Euler(Rot);

    //        Dictionary<string, Tuple<Vector3, Quaternion, Material>> PartData = new Dictionary<string, Tuple<Vector3, Quaternion, Material>>();
    //        string PartName = $"{Part.name}.{PartCounter}";
    //        Part.name = PartName;
    //        PartCounter++;
    //        if (PartStack.ContainsKey(InventoryKey))
    //        {
    //            PartStack[InventoryKey].Add(PartName, new Tuple<Vector3, Quaternion, Material>(Pos, Quaternion.Euler(Rot), Mat));
    //        }
    //        else
    //        {
    //            PartData.Add(PartName, new Tuple<Vector3, Quaternion, Material>(Pos, Quaternion.Euler(Rot), Mat));
    //            PartStack.Add(InventoryKey, PartData);
    //        }
    //        LastParts.Add(Part);
    //    }
        //else if (Step[0] == "K")
        //{
        //    string CompName = Step[2];
        //    int PartCounter = 0;
        //    foreach (GameObject Part in LastSteps)
        //    {
        //        string PartName = $"{Part.name}.{PartCounter}";
        //        Vector3 PartPos = Part.transform.position;
        //        Quaternion PartRot = Part.transform.rotation;
        //        Material Mat = Part.GetComponent<MeshRenderer>().material;
        //        Dictionary<string, Tuple<Vector3, Quaternion, Material>> PartData = new Dictionary<string, Tuple<Vector3, Quaternion, Material>>();
        //        if (ComponentStack.ContainsKey(CompName))
        //        {
        //            ComponentStack[CompName].Add(PartName, new Tuple<Vector3, Quaternion, Material>(PartPos, PartRot, Mat));
        //        }
        //        else
        //        {
        //            PartData.Add(PartName, new Tuple<Vector3, Quaternion, Material>(PartPos, PartRot, Mat));
        //            ComponentStack.Add(CompName, PartData);
        //        }
        //        Destroy(Part);
        //        PartCounter++;
        //    }
        //    Inventory.Add(GameObject.Find($"InstructionLoader/{CompName}"));
        //    LastSteps.Clear();
        //}
    //    else if (Step[0] == "C")
    //    {
    //        string CompName = Step[2];
    //        GameObject Comp = GameObject.Find($"InstructionLoader/{CompName}(Clone)");
    //        if (Comp == null)
    //            Comp = Instantiate(Resources.Load($"Models/Components/{CompName}", typeof(GameObject))) as GameObject;
    //        Vector3 PosComp = new Vector3(
    //            float.Parse(Step[3], CultureInfo.InvariantCulture),
    //            float.Parse(Step[4], CultureInfo.InvariantCulture),
    //            float.Parse(Step[5], CultureInfo.InvariantCulture)
    //            );
    //        Vector3 RotComp = new Vector3(
    //            float.Parse(Step[6], CultureInfo.InvariantCulture),
    //            float.Parse(Step[7], CultureInfo.InvariantCulture),
    //            float.Parse(Step[8], CultureInfo.InvariantCulture)
    //            );
    //        Comp.transform.parent = InsLoader.transform;
    //        Comp.transform.localPosition = PosComp;
    //        Comp.transform.localRotation = Quaternion.Euler(RotComp);
    //        ToggleVisibility(Comp, true);
    //    }
    //    else if (Step[0].StartsWith("R"))
    //    {
    //        Vector3 RotateTo = new Vector3(
    //            float.Parse(Step[2], CultureInfo.InvariantCulture),
    //            float.Parse(Step[3], CultureInfo.InvariantCulture),
    //            float.Parse(Step[4], CultureInfo.InvariantCulture)
    //            );
    //        if (Step[0] == "RS")
    //            InsLoader.transform.rotation = Quaternion.Euler(RotateTo);
    //        else
    //            RotateView(InsLoader, RotateTo);
    //        RotationStack.Add(RotateTo);
    //    }
    //    if (Step[0] == "RS")
    //    {
    //        GoToNextStep();
    //    }
    //}

    //public void LastStep()
    //{
    //    GameObject InsLoader = transform.gameObject;
    //    string[] Step = StepStack[StepStack.Count - 1];
    //    StepStack.RemoveAt(StepStack.Count - 1);

    //    string InventoryKey = Step[1];
    //    if (InventoryKey != ActiveInventoryKey)
    //    {
    //        ClearPanel();
    //        FillInventory(InventoryKey);
    //        if (PartStack.ContainsKey(ActiveInventoryKey))
    //        {
    //            foreach (string PartName in PartStack[ActiveInventoryKey].Keys)
    //            {
    //                GameObject Part = GameObject.Find($"InstructionLoader/{PartName}");
    //                Destroy(Part);
    //                PartStack[ActiveInventoryKey].Remove(PartName);
    //            }
    //            PartStack.Remove(ActiveInventoryKey);
    //        }
    //        if (PartStack.ContainsKey(InventoryKey))
    //        {
    //            foreach (string PartName in PartStack[InventoryKey].Keys)
    //            {
    //                GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartName.Split(".")[0]}", typeof(GameObject))) as GameObject;
    //                Part.transform.localPosition = PartStack[InventoryKey][PartName].Item1;
    //                Part.transform.rotation = PartStack[InventoryKey][PartName].Item2;
    //                Part.GetComponent<MeshRenderer>().material = PartStack[InventoryKey][PartName].Item3;
    //            }
    //        }
    //        ActiveInventoryKey = InventoryKey;
    //    }

    //    DebugText.GetComponent<TMPro.TextMeshProUGUI>().text = $"StepNumber: {StepNumber} Step[0]: {Step[0]}";
    //    //Debug.Log($"Step: {Step[0]} {Step[1]} StepStackCount: {StepStack.Count} StepNumber: {StepNumber}");

    //    InsLoader.transform.position = Vector3.zero;
    //    //if (Instructions.Length > StepNumber + 1 && Instructions[StepNumber + 1][0] == "I")
    //    //{
    //    //    ClearPanel();
    //    //    InventoryStack.RemoveAt(InventoryStack.Count - 1);
    //    //    FillInventory(InventoryStack[InventoryStack.Count - 1]);
    //    //}
    //    if (Step[0] == "P")
    //    {
    //        if (PartStack.Count > 0)
    //        {
    //            GameObject LastPart = LastParts[LastParts.Count - 1];
    //            LastParts.RemoveAt(PartStack.Count - 1);
    //            PartStack[InventoryKey].Remove(LastPart.name);
    //            Destroy(LastPart);
    //        }
    //    }
    //    //else if (Step[0] == "K")
    //    //{
    //    //    //LastSteps.Clear();
    //    //    string CompName = Step[2];
    //    //    foreach (string PartName in ComponentStack[CompName].Keys)
    //    //    {
    //    //        string PartId = PartName.Split(".")[0].Replace("(Clone)", "");
    //    //        GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
    //    //        Part.transform.position = ComponentStack[CompName][PartName].Item1;
    //    //        Part.transform.rotation = ComponentStack[CompName][PartName].Item2;
    //    //        Part.GetComponent<MeshRenderer>().material = ComponentStack[CompName][PartName].Item3;
    //    //        Part.transform.parent = InsLoader.transform;
    //    //        LastSteps.Add(Part);
    //    //    }
    //    //    ComponentStack.Remove(CompName);
    //    //    Inventory.Remove(GameObject.Find($"InstructionLoader/{CompName}"));
    //    //}
    //    else if (Step[0] == "C")
    //    {
    //        string CompName = Step[2];
    //        CompName = $"{Step[2]}(Clone)";
    //        GameObject Comp = GameObject.Find($"InstructionLoader/{CompName}");
    //        if (Comp != null)
    //            Destroy(Comp);
    //    }
    //    else if (Step[0].StartsWith("R"))
    //    {
    //        if (Step[0].EndsWith("S"))
    //        {
    //            InsLoader.transform.rotation = Quaternion.Euler(Vector3.zero);
    //        }
    //        else
    //        {
    //            Vector3 RotateTo = Vector3.zero;
    //            RotationStack.RemoveAt(RotationStack.Count - 1);
    //            if (RotationStack.Count > 0)
    //                RotateTo = RotationStack[RotationStack.Count - 1];
    //            RotateView(InsLoader, RotateTo);
    //        }
            
    //    }
    //    if (Step[0] == "RS")
    //    {
    //        GoToLastStep();
    //    }
    //}
    private void FillInventory(string InventoryKey)
    {
        string[] InvSplit = InventoryDict[InventoryKey];
        GameObject Panel = GameObject.Find("Canvas/Panel");
        foreach (string ListItem in InvSplit)
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
    private void RotateView(Vector3 RotateFrom, Vector3 RotateTo, float SmoothingFactor)
    {
        StartCoroutine(RotateViewAnimation(Quaternion.Euler(RotateFrom), Quaternion.Euler(RotateTo), SmoothingFactor));
    }

    IEnumerator RotateViewAnimation(Quaternion RotateFrom, Quaternion RotateTo, float SmoothingFactor)
    {
        ToggleStepButtons(true);
        float Smooth = Smoothing * SmoothingFactor;
        InsLoader.transform.rotation = RotateFrom;
        while (InsLoader.transform != null && Quaternion.Angle(InsLoader.transform.rotation, RotateTo) > RotationEpsilon)
        {
            InsLoader.transform.rotation = Quaternion.Slerp(InsLoader.transform.rotation, RotateTo, Smooth * Time.deltaTime);

            yield return null;
        }

        ToggleStepButtons(false);
        yield return new WaitForSeconds(0.1f);

    }
    //private void ToggleVisibility(GameObject Obj, bool Visibility)
    //{
    //    foreach (MeshRenderer Child in Obj.GetComponentsInChildren<MeshRenderer>())
    //        Child.enabled = Visibility;
    //}
    //private void ClearPanel()
    //{
    //    GameObject Panel = GameObject.Find("Canvas/Panel");
    //    for (int i = 0; i < Panel.transform.childCount; i++)
    //    {
    //        Destroy(Panel.transform.GetChild(i).gameObject);
    //    }
    //}

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

    //public void GoToNextStep()
    //{
    //    if (!ButtonLocked && StepNumber + 1 < Instructions.Length)
    //    {
    //        StepNumber++;
    //        NextStep();
    //    }
    //}

    //public void GoToLastStep()
    //{
    //    if (!ButtonLocked && StepNumber - 1 >= 0)
    //    {
    //        StepNumber--;
    //        LastStep();
    //    }
    //}

    public void GoToNextStep()
    {
        if (!ButtonLocked && StepNumber + 1 < StepCount)
        {
            GoingBackwards = false;
            StepNumber++;
            RenderStep();
        }
    }

    public void GoToLastStep()
    {
        if (!ButtonLocked && StepNumber - 1 >= 0)
        {
            GoingBackwards = true;
            StepNumber--;
            RenderStep();
        }
    }
}