using System;
using System.Collections;
using Live2D.Cubism.Core;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Live2D
{
    public class Live2dAutoBlink : MonoBehaviour
    {
        [FormerlySerializedAs("MinInterval")]
        [Header("眨眼节奏")]
        [Tooltip("两次眨眼间最短秒数")]
        public float minInterval = 3f;
        [FormerlySerializedAs("MaxInterval")] [Tooltip("两次眨眼间最长秒数")]
        public float maxInterval = 6f;

        [FormerlySerializedAs("CloseDuration")] [Header("闭眼 / 睁眼耗时")]
        public float closeDuration = 0.08f;
        [FormerlySerializedAs("OpenDuration")] public float openDuration  = 0.1f;

        private CubismParameter _eyeL;
        private CubismParameter _eyeR;

        private Coroutine _coroutine;
        private void Awake()
        {
            // 找到 CubismModel
            var model = this.FindCubismModel();
            if (model == null)
            {
                enabled = false;
                Debug.LogError("SimpleAutoBlink: 未找到 CubismModel。");
                return;
            }

            // 缓存左右眼参数
            foreach (var p in model.Parameters)
            {
                switch (p.Id)
                {
                    case "ParamEyeLOpen": _eyeL = p; break;
                    case "ParamEyeROpen": _eyeR = p; break;
                }
            }

            if (_eyeL == null || _eyeR == null)
            {
                enabled = false;
                Debug.LogError("SimpleAutoBlink: 找不到 ParamEyeLOpen/ROpen。");
                return;
            }
        }

        private void OnEnable()
        {
            _coroutine = StartCoroutine(BlinkLoop());
        }

        private void OnDisable()
        {
            StopCoroutine(_coroutine);
            _eyeL.Value = _eyeL.DefaultValue;
            _eyeR.Value = _eyeR.DefaultValue;
        }

        private IEnumerator BlinkLoop()
        {
            var wait = new WaitForSeconds(1f); // 复用 Wait 对象节省 GC

            while (true)
            {
                // 随机等待
                float interval = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(interval);

                // ===== 闭眼阶段 =====
                yield return AnimateEye(1f, 0f, closeDuration);

                // ===== 睁眼阶段 =====
                yield return AnimateEye(0f, 1f, openDuration);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private IEnumerator AnimateEye(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float v = Mathf.Lerp(from, to, t / duration);

                _eyeL.Value = v;
                _eyeR.Value = v;

                yield return null;
            }

            // 确保到达终点
            _eyeL.Value = to;
            _eyeR.Value = to;
        }
    }
}
