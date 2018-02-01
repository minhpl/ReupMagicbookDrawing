using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VeryfyScrollView : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {
        int val = 145;
        val = 1744 - (int)((1080 * Screen.height) / (Screen.width * 1.0f));
        val /= 2;
        Vector3 v3 = transform.localPosition;
        v3.y -= val;
        transform.localPosition = v3;
        GetComponent<UIPanel>().clipOffset = new Vector2(0, val);
    }
}
