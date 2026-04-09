using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스가 화면 끝에 위치하면 카메라(또는 지정한 타겟)를 해당 방향으로 이동시킵니다.
/// 이동 가능한 범위(Bounds)를 Inspector에서 설정할 수 있습니다.
/// </summary>
public class EdgeScrollCamera : MonoBehaviour
{
    [Header("스크롤 설정")]
    [Tooltip("화면 가장자리로 인식할 픽셀 두께")]
    public float edgeThickness = 20f;

    [Tooltip("카메라 이동 속도")]
    public float scrollSpeed = 5f;

    [Header("이동 범위 제한 (월드 좌표)")]
    [Tooltip("카메라가 이동할 수 있는 최소 X 좌표")]
    public float minX = -10f;
    [Tooltip("카메라가 이동할 수 있는 최대 X 좌표")]
    public float maxX = 10f;
    [Tooltip("카메라가 이동할 수 있는 최소 Y 좌표")]
    public float minY = -5f;
    [Tooltip("카메라가 이동할 수 있는 최대 Y 좌표")]
    public float maxY = 5f;

    [Header("옵션")]
    [Tooltip("게임 실행 중에도 씬 뷰에서 기즈모로 범위 표시")]
    public bool showBoundsGizmo = true;

    private Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
        if (_cam == null)
            _cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleEdgeScroll();
    }

    void HandleEdgeScroll()
    {

        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        int screenW = Screen.width;
        int screenH = Screen.height;

        Vector2 move = Vector2.zero;

        // 왼쪽 가장자리
        if (mousePos.x >= 0 && mousePos.x < edgeThickness)
            move.x -= 1f;
        // 오른쪽 가장자리
        else if (mousePos.x <= screenW && mousePos.x > screenW - edgeThickness)
            move.x += 1f;

        // 아래쪽 가장자리
        if (mousePos.y >= 0 && mousePos.y < edgeThickness)
            move.y -= 1f;
        // 위쪽 가장자리
        else if (mousePos.y <= screenH && mousePos.y > screenH - edgeThickness)
            move.y += 1f;

        if (move == Vector2.zero) return;

        // 대각선 이동 속도 정규화
        if (move.magnitude > 1f)
            move.Normalize();

        float newPosX = _cam.transform.position.x + move.x * scrollSpeed * Time.deltaTime;
        float newPosY = _cam.transform.position.y + move.y * scrollSpeed * Time.deltaTime;

        // 범위 클램프
        newPosX = Mathf.Clamp(newPosX, minX, maxX);
        newPosY = Mathf.Clamp(newPosY, minY, maxY);
        //newPos.z = _cam.transform.position.z; // Z 고정 (2D)

        _cam.transform.position = new Vector2(newPosX, newPosY);
    }

    void OnDrawGizmos()
    {
        if (!showBoundsGizmo) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);
        Vector3 size   = new Vector3(maxX - minX, maxY - minY, 0.1f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.08f);
        Gizmos.DrawCube(center, size);
    }
}
