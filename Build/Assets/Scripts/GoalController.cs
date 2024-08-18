using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GoalController : MonoBehaviour
{
    private float deltaStart = -1.0f;
    private float startTime = 0.0f;
    private PlayerController player;
    public TextMeshProUGUI countdown;

    void Start()
    {
        player = FindObjectOfType<PlayerController>();
        startTime = Time.time;
    }

    void Update()
    {
        if (player.currentState == PlayerController.State.WON)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (SceneManager.GetActiveScene().name.Equals("level5"))
                {
                    Time.timeScale = 1.0f;
                    SceneManager.LoadScene("mainmenu", LoadSceneMode.Single);
                }
                else
                {
                    string s = SceneManager.GetActiveScene().name;
                    Debug.Log(SceneManager.GetActiveScene().name.Substring(0, s.Length - 1));
                    Time.timeScale = 1.0f;
                    SceneManager.LoadScene("level" + (int.Parse(SceneManager.GetActiveScene().name.Substring(s.Length - 1)) + 1).ToString(), LoadSceneMode.Single);
                }
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (Time.time - deltaStart < 3.0f)
        {
            countdown.text = string.Format("{0:0}", 3 - (Time.time - deltaStart));
        } else {
            if (player.currentState != PlayerController.State.WON) {
                Time.timeScale = 0.0f;
                float completionTime = Time.time - startTime;
                PlayerPrefs.SetFloat(SceneManager.GetActiveScene().name + "_time", completionTime);
                if (SceneManager.GetActiveScene().name.Equals("level5"))
                {
                    int[] minutes = new int[6];
                    int[] seconds = new int[6];
                    float totalCompletionTime = 0.0f;
                    for (int i = 1; i <= 5; i++)
                    {
                        Debug.Log("level" + i.ToString() + "_time");
                        completionTime = PlayerPrefs.GetFloat("level" + i.ToString() + "_time");
                        
                        totalCompletionTime += completionTime;
                        minutes[i - 1] = Mathf.FloorToInt(completionTime / 60);
                        seconds[i - 1] = Mathf.FloorToInt(completionTime % 60);
                    }
                    minutes[5] = Mathf.FloorToInt(totalCompletionTime / 60);
                    seconds[5] = Mathf.FloorToInt(totalCompletionTime % 60);
                    countdown.text = string.Format("Level 1 completed in {0:00}:{1:00}\n" +
                        "Level 2 completed in {2:00}:{3:00}\n" +
                        "Level 3 completed in {4:00}:{5:00}\n" +
                        "Level 4 completed in {6:00}:{7:00}\n" +
                        "Level 5 completed in {8:00}:{9:00}\n" +
                        "Total Time is {10:00}:{11:00}\n\nPress X to return to main menu, thanks for playing!",
                        minutes[0], seconds[0], minutes[1], seconds[1], minutes[2], seconds[2], minutes[3], seconds[3], minutes[4], seconds[4], minutes[5], seconds[5]);
                } else {
                    float minutes = Mathf.FloorToInt(completionTime / 60);
                    float seconds = Mathf.FloorToInt(completionTime % 60);
                    countdown.text = string.Format("Level completed in {0:00}:{1:00}\nPress X to continue", minutes, seconds);
                }    
                player.currentState = PlayerController.State.WON;  
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        deltaStart = Time.time;
    }

    void OnTriggerExit(Collider other)
    {
        countdown.text = "";
        deltaStart = -1.0f;
    }
}
