using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    public List<Transform> startPoints = new List<Transform>();
    public GameObject mouse;
    public  int point;
    public  int num;
    public int maxNum;
    public static GameManager instance;
    public static Action onNumChange;

    public bool isCustom;
    public int mouseNumber;
    public float mouseSpeed;
    public int baseNumber;
    public Transform table;
    public GameObject prefab;
    private void Awake()
    {
        instance = this;
        
        Time.timeScale = 1;
        if (isCustom)
        {
            mouseNumber = PlayerPrefs.GetInt("Mouse number");
            baseNumber = PlayerPrefs.GetInt("Base number");
            mouseSpeed = PlayerPrefs.GetFloat("Mouse speed");
            maxNum = mouseNumber;
            table.localScale = new Vector3(baseNumber, table.localScale.y, baseNumber);
            
            int num = baseNumber;
            float temp = (float)num / (num + 1);
            float start = -((float )num/ 2);
            float col = start;
            float row = start;

            for (int i = 0; i < num; i++)
            {
                col += temp;
                row = start;
                for (int j = 0; j < num; j++)
                {
                    row += temp;
                    var go = Instantiate(prefab, new Vector3(row,0.522f,col), Quaternion.identity);
                    startPoints.Add(go.transform);
                }
            }
        }
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        for (int i = 0; i < startPoints.Count; i++)
        {
            if(isCustom)
             SpawnMouse(startPoints[i],i,1/mouseSpeed);
            else
            {
                SpawnMouse(startPoints[i],i);
            }
        }
    }

    private void OnEnable()
    {
        // Hammer.attack += (collider1 =>
        // {
        //     var index = collider1.GetComponent<MouseAI>().index;
        //
        //     if (isCustom)
        //     {
        //         StartCoroutine(Spawn(startPoints[index], index,1/mouseSpeed));
        //      //   SpawnMouse(startPoints[index],index,1/mouseSpeed);
        //
        //     }
        //     else
        //     {
        //         SpawnMouse(startPoints[index],index);
        //     }
        // });
        
       onNumChange += NumChange;
    }

    private void NumChange()
    {
        // if (maxNum <= num)
        // {
        //
        //     GameOver();
        //
        // }
    }

    private void Update()
    {
        if (maxNum <= num)
        {
                GameOver();

        }
    }

    private void GameOver()
    {
        Time.timeScale = 0;
        GameObject.Find("GameOverText").GetComponent<Text>().color = new Color(1, 0, 0);
    }


    public void  SpawnMouse(Transform pos ,int index,float speed)
    {
        var go = Instantiate(mouse, pos.position, Quaternion.Euler(new Vector3(90,0,0)));
        go.GetComponent<MouseAI>().index = index;
        go.GetComponent<MouseAI>().stopTime = speed;
    }  
    public void  SpawnMouse(Transform pos ,int index)
    {
        Instantiate(mouse, pos.position, Quaternion.Euler(new Vector3(90,0,0))).GetComponent<MouseAI>().index = index;
    }

    IEnumerator Spawn(Transform pos ,int index,float mouseSpeed)
    {
        yield return new WaitForSeconds(0.5f);
        SpawnMouse(pos, index,1/mouseSpeed);
    }
}
