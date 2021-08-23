using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject prefab;
    void Start()
    {
        int num = 4;
        float temp = (float)num / (num + 1);
        print(temp);
        float start = -((float )num/ 2);
        float col = start;
        float row = start;
        print(start);
        for (int i = 0; i < num; i++)
        {
            col += temp;
            row = start;
            for (int j = 0; j < num; j++)
            {
                row += temp;
                print(row);
                print(col);
                Instantiate(prefab, new Vector3(row,0,col), Quaternion.identity);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
