using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemDebugGUI : MonoBehaviour {
    public int width;
    public int height;

    private void OnGUI() {
        Rect boxRect = new Rect(Screen.width - width, Screen.height - height, width, height);
        GUI.Box(boxRect,"");
        GUI.Label(boxRect, NetworkSendSystem.LastSendMessage);
    }
}
