using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text score;

    [SerializeField] private Button restartUI;
    // Start is called before the first frame update
    void Start()
    {
        restartUI.onClick.AddListener((() => SceneManager.LoadScene("01 Start Scene")));
    }

    // Update is called once per frame
    void Update()
    {
        score.text = GameManager. instance.point.ToString();
    }
}
