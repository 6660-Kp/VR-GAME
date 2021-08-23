using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartSceneButton : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]private Button easyGameBut;
    [SerializeField]private Button hardGameBut;
    [SerializeField]private Button veryHardGameBut;
    [SerializeField] private Button customBut;
    void Start()
    {
        Time.timeScale = 1;
        easyGameBut.onClick.AddListener((() => SceneManager.LoadScene("02 Easy")));
        hardGameBut.onClick.AddListener((() => SceneManager.LoadScene("03 Hard")));
        veryHardGameBut.onClick.AddListener((() => SceneManager.LoadScene("04 Very Hard")));
        customBut.onClick.AddListener((() => SceneManager.LoadScene("05 Custom")));
    }
}
