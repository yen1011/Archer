using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CountdownTimer : MonoBehaviour
{
    [Header("Timer")]
    public float startSeconds = 90f;    // 시작 초(90)
    public bool autoStart = true;       // 시작하자마자 카운트다운
    public bool showMMSS = false;       // false면 "90", true면 "01:30"
    public int warnAtSeconds = 10;      // 경고 색 바꿀 시점(비활성 원하면 음수)
    public Color normalColor = Color.white;
    public Color warnColor = new Color(1f, 0.4f, 0.4f);

    [Header("UI")]
    public TMP_Text timerLabel;

    [Header("Events (선택)")]
    public UnityEvent onTimerEnd;       // 0초 도달 시 1회 호출

    float remain;
    bool running;
    bool ended;

    void Start()
    {
        ResetTimer();
        if (autoStart) StartTimer();
        UpdateLabel(); // 시작 시 바로 UI 반영
    }

    void Update()
    {
        if (!running) return;

        remain -= Time.deltaTime;
        if (remain <= 0f)
        {
            remain = 0f;
            running = false;
            if (!ended)
            {
                ended = true;
                UpdateLabel();
                onTimerEnd.Invoke(); // 여기서 승패 판정 연결 가능(다음 단계에서)
            }
        }
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (!timerLabel) return;

        if (showMMSS)
        {
            int tot = Mathf.CeilToInt(remain);
            int m = Mathf.Max(0, tot / 60);
            int s = Mathf.Max(0, tot % 60);
            timerLabel.text = $"{m:00}:{s:00}";
        }
        else
        {
            timerLabel.text = Mathf.CeilToInt(remain).ToString();
        }

        if (warnAtSeconds >= 0 && remain <= warnAtSeconds)
            timerLabel.color = warnColor;
        else
            timerLabel.color = normalColor;
    }

    // ───── 외부에서 호출용 ─────
    public void StartTimer()
    {
        if (remain <= 0f) ResetTimer();
        running = true; ended = false;
    }

    public void StopTimer() { running = false; }

    public void ResetTimer()
    {
        remain = Mathf.Max(0f, startSeconds);
        running = false; ended = false;
    }

    public float GetRemainingSeconds() => remain;
}
