using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRUiKits.Utils;

public class CustomDataManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private SliderProgressBar baseNumberSlider;
    [SerializeField] private SliderProgressBar mouseNumberSlider;
    [SerializeField] private SliderProgressBar mouseSpeedSlider;
    [SerializeField] private Button startBut;
    
    void Start()
    {
        Time.timeScale = 1;
        baseNumberSlider.slider.onValueChanged.AddListener(arg0 =>  baseNumberSlider.ValueText.text = arg0.ToString());
       mouseNumberSlider.slider.onValueChanged.AddListener(arg0 =>  mouseNumberSlider.ValueText.text =arg0.ToString());
       mouseSpeedSlider.slider.onValueChanged.AddListener(arg0 =>  mouseSpeedSlider.ValueText.text = (Mathf.Round(arg0 * 100f) / 100f).ToString());
       startBut.onClick.AddListener((() => SceneManager.LoadScene("06 Custom Game")));
    }

    private void OnDisable()
    {
        PlayerPrefs.SetInt("Base number",(int)baseNumberSlider.Value);
        PlayerPrefs.SetInt("Mouse number",(int)mouseNumberSlider.Value);
        PlayerPrefs.SetFloat("Mouse speed",mouseSpeedSlider.Value);
        PlayerPrefs.Save();
    }
    
}
