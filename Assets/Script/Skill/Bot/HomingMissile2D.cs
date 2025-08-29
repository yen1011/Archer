using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class HomingMissile2D : MonoBehaviour
{
    public Transform target;
    public Team2D team = Team2D.Bot;
    public LayerMask hitMask;             // Hurtbox
    public float speed = 10f;
    public float turnRateDeg = 360f;      // 초당 회전 가능한 각도
    public float life = 5f;
    public int damage = 15;
    public GameObject hitVFX;

    Rigidbody2D rb;
    float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= life) { Destroy(gameObject); return; }

        Vector2 vel = rb.velocity;
        if (vel.sqrMagnitude < 0.0001f) vel = transform.right * speed;

        if (target)
        {
            Vector2 to = (Vector2)(target.position - transform.position);
            // 현재 각도 → 목표 각도로 부드럽게 회전
            float curDeg = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
            float tarDeg = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
            float nextDeg = Mathf.MoveTowardsAngle(curDeg, tarDeg, turnRateDeg * Time.deltaTime);
            Vector2 newDir = new Vector2(Mathf.Cos(nextDeg * Mathf.Deg2Rad), Mathf.Sin(nextDeg * Mathf.Deg2Rad));

            rb.velocity = newDir * speed;
            transform.rotation = Quaternion.Euler(0, 0, nextDeg);
        }
        else
        {
            rb.velocity = vel.normalized * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((hitMask.value & (1 << other.gameObject.layer)) == 0) return;
        var h = other.GetComponentInParent<Health>();
        if (!h || h.team == team) return;

        h.TakeDamageAt(damage, other.ClosestPoint(transform.position));
        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
