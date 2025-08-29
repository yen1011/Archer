using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ProjectileHit : MonoBehaviour
{
    public Team2D team = Team2D.Player;
    public int damage = 10;
    public bool destroyOnHit = true;
    public LayerMask hitMask = ~0;

    // ★ 같은 투사체로 중복 타격 방지
    bool consumed;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 p = other.ClosestPoint(transform.position);
        TryHit(other, p);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        Vector2 p = (col.contactCount > 0) ? col.GetContact(0).point : (Vector2)transform.position;
        TryHit(col.collider, p);
    }

    void TryHit(Collider2D other, Vector2 hitPoint)
    {
        if (consumed) return; // ★ 이미 맞췄으면 무시

        // 레이어 필터: Hurtbox만 허용
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;

        var h = other.GetComponentInParent<Health>();
        if (!h || h.team == team) return;

        consumed = true; // ★ 더 이상 타격하지 않음
        h.TakeDamageAt(damage, hitPoint);

        if (destroyOnHit) Destroy(gameObject);
    }
}
