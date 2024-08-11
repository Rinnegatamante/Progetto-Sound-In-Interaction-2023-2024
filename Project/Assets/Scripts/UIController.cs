using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    public TextMeshProUGUI timer;
    private float startTime = 0.0f;

    void Start()
    {
        startTime = Time.time;
        timer.text = "Time: 00:00";
    }

    void Update()
    {
        float curTime = Time.time - startTime;
        float minutes = Mathf.FloorToInt(curTime / 60);
        float seconds = Mathf.FloorToInt(curTime % 60);

        timer.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
    }
}
