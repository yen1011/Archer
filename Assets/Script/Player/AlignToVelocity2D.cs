using UnityEngine;

public class AlignToVelocity2D : MonoBehaviour
{
    Rigidbody2D rb;
    void Awake() { rb = GetComponent<Rigidbody2D>(); }
    void LateUpdate()
    {
        if (!rb) return;
        var v = rb.velocity;
        if (v.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
