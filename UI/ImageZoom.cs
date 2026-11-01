using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ============================================================================
/// 檔案名稱: ImageZoom.cs
/// 所屬模組: UI
/// 責任描述: 負責神社歷史圖片之雙指縮放 (Pinch-to-Zoom)、滑鼠滾輪縮放與拖曳平移限制。
/// 架構分層: UI / Interaction Component
/// 聯動關係:
///   - 上游依賴: UnityEngine.EventSystems (觸控與游標事件介面)
///   - 下游消費: 掛載於歷史全景照片之 RectTransform 上
/// 邊界處理: 限制最小/最大縮放比例，在拖曳時限制視圖不超出父容器邊界 (Clamp Bounds)。
/// ----------------------------------------------------------------------------
/// 【Unity 場景掛載指引】
/// 1. 掛載物件: 掛載在需要縮放拖曳的歷史圖片 UI 物件上 (例如 HistoryPanel/ScrollArea/HistoricalPhoto)。
/// 2. 父容器需求: 其父層物件必須具備 RectTransform (例如 Mask 遮罩區域)，以供計算平移邊界。
/// 3. 相依性: 自動依賴 [RequireComponent(typeof(RectTransform))]。
/// 4. Inspector 參數配置:
///    - Zoom Speed: 縮放靈敏度 (預設 0.1)。
///    - Min Scale: 最小縮放倍率 (預設 0.5)。
///    - Max Scale: 最大放大倍率 (預設 3.0)。
///    - Lerp Speed: 游標放開後自動回彈預設大小之平滑插值速度 (預設 5.0)。
/// ============================================================================
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ImageZoom : MonoBehaviour,
    IScrollHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IDragHandler
{
    [Header("Zoom Sensitivity & Boundary Limits")]
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 3.0f;
    [SerializeField] private float lerpSpeed = 5.0f;

    private RectTransform rectTransform;
    private RectTransform parentRectTransform;
    private bool isPointerOver = false;
    private bool isTouching = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (transform.parent != null)
        {
            parentRectTransform = transform.parent.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        HandleTouchZoom();
        HandleReturnScale();
    }

    /// <summary>
    /// 處理行動裝置之雙指捏合縮放 (Pinch to Zoom)
    /// </summary>
    private void HandleTouchZoom()
    {
        if (Input.touchCount == 2)
        {
            isTouching = true;
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 prevPosZero = touchZero.position - touchZero.deltaPosition;
            Vector2 prevPosOne = touchOne.position - touchOne.deltaPosition;

            float prevDistance = (prevPosZero - prevPosOne).magnitude;
            float currentDistance = (touchZero.position - touchOne.position).magnitude;

            float delta = currentDistance - prevDistance;
            ApplyZoom(delta * zoomSpeed * 0.01f);
        }
        else
        {
            isTouching = false;
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (isPointerOver)
        {
            float scroll = eventData.scrollDelta.y;
            ApplyZoom(scroll * zoomSpeed);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += eventData.delta;
            ClampPosition();
        }
    }

    /// <summary>
    /// 套用縮放增量並限制縮放區間
    /// </summary>
    /// <param name="delta">縮放增量數值</param>
    private void ApplyZoom(float delta)
    {
        if (rectTransform == null) return;

        Vector3 targetScale = rectTransform.localScale + Vector3.one * delta;
        targetScale.x = Mathf.Clamp(targetScale.x, minScale, maxScale);
        targetScale.y = Mathf.Clamp(targetScale.y, minScale, maxScale);
        targetScale.z = 1f;

        rectTransform.localScale = targetScale;
        ClampPosition();
    }

    /// <summary>
    /// 當游標與觸碰離開且縮放小於 1 時，平滑回彈至預設大小
    /// </summary>
    private void HandleReturnScale()
    {
        if (!isPointerOver && !isTouching && rectTransform != null)
        {
            if (rectTransform.localScale.x < 1.0f)
            {
                rectTransform.localScale = Vector3.Lerp(
                    rectTransform.localScale,
                    Vector3.one,
                    Time.deltaTime * lerpSpeed
                );
            }
        }
    }

    /// <summary>
    /// 將圖片平移位置限制於父容器範圍內，避免圖片拖出視野外
    /// </summary>
    private void ClampPosition()
    {
        if (rectTransform == null || parentRectTransform == null) return;

        Vector3 pos = rectTransform.anchoredPosition;

        Vector2 parentSize = parentRectTransform.rect.size;
        Vector2 imageSize = rectTransform.rect.size * rectTransform.localScale.x;

        float minX = (parentSize.x - imageSize.x) * 0.5f;
        float maxX = (imageSize.x - parentSize.x) * 0.5f;
        float minY = (parentSize.y - imageSize.y) * 0.5f;
        float maxY = (imageSize.y - parentSize.y) * 0.5f;

        if (imageSize.x > parentSize.x)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
        }
        else
        {
            pos.x = 0f;
        }

        if (imageSize.y > parentSize.y)
        {
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
        }
        else
        {
            pos.y = 0f;
        }

        rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// 重置縮放與位置至初始狀態
    /// </summary>
    public void ResetZoomAndPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
