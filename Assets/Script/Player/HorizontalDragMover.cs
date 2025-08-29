using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody2D))]
public class HorizontalDragMover : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 3f;
    public float leftBound = -8f;
    public float rightBound = -3.5f;
    public float moveDeadZone = 0.02f;

    [Header("Visuals")]
    public Animator animator;
    public SpriteRenderer sprite;
    public Transform model;
    public bool artFacesRightByDefault = false;

    // ───────── 버튼 이동(가상 패드) ─────────
    [Header("Virtual Buttons (optional)")]
    public bool useButtons = true;
    int btnDir;

    Rigidbody2D rb;
    Camera cam;
    bool dragging;
    float targetX;
    Vector3 modelBaseScale;
    Vector3 modelBaseEuler;

    bool uiCaptured = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        targetX = transform.position.x;

        if (!model) model = transform;
        modelBaseScale = model.localScale;
        modelBaseEuler = model.localEulerAngles;
    }

    void Start()
    {
        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.SetBool("IsMoving", false);
            animator.SetFloat("Speed", 0f);
        }
    }

    void Update()
    {
        // 터치/마우스 드래그 이동
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
                uiCaptured = IsPointerOverUI(t.fingerId);

            bool ignore = uiCaptured || IsPointerOverUI(t.fingerId);
            HandlePointer(t.phase == TouchPhase.Began,
                          t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled,
                          t.position,
                          ignore);

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                uiCaptured = false;
        }
        else
        {
            bool down = Input.GetMouseButtonDown(0);
            bool up = Input.GetMouseButtonUp(0);

            if (down) uiCaptured = IsPointerOverUI();
            bool ignore = uiCaptured || IsPointerOverUI();

            if (down || Input.GetMouseButton(0) || up)
                HandlePointer(down, up, Input.mousePosition, ignore);

            if (up) uiCaptured = false;
        }

        // 애니메이션/좌우 반전
        float vx = rb.velocity.x;
        bool movingHoriz = Mathf.Abs(vx) > 0.01f;
        bool isActuallyMoving =
            (btnDir != 0) || (dragging && Mathf.Abs(targetX - transform.position.x) > moveDeadZone);

        if (animator)
        {
            animator.SetBool("IsMoving", isActuallyMoving);
            animator.SetFloat("Speed", isActuallyMoving ? Mathf.Abs(vx) : 0f);
        }

        if (movingHoriz)
        {
            float targetYaw = (vx < 0f) ? 0f : 180f;
            var e = modelBaseEuler; e.y = targetYaw;
            model.localEulerAngles = e;
        }
        else
        {
            var e = modelBaseEuler; e.y = 180f;
            model.localEulerAngles = e;
            if (sprite) sprite.flipX = true;
        }
    }

    void HandlePointer(bool down, bool up, Vector2 screenPos, bool ignoreInput)
    {
        // UI 클릭 시 이동 방지
        if (ignoreInput)
        {
            if (up)
            {
                dragging = false;
                rb.velocity = Vector2.zero;
                targetX = transform.position.x;
            }
            return;
        }

        if (down) dragging = true;

        if (dragging)
        {
            var playerScreen = cam.WorldToScreenPoint(transform.position);
            var sp = new Vector3(screenPos.x, playerScreen.y, playerScreen.z);
            var world = cam.ScreenToWorldPoint(sp);
            targetX = Mathf.Clamp(world.x, leftBound, rightBound);
        }

        if (up)
        {
            dragging = false;
            rb.velocity = Vector2.zero;
            targetX = transform.position.x;
            if (animator)
            {
                animator.SetBool("IsMoving", false);
                animator.SetFloat("Speed", 0f);
            }
            var e = modelBaseEuler; e.y = 180f;
            model.localEulerAngles = e;
            if (sprite) sprite.flipX = true;
        }
    }

    void FixedUpdate()
    {
        // 버튼 이동 우선
        if (useButtons && btnDir != 0)
        {
            float nextX = Mathf.Clamp(
                transform.position.x + btnDir * moveSpeed * Time.fixedDeltaTime,
                leftBound, rightBound);

            float vx = (nextX - transform.position.x) / Time.fixedDeltaTime;
            rb.velocity = new Vector2(vx, 0f);

            targetX = nextX;
            return;
        }

        // 드래그 이동
        float next = Mathf.MoveTowards(transform.position.x, targetX, moveSpeed * Time.fixedDeltaTime);
        float v = (next - transform.position.x) / Time.fixedDeltaTime;
        rb.velocity = new Vector2(v, 0f);
    }

    // ───────── 버튼에서 호출할 메서드 ─────────
    public void OnLeftDown() { btnDir = -1; if (animator) animator.SetBool("IsMoving", true); }
    public void OnLeftUp() { if (btnDir == -1) btnDir = 0; StopIfIdle(); }
    public void OnRightDown() { btnDir = +1; if (animator) animator.SetBool("IsMoving", true); }
    public void OnRightUp() { if (btnDir == +1) btnDir = 0; StopIfIdle(); }

    void StopIfIdle()
    {
        if (btnDir == 0 && !dragging)
        {
            rb.velocity = Vector2.zero;
            targetX = transform.position.x;
            if (animator) animator.SetBool("IsMoving", false);
        }
    }

    bool IsPointerOverUI(int fingerId = -1)
    {
        if (EventSystem.current == null) return false;
        return (fingerId >= 0)
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
