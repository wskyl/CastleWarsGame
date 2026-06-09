using UnityEngine;
using UnityEngine.EventSystems;

namespace CastleWars.UI
{
    /// <summary>
    /// 触摸输入处理器
    /// 处理移动端触摸操作、相机控制、建筑槽位点击
    /// </summary>
    public class TouchInputHandler : MonoBehaviour
    {
        [Header("相机引用")]
        [Tooltip("主相机")]
        [SerializeField] private Camera mainCamera;

        [Header("相机平移")]
        [Tooltip("平移速度")]
        [SerializeField] private float panSpeed = 20f;

        [Tooltip("平移阻尼")]
        [SerializeField] private float panDamping = 5f;

        [Header("相机缩放")]
        [Tooltip("缩放速度")]
        [SerializeField] private float zoomSpeed = 0.5f;

        [Tooltip("最小缩放")]
        [SerializeField] private float minZoom = 5f;

        [Tooltip("最大缩放")]
        [SerializeField] private float maxZoom = 20f;

        [Header("边界限制")]
        [Tooltip("X轴最小值")]
        [SerializeField] private float minX = -30f;

        [Tooltip("X轴最大值")]
        [SerializeField] private float maxX = 30f;

        [Header("点击检测")]
        [Tooltip("点击检测的射线距离")]
        [SerializeField] private float raycastDistance = 100f;

        [Tooltip("可点击的图层")]
        [SerializeField] private LayerMask clickableLayer;

        // 触摸状态
        private Vector3 _lastTouchPosition;
        private bool _isDragging = false;
        private float _dragThreshold = 10f; // 拖拽阈值（像素）
        private Vector3 _touchStartPosition;

        // 相机惯性
        private Vector3 _velocity;

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
            ApplyCameraInertia();
        }

        #region 输入处理

        /// <summary>
        /// 处理触摸输入
        /// </summary>
        private void HandleTouchInput()
        {
            // 单指触摸
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // 检查是否点击在UI上
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    return;
                }

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnTouchBegan(touch.position);
                        break;

                    case TouchPhase.Moved:
                        OnTouchMoved(touch);
                        break;

                    case TouchPhase.Ended:
                        OnTouchEnded(touch.position);
                        break;

                    case TouchPhase.Canceled:
                        _isDragging = false;
                        break;
                }
            }
            // 双指缩放
            else if (Input.touchCount == 2)
            {
                HandlePinchZoom();
            }

            // PC端测试：鼠标控制
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseInput();
#endif
        }

        /// <summary>
        /// 触摸开始
        /// </summary>
        private void OnTouchBegan(Vector2 position)
        {
            _lastTouchPosition = position;
            _touchStartPosition = position;
            _isDragging = true;
            _velocity = Vector3.zero;
        }

        /// <summary>
        /// 触摸移动
        /// </summary>
        private void OnTouchMoved(Touch touch)
        {
            if (!_isDragging) return;

            // 计算拖拽距离
            float dragDistance = Vector2.Distance(touch.position, _touchStartPosition);

            // 超过阈值才执行相机平移
            if (dragDistance > _dragThreshold)
            {
                PanCamera(touch.position);
            }

            _lastTouchPosition = touch.position;
        }

        /// <summary>
        /// 触摸结束
        /// </summary>
        private void OnTouchEnded(Vector2 position)
        {
            _isDragging = false;

            // 如果移动距离小于阈值，视为点击
            float dragDistance = Vector2.Distance(position, _touchStartPosition);
            if (dragDistance < _dragThreshold)
            {
                DetectClick(position);
            }
        }

        /// <summary>
        /// 处理双指缩放
        /// </summary>
        private void HandlePinchZoom()
        {
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            // 计算前一帧的距离
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;

            float prevMagnitude = (touch1PrevPos - touch2PrevPos).magnitude;
            float currentMagnitude = (touch1.position - touch2.position).magnitude;

            float difference = currentMagnitude - prevMagnitude;

            ZoomCamera(difference * zoomSpeed * Time.deltaTime);
        }

        #endregion

        #region 鼠标输入（PC端）

        /// <summary>
        /// 处理鼠标输入
        /// </summary>
        private void HandleMouseInput()
        {
            // 检查UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 鼠标左键
            if (Input.GetMouseButtonDown(0))
            {
                _lastTouchPosition = Input.mousePosition;
                _touchStartPosition = Input.mousePosition;
                _isDragging = true;
                _velocity = Vector3.zero;
            }
            else if (Input.GetMouseButton(0))
            {
                if (_isDragging)
                {
                    float dragDistance = Vector2.Distance(Input.mousePosition, _touchStartPosition);
                    if (dragDistance > _dragThreshold)
                    {
                        PanCamera(Input.mousePosition);
                    }
                    _lastTouchPosition = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;

                float dragDistance = Vector2.Distance(Input.mousePosition, _touchStartPosition);
                if (dragDistance < _dragThreshold)
                {
                    DetectClick(Input.mousePosition);
                }
            }

            // 鼠标滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                ZoomCamera(scroll * 10f);
            }
        }

        #endregion

        #region 相机控制

        /// <summary>
        /// 相机平移
        /// </summary>
        private void PanCamera(Vector2 currentPosition)
        {
            if (mainCamera == null) return;

            Vector3 deltaWorld = mainCamera.ScreenToWorldPoint(currentPosition) -
                                mainCamera.ScreenToWorldPoint(_lastTouchPosition);

            Vector3 newPos = mainCamera.transform.position - new Vector3(deltaWorld.x, 0, 0);

            // 限制边界
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);

            // 记录速度（用于惯性）
            _velocity = (mainCamera.transform.position - newPos) / Time.deltaTime;

            mainCamera.transform.position = newPos;
        }

        /// <summary>
        /// 相机缩放
        /// </summary>
        private void ZoomCamera(float increment)
        {
            if (mainCamera == null) return;

            if (mainCamera.orthographic)
            {
                // 正交相机
                mainCamera.orthographicSize = Mathf.Clamp(
                    mainCamera.orthographicSize - increment,
                    minZoom,
                    maxZoom
                );
            }
            else
            {
                // 透视相机
                Vector3 pos = mainCamera.transform.position;
                pos.y = Mathf.Clamp(pos.y - increment, minZoom, maxZoom);
                mainCamera.transform.position = pos;
            }
        }

        /// <summary>
        /// 应用相机惯性
        /// </summary>
        private void ApplyCameraInertia()
        {
            if (_isDragging || mainCamera == null) return;

            // 衰减速度
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, Time.deltaTime * panDamping);

            if (_velocity.magnitude > 0.01f)
            {
                Vector3 newPos = mainCamera.transform.position - _velocity * Time.deltaTime;
                newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
                mainCamera.transform.position = newPos;
            }
        }

        #endregion

        #region 点击检测

        /// <summary>
        /// 检测点击
        /// </summary>
        private void DetectClick(Vector2 screenPosition)
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, raycastDistance, clickableLayer))
            {
                // 检测建筑槽位
                BuildingSlot slot = hit.collider.GetComponent<BuildingSlot>();
                if (slot != null)
                {
                    slot.OnSlotClicked_Handler();
                    return;
                }

                // 可以添加其他可点击对象的检测
                Debug.Log($"[TouchInputHandler] 点击: {hit.collider.gameObject.name}");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置相机边界
        /// </summary>
        public void SetCameraBounds(float min, float max)
        {
            minX = min;
            maxX = max;
        }

        /// <summary>
        /// 设置缩放范围
        /// </summary>
        public void SetZoomRange(float min, float max)
        {
            minZoom = min;
            maxZoom = max;
        }

        /// <summary>
        /// 移动相机到指定位置
        /// </summary>
        public void MoveCameraTo(float x)
        {
            if (mainCamera == null) return;

            Vector3 pos = mainCamera.transform.position;
            pos.x = Mathf.Clamp(x, minX, maxX);
            mainCamera.transform.position = pos;
        }

        /// <summary>
        /// 启用/禁用输入
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            this.enabled = enabled;

            if (!enabled)
            {
                _isDragging = false;
                _velocity = Vector3.zero;
            }
        }

        #endregion
    }
}
