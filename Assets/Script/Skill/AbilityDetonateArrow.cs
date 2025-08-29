using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityDetonateArrow : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public Image cooldownFill;         // Filled Image
    public TMP_Text cooldownText;

    [Header("Grayscale")]
    public Image[] grayscaleImages;
    public Material grayscaleMat;      // 선택
    public Color disabledTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Refs")]
    public Transform firePoint;        // 플레이어 활 끝
    public Transform model;            // 좌/우 판정(Y=180 → 오른쪽)
    public MarkExplodeProjectile projectilePrefab;

    [Header("Config")]
    public Team2D team = Team2D.Player;
    public LayerMask hurtboxMask;      // ← Hurtbox만 체크
    public LayerMask groundMask;       // ← Ground/Default 등 바닥/벽
    public float cooldown = 8f;

    bool cooling;
    Color[] _origColors; Material[] _origMats;

    void Awake()
    {
        if (button) button.onClick.AddListener(Activate);
        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (cooldownText) cooldownText.gameObject.SetActive(false);

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
        if (cooling || !projectilePrefab || !firePoint) return;

        int dirX = (model && model.eulerAngles.y > 90f) ? 1 : -1;

        var proj = Instantiate(projectilePrefab);
        proj.team = team;
        proj.hurtboxMask = hurtboxMask;   // ★ 바뀐 필드명
        proj.groundMask = groundMask;    // ★ 바뀐 필드명
        proj.FireFrom(firePoint, dirX);

        StartCoroutine(CooldownRoutine());
    }

    IEnumerator CooldownRoutine()
    {
        cooling = true;
        if (button) button.interactable = false;
        SetGray(true);

        float t = 0f;
        if (cooldownFill) cooldownFill.fillAmount = 1f;
        if (cooldownText) { cooldownText.gameObject.SetActive(true); cooldownText.text = Mathf.Ceil(cooldown).ToString("0"); }

        while (t < cooldown)
        {
            t += Time.unscaledDeltaTime;
            float r = Mathf.Clamp01(t / cooldown);
            if (cooldownFill) cooldownFill.fillAmount = 1f - r;
            if (cooldownText) cooldownText.text = Mathf.Ceil(cooldown - t).ToString("0");
            yield return null;
        }

        if (cooldownFill) cooldownFill.fillAmount = 0f;
        if (cooldownText) cooldownText.gameObject.SetActive(false);
        SetGray(false);
        if (button) button.interactable = true;
        cooling = false;
    }

    void SetGray(bool on)
    {
        if (grayscaleImages == null) return;
        for (int i = 0; i < grayscaleImages.Length; i++)
        {
            var img = grayscaleImages[i];
            if (!img) continue;

            if (on)
            {
                if (grayscaleMat) img.material = grayscaleMat;
                img.color = disabledTint;
            }
            else
            {
                img.material = (_origMats != null) ? _origMats[i] : null;
                if (_origColors != null) img.color = _origColors[i];
            }
        }
    }
}
