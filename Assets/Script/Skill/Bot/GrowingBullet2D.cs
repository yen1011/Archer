using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GrowingBullet2D : MonoBehaviour
{
    public Team2D team = Team2D.Bot;
    public LayerMask hitMask;          // Hurtbox
    public float speed = 10f;
    public float gravityScale = 0f;
    public Vector3 startScale = new Vector3(0.6f, 0.6f, 1f);
    public Vector3 maxScale = new Vector3(1.8f, 1.8f, 1f);
    public float growTime = 0.8f;      // 이 시간 동안 start → max
    public int damage = 20;
    public GameObject hitVFX;

    Rigidbody2D rb;
    float t;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetComponent<Collider2D>().isTrigger = true;
        transform.localScale = startScale;
    }

    public void Fire(int dirX)
    {
        rb.velocity = new Vector2(speed * dirX, rb.velocity.y);
    }

    void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / growTime);
        transform.localScale = Vector3.Lerp(startScale, maxScale, k);
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
