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

    private Dictionary<string, string> InventoryStepRelation = new Dictionary<string, string>();
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> Instr = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
    private Button ButtonNext;
    private Button ButtonLast;
    private Slider Slider;

    private GameObject TextStepCounter;
    private GameObject InsLoader;
    private GameObject Panel;

    private string ActiveInventoryKey = "";
    private int StepNumber = 0;
    private int StepCount = 0;

    private bool ButtonLocked = false;
    //private bool OverrideSmoothing = false;


    public void OnSliderDrag()
    {
        int TempStepNumber = (int)Slider.value;
        if (StepNumber != TempStepNumber)
        {
            StepNumber = TempStepNumber;
            RenderStep(true);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        InsLoader = GameObject.Find("InstructionLoader");
        Panel = GameObject.Find("Canvas/Panel");
        ButtonNext = GameObject.Find("Canvas/ButtonNext").GetComponent<Button>();
        ButtonLast = GameObject.Find("Canvas/ButtonLast").GetComponent<Button>();
        TextStepCounter = GameObject.Find("Canvas/StepCounter");
        ButtonNext.image.color = ColorActive;
        ButtonLast.image.color = ColorActive;
        Slider = GameObject.Find("Canvas/Slider").GetComponent<Slider>();

        Instr = InstructionParser.ParseInstructionsYaml($"Instructions/YAML/{Filename}");
        foreach (string Key in Instr["inventory"].Keys)
        {
            string StepsStr = Instr["inventory"][Key]["steps"];
            StepsStr = StepsStr.Replace("[", "").Replace("]", "");
            foreach (string StepIdStr in StepsStr.Split(","))
                InventoryStepRelation[StepIdStr] = Key;
        }

        StepCount = Instr["steps"].Keys.Count;
        Slider.maxValue = StepCount - 1;
        Slider.minValue = 0;
        StepNumber = 0;
        RenderStep(false);
    }

    // Update is called once per frame
    void Update()
    {
        DebugRotation();
    }

    private string[] PartsExcept(string[] Base, string[] Diff)
    {
        List<string> Except = new List<string>();
        foreach (string Value in Base)
        {
            if (!Diff.Contains(Value))
                Except.Add(Value);
        }
        return Except.ToArray();
    }

    public void RenderStep(bool OverrideSmoothing)
    {
        // Get Inventory Key of Active Step
        string InventoryKey = InventoryStepRelation[$"{StepNumber:D3}"];
        if (InventoryKey != ActiveInventoryKey)
        {
            // Clear Inventory
            ClearPanel();
            // Fill Inventory
            FillInventory(InventoryKey);
            // Set ActiveInventoryKey
            ActiveInventoryKey = InventoryKey;
        }

        // Update StepCounter
        TextStepCounter.GetComponent<TMPro.TextMeshProUGUI>().text = $"{StepNumber + 1}/{StepCount}";

        // Init empty Lists
        Dictionary<string, string> PartList = new Dictionary<string, string>();
        List<string> PartsAdded = new List<string>();

        // Init Steps Dictionary
        Dictionary<string, Dictionary<string, string>> Steps = Instr["steps"];
        // Get StepId
        string StepId = $"&id{StepNumber:D3}";
        // Get Active Step
        Dictionary<string, string> Step = Steps[StepId];

        // Set Height of InstructionLoader if required
        if (Step.ContainsKey("height_y"))
        {
            float NewHeight = float.Parse(Step["height_y"], CultureInfo.InvariantCulture);
            if (NewHeight - InsLoader.transform.position.y > 0.00001f)
            {
                Vector3 Pos = InsLoader.transform.position;
                Pos.y = NewHeight;
                InsLoader.transform.position = Pos;
            }
        }

        // Rotate view
        if (Step.ContainsKey("rot_view_x"))
        {
            // Get Rotation Vector from Instructions
            Vector3 RotView = new Vector3(
                float.Parse(Step["rot_view_x"], CultureInfo.InvariantCulture),
                float.Parse(Step["rot_view_y"], CultureInfo.InvariantCulture),
                float.Parse(Step["rot_view_z"], CultureInfo.InvariantCulture)
                );
            // Check if actual Rotation of View is close to wanted Rotation
            if (!(Vector3.SqrMagnitude(InsLoader.transform.rotation.eulerAngles - RotView) < 0.001))
            {
                // Instant rotation if smoothing is 0 or going backwards to a "part" step
                if (OverrideSmoothing || Step.ContainsKey("part") || (Step.ContainsKey("smoothing") && (Step["smoothing"] == "0")))
                    InsLoader.transform.rotation = Quaternion.Euler(RotView);
                // Animated Rotation
                else {
                    RotView += InsLoader.transform.rotation.eulerAngles;
                    RotateView(InsLoader.transform.rotation.eulerAngles, RotView, 1.0f);
                }
            }
        }

        // Add Parts or Components to PartList
        if (Step.ContainsKey("part"))
            PartList.Add(Step["part"], StepId);
        if (Step.ContainsKey("comp"))
            PartList.Add(Step["comp"], StepId);

        // Go through step referrecnes to collect all required parts and components
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

        // Fill PartsAdded with parts that are already attached to InstructionLoader
        for (int i = 0; i < InsLoader.transform.childCount; i++)
            PartsAdded.Add(InsLoader.transform.GetChild(i).name);

        // Add parts to InstructonLoader
        if (PartList.Count > 0)
        {
            // Init arrays required to calculate a diff of required parts and attached parts
            string[] PartsAddedArr = PartsAdded.ToArray();
            string[] PartListArr = PartList.Keys.ToArray();

            // Get parts that need to be destroyed
            string[] PartsToDel = PartsExcept(PartsAddedArr, PartListArr);
            // Destroy parts
            foreach (var PartName in PartsToDel)
            {
                GameObject Part = GameObject.Find($"InstructionLoader/{PartName}");
                Destroy(Part);
            }

            // Get parts that need to be added
            string[] PartsToAdd = PartsExcept(PartListArr, PartsAddedArr);
            // Add parts
            foreach (var PartName in PartsToAdd)
            {
                // Init PartId and Steps
                string PartId = PartName.ToString().Split(".")[0];
                StepId = PartList[PartName];
                Step = Steps[StepId];

                // Instantiate GameObject according to PartId
                GameObject Part = null;
                if (Step.ContainsKey("part"))
                    Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
                if (Step.ContainsKey("comp"))
                    Part = Instantiate(Resources.Load($"Models/Components/{PartId}", typeof(GameObject))) as GameObject;

                // Set part name for later use
                Part.name = PartName;
                // Set parent of part to InstructionLoader
                Part.transform.SetParent(InsLoader.transform);

                // Get position of part from Instructions
                Vector3 Pos = new Vector3(
                    float.Parse(Step["pos_x"], CultureInfo.InvariantCulture),
                    float.Parse(Step["pos_y"], CultureInfo.InvariantCulture),
                    float.Parse(Step["pos_z"], CultureInfo.InvariantCulture)
                    );
                // Set position
                Part.transform.localPosition = Pos;

                // Get rotation of part from Instructions
                Vector3 Rot = Vector3.zero;
                if (Step.ContainsKey("part"))
                {
                    Rot = new Vector3(
                    float.Parse(Step["rot_x"], CultureInfo.InvariantCulture),
                    float.Parse(Step["rot_y"], CultureInfo.InvariantCulture),
                    float.Parse(Step["rot_z"], CultureInfo.InvariantCulture)
                    );
                    // Set rotation of part
                    Part.transform.localRotation = Quaternion.Euler(Rot);

                    // Get color from Instructions
                    string Color = Step["color"];
                    Material Mat = Resources.Load($"Materials/{Color}", typeof(Material)) as Material;
                    // Set color
                    Part.GetComponent<MeshRenderer>().material = Mat;
                }
                if (Step.ContainsKey("comp"))
                {
                    // Set rotation of component
                    Part.transform.localRotation = Quaternion.Euler(Rot);
                }

                ToggleAR ToggleAr = GameObject.Find("Canvas/ButtonToggleAR").GetComponent<ToggleAR>();
                if (ToggleAr.IsActive)
                {
                    Part.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void FillInventory(string InventoryKey)
    {
        string PartKeyStr = "part_";
        string CompKeyStr = "comp_";
        string QuantityKeyStr = "quantity_";
        string ColorKeyStr = "color_";

        int Counter = 0;
        while (Instr["inventory"][InventoryKey].ContainsKey($"{PartKeyStr}{Counter}"))
        {
            string PartId = Instr["inventory"][InventoryKey][$"{PartKeyStr}{Counter}"];
            string Quantity = Instr["inventory"][InventoryKey][$"{QuantityKeyStr}{Counter}"];
            string Color = Instr["inventory"][InventoryKey][$"{ColorKeyStr}{Counter}"];

            GameObject InvPartParent = Instantiate(Resources.Load("UIElements/InvPartParent", typeof(GameObject))) as GameObject;
            InvPartParent.transform.parent = Panel.transform;
            InvPartParent.layer = 5;
            InvPartParent.transform.localPosition = Vector3.zero;

            GameObject Part = Instantiate(Resources.Load($"Models/Bricks/{PartId}", typeof(GameObject))) as GameObject;
            Material Mat = Resources.Load($"Materials/{Color}", typeof(Material)) as Material;
            Part.GetComponent<MeshRenderer>().material = Mat;
            InvPartParent.transform.GetChild(0).gameObject.GetComponent<TMPro.TextMeshPro>().text = $"{Quantity}x";
            Part.layer = 5;
            Part.transform.parent = InvPartParent.transform;
            Part.transform.localPosition = new Vector3(0, 0, -1.25f);
            Part.transform.localRotation = Quaternion.Euler(-197.3f, 36.3f, 5.8f);
            int ScaleFactor = 40;
            Part.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);

            Counter++;
        }
        Counter = 0;
        while (Instr["inventory"][InventoryKey].ContainsKey($"{CompKeyStr}{Counter}"))
        {
            string CompName = Instr["inventory"][InventoryKey][$"{CompKeyStr}{Counter}"];
            string Quantity = Instr["inventory"][InventoryKey][$"{QuantityKeyStr}{Counter}"];

            GameObject InvComp = Instantiate(Resources.Load($"UIElements/Inv{CompName}", typeof(GameObject))) as GameObject;
            InvComp.transform.parent = Panel.transform;
            InvComp.transform.localPosition = Vector3.zero;
            InvComp.transform.GetChild(1).gameObject.GetComponent<TMPro.TextMeshPro>().text = $"{Quantity}x";
            float yOffset = 0f;
            if (CompName.Split("_")[0] == "Core")
                yOffset = -0.28f;
            InvComp.transform.GetChild(0).transform.localPosition = new Vector3(0, yOffset, -1.25f);
            int ScaleFactor = 40;
            InvComp.transform.GetChild(0).transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);

            //GameObject InvPartParent = Instantiate(Resources.Load("UIElements/InvPartParent", typeof(GameObject))) as GameObject;
            //InvPartParent.transform.parent = Panel.transform;
            //InvPartParent.layer = 5;
            //InvPartParent.transform.localPosition = Vector3.zero;

            //GameObject Comp = Instantiate(Resources.Load($"Models/Components/{CompName}", typeof(GameObject))) as GameObject;
            //Comp.layer = 5;
            //for (int i = 0; i < Comp.transform.childCount; i++)
            //{
            //    Comp.transform.GetChild(i).gameObject.layer = 5;
            //}
            //Comp.transform.parent = InvPartParent.transform;
            //Comp.transform.localPosition = new Vector3(0, 0, -1.25f);
            //Comp.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            //int ScaleFactor = 40;
            //Comp.transform.localScale = new Vector3(ScaleFactor, ScaleFactor, ScaleFactor);

            Counter++;
        }
    }

    private void RotateView(Vector3 RotateFrom, Vector3 RotateTo, float SmoothingFactor)
    {
        StartCoroutine(RotateViewAnimation(Quaternion.Euler(RotateFrom), Quaternion.Euler(RotateTo), SmoothingFactor));
    }

    IEnumerator RotateViewAnimation(Quaternion RotateFrom, Quaternion RotateTo, float SmoothingFactor)
    {
        ToggleStepButtons(true);
        Slider.interactable = false;
        float Smooth = Smoothing * SmoothingFactor;
        InsLoader.transform.rotation = RotateFrom;
        while (InsLoader.transform != null && Quaternion.Angle(InsLoader.transform.rotation, RotateTo) > RotationEpsilon)
        {
            InsLoader.transform.rotation = Quaternion.Slerp(InsLoader.transform.rotation, RotateTo, Smooth * Time.deltaTime);

            yield return null;
        }

        ToggleStepButtons(false);
        Slider.interactable = true;
        yield return new WaitForSeconds(0.1f);

    }

    private void DebugRotation()
    {
        TMPro.TextMeshProUGUI DebugText = GameObject.Find("DebugText").GetComponent<TMPro.TextMeshProUGUI>();
        Vector3 Rot = InsLoader.transform.rotation.eulerAngles;
        DebugText.text = $"Debug:\nRotX {Rot.x}\nRotY {Rot.y}\nRotZ {Rot.z}";
    }

    private void ClearPanel()
    {
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
        if (!ButtonLocked && StepNumber + 1 < StepCount)
        {
            StepNumber++;
            Slider.value = StepNumber;
            RenderStep(false);
        }
    }

    public void GoToLastStep()
    {
        if (!ButtonLocked && StepNumber - 1 >= 0)
        {
            StepNumber--;
            Slider.value = StepNumber;
            RenderStep(false);
        }
    }
}