using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

public sealed class GameUtilities
{
    //------------------------------------------------------------------------------------
    // Text
    //------------------------------------------------------------------------------------
    public static void SetTextColour(TextMeshProUGUI text, Color colour)
    {
        if (text != null)
        {
            text.color = colour;
        }
    }

    //------------------------------------------------------------------------------------
    public static void SetOptionalText(TextMeshProUGUI inTextField, string inText)
    {
        if (inTextField != null && inTextField.text != inText)
        {
            inTextField.SetText(inText);
        }
    }

    //------------------------------------------------------------------------------------
    // Hierarchy
    //------------------------------------------------------------------------------------
    public static void SetActive(GameObject inGameObject, bool inIsActive)
    {
        if (inGameObject != null && inGameObject.activeSelf != inIsActive)
        {
            inGameObject.SetActive(inIsActive);
        }
    }

    //------------------------------------------------------------------------------------
    public static void SetActive(GameObject[] inGameObjects, bool inIsActive)
    {
        if (inGameObjects != null)
        {
            for (int index = 0; index < inGameObjects.Length; index++)
            {
                GameObject gameObject = inGameObjects[index];

                if (gameObject != null && gameObject.activeSelf != inIsActive)
                {
                    gameObject.SetActive(inIsActive);
                }
            }
        }
    }

}
