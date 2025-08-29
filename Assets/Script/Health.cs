using UnityEngine;
using System;
using TMPro;


public enum Team2D { Player, Bot }

public class Health : MonoBehaviour
{
    [Header("Team & HP")]
    public Team2D team = Team2D.Player;
    public int maxHP = 100;

    [SerializeField] private int hp;
    public int HP => hp;

    [Header("HP Bar (scale-x)")]
    public Transform hpLine;
    private Vector3 hpLineBaseScale;

    [Header("VFX")]
    public DamagePopup damagePopupPrefab;
    public Vector3 popupOffset = new Vector3(0f, 0.2f, 0f);

    [Header("Animation Triggers")]
    public Animator animator;
    public string hurtTrigger = "Hurt";
    public string deathTrigger = "Die";

    [Header("Death Options")]
    public bool disableCollidersOnDeath = true;
    public bool disableScriptsOnDeath = true;
    public bool destroyOnDeath = true;
    public float destroyDelay = 1.5f;
    public MonoBehaviour[] extraDisableOnDeath;

    [Header("Hit-Stun (optional)")]
    public bool useHitStun = false;
    public float hitStunDuration = 0.15f;
    public MonoBehaviour[] disableOnHurt;

    // ===== Shield =====
    [Header("Shield")]
    public bool useShield = true;          // 켜두면 사용 가능
    public float shieldRemain = 0f;        // 남은 지속시간(초). 0보다 크면 시간형 실드
    public int shieldHP = 0;             // 남은 흡수량(HP). >0이면 흡수형 실드
    public GameObject shieldVFX;           // 실드 이펙트 오브젝트(켜고/끄기만)
    public string shieldBoolParam = "";    // Animator Bool 파라미터명(있으면 사용)

    public event Action<Health> OnDied;

    [Header("HP Text (optional)")]
    public TMP_Text hpText;           // HP 숫자 출력용 (월드 TMP 또는 TMP UGUI)
    public bool showMaxInText = true; // "현재/최대" 표기
    public bool showPercent = false;  // 퍼센트로 표시 (정수%)
    public bool hideTextWhenFull = false; // 풀HP면 텍스트 숨김


    bool isDead;
    float lastHurtTime;

    void Awake()
    {
        hp = maxHP;
        if (hpLine) hpLineBaseScale = hpLine.localScale;
        UpdateHPBar();
        if (!animator) animator = GetComponentInChildren<Animator>(true);
    }

    void Update()
    {
        // 실드 시간 소모
        if (!isDead && useShield && shieldRemain > 0f)
        {
            shieldRemain -= Time.deltaTime;
            if (shieldRemain <= 0f && shieldHP <= 0) EndShield();
        }
    }

    public void ActivateShield(float duration, int absorbHP = 0)
    {
        if (!useShield || isDead) return;

        shieldRemain = Mathf.Max(shieldRemain, duration);
        shieldHP = Mathf.Max(shieldHP, absorbHP); // 이미 켜져 있으면 더 강한 값으로 갱신

        if (shieldVFX) shieldVFX.SetActive(true);
        if (animator && !string.IsNullOrEmpty(shieldBoolParam))
            animator.SetBool(shieldBoolParam, true);
    }

    void EndShield()
    {
        shieldRemain = 0f;
        shieldHP = 0;
        if (shieldVFX) shieldVFX.SetActive(false);
        if (animator && !string.IsNullOrEmpty(shieldBoolParam))
            animator.SetBool(shieldBoolParam, false);
    }

    bool ShieldActive => (useShield && (shieldRemain > 0f || shieldHP > 0));

    // ===== Damage =====
    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        // 실드 처리: 시간형은 전부 무효, 흡수형은 깎고 남으면 통과
        if (ShieldActive)
        {
            if (shieldHP > 0)
            {
                int left = shieldHP - dmg;
                shieldHP = Mathf.Max(0, left);
                if (left <= 0 && shieldRemain <= 0) EndShield(); // 둘 다 소진 시 종료
                if (left <= 0) dmg = -left; else dmg = 0;        // 남은 데미지만 적용
            }
            else
            {
                dmg = 0; // 시간형 실드는 전부 막음
            }

            if (dmg <= 0) { ShowBlockPopup(); return; }
        }

        int before = hp;
        hp = Mathf.Clamp(hp - Mathf.Max(0, dmg), 0, maxHP);
        UpdateHPBar();

        if (hp <= 0) { Die(); return; }
        PlayHurt();
    }

    public void TakeDamageAt(int dmg, Vector3 worldPos)
    {
        if (isDead) return;

        // 팝업은 나중에 상황 따라 띄움
        if (ShieldActive)
        {
            if (shieldHP > 0)
            {
                int left = shieldHP - dmg;
                shieldHP = Mathf.Max(0, left);
                if (left <= 0 && shieldRemain <= 0) EndShield();
                if (left <= 0) dmg = -left; else dmg = 0;
            }
            else
            {
                dmg = 0;
            }

            if (dmg <= 0)
            {
                ShowBlockPopup(worldPos);
                return;
            }
        }

        // 정상 피격
        hp = Mathf.Clamp(hp - Mathf.Max(0, dmg), 0, maxHP);
        UpdateHPBar();

        if (damagePopupPrefab)
        {
            var p = Instantiate(damagePopupPrefab, worldPos + popupOffset, Quaternion.identity);
            p.Setup(dmg, team);
        }

        if (hp <= 0) { Die(); return; }
        PlayHurt();
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        hp = Mathf.Clamp(hp + Mathf.Max(0, amount), 0, maxHP);
        UpdateHPBar();
    }

    private void UpdateHPBar()
    {
        if (hpLine)
        {
            float t = (maxHP > 0) ? (float)hp / maxHP : 0f;
            var s = hpLineBaseScale; s.x = hpLineBaseScale.x * t;
            hpLine.localScale = s;
        }

        if (hpText)
        {
            if (hideTextWhenFull && hp >= maxHP)
            {
                hpText.enabled = false;
            }
            else
            {
                hpText.enabled = true;

                if (showPercent)
                {
                    int pct = (maxHP > 0) ? Mathf.RoundToInt(100f * hp / maxHP) : 0;
                    hpText.text = pct + "%";
                }
                else
                {
                    hpText.text = showMaxInText ? $"{hp}/{maxHP}" : hp.ToString();
                }
            }
        }
    }


    void ShowBlockPopup()
    {
        ShowBlockPopup(transform.position);
    }
    void ShowBlockPopup(Vector3 pos)
    {
        if (!damagePopupPrefab) return;
        var popup = Instantiate(damagePopupPrefab, pos + popupOffset, Quaternion.identity);
        popup.Setup(0, team);
        if (popup.text) { popup.text.text = "BLOCK"; popup.text.color = new Color(0.6f, 0.9f, 1f, 1f); }
    }

    void PlayHurt()
    {
        if (Time.time - lastHurtTime < 0.05f) return;
        lastHurtTime = Time.time;

        if (animator && !string.IsNullOrEmpty(hurtTrigger))
            animator.SetTrigger(hurtTrigger);

        if (useHitStun && hitStunDuration > 0f)
            StartCoroutine(HitStunCoroutine(hitStunDuration));
    }

    System.Collections.IEnumerator HitStunCoroutine(float dur)
    {
        foreach (var mb in disableOnHurt) if (mb) mb.enabled = false;
        yield return new WaitForSeconds(dur);
        foreach (var mb in disableOnHurt) if (mb) mb.enabled = true;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        EndShield(); // 죽을 때 실드 종료

        if (animator && !string.IsNullOrEmpty(deathTrigger))
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.SetTrigger(deathTrigger);
        }

        if (disableCollidersOnDeath)
            foreach (var col in GetComponentsInChildren<Collider2D>(true)) col.enabled = false;

        if (disableScriptsOnDeath)
        {
            var mover = GetComponent<HorizontalDragMover>(); if (mover) mover.enabled = false;
            var botAI = GetComponent<BotPatternController>(); if (botAI) botAI.enabled = false;
            foreach (var atk in GetComponentsInChildren<Attack_Normal>(true)) atk.enabled = false;
            if (extraDisableOnDeath != null)
                foreach (var mb in extraDisableOnDeath) if (mb) mb.enabled = false;
        }

        OnDied?.Invoke(this);
        if (destroyOnDeath) Destroy(gameObject, destroyDelay);
    }
}
