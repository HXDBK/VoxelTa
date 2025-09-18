using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.HarmonicMotion;
using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Rendering;
using Model;
using UnityEngine;
using UnityEngine.Rendering;
using Object = System.Object;

namespace Live2D
{
    public class Live2DController : GeneralModel
    {
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

        // public Live2dDragController dragController;

        [Header("动画")] 
        public List<ModelMotion> motionPageItems = new();
        public CubismMotionController motionCtrl;
        public Queue<AnimationClip> needPlayQueue;
        public Live2DMotionPlayer motionPlayer;

        [Header("表情")]
        public List<ModelExp> expPageItems = new();
        private readonly List<ExpressionState> _activeExpressions = new();
        // private Coroutine expressionCoroutine;
        public Dictionary<string,CubismExp3Json> expressions;
        public List<ModelParameter> parameterPageItems = new();
        private Dictionary<ModelParameter,float> _needSetParameter = new();
        private bool _needSetParameterFlag;

        private void Awake()
        {
            modelData = this.FindCubismModel();
            sortingGroup = gameObject.AddComponent<SortingGroup>();
            motionPlayer = gameObject.AddComponent<Live2DMotionPlayer>();
            harmonicMotionController = GetComponent<CubismHarmonicMotionController>();
            autoLookAt = GetComponent<Live2dAutoLookAt>();
            autoBlink = GetComponent<Live2dAutoBlink>();
            boxCollider2d = GetComponent<BoxCollider2D>();
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
            // if (Input.GetKeyDown(KeyCode.Space))
            // {
            //     Debug.Log("play");
            //     var player = GetComponent<Live2DMotionPlayer>();
            //     player.LoadMotion(motion3Jsons[4]);
            // }
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

        private void LateUpdate()
        {

            if (_needSetParameterFlag)
            {
                foreach (var parameterPageItem in parameterPageItems)
                {
                    parameterPageItem.parameter.Value = _needSetParameter[parameterPageItem];
                }
                _needSetParameter.Clear();
                _needSetParameterFlag = false;
            }
        }

        public override void OnLoadSuccess(CharacterData character,ModelLoader loader)
        {
            base.OnLoadSuccess(character,loader);
            LoadParametersValue();
            LoadExpValue(loader as Live2DModelLoader);
            LoadMotion(loader as Live2DModelLoader);
            SetBlink(character.isBlink);
            SetBreath(character.isBreath);
            SetLookMouse(character.isLookAt);
            autoLookAtCenter.transform.localPosition = character.lookCenter;
        }

        /// <summary>
        /// 获取参数
        /// </summary>
        private void LoadParametersValue()
        {
            if (modelData == null)
            {
                parameterPageItems.Clear();
                return;
            }
            var parameters = modelData.Parameters;
            var savedParamDict = characterData.modelParameters
                .ToDictionary(p => p.parameterId);

            parameterPageItems = new List<ModelParameter>();
            foreach (var parameter in parameters)
            {
                if (savedParamDict.TryGetValue(parameter.Id, out var savedParam))
                {
                    savedParam.SetParameter(parameter);
                    parameterPageItems.Add(savedParam);
                }
                else
                {
                    parameterPageItems.Add(new ModelParameter(parameter));
                }
            }
        }

        /// <summary>
        /// 设置表情列表
        /// </summary>
        private void LoadExpValue(Live2DModelLoader live2DModelLoader)
        {
            // 模型的表情数据
            var exps = live2DModelLoader.expressions;
            // curCharacter.modelExps.Clear();
            // 用户存储的表情数据
            var savedExpDict = characterData.modelExps.ToDictionary(e => e.expName);

            // 最终页面展示的表情数据
            expPageItems = new List<ModelExp>();

            // 记录已添加的 key，避免重复添加
            HashSet<string> addedKeys = new();

            // 遍历模型提供的表情数据，优先使用用户存储数据
            foreach (var exp in exps)
            {
                if (savedExpDict.TryGetValue(exp.Key, out var savedExp))
                {
                    if (savedExp.exp3Json.Parameters == null)
                    {
                        savedExp.exp3Json = exp.Value;
                    }

                    savedExp.type = 0;
                    expPageItems.Add(savedExp);
                }
                else
                {
                    var newItem = new ModelExp(exp.Value, exp.Key, exp.Key, true)
                    {
                        type = 0
                    };
                    expPageItems.Add(newItem);
                }

                addedKeys.Add(exp.Key);
            }
            
            // 添加 savedExpDict 中独有的表情（模型中没有）
            foreach (var kv in savedExpDict)
            {
                if (!addedKeys.Contains(kv.Key))
                {
                    Debug.Log(kv.Value.expName);
                    kv.Value.type = 1;
                    expPageItems.Add(kv.Value);
                }
            }
        }

        public void LoadMotion(Live2DModelLoader loader)
        {
            // 模型的表情数据
            var motion3Jsons = loader.motions;
            // curCharacter.modelExps.Clear();
            // 用户存储的表情数据
            var savedMotionDict = characterData.modelMotions.ToDictionary(e => e.motionName);
            // 最终页面展示的表情数据
            motionPageItems = new List<ModelMotion>();

            // 记录已添加的 key，避免重复添加
            HashSet<string> addedKeys = new();

            // 遍历模型提供的表情数据，优先使用用户存储数据
            foreach (var motion in motion3Jsons)
            {
                if (savedMotionDict.TryGetValue(motion.Key, out var savedMotion))
                {
                    savedMotion.motion3Json = motion.Value;
                    motionPageItems.Add(savedMotion);
                }
                else
                {
                    var newItem = new ModelMotion(motion.Value, motion.Key, motion.Key,true);
                    motionPageItems.Add(newItem);
                }

                addedKeys.Add(motion.Key);
            }
            
            // 添加 savedExpDict 中独有的表情（模型中没有）
            foreach (var kv in savedMotionDict)
            {
                if (!addedKeys.Contains(kv.Key))
                {
                    Debug.Log(kv.Value.motionName);
                    motionPageItems.Add(kv.Value);
                }
            }
        }
        public void SetParameterValue(Dictionary<ModelParameter,float> target)
        {
            _needSetParameter = target;
            _needSetParameterFlag = true;
        }
        public override void PlayMotion(Object target)
        {
            // needPlayQueue.Enqueue(target);
            // motionCtrl.PlayAnimation(target, isLoop: false);
            var motionTarget = target as ModelMotion;
            motionPlayer.LoadMotion(motionTarget);
        }

        public override void SetExpression(Object target)
        {
            var expressionJson = target as CubismExp3Json;
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
        
        public override void CancelExpression(Object expressionJson)
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
        public override void ClearAllExpressions()
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
        public void ClearAllExpressionsNow()
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
                ResetExpressionNow(state.Json,state.BaseValues); // 立即恢复基准值
            }
        }
        private IEnumerator PlayExpressionFadeIn(ExpressionState state)
        {
            Debug.Log("Play");
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
            // GameManager.instance.SaveData();
        }

        private IEnumerator FadeOutAndRemove(ExpressionState state)
        {
            Debug.Log("Remove");
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

        public override void CheckExp(DialogueEntry entry)
        {
            var message = entry.content;
            ModelExp target = null;
            foreach (var item in expPageItems)
            {
                if (!item.expOn){continue;}

                var keyStr = item.expNickname;
                try
                {
                    // 尝试将 pattern 作为正则表达式匹配 input
                    if (Regex.IsMatch(message, keyStr))
                    {
                        target = item;
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    // pattern 不是有效正则，退而求其次使用普通字符串匹配
                    if (message.Contains(keyStr))
                    {
                        target = item;
                        break;
                    }
                }
            }
            
            if (target == null)
            {
                ClearAllExpressions();
            }
            else
            {
                ClearAllExpressionsNow();
                SetExpression(target.exp3Json);
            }
        }

        public override void CheckMotion(DialogueEntry entry)
        {
            var message = entry.content;
            motionPlayer.Stop();
            foreach (var item in motionPageItems)
            {
                if (!item.motionOn){continue;}

                var keyStr = item.motionNickname;
                try
                {
                    // 尝试将 pattern 作为正则表达式匹配 input
                    if (Regex.IsMatch(message, keyStr))
                    {
                        Debug.Log("get");
                        PlayMotion(item);
                    }
                }
                catch (ArgumentException)
                {
                    // pattern 不是有效正则，退而求其次使用普通字符串匹配
                    if (message.Contains(keyStr))
                    {
                        Debug.Log("get");
                        PlayMotion(item);
                    }
                }
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
        public override void SetLayer(int layer)
        {
            if (sortingGroup == null)
            {
                sortingGroup = GetComponent<SortingGroup>();
            }
            sortingGroup.sortingOrder = layer;
        }
        public override void SetColor(Color target)
        {
            foreach (var drawable in modelData.Drawables)
            {
                var render = drawable.GetComponent<CubismRenderer>();
                render.Color = target;
            }
        }

        public override Bounds GetBounds()
        {
            var drawables = modelData.Drawables;
            if (drawables == null || drawables.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            Bounds combinedBounds = drawables[0].GetComponent<MeshRenderer>().bounds;

            for (int i = 1; i < drawables.Length; i++)
            {
                combinedBounds.Encapsulate(drawables[i].GetComponent<MeshRenderer>().bounds);
            }

            return combinedBounds;
        }

        public override void FakeTalk(float duration)
        {
            mouthController.FakeTalk(duration);
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