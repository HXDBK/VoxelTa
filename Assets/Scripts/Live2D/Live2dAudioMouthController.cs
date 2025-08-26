using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEngine;
using UnityEngine.Serialization;

namespace Live2D
{
    public class Live2dAudioMouthController : MonoBehaviour, ICubismUpdatable
    {
        [Header("嘴巴参数名（0 = 闭，1 = 张开）")] 
        public string mouthParam = "ParamMouthOpenY";

        [Header("音量 → 嘴巴曲线")] [Tooltip("RMS 低于此值视为静音")]
        public float minVolume = 0.02f;

        [Tooltip("RMS 达到此值时完全张口")] public float maxVolume = 0.25f;
        [Tooltip("越大越跟得紧，建议 10~20")] public float smooth = 15f;

        [Header("输入选项")] public bool useMicrophone = false;
        [Tooltip("留空则使用首个麦克风设备")] 
        public string microphoneDevice = "";

        private bool _needSetZero;

        // ─────────────── 内部成员 ───────────────
        private CubismParameter _mouthParam;
        [FormerlySerializedAs("_source")] public AudioSource source;
        private readonly float[] _samples = new float[256];
        private bool _isFakeTalking = false; // 标记是否正在假装说话
        private Coroutine _fakeTalkCoroutine;

        // ========= ICubismUpdatable =========
        // 确保本脚本在所有官方组件之后调用，相当于“最高优先级覆盖”。
        public int ExecutionOrder => int.MaxValue; // 最大值代表最后执行
        public bool NeedsUpdateOnEditing => false;
        public bool HasUpdateController { get; set; } = false;

        public void OnLateUpdate()
        {
            if( _mouthParam == null || !enabled){return;}

            if (_needSetZero)
            {
                _mouthParam.Value = 0;
                _needSetZero = false;
            }
            if (source == null || !source.isPlaying || _isFakeTalking)
            {
                return;
            }

            // 1. 采样音频并计算 RMS
            source.GetOutputData(_samples, 0);
            float sum = 0f;
            foreach (var item in _samples)
            {
                sum += item * item;
            }

            float rms = Mathf.Sqrt(sum / _samples.Length);

            // 2. RMS → 0‑1，Clamp，再平滑插值
            float t = Mathf.InverseLerp(minVolume, maxVolume, rms);
            t = Mathf.Clamp01(t);
            t = Mathf.Pow(t, 0.6f);
            _mouthParam.Value = Mathf.Lerp(_mouthParam.Value, t, Time.deltaTime * smooth);
        }

        // ========= Unity 生命周期 =========
        private void Awake()
        {
            // 1. 拿到 CubismModel
            var model = this.FindCubismModel();
            if (!model)
            {
                Debug.LogError("CubismAudioMouthController: 未找到 CubismModel。");
                enabled = false;
                return;
            }

            // 2. 缓存参数
            _mouthParam = model.Parameters.FindById(mouthParam);
            if (!_mouthParam)
            {
                Debug.LogError($"CubismAudioMouthController: 未找到参数 '{mouthParam}'。");
                enabled = false;
                return;
            }
            // 3. AudioSource
            source = GetComponent<AudioSource>();
            _needSetZero = true;
            if (useMicrophone)
            {
                if (Microphone.devices.Length == 0)
                {
                    Debug.LogError("CubismAudioMouthController: 未检测到麦克风。");
                    enabled = false;
                    return;
                }

                if (string.IsNullOrEmpty(microphoneDevice))
                    microphoneDevice = Microphone.devices[0];

                source.clip = Microphone.Start(microphoneDevice, true, 10, 44100);
                source.loop = true;
                source.mute = true; // 避免回授

                while (Microphone.GetPosition(microphoneDevice) <= 0)
                {
                }

                source.Play();
            }

            // 4. 若场景里已存在 CubismUpdateController，则刷新一次，
            //    将本脚本注册进 Updatables 列表。
            var updater = model.gameObject.GetComponent<CubismUpdateController>();
            HasUpdateController = updater;
            if (HasUpdateController)
            {
                updater.Refresh();
            }
        }
        /// <summary>
        /// 假装说话一段时间（单位：秒）
        /// </summary>
        public void FakeTalk(float duration)
        {
            if (_fakeTalkCoroutine != null)
            {
                StopCoroutine(_fakeTalkCoroutine);
            }

            _fakeTalkCoroutine = StartCoroutine(FakeTalkCoroutine(duration));
        }

        private System.Collections.IEnumerator FakeTalkCoroutine(float duration)
        {
            _isFakeTalking = true;
            float timer = 0f;

            while (timer < duration)
            {
                // 模拟嘴巴开合（可使用随机值或正弦波）
                float fakeValue = Mathf.Abs(Mathf.Sin(Time.time * 10f)); // 嘴巴以频率 10Hz 开合
                _mouthParam.Value = fakeValue;

                timer += Time.deltaTime;
                yield return null;
            }

            // 结束假说话后闭嘴
            _mouthParam.Value = 0f;

            _isFakeTalking = false;
        }
        /// <summary>
        /// 用于在不挂 CubismUpdateController 的场景中手动调用。
        /// </summary>
        private void LateUpdate()
        {
            if (!HasUpdateController)
            {
                OnLateUpdate();
            }
        }
    }
}
