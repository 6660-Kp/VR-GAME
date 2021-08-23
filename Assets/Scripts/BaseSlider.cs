using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BaseSlider : MonoBehaviour
{
    // Start is called before the first frame update
    public Text displayText;
    private Slider slider;
    void Start()
    {
        slider = GetComponent<Slider>();
    }

    // Update is called once per frame
    void Update()
    {
        displayText.text = slider.value.ToString() + "*" + slider.value.ToString();
    }
}
