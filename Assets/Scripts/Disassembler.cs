using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Disassembler : MonoBehaviour
{
    [Tooltip("GameObject to be disassembled.")]
    public GameObject ObjToDisassemble;
    [Tooltip("Set this to true when the GameObject is just a Component. Set to false when the GameObject is the whole model, which consists of Components.")]
    public bool isComponent = false;
    void Start()
    {
        // Hierarchy of Comp_Earth:
        //  CompEarth
        //      Comp_Core
        //          Comp_Core_Top
        //          Comp_Core_Bottom
        //      Comp_Sides
        //          Comp_Side_North
        //          Comp_Side_East
        //          Comp_Side_South
        //          Comp_Side_West
        //          Comp_Side_Back
        //          Comp_Side_Front

        if (ObjToDisassemble != null)
        {
            // Get the Comp_Earth GameObject
            //ObjToDisassemble.transform.rotation = Quaternion.Euler(Vector3.zero);
            ObjToDisassemble.transform.localScale = new Vector3(100, 100, 100);
            // Create empty list to store components of Comp_Earth
            List<GameObject> CompChildren = new List<GameObject>();
            // Loop through children of Comp_Earth
            for (int i = 0; i < ObjToDisassemble.transform.childCount; i++)
            {
                // Store child components
                CompChildren.Add(ObjToDisassemble.transform.GetChild(i).gameObject);
            }
            List<GameObject> SortedComponents = CompChildren;
            if (!isComponent)
                SortedComponents = CompChildren.OrderBy(d => d.transform.position.y).ToList();
            string InstructionStr = "";
            foreach (GameObject Obj in SortedComponents)
            {
                //Debug.Log(obj.name + " " + obj.transform.position.y);
                if (isComponent)
                    InstructionStr += $"P {Obj.name.Split(".")[0]}";
                else
                    InstructionStr += $"C {Obj.name}";
                Vector3 pos = Obj.transform.localPosition;
                InstructionStr += $" {pos.x} {pos.y} {pos.z}".Replace(",", ".");
                Vector3 Rot = Obj.transform.rotation.eulerAngles;
                InstructionStr += $" {Rot.x} {Rot.y} {Rot.z}".Replace(",", ".");
                if (isComponent)
                {
                    string color = Obj.GetComponent<MeshRenderer>().material.name;
                    color = "Color";
                    InstructionStr += $" {color}";
                }
                InstructionStr += "\n";
            }
            Debug.Log(InstructionStr);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
