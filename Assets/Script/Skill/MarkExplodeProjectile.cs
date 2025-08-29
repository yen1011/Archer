using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MarkExplodeProjectile : MonoBehaviour
{
    [Header("Team / Targeting")]
    public Team2D team = Team2D.Player;

    [Header("Masks")]
    public LayerMask hurtboxMask;         // 적 피격(Hurtbox)
    public LayerMask groundMask;          // 바닥/벽 등 환경

    [Header("Mark & Explode")]
    public float detonateDelay = 2f;      // 표식 유지 시간
    public int explodeDamage = 40;      // 폭발 데미지
    public float aoeRadius = 1.2f;        // 범위(0이면 단일 대상)
    public GameObject markVFX;            // 표식 아이콘 프리팹(허트박스/바닥 모두 부착)
    public Vector3 markOffset = new Vector3(0f, 0.4f, 0f);      // 허트박스용 오프셋
    public Vector3 groundMarkOffset = new Vector3(0f, 0.2f, 0f); // 바닥용 오프셋 ★추가
    public GameObject explosionVFX;       // 폭발 이펙트

    [Header("Ballistics")]
    public bool useBallistic = true;      // 포물선 사용
    [Range(0f, 80f)] public float launchAngleDeg = 30f; // 발사각
    public float initialSpeed = 14f;      // 초기 속도
    public float gravityScale = 2f;       // 중력 스케일(포물선용)
    public Vector2 extraVelocity;         // 추가 속도(선택)
    public bool rotateToVelocity = true;  // 속도 방향으로 회전

    Rigidbody2D rb;
    Collider2D col;

    bool consumed;                        // 한 번 꽂히면 다시는 판정 안 함
    bool stuck;                           // 꽂힌 상태인지
    Transform stuckParent;                // 꽂힌 대상(허트박스/바닥)
    Vector3 stuckLocalPos;
    Quaternion stuckLocalRot;
    Health stuckHealth;                   // 허트박스였을 때 저장
    GameObject markVfxInst;               // 생성한 Mark VFX 인스턴스

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    public void FireFrom(Transform firePoint, int dirX)
    {
        transform.position = firePoint.position;

        if (rb)
        {
            if (useBallistic)
            {
                rb.gravityScale = gravityScale;
                float rad = launchAngleDeg * Mathf.Deg2Rad;
                Vector2 v0 = new Vector2(Mathf.Cos(rad) * dirX, Mathf.Sin(rad)) * initialSpeed;
                rb.velocity = v0 + extraVelocity;
            }
            else
            {
                rb.gravityScale = 0f;
                rb.velocity = new Vector2(initialSpeed * dirX, 0f) + extraVelocity;
            }
        }

        // 초기 방향 정렬
        if (rotateToVelocity && rb)
        {
            var v = rb.velocity;
            if (v.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, ang);
            }
        }
    }

    void Update()
    {
        // 비행 중에는 속도 방향으로 계속 회전
        if (!stuck && rotateToVelocity && rb)
        {
            var v = rb.velocity;
            if (v.sqrMagnitude > 0.0001f)
            {
                float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0, 0, ang);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryStick(other, other.ClosestPoint(transform.position));
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        TryStick(c.collider, (c.contactCount > 0) ? c.GetContact(0).point : (Vector2)transform.position);
    }

    void TryStick(Collider2D other, Vector2 hitPoint)
    {
        if (consumed) return;

        int bit = 1 << other.gameObject.layer;
        bool isHurt = (hurtboxMask.value & bit) != 0;
        bool isGround = (groundMask.value & bit) != 0;
        if (!isHurt && !isGround) return;

        // 아군 허트박스면 무시
        if (isHurt)
        {
            var h = other.GetComponentInParent<Health>();
            if (!h || h.team == team) return;
            stuckHealth = h;
        }

        // 꽂힘 처리
        consumed = true;
        stuck = true;
        stuckParent = other.transform;

        if (rb) { rb.velocity = Vector2.zero; rb.isKinematic = true; rb.gravityScale = 0f; }
        if (col) col.enabled = false;

        transform.position = hitPoint;
        stuckLocalRot = Quaternion.Inverse(stuckParent.rotation) * transform.rotation;
        stuckLocalPos = stuckParent.InverseTransformPoint(transform.position);
        transform.SetParent(stuckParent, true);

        // 허트박스/바닥 모두 마크 표시 (오프셋만 다르게)
        if (markVFX)
        {
            markVfxInst = Instantiate(markVFX, stuckParent);
            markVfxInst.transform.localPosition = stuckLocalPos + (isHurt ? markOffset : groundMarkOffset);
        }

        StartCoroutine(DetonateAfterDelay());
    }

    IEnumerator DetonateAfterDelay()
    {
        float t = 0f;
        while (t < detonateDelay)
        {
            // 대상이 죽으면 바로 폭발
            if (stuckHealth && stuckHealth.HP <= 0) break;

            // 꽂힌 자리 유지
            if (stuckParent)
            {
                transform.localPosition = stuckLocalPos;
                transform.localRotation = stuckLocalRot;
            }
            t += Time.deltaTime;
            yield return null;
        }
        Explode();
    }

    void Explode()
    {
        Vector3 pos = transform.position;

        if (explosionVFX) Instantiate(explosionVFX, pos, Quaternion.identity);
        if (markVfxInst) Destroy(markVfxInst); // 폭발 시 마크 제거

        if (aoeRadius > 0.01f)
        {
            // 범위 피해: 주변 허트박스만 데미지
            var hits = Physics2D.OverlapCircleAll(pos, aoeRadius, hurtboxMask);
            foreach (var hit in hits)
            {
                var h = hit.GetComponentInParent<Health>();
                if (!h || h.team == team) continue;
                h.TakeDamageAt(explodeDamage, hit.ClosestPoint(pos));
            }
        }
        else
        {
            // 단일 대상
            if (stuckHealth && stuckHealth.team != team)
                stuckHealth.TakeDamageAt(explodeDamage, pos);
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (aoeRadius > 0.01f)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, aoeRadius);
        }
    }
#endif
}
