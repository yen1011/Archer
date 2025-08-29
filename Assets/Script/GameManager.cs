using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // TMP를 안 쓰면 지우고, 아래 resultTextUGUI 대신 UnityEngine.UI.Text 사용

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public Health playerHealth;
    public Health botHealth;

    [Header("Result UI")]
    public GameObject resultPanel;            // Canvas 아래 결과 패널(처음에 비활성화)
    public TextMeshProUGUI resultTextUGUI;    // "WIN" / "LOSE" 표기 텍스트

    bool gameOver;

    void Start()
    {
        if (playerHealth) playerHealth.OnDied += OnPlayerDied;
        if (botHealth) botHealth.OnDied += OnBotDied;

        if (resultPanel) resultPanel.SetActive(false);
        Time.timeScale = 1f; // 혹시 모를 정지 해제
    }

    void OnDestroy()
    {
        if (playerHealth) playerHealth.OnDied -= OnPlayerDied;
        if (botHealth) botHealth.OnDied -= OnBotDied;
    }

    void OnPlayerDied(Health h)
    {
        if (gameOver) return;
        gameOver = true;
        ShowResult("LOSE");
    }

    void OnBotDied(Health h)
    {
        if (gameOver) return;
        gameOver = true;
        ShowResult("WIN");
    }

    void ShowResult(string msg)
    {
        if (resultTextUGUI) resultTextUGUI.text = msg;
        if (resultPanel) resultPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // UI 버튼에 연결해서 재시작하고 싶을 때 사용
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
