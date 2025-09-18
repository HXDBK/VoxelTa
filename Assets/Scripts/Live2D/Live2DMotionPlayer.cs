using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework.Json;
using UnityEngine;

namespace Live2D
{

    /// <summary>
    /// 动画曲线段
    /// </summary>
    public class MotionSegment
    {
        public float Time;
        public float Value;
        public float InTangent;
        public float OutTangent;
        
        public MotionSegment(float time, float value, float inTangent = 0, float outTangent = 0)
        {
            Time = time;
            Value = value;
            InTangent = inTangent;
            OutTangent = outTangent;
        }
    }

    /// <summary>
    /// 动画曲线
    /// </summary>
    public class MotionCurve
    {
        public string TargetType;
        public string TargetId;
        public List<MotionSegment> Segments;
        public float FadeInTime;
        public float FadeOutTime;
        
        public MotionCurve()
        {
            Segments = new List<MotionSegment>();
        }
        
        /// <summary>
        /// 在指定时间评估曲线值
        /// </summary>
        public float Evaluate(float time)
        {
            if (Segments.Count == 0) return 0;
            if (time <= Segments[0].Time) return Segments[0].Value;
            if (time >= Segments[Segments.Count - 1].Time) return Segments[Segments.Count - 1].Value;
            
            // 找到时间所在的段
            for (int i = 0; i < Segments.Count - 1; i++)
            {
                var seg1 = Segments[i];
                var seg2 = Segments[i + 1];
                
                if (time >= seg1.Time && time <= seg2.Time)
                {
                    float t = (time - seg1.Time) / (seg2.Time - seg1.Time);
                    
                    // 使用Hermite插值
                    return HermiteInterpolate(
                        seg1.Value, seg1.OutTangent,
                        seg2.Value, seg2.InTangent,
                        t, seg2.Time - seg1.Time
                    );
                }
            }
            
            return Segments[Segments.Count - 1].Value;
        }
        
        private float HermiteInterpolate(float p0, float m0, float p1, float m1, float t, float duration)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            float h00 = 2 * t3 - 3 * t2 + 1;
            float h10 = t3 - 2 * t2 + t;
            float h01 = -2 * t3 + 3 * t2;
            float h11 = t3 - t2;
            
            return h00 * p0 + h10 * duration * m0 + h01 * p1 + h11 * duration * m1;
        }
        /// <summary>
        /// 这条曲线是否几乎恒定（所有关键帧值都几乎相等）
        /// </summary>
        public bool IsAlmostConstant(float eps = 1e-4f)
        {
            // Debug.Log("----------------");
            // Debug.Log(TargetId);
            // 防御：空或单点都当恒定
            if (Segments == null || Segments.Count <= 1)
                return true;

            float v0 = Segments[0].Value;
            // Debug.Log(Segments[0].Value);
            for (int i = 1; i < Segments.Count; i++)
            {
                // Debug.Log(Segments[i].Value);
                if (Mathf.Abs(Segments[i].Value - v0) > eps)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Motion数据
    /// </summary>
    public class MotionData
    {
        public float Duration;
        public float Fps;
        public bool Loop;
        public float FadeInTime;
        public float FadeOutTime;
        public List<MotionCurve> Curves;
        
        public MotionData()
        {
            Curves = new List<MotionCurve>();
        }
    }

    /// <summary>
    /// Motion播放器
    /// </summary>
    public class Live2DMotionPlayer : MonoBehaviour
    {
        [Header("Live2D Model")]
        [SerializeField] private CubismModel cubismModel;
        
        [Header("Motion Settings")]
        [SerializeField] private bool autoPlay = true;
        [SerializeField] private float playbackSpeed = 1.0f;
        [SerializeField] private bool loopMotion = false;
        
        private MotionData currentMotion;
        private float currentTime;
        private bool isPlaying;
        private float fadeWeight = 1.0f;
        private float fadeStartTime;
        private bool isFadingIn;
        private bool isFadingOut;
        private bool needRest;
        private bool needPlay;
        
        private Dictionary<string, float> baselineParams;   // paramId -> value
        private Dictionary<string, float> baselineParts;    // partId  -> opacity
        private bool baselinesCaptured;                     // 已捕获标记（避免重复覆盖）
        
        // 参数缓存
        private Dictionary<string, CubismParameter> parameterCache;
        private Dictionary<string, CubismPart> partCache;
        
        void Start()
        {
            if (cubismModel == null)
            {
                cubismModel = GetComponent<CubismModel>();
            }
            
            InitializeCache();
        }
        
        void Update()
        {
            if (needRest)
            {
                needRest = false;
                RestoreBaselines();
                return;
            }

            if (needPlay)
            {
                Play();
                needPlay = false;
                return;
            }
            if (isPlaying && currentMotion != null)
            {
                UpdateMotion(Time.deltaTime);
            }
        }
        
        /// <summary>
        /// 初始化参数缓存
        /// </summary>
        private void InitializeCache()
        {
            parameterCache = new Dictionary<string, CubismParameter>();
            partCache = new Dictionary<string, CubismPart>();
            
            if (cubismModel != null)
            {
                foreach (var param in cubismModel.Parameters)
                {
                    parameterCache[param.Id] = param;
                }
                
                foreach (var part in cubismModel.Parts)
                {
                    partCache[part.Id] = part;
                }
            }
        }
        public void LoadMotion(ModelMotion modelMotion)
        {
            if (isPlaying)
            {
                Stop();
                currentMotion = ParseMotionData(modelMotion);
                if (autoPlay)
                {
                    needPlay = true;
                }
            }
            else
            {
                currentMotion = ParseMotionData(modelMotion);
                if (autoPlay)
                {
                    needPlay = true;
                }
            }

        }
        private void LogFirstKeyForParam(string paramId)
        {
            if (currentMotion == null || currentMotion.Curves == null) return;
            var c = currentMotion.Curves.Find(k =>
                string.Equals(k.TargetType, "parameter", StringComparison.OrdinalIgnoreCase) &&
                k.TargetId == paramId);

            if (c == null || c.Segments == null || c.Segments.Count == 0)
            {
                Debug.Log($"[Motion Inspect] {paramId} 没有曲线或没有关键帧。");
                return;
            }

            Debug.Log($"[Motion Inspect] {paramId} firstKey: t={c.Segments[0].Time}, v={c.Segments[0].Value}");
        }
        /// <summary>
        /// 解析Motion数据
        /// </summary>
        private MotionData ParseMotionData(ModelMotion modelMotion)
        {
            var motion = new MotionData
            {
                Duration = modelMotion.motion3Json.Meta.Duration,
                Fps = modelMotion.motion3Json.Meta.Fps,
                Loop = modelMotion.motionLoop,
                FadeInTime = modelMotion.motion3Json.Meta.FadeInTime,
                FadeOutTime = modelMotion.motion3Json.Meta.FadeOutTime
            };
            
            foreach (var curveJson in modelMotion.motion3Json.Curves)
            {
                var curve = new MotionCurve
                {
                    TargetType = curveJson.Target,
                    TargetId = curveJson.Id,
                    FadeInTime = curveJson.FadeInTime,
                    FadeOutTime = curveJson.FadeOutTime
                };
                
                // 解析segments数组
                ParseSegmentsFixed(curveJson.Segments, curve);
                motion.Curves.Add(curve);
            }
            return motion;
        }
        /// <summary>
        /// 解析Motion数据
        /// </summary>
        private MotionData ParseMotionData(CubismMotion3Json motionJson)
        {
            var motion = new MotionData
            {
                Duration = motionJson.Meta.Duration,
                Fps = motionJson.Meta.Fps,
                Loop = motionJson.Meta.Loop,
                FadeInTime = motionJson.Meta.FadeInTime,
                FadeOutTime = motionJson.Meta.FadeOutTime
            };
            
            foreach (var curveJson in motionJson.Curves)
            {
                var curve = new MotionCurve
                {
                    TargetType = curveJson.Target,
                    TargetId = curveJson.Id,
                    FadeInTime = curveJson.FadeInTime,
                    FadeOutTime = curveJson.FadeOutTime
                };
                
                // 解析segments数组
                ParseSegmentsFixed(curveJson.Segments, curve);
                motion.Curves.Add(curve);
            }
            // 调试用：统计曲线目标
#if UNITY_EDITOR
            var ids = new HashSet<string>();
            int paramCount = 0, partCount = 0;
            foreach (var c in motion.Curves)
            {
                ids.Add($"{c.TargetType}:{c.TargetId}");
                if (string.Equals(c.TargetType, "parameter", StringComparison.OrdinalIgnoreCase)) paramCount++;
                if (string.Equals(c.TargetType, "partopacity", StringComparison.OrdinalIgnoreCase)) partCount++;
            }
            Debug.Log($"[Motion Inspect] Curves total: {motion.Curves.Count}, Parameters: {paramCount}, Parts: {partCount}\n" +
                      string.Join("\n", ids));
#endif
            return motion;
        }
        /// <summary>
        /// 解析曲线段数据 - 更稳健的版本
        /// </summary>
        private void ParseSegmentsFixed(float[] data, MotionCurve curve)
        {
            int i = 0;
            if (data == null || data.Length < 2) return;

            // 1) 先读首点
            float t0 = data[i++];
            float v0 = data[i++];
            curve.Segments.Add(new MotionSegment(t0, v0, 0, 0));

            // 2) 从这里开始每段都是：segmentType + payload
            while (i < data.Length)
            {
                int type = (int)data[i++]; // 0/1/2/3

                switch (type)
                {
                    case 0: // Linear
                    {
                        if (i + 1 >= data.Length) { Debug.LogWarning("Linear payload not enough"); return; }
                        float t1 = data[i++];
                        float v1 = data[i++];
                        // 计算切线（两端一致）
                        var prev = curve.Segments[^1];
                        float dt = t1 - prev.Time;
                        float m  = (dt > 0f) ? (v1 - prev.Value) / dt : 0f;
                        prev.OutTangent = m;
                        curve.Segments.Add(new MotionSegment(t1, v1, m, m));
                        break;
                    }
                    case 1: // Bezier
                    {
                        // Cubism 格式：c1x, c1y, c2x, c2y, t1, v1 （注意顺序）
                        if (i + 5 >= data.Length) { Debug.LogWarning("Bezier payload not enough"); return; }
                        float c1x = data[i++], c1y = data[i++];
                        float c2x = data[i++], c2y = data[i++];
                        float t1  = data[i++], v1  = data[i++];

                        var prev = curve.Segments[^1];

                        // 用控制点估算入/出切线（与 Cubism 内部采样并不完全等价，但足以驱动 Hermite）
                        float inDt  = (t1 - c2x);
                        float outDt = (c1x - prev.Time);
                        float inTan  = (inDt  > 0f) ? (v1  - c2y) / inDt  : 0f;
                        float outTan = (outDt > 0f) ? (c1y - prev.Value) / outDt : 0f;

                        prev.OutTangent = outTan;
                        curve.Segments.Add(new MotionSegment(t1, v1, inTan, 0f /*outTan 会在下一段更新*/));
                        break;
                    }
                    case 2: // Stepped
                    {
                        if (i + 1 >= data.Length) { Debug.LogWarning("Stepped payload not enough"); return; }
                        float t1 = data[i++], v1 = data[i++];
                        // 阶梯：前值保持到 t1，t1 再跳
                        var prev = curve.Segments[^1];
                        if (t1 - prev.Time > 0f)
                        {
                            curve.Segments.Add(new MotionSegment(t1, v1, 0, 0));
                        }
                        else
                        {
                            // 时间没推进，直接覆盖下一点
                            prev.Value = v1;
                        }
                        break;
                    }
                    case 3: // InverseStepped
                    {
                        if (i + 1 >= data.Length) { Debug.LogWarning("InvStepped payload not enough"); return; }
                        float t1 = data[i++], v1 = data[i++];
                        var prev = curve.Segments[^1];
                        // 逆阶梯：在前一瞬间就变为新值（常见“提前一步”）
                        if (t1 - prev.Time > 0f)
                        {
                            curve.Segments.Add(new MotionSegment(prev.Time + Mathf.Epsilon, v1, 0, 0));
                            curve.Segments.Add(new MotionSegment(t1, v1, 0, 0));
                        }
                        else
                        {
                            prev.Value = v1;
                        }
                        break;
                    }
                    default:
                        Debug.LogWarning($"Unknown segment type: {type}"); 
                        return; // 类型都不对了，直接停，避免继续错读
                }
            }
        }
        /// <summary>
        /// 更新Motion
        /// </summary>
        private void UpdateMotion(float deltaTime)
        {
            currentTime += deltaTime * playbackSpeed;
            
            // 处理循环
            if (currentTime >= currentMotion.Duration)
            {
                if (loopMotion || currentMotion.Loop)
                {
                    currentTime = currentTime % currentMotion.Duration;
                }
                else
                {
                    currentTime = currentMotion.Duration;
                    Stop();
                }
            }
            
            // 更新淡入淡出
            UpdateFade(deltaTime);
            
            // 应用动画
            ApplyMotion();
        }
        
        /// <summary>
        /// 更新淡入淡出
        /// </summary>
        private void UpdateFade(float deltaTime)
        {
            if (isFadingIn)
            {
                float fadeTime = Time.time - fadeStartTime;
                fadeWeight = Mathf.Clamp01(fadeTime / currentMotion.FadeInTime);
                
                if (fadeWeight >= 1.0f)
                {
                    isFadingIn = false;
                }
            }
            else if (isFadingOut)
            {
                float fadeTime = Time.time - fadeStartTime;
                fadeWeight = 1.0f - Mathf.Clamp01(fadeTime / currentMotion.FadeOutTime);

                if (fadeWeight <= 0.0f)
                {
                    isPlaying = false;
                    isFadingOut = false;

                    // 淡出完成 → 现在还原
                    RestoreBaselines();
                }
            }
        }
        
        /// <summary>
        /// 应用Motion到模型
        /// </summary>
        private void ApplyMotion()
        {
            foreach (var curve in currentMotion.Curves)
            {
                // 跳过几乎恒定的曲线（可选：只对 Parameter 应用这个逻辑）
                if (curve.IsAlmostConstant(0.1f)) continue;

                float value = curve.Evaluate(currentTime);
                float weight = fadeWeight;

                if (curve.FadeInTime > 0 && currentTime < curve.FadeInTime)
                    weight *= currentTime / curve.FadeInTime;
                else if (curve.FadeOutTime > 0 && currentTime > currentMotion.Duration - curve.FadeOutTime)
                    weight *= (currentMotion.Duration - currentTime) / curve.FadeOutTime;

                ApplyValue(curve.TargetType, curve.TargetId, value, weight);
            }
        }
        /// <summary>
        /// 应用值到目标
        /// </summary>
        private void ApplyValue(string targetType, string targetId, float value, float weight)
        {
            switch (targetType.ToLower())
            {
                case "parameter":
                    if (parameterCache.TryGetValue(targetId, out var param))
                    {
                        float target = Mathf.Clamp(value, param.MinimumValue, param.MaximumValue);

                        // 方案A：覆盖式（常见于单一播放器）
                        // param.Value = Mathf.Lerp(param.Value, target, weight);

                        // 方案B：以默认值为基的权重混合（更不容易“拖偏”无关状态）
                        float baseValue = param.Value; // 或者 param.DefaultValue，看你希望是否回弹
                        float blended   = Mathf.Lerp(baseValue, target, weight);
                        param.Value = blended;
                    }
                    break;
                    
                case "partopacity":
                    if (partCache.TryGetValue(targetId, out var part))
                    {
                        part.Opacity = Mathf.Lerp(part.Opacity, value, weight);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 播放Motion
        /// </summary>
        public void Play()
        {
            if (currentMotion == null)
            {
                Debug.LogWarning("No motion loaded");
                return;
            }
            CaptureBaselinesForCurrentMotion();
            isPlaying = true;
            currentTime = 0;
            
            if (currentMotion.FadeInTime > 0)
            {
                isFadingIn = true;
                fadeStartTime = Time.time;
                fadeWeight = 0;
            }
            else
            {
                fadeWeight = 1;
            }
        }

        private IEnumerator DoPlay()
        {
            yield return new WaitForSeconds(5);
            Debug.Log("set2");
            CaptureBaselinesForCurrentMotion();
            isPlaying = true;
            currentTime = 0;
            
            if (currentMotion.FadeInTime > 0)
            {
                isFadingIn = true;
                fadeStartTime = Time.time;
                fadeWeight = 0;
            }
            else
            {
                fadeWeight = 1;
            }
        }
        /// 捕获这支 motion 将要改动的目标的“当前值”作为基线
        private void CaptureBaselinesForCurrentMotion()
        {
            if (baselinesCaptured || currentMotion == null) return;
            Debug.Log("jilu");
            baselineParams ??= new Dictionary<string, float>();
            baselineParts  ??= new Dictionary<string, float>();
            baselineParams.Clear();
            baselineParts.Clear();

            foreach (var curve in currentMotion.Curves) // ← 注意你代码里是 Curves
            {
                if (curve == null) continue;

                if (curve.TargetType.Equals("parameter", StringComparison.OrdinalIgnoreCase))
                {
                    if (parameterCache.TryGetValue(curve.TargetId, out var p))
                    {
                        // 用“开播当下”的值做基线；如果你想回默认，改成 p.DefaultValue
                        if (!baselineParams.ContainsKey(curve.TargetId))
                            baselineParams.Add(curve.TargetId, p.Value);
                    }
                }
                else if (curve.TargetType.Equals("partopacity", StringComparison.OrdinalIgnoreCase))
                {
                    if (partCache.TryGetValue(curve.TargetId, out var part))
                    {
                        if (!baselineParts.ContainsKey(curve.TargetId))
                            baselineParts.Add(curve.TargetId, part.Opacity);
                    }
                }
            }

            baselinesCaptured = true;
        }

        /// 把这支 motion 涉及到的目标恢复到捕获的基线
        private void RestoreBaselines()
        {
            if (!baselinesCaptured) return;
            Debug.Log("-----------------");
            if (baselineParams != null)
            {
                foreach (var kv in baselineParams)
                {
                    Debug.Log(kv.Key);
                    if (parameterCache.TryGetValue(kv.Key, out var p))
                        p.Value = kv.Value;
                }
            }
            if (baselineParts != null)
            {
                foreach (var kv in baselineParts)
                {
                    if (partCache.TryGetValue(kv.Key, out var part))
                        part.Opacity = kv.Value;
                }
            }

            baselinesCaptured = false;
            baselineParams?.Clear();
            baselineParts?.Clear();
        }
        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            isPlaying = false;
        }
        
        /// <summary>
        /// 继续播放
        /// </summary>
        public void Resume()
        {
            isPlaying = true;
        }
        
        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            Debug.Log("stop");
            isPlaying = false;
            currentTime = 0;
            fadeWeight = 1;
            isFadingIn = false;
            isFadingOut = false;
            needRest = true;
        }
        
        /// <summary>
        /// 带淡出的停止
        /// </summary>
        public void StopWithFadeOut()
        {
            if (currentMotion is { FadeOutTime: > 0 })
            {
                isFadingOut = true;
                fadeStartTime = Time.time;
            }
            else
            {
                Stop();
            }
        }
        
        /// <summary>
        /// 设置播放速度
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Max(0, speed);
        }
        
        /// <summary>
        /// 获取当前播放进度 (0-1)
        /// </summary>
        public float GetProgress()
        {
            if (currentMotion == null) return 0;
            return Mathf.Clamp01(currentTime / currentMotion.Duration);
        }
        
        /// <summary>
        /// 设置播放进度 (0-1)
        /// </summary>
        public void SetProgress(float progress)
        {
            if (currentMotion == null) return;
            currentTime = Mathf.Clamp01(progress) * currentMotion.Duration;
        }
        
        public bool IsPlaying => isPlaying;
        public float CurrentTime => currentTime;
        public float Duration => currentMotion?.Duration ?? 0;
    }
#if UNITY_EDITOR
    /// <summary>
    /// Motion播放器编辑器扩展
    /// </summary>
    [UnityEditor.CustomEditor(typeof(Live2DMotionPlayer))]
    public class Live2DMotionPlayerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var player = (Live2DMotionPlayer)target;
            
            UnityEditor.EditorGUILayout.Space();
            UnityEditor.EditorGUILayout.LabelField("Runtime Controls", UnityEditor.EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                UnityEditor.EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button(player.IsPlaying ? "Pause" : "Play"))
                {
                    if (player.IsPlaying)
                        player.Pause();
                    else
                        player.Play();
                }
                
                if (GUILayout.Button("Stop"))
                {
                    player.Stop();
                }
                
                if (GUILayout.Button("Stop with Fade"))
                {
                    player.StopWithFadeOut();
                }
                
                UnityEditor.EditorGUILayout.EndHorizontal();
                
                // 进度条
                float progress = player.GetProgress();
                float newProgress = UnityEditor.EditorGUILayout.Slider("Progress", progress, 0, 1);
                if (Mathf.Abs(newProgress - progress) > 0.001f)
                {
                    player.SetProgress(newProgress);
                }
                
                // 显示时间信息
                UnityEditor.EditorGUILayout.LabelField($"Time: {player.CurrentTime:F2} / {player.Duration:F2}");
                
                // 加载Motion文件
                if (GUILayout.Button("Load Motion File"))
                {
                    string path = UnityEditor.EditorUtility.OpenFilePanel("Select Motion3.json", "", "json");
                    if (!string.IsNullOrEmpty(path))
                    {
                        // player.LoadMotionFromFile(path);
                    }
                }
            }
            else
            {
                UnityEditor.EditorGUILayout.HelpBox("Enter Play Mode to use runtime controls", UnityEditor.MessageType.Info);
            }
        }
    }
#endif
}