using System.Collections;
using UnityEngine;

public class BotPatternController : MonoBehaviour
{
    [Header("Refs")]
    public Animator animator;          // IsMoving/Attack 사용
    public Transform model;            // Y=180=오른쪽, Y=0=왼쪽
    public Rigidbody2D rb;
    public Attack_Normal attack;       // 기본공격(애니 이벤트)
    public Transform player;           // 타겟(플레이어)

    [Header("Move")]
    public float cellSize = 1f;
    public float moveSpeed = 3f;
    public float stopTolerance = 0.02f;
    public float pauseAfterMove = 0.1f;
    public bool faceLock = true;

    [Header("Pacing")]
    public float attackTail = 0.25f;       // 공격이 끝난 후 텀
    public float afterHomingPause = 0.4f;  // 유도탄 후 텀
    public float afterRainPause = 0.6f;    // 비 시전 후 텀

    // ─────────────────────────────────────────────
    // ▼ Ability #1 : Homing(유도탄)
    [Header("Ability - Homing")]
    public Transform homingFirePoint;
    public GameObject homingProjectilePrefab;
    public float homingWindup = 0.2f;
    public float homingTail = 0.3f;

    // ▼ Ability #2 : Rain(탄환 비)
    [Header("Ability - Rain")]
    public GameObject rainProjectilePrefab;
    public string castStateName = "casting";
    public float rainWindup = 0.35f;
    public int rainCount = 14;
    public float rainInterval = 0.05f;
    public float rainAreaHalfWidth = 2.5f;
    public float rainHeight = 5f;
    public float rainSpeedY = -12f;
    public bool rainUseGravity = false;
    public float rainTail = 0.2f;

    // ▶ Rain 데미지 인스펙터에서 조절하고 싶다면 사용
    public int rainDamage = 15; // 0 이면 프리팹 기본값 유지

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        model = transform;
        rb = GetComponent<Rigidbody2D>();
        attack = GetComponentInChildren<Attack_Normal>();
    }

    void OnEnable() { StartCoroutine(MainLoop()); }

    IEnumerator MainLoop()
    {
        while (true)
        {
            yield return AttackOnce();          // 1) 기본공격
            yield return MoveTiles(-2);         // 2) 뒤로 2칸
            yield return CastHoming(player);    // 3) 유도탄
            yield return MoveTiles(+1);         // 4) 앞으로 1칸
            yield return AttackOnce();          // 5) 기본공격
            yield return AttackOnce();          // 6) 기본공격
            yield return MoveTiles(+2);         // 7) 앞으로 2칸
            yield return CastRain(player);      // 8) 탄환 비
            yield return MoveTiles(-1);         // 9) 뒤로 1칸
        }
    }

    IEnumerator AttackOnce()
    {
        if (rb) rb.velocity = Vector2.zero;
        if (animator) animator.SetBool("IsMoving", false);

        // 항상 왼쪽을 보게(왼쪽으로 쏘게)
        if (model) model.localEulerAngles = Vector3.zero;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        int layer = 0;
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(layer).IsName("attack"));
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(layer).normalizedTime >= 0.99f);

        if (attackTail > 0f) yield return new WaitForSeconds(attackTail); // ★ 후딜 추가
    }

    IEnumerator MoveTiles(int tiles)
    {
        float forward = (model && model.eulerAngles.y > 90f) ? +1f : -1f;
        float dx = tiles * cellSize * forward;
        Vector3 target = transform.position + new Vector3(dx, 0f, 0f);

        if (animator) animator.SetBool("IsMoving", true);

        while (Mathf.Abs(target.x - transform.position.x) > stopTolerance)
        {
            float nextX = Mathf.MoveTowards(transform.position.x, target.x, moveSpeed * Time.fixedDeltaTime);
            if (rb)
            {
                float vx = (nextX - transform.position.x) / Time.fixedDeltaTime;
                rb.velocity = new Vector2(vx, 0f);
            }
            else
            {
                transform.position = new Vector3(nextX, transform.position.y, transform.position.z);
            }
            yield return new WaitForFixedUpdate();
        }

        if (rb) rb.velocity = Vector2.zero;
        if (animator) animator.SetBool("IsMoving", false);
        if (pauseAfterMove > 0f) yield return new WaitForSeconds(pauseAfterMove);
    }

    IEnumerator CastHoming(Transform target)
    {
        if (!homingProjectilePrefab || !homingFirePoint) yield break;

        if (animator) animator.SetTrigger("Attack");
        if (homingWindup > 0) yield return new WaitForSeconds(homingWindup);

        var go = Instantiate(homingProjectilePrefab, homingFirePoint.position, Quaternion.identity);

        var hm = go.GetComponent<HomingMissile2D>();
        if (hm)
        {
            hm.team = Team2D.Bot;
            hm.target = target;
        }
        else
        {
            var rb2 = go.GetComponent<Rigidbody2D>(); if (!rb2) rb2 = go.AddComponent<Rigidbody2D>();
            rb2.gravityScale = 0f;
            rb2.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb2.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb2.velocity = Vector2.left * 10f;
            var col = go.GetComponent<Collider2D>(); if (col) col.isTrigger = true;
        }

        if (homingTail > 0) yield return new WaitForSeconds(homingTail);
        if (afterHomingPause > 0) yield return new WaitForSeconds(afterHomingPause); // ★ 추가 텀
    }

    IEnumerator CastRain(Transform target)
    {
        if (!rainProjectilePrefab) yield break;

        // 캐스팅 재생
        if (rb) rb.velocity = Vector2.zero;
        if (animator) animator.SetBool("IsMoving", false);
        if (animator && !string.IsNullOrEmpty(castStateName))
            animator.Play(castStateName, 0, 0f);

        if (rainWindup > 0) yield return new WaitForSeconds(rainWindup);

        float baseX = (target ? target.position.x : transform.position.x);
        float topY = (target ? target.position.y : transform.position.y) + rainHeight;

        for (int i = 0; i < rainCount; i++)
        {
            float x = baseX + Random.Range(-rainAreaHalfWidth, rainAreaHalfWidth);
            Vector3 pos = new Vector3(x, topY, 0f);

            var go = Instantiate(rainProjectilePrefab, pos, Quaternion.identity);

            // 팀/데미지 오버라이드
            var ph = go.GetComponent<ProjectileHit>();
            if (ph)
            {
                ph.team = Team2D.Bot;
                if (rainDamage > 0) ph.damage = rainDamage;     // ★ 인스펙터로 조절
            }

            var rb2 = go.GetComponent<Rigidbody2D>(); if (!rb2) rb2 = go.AddComponent<Rigidbody2D>();
            rb2.gravityScale = rainUseGravity ? 1f : 0f;
            rb2.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb2.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb2.velocity = new Vector2(0f, rainSpeedY);

            var col = go.GetComponent<Collider2D>(); if (col) col.isTrigger = true;

            if (!go.GetComponent<AlignToVelocity2D>()) go.AddComponent<AlignToVelocity2D>();

            if (rainInterval > 0f) yield return new WaitForSeconds(rainInterval);
        }

        if (rainTail > 0) yield return new WaitForSeconds(rainTail);
        if (afterRainPause > 0) yield return new WaitForSeconds(afterRainPause); // ★ 추가 텀
    }
}
