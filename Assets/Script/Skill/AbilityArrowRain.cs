using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityArrowRain : MonoBehaviour
{
    [Header("UI")]
    public Button button;                 // 스킬 아이콘 버튼
    public Image cooldownFill;            // Type=Filled인 오버레이 (없으면 null)
    public TMP_Text cooldownText;         // 버튼 위 숫자 (없으면 null)

    [Header("Grayscale (쿨동안 비활성 연출)")]
    public Image[] grayscaleImages;       // 회색 처리할 이미지들(아이콘, 테두리 등)
    public Material grayscaleMat;         // (선택) 그레이스케일 UI 머티리얼. 없으면 색만 어둡게
    public Color disabledTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Target / Spawn")]
    public Transform target;              // 보통 Bot
    public float spreadX = 3f;
    public float spawnHeight = 4f;

    [Header("Projectiles")]
    public GameObject projectilePrefab;
    public int arrowCount = 18;
    public float duration = 1.0f;
    public float fallSpeed = 20f;
    public float projGravityScale = 0f;
    public int damagePerHit = 24;
    public LayerMask hitMask;
    public bool alignToVelocity = true;

    [Header("Team / Cooldown")]
    public Team2D team = Team2D.Player;
    public float cooldown = 6f;

    bool cooling;
    Color[] _origColors;
    Material[] _origMats;

    void Awake()
    {
        if (button) button.onClick.AddListener(Activate);
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (cooldownText) cooldownText.gameObject.SetActive(false);

        // 그레이스케일 원상복구용 백업
        if (grayscaleImages != null && grayscaleImages.Length > 0)
        {
            _origColors = new Color[grayscaleImages.Length];
            _origMats = new Material[grayscaleImages.Length];
            for (int i = 0; i < grayscaleImages.Length; i++)
            {
                if (!grayscaleImages[i]) continue;
                _origColors[i] = grayscaleImages[i].color;
                _origMats[i] = grayscaleImages[i].material;
            }
        }
    }

    public void Activate()
    {
        if (cooling || !projectilePrefab || !target) return;
        StartCoroutine(RainRoutine());
        StartCoroutine(CooldownRoutine());
    }

    IEnumerator RainRoutine()
    {
        float interval = Mathf.Max(0.01f, duration / Mathf.Max(1, arrowCount));

        for (int i = 0; i < arrowCount; i++)
        {
            float x = target.position.x + Random.Range(-spreadX * 0.5f, spreadX * 0.5f);
            float y = target.position.y + spawnHeight;
            var pos = new Vector3(x, y, 0f);

            var go = Instantiate(projectilePrefab, pos, Quaternion.identity);

            var hit = go.GetComponent<ProjectileHit>();
            if (hit)
            {
                hit.team = team;
                hit.damage = damagePerHit;
                hit.hitMask = hitMask;
            }

            var rb = go.GetComponent<Rigidbody2D>();
            if (!rb) rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = projGravityScale;
            rb.velocity = Vector2.down * fallSpeed;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (alignToVelocity && !go.GetComponent<AlignToVelocity2D>())
                go.AddComponent<AlignToVelocity2D>();

            yield return new WaitForSeconds(interval);
        }
    }

    IEnumerator CooldownRoutine()
    {
        cooling = true;
        if (button) button.interactable = false;
        SetGrayscale(true);

        float t = 0f;
        if (cooldownFill) cooldownFill.fillAmount = 1f;
        if (cooldownText) { cooldownText.gameObject.SetActive(true); cooldownText.text = Mathf.Ceil(cooldown).ToString("0"); }

        // 타임스케일 0에서도 내려가게 UnscaledTime 사용
        while (t < cooldown)
        {
            t += Time.unscaledDeltaTime;
            float ratio = Mathf.Clamp01(t / cooldown);
            if (cooldownFill) cooldownFill.fillAmount = 1f - ratio;
            if (cooldownText) cooldownText.text = Mathf.Ceil(cooldown - t).ToString("0");
            yield return null;
        }

        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (cooldownText) cooldownText.gameObject.SetActive(false);
        SetGrayscale(false);
        if (button) button.interactable = true;
        cooling = false;
    }

    void SetGrayscale(bool on)
    {
        if (grayscaleImages == null) return;
        for (int i = 0; i < grayscaleImages.Length; i++)
        {
            var img = grayscaleImages[i];
            if (!img) continue;

            if (on)
            {
                if (grayscaleMat) img.material = grayscaleMat; // 정확한 그레이스케일(머티리얼 있을 때)
                img.color = disabledTint;                      // 없으면 색만 어둡게
            }
            else
            {
                img.material = (_origMats != null) ? _origMats[i] : null;
                if (_origColors != null) img.color = _origColors[i];
            }
        }
    }
}
