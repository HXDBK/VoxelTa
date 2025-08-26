using System;
using Live2D.Cubism.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Live2D
{
    public class Live2dAutoLookAt : MonoBehaviour
    {
        [Header("必填：要看的物体")]
        public Transform Target;

        // ───────────────────  眼球  ───────────────────
        [Header("眼球参数名")]
        public string eyeX = "ParamEyeBallX";
        public string eyeY = "ParamEyeBallY";
        [Header("眼球最大位移 (-1~1)")]
        public float eyeMaxHorizontal = 1f;
        public float eyeMaxVertical   = 1f;
        public float eyeSmooth = 15f;          // 数字越大越跟得紧

        // ───────────────────  头部  ───────────────────
        [Header("头部角度参数名")]
        public string headX = "ParamAngleX";
        public string headY = "ParamAngleY";
        public string headZ = "ParamAngleZ";
        [Header("头部最大角度 (度)")]
        public float headMaxYaw   = 20f;       // 水平
        public float headMaxPitch = 15f;       // 垂直
        public float headMaxRoll  = 10f;       // 倾斜
        public float headSmooth   = 10f;        // 故意比眼球慢

        // ───────────────────  身体  ───────────────────
        [Header("身体角度参数名（用于附加身体动作）")]
        public bool affectBody = false;
        public string bodyX = "ParamBodyAngleX";
        public string bodyY = "ParamBodyAngleY";
        public string bodyZ = "ParamBodyAngleZ";

        private CubismParameter _bodyX, _bodyY, _bodyZ;
        // ───────────────────  内部缓存  ───────────────────
        private CubismParameter _eyeX, _eyeY;
        private CubismParameter _headX, _headY, _headZ;
        private Camera _cam;

        [Header("中心点（用于替代模型位置）")]
        public Transform center;

        private bool _isDisable;
        private void Awake()
        {
            // 找模型
            var model = this.FindCubismModel();
            if (!model) { enabled = false; Debug.LogError("LookAt: 未找到 CubismModel"); return; }

            // 缓存参数（找不到就保持 null，后面会自动跳过写入）
            _eyeX  = model.Parameters.FindById(eyeX);
            _eyeY  = model.Parameters.FindById(eyeY);
            _headX = model.Parameters.FindById(headX);
            _headY = model.Parameters.FindById(headY);
            _headZ = model.Parameters.FindById(headZ);
            _bodyX = model.Parameters.FindById(bodyX);
            _bodyY = model.Parameters.FindById(bodyY);
            _bodyZ = model.Parameters.FindById(bodyZ);
            _cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (Target == null) return;
            if (center == null)
            {
                center = transform;
            }

            if (_isDisable)
            {
                _eyeX.Value = _eyeX.DefaultValue;
                _eyeY.Value = _eyeY.DefaultValue;
                _headX.Value = _headX.DefaultValue;
                _headY.Value = _headY.DefaultValue;
                _headZ.Value = _headZ.DefaultValue;
                enabled = false;
            }
            // 使用中心点或模型本身位置
            Vector3 scrModel = _cam.WorldToScreenPoint(center != null ? center.position : transform.position);
            Vector3 scrTarget = _cam.WorldToScreenPoint(Target.position);

            Vector2 delta = scrTarget - scrModel;
            float nx = Mathf.Clamp(delta.x / (Screen.width * 0.5f), -1f, 1f);
            float ny = Mathf.Clamp(delta.y / (Screen.height * 0.5f), -1f, 1f);

            // 眼球
            if (_eyeX) _eyeX.Value = Mathf.Lerp(_eyeX.Value, nx * eyeMaxHorizontal, Time.deltaTime * eyeSmooth);
            if (_eyeY) _eyeY.Value = Mathf.Lerp(_eyeY.Value, ny * eyeMaxVertical, Time.deltaTime * eyeSmooth);

            // 头部
            if (_headX) _headX.Value = Mathf.Lerp(_headX.Value, nx * headMaxYaw, Time.deltaTime * headSmooth);
            if (_headY) _headY.Value = Mathf.Lerp(_headY.Value, ny * headMaxPitch, Time.deltaTime * headSmooth);
            if (_headZ) _headZ.Value = Mathf.Lerp(_headZ.Value, -nx * headMaxRoll, Time.deltaTime * headSmooth);
            if (affectBody)
            {
                if (_bodyX) _bodyX.Value = Mathf.Lerp(_bodyX.Value, nx * headMaxYaw * 0.5f, Time.deltaTime * headSmooth);
                if (_bodyY) _bodyY.Value = Mathf.Lerp(_bodyY.Value, ny * headMaxPitch * 0.5f, Time.deltaTime * headSmooth);
                if (_bodyZ) _bodyZ.Value = Mathf.Lerp(_bodyZ.Value, -nx * headMaxRoll * 0.5f, Time.deltaTime * headSmooth);
            }
        }
        public void Reset()
        {
            Target = null;

            eyeX = "ParamEyeBallX";
            eyeY = "ParamEyeBallY";
            eyeMaxHorizontal = 1f;
            eyeMaxVertical = 1f;
            eyeSmooth = 15f;

            headX = "ParamAngleX";
            headY = "ParamAngleY";
            headZ = "ParamAngleZ";
            headMaxYaw = 20f;
            headMaxPitch = 15f;
            headMaxRoll = 10f;
            headSmooth = 10f;
        }

        public void DoDisable()
        {
            _isDisable = true;
        }

        private void OnDisable()
        {
            if (Target && center)
            {
                Target.position = center.position;
            }
        }

        private void OnEnable()
        {
            _isDisable = false;
        }
    }
}
