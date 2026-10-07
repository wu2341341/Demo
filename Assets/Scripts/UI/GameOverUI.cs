using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI resultText;
    public Button restartButton;
    public Button menuButton;

    [Header("场景名")]
    public string mainMenuSceneName = "StartMenu";

    void Start()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);

        if (menuButton != null)
            menuButton.onClick.AddListener(OnMenuClicked);
    }

    public void ShowGameOver(bool victory)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = victory ? "胜利!" : "败北...";
            resultText.color = victory
                ? new Color(1f, 0.85f, 0.2f)
                : new Color(0.8f, 0.1f, 0.1f);
        }

        // 暂停游戏
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}