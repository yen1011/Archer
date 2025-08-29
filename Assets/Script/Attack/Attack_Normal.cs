using UnityEngine;

public class Attack_Normal : MonoBehaviour
{
    public Animator animator;
    public Transform model;
    public Transform firePoint;
    public GameObject projectilePrefab;

    [Header("Ballistics")]
    [Range(0f, 80f)] public float launchAngleDeg = 25f;
    public float projectileSpeed = 12f;
    public float projectileGravityScale = 2f;
    public bool rotateArrowToVelocity = true;

    int cachedDirX = 1; // +1=오른쪽, -1=왼쪽 (공격 시작 시 고정)

    // 공격 모션 시작 프레임에 호출 (애니메이션 이벤트)
    public void AnimEvent_AttackStart()
    {
        cachedDirX = (model && model.eulerAngles.y > 90f) ? 1 : -1;
    }

    // 활시위 놓는 프레임에 호출 (애니메이션 이벤트)
    public void AnimEvent_Fire()
    {
        SpawnProjectile(cachedDirX);
    }

    void SpawnProjectile(int dirX)
    {
        if (!projectilePrefab || !firePoint) return;

        float rad = launchAngleDeg * Mathf.Deg2Rad;
        Vector2 v0 = new Vector2(Mathf.Cos(rad) * dirX, Mathf.Sin(rad)) * projectileSpeed;

        var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        var rb2d = go.GetComponent<Rigidbody2D>();
        if (!rb2d) rb2d = go.AddComponent<Rigidbody2D>();
        rb2d.gravityScale = (projectileGravityScale <= 0f) ? 1f : projectileGravityScale;
        rb2d.velocity = v0;
        rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (rotateArrowToVelocity && !go.GetComponent<AlignToVelocity2D>())
            go.AddComponent<AlignToVelocity2D>();
    }
}
