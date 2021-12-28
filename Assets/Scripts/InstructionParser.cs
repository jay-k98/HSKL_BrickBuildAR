using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class InstructionParser
{
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
            if (s.StartsWith("        ") || s.StartsWith("      "))
            {
                var split = s.Split(":");
                var key = split[0].Replace("-", "").Trim();
                var value = split[1].Trim();
                if (Instructions[activeKey].ContainsKey(activeStepId))
                {
                    Instructions[activeKey][activeStepId].Add(key, value);
                }
            }
        }
        return Instructions;
    }
}
