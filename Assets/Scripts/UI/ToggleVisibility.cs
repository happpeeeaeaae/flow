using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleVisibility : MonoBehaviour
{
    public GameObject settingsPanel;

    public void toggleShowControls()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}
