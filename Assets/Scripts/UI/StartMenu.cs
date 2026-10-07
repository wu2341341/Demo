using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [Header("按钮")]
    public Button startButton;
    public Button quitButton;

    [Header("场景名")]
    public string gameSceneName = "SampleScene";

    void Start()
    {
        // 确保时间正常（防止从暂停的场景返回时被冻结）
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    void OnStartClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void OnQuitClicked()
    {
        Debug.Log("退出游戏");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}