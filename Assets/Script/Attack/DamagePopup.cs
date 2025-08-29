// DamagePopup.cs
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Refs")]
    public TMP_Text text;

    [Header("Motion")]
    public float lifetime = 0.6f;
    public float floatUpSpeed = 1.2f;
    public Vector2 spawnJitter = new Vector2(0.06f, 0.04f);

    [Header("Render Order")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 100;

    [Header("Colors")]
    public Color damagePlayerColor = new Color(1f, 0.3f, 0.3f); // 플레이어가 맞았을 때(빨강)
    public Color damageBotColor = new Color(1f, 0.9f, 0.4f); // 봇이 맞았을 때(노랑)
    public Color healColor = new Color(0.35f, 0.85f, 1f); // 회복(파랑)

    [Header("Style")]
    public bool showSign = true;          // + / - 기호 붙이기
    public float critScale = 1.15f;       // 치명타 폰트 배율

    float t;
    Color baseColor;

    void Awake()
    {
        if (!text) text = GetComponent<TMP_Text>();
        if (TryGetComponent<Renderer>(out var r))
        {
            r.sortingLayerName = sortingLayerName;
            r.sortingOrder = sortingOrder;
        }

        // 소환 위치 약간 랜덤
        transform.position += new Vector3(
            Random.Range(-spawnJitter.x, spawnJitter.x),
            Random.Range(0f, spawnJitter.y), 0f);

        if (text) baseColor = text.color;
    }

    // ─────────────────────────────────────────────────────────────
    // 외부에서 호출하는 셋업들

    public void SetupDamage(int amount, Team2D hitTeam, bool crit = false)
    {
        if (!text) return;
        string sign = showSign ? "-" : "";
        text.text = $"{sign}{amount}";
        text.color = (hitTeam == Team2D.Player) ? damagePlayerColor : damageBotColor;
        if (crit) text.fontSize *= critScale;
        baseColor = text.color;
    }

    public void SetupHeal(int amount, Color? overrideColor = null)
    {
        if (!text) return;
        string sign = showSign ? "+" : "";
        text.text = $"{sign}{amount}";
        text.color = overrideColor.HasValue ? overrideColor.Value : healColor;
        baseColor = text.color;
    }

    // 간편 Spawn 헬퍼들(프리팹과 위치만 있으면 바로 생성)
    public static DamagePopup Spawn(GameObject prefab, Vector3 worldPos)
    {
        var go = Instantiate(prefab, worldPos, Quaternion.identity);
        return go.GetComponent<DamagePopup>();
    }
    public static void SpawnDamage(GameObject prefab, Vector3 pos, int amount, Team2D hitTeam, bool crit = false)
    {
        var p = Spawn(prefab, pos);
        if (p) p.SetupDamage(amount, hitTeam, crit);
    }
    public static void SpawnHeal(GameObject prefab, Vector3 pos, int amount, Color? color = null)
    {
        var p = Spawn(prefab, pos);
        if (p) p.SetupHeal(amount, color);
    }
    // ─────────────────────────────────────────────────────────────

    void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * (floatUpSpeed * Time.deltaTime);

        if (text)
        {
            float a = 1f - Mathf.Clamp01(t / lifetime);
            var c = baseColor; c.a = a;
            text.color = c;
        }

        if (t >= lifetime) Destroy(gameObject);
    }
    // 기존 코드와의 호환: Health.cs가 호출하는 Setup(...)을 지원
    public void Setup(int amount, Team2D hitTeam, bool crit = false)
    {
        SetupDamage(amount, hitTeam, crit);
    }

}
