using System;
using UnityEngine;

public class SaveSystemTestScript : MonoBehaviour
{
    private void Awake()
    {
        int i = GamePrefs.GetInt("Loaded")+ 1;
        GamePrefs.SetInt("Loaded", i );
    }

    private void Start()
    {
        Debug.Log("Active Slot:" + GamePrefs.ActiveSlot + " :: Loaded Test:" + GamePrefs.GetInt("Loaded"));
    }
}