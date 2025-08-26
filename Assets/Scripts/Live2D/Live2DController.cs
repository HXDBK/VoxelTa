using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.HarmonicMotion;
using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.Raycasting;
using Live2D.Cubism.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Live2D
{
    public class Live2DController : MonoBehaviour
    {
        public CharacterData characterData;

        public CubismModel modelData;
        public SortingGroup sortingGroup;
        public Transform lookAtTarget;

        public bool isBreath;
        public CubismHarmonicMotionController harmonicMotionController;
        public bool isBlink;
        public Live2dAutoBlink autoBlink;
        public Live2dAudioMouthController mouthController;
        public bool isLookMouse;
        public Live2dAutoLookAt autoLookAt;
        public Transform autoLookAtCenter;
        public BoxCollider2D boxCollider2D;

        // public Live2dDragController dragController;

        [Header("动画")] public CubismMotionController motionCtrl;
        public Queue<AnimationClip> needPlayQueue;

        [Header("表情")] 
        private readonly List<ExpressionState> _activeExpressions = new();
        // private Coroutine expressionCoroutine;

        private void Awake()
        {
            modelData = this.FindCubismModel();
            sortingGroup = gameObject.AddComponent<SortingGroup>();
            harmonicMotionController = GetComponent<CubismHarmonicMotionController>();
            autoLookAt = GetComponent<Live2dAutoLookAt>();
            autoBlink = GetComponent<Live2dAutoBlink>();
            boxCollider2D = GetComponent<BoxCollider2D>();
            mouthController = GetComponent<Live2dAudioMouthController>();
            // dragController = gameObject.AddComponent<Live2dDragController>();

            autoLookAtCenter = new GameObject("autoLookAtCenter").transform;
            autoLookAtCenter.parent = transform;
            autoLookAt.center = autoLookAtCenter;
        }

        public virtual void Start()
        {
            motionCtrl = GetComponent<CubismMotionController>();
            needPlayQueue = new Queue<AnimationClip>();
        }

        public virtual void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                Test();
            }
            // Debug.Log($"needPlayQueue:{needPlayQueue.Count} | motionCtrl:{motionCtrl.IsPlayingAnimation()}");
            if (needPlayQueue.Count > 0 && !motionCtrl.IsPlayingAnimation())
            {
                var tmp = needPlayQueue.Dequeue();
                motionCtrl.PlayAnimation(tmp, isLoop: false);
            }

            if (!isLookMouse)
            {
                return;
            }

            lookAtTarget.transform.position = Camera.main!.ScreenToWorldPoint(Input.mousePosition);
        }

        public virtual void PlayMotion(AnimationClip target)
        {
            needPlayQueue.Enqueue(target);
        }

        public virtual void PlayMotions(List<AnimationClip> targets)
        {
            foreach (var target in targets)
            {
                needPlayQueue.Enqueue(target);
            }
        }

        public virtual void SetExpression(CubismExp3Json expressionJson)
        {
            if (expressionJson == null) return;

            // 创建快照
            var state = new ExpressionState
            {
                Json = expressionJson,
                BaseValues = new Dictionary<string, float>()
            };
            foreach (var paramData in expressionJson.Parameters)
            {
                var parameter = modelData.Parameters.FindById(paramData.Id);
                if (parameter != null)
                {
                    state.BaseValues[paramData.Id] = parameter.Value;
                }
            }

            // 启动协程
            state.Coroutine = StartCoroutine(PlayExpressionFadeIn(state));
            characterData.activeModelExps.Add(state);
            _activeExpressions.Add(state);
        }

        public void Test()
        {
            var exp = new CubismExp3Json
            {
                Type = "Expression",
                FadeInTime = 0.3f,
                FadeOutTime = 0.3f,
                Parameters = new[]
                {
                    new CubismExp3Json.SerializableExpressionParameter { Id = "ParamBrowAngle", Value = 1.0f, Blend = "Overwrite" },
                    new CubismExp3Json.SerializableExpressionParameter { Id = "ParamEyeOpen", Value = 0.5f, Blend = "Multiply" },
                    new CubismExp3Json.SerializableExpressionParameter { Id = "ParamMouthForm", Value = -0.8f, Blend = "Overwrite" }
                }
            };
            SetExpression(exp);
        }
        public virtual void CancelExpression(CubismExp3Json expressionJson)
        {
            var state = _activeExpressions.Find(e => e.Json == expressionJson);
            if (state == null)
                return;

            if (state.Coroutine != null)
            {
                StopCoroutine(state.Coroutine);
            }

            StartCoroutine(FadeOutAndRemove(state));
            characterData.activeModelExps.Remove(state);
            GameManager.instance.SaveData();
        }
        public virtual void ClearAllExpressions()
        {
            foreach (var activeModelExp in characterData.activeModelExps)
            {
                if (!_activeExpressions.Contains(activeModelExp))
                {
                    _activeExpressions.Add(activeModelExp);
                }
            }
            // 停止所有淡入/淡出协程
            foreach (var state in _activeExpressions)
            {
                if (characterData.activeModelExps.Contains(state))
                {
                    characterData.activeModelExps.Remove(state);
                }
                if (state.Coroutine != null)
                {
                    StopCoroutine(state.Coroutine);
                }
            }

            // 遍历所有激活表情，逐个淡出（可选，也可以直接清除）
            foreach (var state in _activeExpressions)
            {
                CancelExpression(state.Json); // 立即恢复基准值
            }
        }
        private IEnumerator PlayExpressionFadeIn(ExpressionState state)
        {
            var expressionJson = state.Json;
            float duration = expressionJson.FadeInTime;
            float time = 0f;

            while (time < duration)
            {
                float weight = time / duration;
                ApplyExpression(expressionJson, weight, state.BaseValues);
                time += Time.deltaTime;
                yield return null;
            }

            ApplyExpression(expressionJson, 1f, state.BaseValues);
            GameManager.instance.SaveData();
        }

        private IEnumerator FadeOutAndRemove(ExpressionState state)
        {
            var expressionJson = state.Json;
            float duration = expressionJson.FadeOutTime;
            float time = 0f;

            while (time < duration)
            {
                float weight = 1f - (time / duration);
                ApplyExpression(expressionJson, weight, state.BaseValues);
                time += Time.deltaTime;
                yield return null;
            }

            ApplyExpression(expressionJson, 0f, state.BaseValues);
            _activeExpressions.Remove(state);
            GameManager.instance.SaveData();
        }

        private void ApplyExpression(CubismExp3Json expressionJson, float weight, Dictionary<string, float> baseValues)
        {
            foreach (var paramData in expressionJson.Parameters)
            {
                var parameter = modelData.Parameters.FindById(paramData.Id);
                if (parameter == null || !baseValues.TryGetValue(paramData.Id, out var baseValue))
                    continue;

                float targetValue = paramData.Value;

                switch (ParseBlendMode(paramData.Blend))
                {
                    case CubismParameterBlendMode.Additive:
                        parameter.Value = baseValue + targetValue * weight;
                        break;

                    case CubismParameterBlendMode.Multiply:
                        parameter.Value = baseValue * Mathf.Lerp(1f, targetValue, weight);
                        break;

                    case CubismParameterBlendMode.Override:
                        parameter.Value = Mathf.Lerp(baseValue, targetValue, weight);
                        break;
                }
            }
        }
        private void ResetExpressionNow(CubismExp3Json expressionJson, Dictionary<string, float> baseValues)
        {
            foreach (var paramData in expressionJson.Parameters)
            {
                var parameter = modelData.Parameters.FindById(paramData.Id);
                if (parameter == null || !baseValues.TryGetValue(paramData.Id, out var baseValue))
                    continue;
                Debug.Log($"数据： parameter.Value:{parameter.Value},baseValue:{baseValue}");
                parameter.Value = baseValue;
            }
        }
        /// <summary>
        /// 将字符串 Blend 模式解析为枚举
        /// </summary>
        private CubismParameterBlendMode ParseBlendMode(string blend)
        {
            switch (blend.ToLowerInvariant())
            {
                case "add":
                case "additive":
                    return CubismParameterBlendMode.Additive;
                case "multiply":
                    return CubismParameterBlendMode.Multiply;
                case "override":
                default:
                    return CubismParameterBlendMode.Override;
            }
        }

        public virtual void SetBreath(bool target)
        {
            isBreath = target;
            harmonicMotionController.enabled = target;
        }

        public virtual void SetBlink(bool target)
        {
            isBlink = target;
            autoBlink.enabled = target;
        }

        public virtual void SetLookMouse(bool target)
        {
            isLookMouse = target;
            if (target)
            {
                autoLookAt.enabled = true;
            }
            else
            {
                autoLookAt.DoDisable();
            }
        }

        public virtual void SetLayer(int layer)
        {
            if (sortingGroup == null)
            {
                sortingGroup = GetComponent<SortingGroup>();
            }
            sortingGroup.sortingOrder = layer;
        }

        public virtual void SetColor(Color target)
        {
            foreach (var drawable in modelData.Drawables)
            {
                var render = drawable.GetComponent<CubismRenderer>();
                render.Color = target;
            }
        }
        [Serializable]
        public class ExpressionState
        {
            public CubismExp3Json Json;
            public Dictionary<string, float> BaseValues;
            [NonSerialized]
            public Coroutine Coroutine;
        }
    }
}