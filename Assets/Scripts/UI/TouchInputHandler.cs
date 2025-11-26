using UnityEngine;
using UnityEngine.EventSystems;

namespace CastleWars.UI
{
    /// <summary>
    /// 触摸输入处理器 - 处理移动端的触摸操作
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("相机")]
        [SerializeField] private Camera mainCamera;

        [Header("相机控制")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float zoomSpeed = 0.5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;

        [Header("边界")]
        [SerializeField] private float minX = -30f;
        [SerializeField] private float maxX = 30f;

        private Vector3 lastTouchPosition;
        private bool isDragging = false;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            HandleTouchInput();
        }

        private void HandleTouchInput()
        {
            // 处理触摸输入
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // 检查是否点击在UI上
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        lastTouchPosition = touch.position;
                        isDragging = true;
                        break;

                    case TouchPhase.Moved:
                        if (isDragging)
                        {
                            PanCamera(touch);
                        }
                        break;

                    case TouchPhase.Ended:
                        isDragging = false;
                        // 检测点击建筑槽位
                        DetectBuildingSlotTap(touch.position);
                        break;
                }
            }
            else if (Input.touchCount == 2)
            {
                // 双指缩放
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);

                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;

                float prevMagnitude = (touch1PrevPos - touch2PrevPos).magnitude;
                float currentMagnitude = (touch1.position - touch2.position).magnitude;

                float difference = currentMagnitude - prevMagnitude;

                ZoomCamera(difference * zoomSpeed * Time.deltaTime);
            }

            // PC端测试：鼠标控制
#if UNITY_EDITOR
            HandleMouseInput();
#endif
        }

        private void PanCamera(Touch touch)
        {
            Vector3 delta = mainCamera.ScreenToWorldPoint(touch.position) -
                           mainCamera.ScreenToWorldPoint(lastTouchPosition);

            Vector3 newPos = mainCamera.transform.position - new Vector3(delta.x, 0, 0);

            // 限制边界
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);

            mainCamera.transform.position = newPos;
            lastTouchPosition = touch.position;
        }

        private void ZoomCamera(float increment)
        {
            if (mainCamera.orthographic)
            {
                mainCamera.orthographicSize = Mathf.Clamp(
                    mainCamera.orthographicSize - increment,
                    minZoom,
                    maxZoom
                );
            }
            else
            {
                Vector3 pos = mainCamera.transform.position;
                pos.z = Mathf.Clamp(pos.z - increment, -maxZoom, -minZoom);
                mainCamera.transform.position = pos;
            }
        }

        private void DetectBuildingSlotTap(Vector2 screenPosition)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                BuildingSlot slot = hit.collider.GetComponent<BuildingSlot>();
                if (slot != null)
                {
                    slot.OnSlotClicked();
                }
            }
        }

        private void HandleMouseInput()
        {
            // 鼠标拖拽
            if (Input.GetMouseButtonDown(0))
            {
                lastTouchPosition = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButton(0))
            {
                if (isDragging && !EventSystem.current.IsPointerOverGameObject())
                {
                    Vector3 delta = mainCamera.ScreenToWorldPoint(Input.mousePosition) -
                                   mainCamera.ScreenToWorldPoint(lastTouchPosition);

                    Vector3 newPos = mainCamera.transform.position - new Vector3(delta.x, 0, 0);
                    newPos.x = Mathf.Clamp(newPos.x, minX, maxX);

                    mainCamera.transform.position = newPos;
                    lastTouchPosition = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;

                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    DetectBuildingSlotTap(Input.mousePosition);
                }
            }

            // 鼠标滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                ZoomCamera(scroll * 10f);
            }
        }
    }
}
