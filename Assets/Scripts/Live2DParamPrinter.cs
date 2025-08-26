using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Live2D.Cubism.Core;
using Newtonsoft.Json.Linq;
using UnityEngine.Serialization;

public class Live2DParamPrinter : MonoBehaviour
{
    [Header("输出选项")]
    public bool printOnStart = true;
    public bool includeMinMax = true; // 是否包含参数范围
    public bool includeCurrentValue = true; // 是否包含当前值
    [SerializeField]
    private CubismModel model;
    [FormerlySerializedAs("model3JsonFile")] [Header("cdi3.json 文件（拖入 TextAsset）")]
    public TextAsset cdi3JsonFile;

    private readonly Dictionary<string, string> _paramNameMap = new();
    void Start()
    {
        if (!model)
        {
            model = GetComponent<CubismModel>();
            return;
        }

        LoadChineseParamNames();
        if (printOnStart)
        {
            PrintParameters();
        }
    }
    private void LoadChineseParamNames()
    {
        if (!cdi3JsonFile)
        {
            Debug.LogWarning("未拖入 TextAsset。无法显示中文参数名。");
            return;
        }

        try
        {
            JObject root = JObject.Parse(cdi3JsonFile.text);
            var parameters = root["Parameters"];

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    string id = param["Id"]?.ToString();
                    string paramName = param["Name"]?.ToString();

                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(paramName))
                    {
                        if (id != null) _paramNameMap[id] = paramName;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"解析 json 时出错: {ex.Message}");
        }
    }

    // 打印所有参数到控制台
    public void PrintParameters()
    {
        if (!model || model.Parameters == null)
        {
            Debug.LogError("模型或参数列表为空!");
            return;
        }
        LoadChineseParamNames();
        StringBuilder sBuilder = new StringBuilder();
        sBuilder.AppendLine($"===== Live2D 模型参数列表 =====");
        sBuilder.AppendLine($"模型名称: {gameObject.name}");
        sBuilder.AppendLine($"参数总数: {model.Parameters.Length}");
        sBuilder.AppendLine("-------------------------------");
        foreach (var param in model.Parameters)
        {
            string info = $"{param.Id}";
            info += $"[{_paramNameMap[param.Id]}]";

            if (includeMinMax)
            {
                info += $" [范围: {param.MinimumValue} ~ {param.MaximumValue}]";
            }
            
            if (includeCurrentValue)
            {
                info += $" [当前值: {param.Value}]";
            }
            sBuilder.AppendLine(info);
        }
        sBuilder.AppendLine("===============================");
        Debug.Log(sBuilder);
    }

    // 编辑器按钮
#if UNITY_EDITOR
    [ContextMenu("打印参数")]
    private void PrintInEditor()
    {
        PrintParameters();
    }
#endif
}