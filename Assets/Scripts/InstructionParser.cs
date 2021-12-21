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
        int NbrComments = CountComments(Lines);
        string[][] Instructions = new string[Lines.Length - NbrComments][];

        int i = 0;
        foreach (string Line in Lines)
        {
            if (Line.StartsWith("#"))
                continue;
            Instructions[i] = Line.Replace("\r", "").Split(" ");
            i += 1;
        }

        return Instructions;
    }

    private static int CountComments(string[] Lines)
    {
        int NbrComments = 0;
        foreach (string Line in Lines)
            if (Line.StartsWith("#") || Line.Trim() == "")
                NbrComments++;
        return NbrComments;
    }
}
