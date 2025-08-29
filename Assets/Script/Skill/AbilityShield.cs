using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityShield : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public Image cooldownFill;          // Type=Filled 이미지
    public TMP_Text cooldownText;       // 남은 시간 숫자

    [Header("Grayscale")]
    public Image[] grayscaleImages;     // 아이콘/프레임 등 회색 처리 대상
    public Material grayscaleMat;       // (선택) 그레이스케일 머티리얼
    public Color disabledTint = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Header("Target & Shield")]
    public Health target;               // 보통 Player의 Health
    public float shieldDuration = 2.0f; // 시간형
    public int shieldAbsorb = 0;    // 흡수형(0이면 시간형만)
    public float cooldown = 8f;

    bool cooling;
    Color[] _origColors;
    Material[] _origMats;

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
        if (cooling || !target) return;

        target.ActivateShield(shieldDuration, shieldAbsorb);
        StartCoroutine(CooldownRoutine());
    }

    IEnumerator CooldownRoutine()
    {
        cooling = true;
        if (button) button.interactable = false;
        SetGrayscale(true);

        float t = 0f;
        if (cooldownFill) cooldownFill.fillAmount = 1f;
        if (cooldownText) { cooldownText.gameObject.SetActive(true); cooldownText.text = Mathf.Ceil(cooldown).ToString("0"); }

        while (t < cooldown)
        {
            t += Time.unscaledDeltaTime;             // 타임스케일 0에서도 진행
            float r = Mathf.Clamp01(t / cooldown);
            if (cooldownFill) cooldownFill.fillAmount = 1f - r;
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
