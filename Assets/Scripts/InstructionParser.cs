using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class InstructionParser
{
    public static string[][] ParseInstructions(string Filename)
    {
        TextAsset RawInstructions = (TextAsset)Resources.Load(Filename);
        string[] Lines = RawInstructions.ToString().Split("\n");
        int InstructionLines = CountInstructionLines(Lines);
        string[][] Instructions = new string[InstructionLines][];

        int i = 0;
        foreach (string Line in Lines)
        {
            if (Line.StartsWith("#") || Line.StartsWith("I") || Line.Trim() == "")
                continue;
            Instructions[i] = Line.Replace("\r", "").Split(" ");
            i += 1;
        }

        return Instructions;
    }

    public static Dictionary<string, string[]> ParseInventory(string Filename)
    {
        Dictionary<string, string[]> InventoryDict = new Dictionary<string, string[]>();

        TextAsset RawInstructions = (TextAsset)Resources.Load(Filename);
        string[] Lines = RawInstructions.ToString().Split("\n");
        foreach (string Line in Lines)
        {
            if(Line.StartsWith("I"))
            {
                string[] LineSplit = Line.Split(" ");
                string InvStr = string.Join(' ', LineSplit, 2, LineSplit.Length - 2).Replace("\r", "");
                string[] InvSplit = InvStr.Split(":");
                InventoryDict.Add(LineSplit[1], InvSplit);
            }
        }

        return InventoryDict;
    }
    
    public static Dictionary<string, Dictionary<string, Dictionary<string, string>>> ParseInstructionsYaml(string Filename)
    {
        TextAsset RawInstructions = (TextAsset)Resources.Load(Filename);
        Dictionary<string, Dictionary<string, Dictionary<string, string>>> Instructions = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        
        string activeKey = "";
        string activeStepId = "";
        foreach (string s in RawInstructions.ToString().Split("\n"))
        {
            if (!s.StartsWith(" "))
            {
                string actKey = s.Trim().Replace(":", "");
                Instructions.Add(actKey, new Dictionary<string, Dictionary<string, string>>());
                activeKey = actKey;
            }
            if (s.StartsWith("    -"))
            {
                string stepId = s.Split(":")[1].Trim();
                if (Instructions.ContainsKey(activeKey))
                {
                    Instructions[activeKey].Add(stepId, new Dictionary<string, string>());
                }
                activeStepId = stepId;
            }
            if (s.StartsWith("        "))
            {
                var split = s.Split(":");
                var key = split[0].Trim();
                var value = split[1].Trim();
                if (Instructions[activeKey].ContainsKey(activeStepId))
                {
                    Instructions[activeKey][activeStepId].Add(key, value);
                }
            }
        }
            return Instructions;
        }

    private static int CountInstructionLines(string[] Lines)
    {
        int NbrLines = 0;
        foreach (string Line in Lines)
        {
            if (Line.StartsWith("#") || Line.StartsWith("I") || Line.Trim() == "")
                continue;
            else
                NbrLines++;
        }
        return NbrLines;
    }
}
