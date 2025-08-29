using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealPickup2D : MonoBehaviour
{
    [Header("Heal Rules")]
    public int healAmount = 20;
    public int maxHeals = 1;
    public bool consumeProjectile = true;

    [Header("Lifetime / Despawn")]
    public float killBelowY = -4f;
    public float lifeTime = 0f;

    [Header("Auto Fall")]
    public bool autoFall = true;
    public float fallSpeed = 2.5f;

    [Header("Optional FX")]
    public GameObject hitVFX;
    public GameObject vanishVFX;

    [Header("Heal Popup")]
    public GameObject healPopupPrefab;
    public Vector3 healPopupOffset = new Vector3(0f, 0.6f, 0f);
    public Color healPopupColor = new Color(0.3f, 0.8f, 1f);

    [Header("References (optional)")]
    public Health playerHealth;
    public Health botHealth;

    [Header("Physics Filter")]
    public LayerMask projectileLayers = ~0;

    int healsGiven;
    float lifeTimer;
    bool consumed;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (!playerHealth || !botHealth)
        {
            foreach (var h in FindObjectsOfType<Health>())
            {
                if (h.team == Team2D.Player && !playerHealth) playerHealth = h;
                else if (h.team == Team2D.Bot && !botHealth) botHealth = h;
            }
        }
    }

    void Update()
    {
        if (autoFall) transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

        if (transform.position.y <= killBelowY)
        { if (vanishVFX) Instantiate(vanishVFX, transform.position, Quaternion.identity); Destroy(gameObject); return; }

        if (lifeTime > 0f)
        {
            lifeTimer += Time.deltaTime;
            if (lifeTimer >= lifeTime)
            { if (vanishVFX) Instantiate(vanishVFX, transform.position, Quaternion.identity); Destroy(gameObject); }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((projectileLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var proj = other.GetComponent<ProjectileHit>();
        if (!proj) return;

        Health target = (proj.team == Team2D.Player) ? playerHealth : botHealth;
        if (!target) return;

        // 회복
        target.Heal(healAmount);

        // ★ 팝업을 '사과'가 아니라 '회복된 캐릭터' 위에 표시
        if (healPopupPrefab)
        {
            Vector3 pos = GetPopupPosFor(target); // ← 아래 헬퍼 사용
            DamagePopup.SpawnHeal(healPopupPrefab, pos, healAmount, healPopupColor);
        }

        if (hitVFX)
        {
            // 맞춘 투사체와 사과의 중간 정도에 연출하고 싶으면 위치 조정 가능
            Vector3 hitPos = other.ClosestPoint(transform.position);
            Instantiate(hitVFX, hitPos, Quaternion.identity);
        }

        if (consumeProjectile) Destroy(other.gameObject);
        if (vanishVFX) Instantiate(vanishVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    // ====== 헬퍼: 대상의 콜라이더 위쪽을 기준으로 머리 위 좌표 계산 ======
    [SerializeField] Vector3 healPopupOffsetOnTarget = new Vector3(0f, 0.6f, 0f);
    Vector3 GetPopupPosFor(Health t)
    {
        // 대상(플레이어/봇)에서 아무 콜라이더나 찾아서 bounds를 사용
        var col = t.GetComponentInChildren<Collider2D>();
        if (col)
        {
            var b = col.bounds;
            return new Vector3(b.center.x, b.max.y, 0f) + healPopupOffsetOnTarget;
        }
        // 콜라이더가 없다면 Transform 기준으로 오프셋만
        return t.transform.position + healPopupOffsetOnTarget;
    }

}
