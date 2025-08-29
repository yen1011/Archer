using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HorizontalDragMover : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 3f;          // 초당 이동 속도
    public float leftBound = -8f;         // X 최소
    public float rightBound = -3.5f;      // X 최대
    public float moveDeadZone = 0.02f;

    [Header("Visuals")]
    public Animator animator;
    public SpriteRenderer sprite;
    public Transform model;
    public bool artFacesRightByDefault = false;

    Rigidbody2D rb;
    Camera cam;
    bool dragging;
    float targetX;
    Vector3 modelBaseScale;
    Vector3 modelBaseEuler;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cam = Camera.main;

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        targetX = transform.position.x;

        if (model == null) model = transform;   // 모델 미지정 시 자기 자신을 사용
        modelBaseScale = model.localScale;
        modelBaseEuler = model.localEulerAngles; // 시작: 오른쪽(Y=180) 상태 그대로 기준
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
        // 입력 처리 (터치, 없으면 마우스)
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            HandlePointer(t.phase == TouchPhase.Began,
                          t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled,
                          t.position);
        }
        else
        {
            bool down = Input.GetMouseButtonDown(0);
            bool up = Input.GetMouseButtonUp(0);
            if (down || Input.GetMouseButton(0) || up)
                HandlePointer(down, up, Input.mousePosition);
        }

        // 애니메이션 & 좌우 반전
        float vx = rb.velocity.x;
        bool movingHoriz = Mathf.Abs(vx) > 0.01f;
        bool isActuallyMoving = dragging && Mathf.Abs(targetX - transform.position.x) > moveDeadZone;

        if (animator)
        {
            animator.SetBool("IsMoving", isActuallyMoving);
            animator.SetFloat("Speed", isActuallyMoving ? Mathf.Abs(vx) : 0f);
        }

        // 오른쪽 이동 시 오른쪽(Y=180), 왼쪽 이동 시 왼쪽(Y=0)
        if (movingHoriz)
        {
            float targetYaw = (vx < 0f) ? 0f : 180f;
            var e = modelBaseEuler; e.y = targetYaw;
            model.localEulerAngles = e;
        }
        else
        {
            // 정지 상태에서는 항상 오른쪽(Y=180)을 보게 유지
            var e = modelBaseEuler; e.y = 180f;
            model.localEulerAngles = e;

            if (sprite != null)
            {
                // 단일 스프라이트일 때 모양도 오른쪽을 보도록 강제(아트 기본이 왼쪽이라면 true)
                sprite.flipX = true; // 정지 시 항상 오른쪽
            }
        }
    }

    void HandlePointer(bool down, bool up, Vector2 screenPos)
    {
        if (down) dragging = true;

        if (dragging)
        {
            // 현재 Y 높이에 맞춰 손가락 X를 월드좌표로
            var playerScreen = cam.WorldToScreenPoint(transform.position);
            var sp = new Vector3(screenPos.x, playerScreen.y, playerScreen.z);
            var world = cam.ScreenToWorldPoint(sp);
            targetX = Mathf.Clamp(world.x, leftBound, rightBound);
        }

        if (up)
        {
            dragging = false;
            rb.velocity = Vector2.zero;           // 손 떼면 멈춤
            targetX = transform.position.x;
            if (animator)
            {
                animator.SetBool("IsMoving", false);
                animator.SetFloat("Speed", 0f);
            }

            // 손을 뗀 직후에도 바로 오른쪽을 보도록 보정
            var e = modelBaseEuler; e.y = 180f;
            model.localEulerAngles = e;
            if (sprite != null) sprite.flipX = true;
        }
    }

    void FixedUpdate()
    {
        float nextX = Mathf.MoveTowards(transform.position.x, targetX, moveSpeed * Time.fixedDeltaTime);
        float vx = (nextX - transform.position.x) / Time.fixedDeltaTime;
        rb.velocity = new Vector2(vx, 0f);
    }
}
