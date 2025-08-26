using Live2D.Cubism.Core;
using UnityEngine;

namespace Live2D
{
    public class Live2dDragController : MonoBehaviour
    {
        public float dragSensitivity = 30f;      // 拖动影响系数
        public float returnSpeed = 5f;           // 回弹速度
        public float maxRotation = 30f;          // 最大旋转角度

        private CubismModel _model;
        private CubismParameter _paramAngleX;
        private CubismParameter _paramAngleY;
        private CubismParameter _paramBodyAngleX;
        private CubismParameter _paramBodyAngleY;

        private Vector2 _dragOffset;
        private Vector2 _currentDrag;
        private bool _isDragging;

        void Start()
        {
            _model = this.FindCubismModel();
            if (_model == null) return;

            _paramAngleX = _model.Parameters.FindById("ParamAngleX");
            _paramAngleY = _model.Parameters.FindById("ParamAngleY");
            _paramBodyAngleX = _model.Parameters.FindById("ParamBodyAngleX");
            _paramBodyAngleY = _model.Parameters.FindById("ParamBodyAngleY");
        }

        public void StartDrag()
        {
            _isDragging = true;
            _dragOffset = Input.mousePosition;
        }

        public void EndDrag()
        {
            _isDragging = false;
        }

        void Update()
        {
            if (_isDragging)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _dragOffset;

                // ✅ 反转方向 + 应用灵敏度
                Vector2 drag = -delta / dragSensitivity;

                // 限制最大旋转
                _currentDrag = Vector2.ClampMagnitude(drag, maxRotation);
            }
            else
            {
                // ✅ 回弹逻辑（线性插值）
                _currentDrag = Vector2.Lerp(_currentDrag, Vector2.zero, Time.deltaTime * returnSpeed);
            }

            // 提取旋转角度
            float headX = -_currentDrag.x;
            float headY = -_currentDrag.y;

            // 设置参数值
            if (_paramAngleX != null) _paramAngleX.Value = headX;
            if (_paramAngleY != null) _paramAngleY.Value = headY;

            if (_paramBodyAngleX != null) _paramBodyAngleX.Value = headX * 0.5f;
            if (_paramBodyAngleY != null) _paramBodyAngleY.Value = headY * 0.5f;
        }
    }
}
