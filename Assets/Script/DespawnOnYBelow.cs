using UnityEngine;

public class DespawnOnYBelow : MonoBehaviour
{
    public float minY = -1f;      // 바닥보다 조금 아래 값
    public float maxLifetime = 6f; // 혹시 모를 누수 방지용(초)

    float dieAt;

    void OnEnable()
    {
        dieAt = Time.time + maxLifetime;
    }

    void Update()
    {
        if (transform.position.y < minY || Time.time >= dieAt)
            Destroy(gameObject);
    }
}
