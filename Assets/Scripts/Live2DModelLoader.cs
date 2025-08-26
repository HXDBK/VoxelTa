using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Live2D;
using UnityEngine;
using UnityEngine.Networking;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Expression;
using Live2D.Cubism.Framework.Json;
using Live2D.Cubism.Framework.HarmonicMotion;
using Live2D.Cubism.Framework.Motion;
using Live2D.Cubism.Framework.MotionFade;
using Live2D.Cubism.Framework.Pose;
using Live2D.Cubism.Rendering;
using Newtonsoft.Json;
using SFB; // StandaloneFileBrowser

public class Live2DModelLoader : MonoBehaviour
{
    [Header("Runtime Settings")]
    public Transform lookTarget;
    public AudioSource audioSource;

    private CubismModel _model;
    public CharacterData modelData;
    public Live2DController controller;
    public CubismExpressionList list;
    private CubismUpdateController _updater;
    private CubismMotionController _motionCtrl;
    public readonly Dictionary<string,CubismExp3Json> expressions = new();
    private readonly Dictionary<string, CubismMotion3Json> _motions = new();
    private readonly Dictionary<string, CubismFadeMotionData> _motionDatas = new();
    public readonly Dictionary<string, AnimationClip> clips = new();

    private string _modelDir;
    public List<ModelParameter> modelParameters;
    public void LoadModelFromFile(CharacterData target,string modelJsonPath, Action<Live2DController> onComplete = null)
    {
        Debug.Log("loading model from file");
        expressions.Clear();
        _motions.Clear();
        _motionDatas.Clear();
        clips.Clear();
        modelData = target;
        modelParameters = modelData.modelParameters;
        StartCoroutine(LoadModelFromFileIE(modelJsonPath, onComplete));
    }

    public void Update()
    {
        // _motionCtrl.PlayAnimation(_clips[""]);
    }

    private IEnumerator LoadModelFromFileIE(string modelJsonPath, Action<Live2DController> onComplete = null)
    {
        _modelDir = Path.GetDirectoryName(modelJsonPath);
        if (_model != null)
        {
            Destroy(_model.gameObject);
        }

        if (!File.Exists(modelJsonPath))
        {
            Debug.LogError($"模型文件不存在: {modelJsonPath}");
            MessageManager.instance.ShowMessage($"模型文件不存在: {modelJsonPath}", MessageType.Warning);
            onComplete?.Invoke(null);
            yield break;
        }

        string ext = Path.GetExtension(modelJsonPath).ToLowerInvariant();

        // === 图片模型加载 ===
        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
        {
            byte[] bytes = File.ReadAllBytes(modelJsonPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);

            var go = new GameObject(Path.GetFileNameWithoutExtension(modelJsonPath));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);

            var imgModel = go.AddComponent<ImageModel>();
            imgModel.spriteRenderer = sr;
            imgModel.sprite = sr.sprite;
            controller = imgModel;

            // 加上简单 collider
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            onComplete?.Invoke(controller);
            yield break;
        }

        // === Cubism 模型加载 ===
        if (ext == ".json")
        {
            var model3Json = CubismModel3Json.LoadAtPath(modelJsonPath, BuiltinLoadAssetAtPath);
            _model = model3Json.ToModel();

            yield return StartCoroutine(ResetControllers());

            LoadExpressions();
            LoadMotions();
            _updater.Refresh();
            onComplete?.Invoke(controller);
            yield break;
        }

        Debug.LogError($"不支持的文件类型: {ext}");
        onComplete?.Invoke(null);
    }

    public void RemoveCurModel()
    {
        if (_model != null)
        {
            Debug.Log(_model.gameObject.name);  
            Destroy(_model.gameObject);
        }
    }
    
    private void LoadExpressions()
    {
        var expFiles = Directory.GetFiles(_modelDir, "*.exp3.json", SearchOption.TopDirectoryOnly);
        foreach (var path in expFiles)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var exp = CubismExp3Json.LoadFrom(json);
            string key = Path.GetFileNameWithoutExtension(path).Replace(".exp3", "");
            var data = CubismExpressionData.CreateInstance(exp);
            for (var i = 0; i < data.Parameters.Length; i++)
            {
                data.Parameters[i].Blend = CubismParameterBlendMode.Override;
            }
            expressions.TryAdd(key,exp);
        }
        Debug.Log($"已加载 {expressions.Count} 个表情");
    }

    private void LoadMotions()
    {
        var ids = new List<int>();
        var motionFiles = Directory.GetFiles(_modelDir, "*.motion3.json", SearchOption.TopDirectoryOnly);

        foreach (var path in motionFiles)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            var motion3Json = CubismMotion3Json.LoadFrom(json);
            var clip = motion3Json.ToAnimationClip();
            clip.legacy = false;
            int id = clip.GetInstanceID();
            ids.Add(id);

            clip.events = Array.Empty<AnimationEvent>();
            clip.AddEvent(new AnimationEvent
            {
                functionName = "InstanceId",
                intParameter = id,
                time = 0f,
                messageOptions = SendMessageOptions.DontRequireReceiver
            });

            var fade = CubismFadeMotionData.CreateInstance(motion3Json, clip.name, clip.length);

            string key = Path.GetFileNameWithoutExtension(path).Replace(".motion3", "");
            clip.name = key;
            clips[key] = clip;
            _motions[key] = motion3Json;
            _motionDatas[key] = fade;
        }

        var fadeList = ScriptableObject.CreateInstance<CubismFadeMotionList>();
        fadeList.CubismFadeMotionObjects = _motionDatas.Values.ToArray();
        fadeList.MotionInstanceIds = ids.ToArray();

        var fadeController = _model.gameObject.AddComponent<CubismFadeController>();
        fadeController.CubismFadeMotionList = fadeList;
        fadeController.Refresh();

        _motionCtrl = _model.gameObject.AddComponent<CubismMotionController>();
        var render = _model.GetComponent<CubismRenderController>();
        render.SortingMode = CubismSortingMode.BackToFrontOrder;
        _motionCtrl.LayerCount = 1; // 设置为1层，表示只有一个播放层，也就是“覆盖”

        Debug.Log($"已加载 {clips.Count} 个动作");
    }

    private IEnumerator ResetControllers()
    {
        yield return new WaitForSeconds(0.5f);

        _updater = _model.gameObject.AddComponent<CubismUpdateController>();
        // var store = _model.gameObject.AddComponent<CubismParameterStore>(); store.Refresh();
        var pose = _model.gameObject.AddComponent<CubismPoseController>(); pose.Refresh();

        var lookAt = _model.gameObject.AddComponent<Live2dAutoLookAt>();
        lookAt.Target = lookTarget;
        
        var boxCollider2D = _model.gameObject.AddComponent<BoxCollider2D>(); 
        boxCollider2D.isTrigger = true;
        _model.tag = "Character";
        _model.gameObject.AddComponent<Live2dAutoBlink>();

        var breathParam = _model.Parameters.FindById("ParamBreath");
        if (breathParam != null)
        {
            var motionParam = breathParam.gameObject.AddComponent<CubismHarmonicMotionParameter>();
            motionParam.Direction = CubismHarmonicMotionDirection.Centric;

            var breath = _model.gameObject.AddComponent<CubismHarmonicMotionController>();
            breath.BlendMode = CubismParameterBlendMode.Override;
            breath.ChannelTimescales = new float[] { 1 };
            breath.Refresh();
        }

        var mouth = _model.gameObject.AddComponent<Live2dAudioMouthController>();
        mouth.source = audioSource;
        controller = _model.gameObject.AddComponent<Live2DController>();
        controller.lookAtTarget = lookTarget;
        _updater.Refresh();
    }

    // 文件资源加载器
    private static object BuiltinLoadAssetAtPath(Type assetType, string absolutePath)
    {
        if (assetType == typeof(byte[]))
            return File.ReadAllBytes(absolutePath);

        if (assetType == typeof(string))
            return File.ReadAllText(absolutePath);

        if (assetType == typeof(Texture2D))
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(absolutePath));
            return tex;
        }

        throw new NotSupportedException($"不支持的资源类型: {assetType}");
    }
}
