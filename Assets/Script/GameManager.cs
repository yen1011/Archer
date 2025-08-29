using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;        // ← 카운트다운 텍스트가 UI.Text일 때
using TMPro;                 // ← 결과/타이머가 TMP라면 유지

public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    public Health playerHealth;
    public Health botHealth;

    [Header("Result UI")]
    public GameObject resultPanel;
    public TextMeshProUGUI resultTextUGUI;

    [Header("Match Timer")]
    public float matchTime = 90f;                // 경기 시간(초)
    public TextMeshProUGUI timerText;            // 상단에 남은 시간 표시
    bool matchStarted;                           // 카운트다운 완료 후 true

    [Header("Countdown UI")]
    public GameObject countdownPanel;            // 전체를 가리는 Panel(처음엔 비활성 OK)
    public Text countdownText;                   // UI.Text (YOUR GAME TITLE 객체 재활용 가능)
    public float countdownSeconds = 3f;          // 3이면 '3 2 1' 표시

    bool gameOver;

    void Start()
    {
        if (playerHealth) playerHealth.OnDied += OnPlayerDied;
        if (botHealth) botHealth.OnDied += OnBotDied;

        if (resultPanel) resultPanel.SetActive(false);

        // 시작하자마자 카운트다운 + 전체 정지
        StartCoroutine(CountdownAndFreeze());
    }

    void OnDestroy()
    {
        if (playerHealth) playerHealth.OnDied -= OnPlayerDied;
        if (botHealth) botHealth.OnDied -= OnBotDied;
    }

    // ── 카운트다운 동안 전체 정지 ─────────────────────────────
    System.Collections.IEnumerator CountdownAndFreeze()
    {
        matchStarted = false;
        if (countdownPanel) countdownPanel.SetActive(true);

        // 전체 정지
        Time.timeScale = 0f;

        float t = countdownSeconds;
        while (t > 0f)
        {
            if (countdownText) countdownText.text = Mathf.CeilToInt(t).ToString();
            // unscaled 시간으로 감소
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (countdownText) countdownText.text = "GAME START!";
        yield return new WaitForSecondsRealtime(0.6f);

        if (countdownPanel) countdownPanel.SetActive(false);

        // 재생 시작
        Time.timeScale = 1f;
        matchStarted = true;
    }

    void Update()
    {
        if (gameOver || !matchStarted) return;

        // 경기 타이머
        matchTime -= Time.deltaTime;
        if (timerText)
        {
            int sec = Mathf.Max(0, Mathf.CeilToInt(matchTime));
            timerText.text = sec.ToString();
        }

        // 시간 만료 판정
        if (matchTime <= 0f)
        {
            if (gameOver) return;
            gameOver = true;

            // 체력 비교해 결과
            int p = playerHealth ? playerHealth.HP : 0;
            int b = botHealth ? botHealth.HP : 0;
            string msg = (p > b) ? "WIN" : (p < b) ? "LOSE" : "DRAW";
            ShowResult(msg);
        }
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
        Time.timeScale = 0f; // 결과 화면에서 멈춤
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
