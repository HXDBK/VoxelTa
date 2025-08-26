using System.Collections.Generic;
using UnityEngine;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Newtonsoft.Json;

public class Live2DParamController : MonoBehaviour
{
    [System.Serializable]
    public class ParamData
    {
        public string param;
        public float value;
    }

    private CubismModel model;

    void Start()
    {
        model = this.FindCubismModel();

        if (model == null)
        {
            Debug.LogError("找不到 CubismModel！");
            return;
        }
    }

    /// <summary>
    /// 接收 JSON 字符串，设置模型参数
    /// </summary>
    public void SetAction(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogWarning("传入的 JSON 字符串为空！");
            return;
        }

        Debug.Log(jsonString);
        List<ParamData> paramList = null;

        var dict = JsonConvert.DeserializeObject<Dictionary<string, float>>(jsonString);
        paramList = new List<ParamData>();

        foreach (var kv in dict)
        {
            paramList.Add(new ParamData { param = kv.Key, value = kv.Value });
        }

        foreach (var p in paramList)
        {
            Debug.Log(p.param);
            Debug.Log(p.value);
            Debug.Log("--");
            var cubismParam = model.Parameters.FindById(p.param);
            if (cubismParam != null)
            {
                cubismParam.Value = p.value;
                Debug.Log($"设置参数：{p.param} = {p.value}");
            }
            else
            {
                Debug.LogWarning($"参数ID未找到：{p.param}");
            }
        }
    }
}