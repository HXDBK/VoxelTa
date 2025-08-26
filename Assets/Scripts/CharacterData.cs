using System;
using System.Collections.Generic;
using System.Text;
using Live2D;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Json;
using UnityEngine;
using UnityEngine.Serialization;
using WUI;

/// <summary>
/// 角色设置
/// </summary>
[Serializable]
public class CharacterData : IPageListItem
{
    public string characterTitle;
    public string characterDescription;
    public string characterName;
    public string userName;
    public string iconPath;
    public string live2dPath;
    public string backgroundPath;
    public Vector3 backgroundPos = Vector3.zero;
    public Vector3 backgroundScale = Vector3.one;
    public Color backgroundColor = new(0.24f, 0.24f, 0.24f);
    public Color backgroundLight = Color.white;
    public Color aiNameColor = new(0.84f, 0.43f, 0.38f);
    public bool isBreath = true;
    public bool isLookAt = true;
    public bool isBlink = true;
    public Vector3 pos = Vector3.zero;
    public Vector3 lookCenter = Vector3.zero;
    public Vector3 scale = Vector3.one;
    
    public Vector3 uiPos = Vector3.zero;
    public Vector3 uiScale = Vector3.one;
    
    public Vector3 deskPos = Vector3.zero;
    public Vector3 deskScale = Vector3.one;
    public Vector3 easyInputPos = Vector3.zero;
    public Vector3 easyDialogPos = Vector3.zero;
    
    public List<Memory> memories = new ();
    public List<ModelParameter> modelParameters = new ();
    public List<ModelMotion> modelMotions = new ();
    public List<ModelExp> modelExps = new ();
    public List<Live2DController.ExpressionState> activeModelExps = new ();
    
    
    public TalkData talkData;
    public SettingData  SettingData => settingData ??= new SettingData();

    public SettingData settingData;
    
    public bool HasMemory=>memories is {Count: > 0};
    /// <summary>
    /// 获取记忆字符串
    /// </summary>
    /// <returns></returns>
    public string GetMemorise()
    {
        StringBuilder strB = new StringBuilder();
        for (int i = 0; i < memories.Count; i++)
        {
            var mem = memories[i];
            strB.Append($"{i}.{mem.content}");
        }
        return strB.ToString();
    }

    public CharacterData Clone()
    {
        var clone = (CharacterData)this.MemberwiseClone();

        // 深拷贝列表
        clone.memories = new List<Memory>();
        foreach (var memory in this.memories)
        {
            clone.memories.Add(new Memory
            {
                title = memory.title,
                content = memory.content
            });
        }

        clone.modelParameters = new List<ModelParameter>();
        foreach (var param in this.modelParameters)
        {
            clone.modelParameters.Add(new ModelParameter(param.parameterId,param.parameterName,param.parameterValue));
        }

        clone.modelMotions = new List<ModelMotion>();
        foreach (var motion in this.modelMotions)
        {
            clone.modelMotions.Add(new ModelMotion
            {
                motionName = motion.motionName,
                motionNickname = motion.motionNickname,
                motionOn = motion.motionOn
            });
        }

        clone.modelExps = new List<ModelExp>();
        foreach (var exp in this.modelExps)
        {
            clone.modelExps.Add(new ModelExp(exp.exp3Json,exp.expName,exp.expNickname,exp.expOn));;
        }

        // 深拷贝设置和对话数据（前提是这些类可序列化或你手动写了 Clone）
        if (settingData != null)
        {
            clone.settingData = settingData.Clone();
        }

        if (talkData != null)
        {
            clone.talkData = talkData.Clone();
        }

        return clone;
    }
}
[Serializable]
public class Memory : IPageListItem
{
    public string title;
    public string content;
}
/// <summary>
/// 模型参数存储
/// </summary>
[Serializable]
public class ModelParameter : IPageListItem
{
    [NonSerialized]
    public CubismParameter parameter;
    public string parameterId;
    public string parameterName;
    public string displayName;
    public float parameterValue;
    public ModelParameter(CubismParameter parameter)
    {
        this.parameter = parameter;
        parameter.OnSetValue += SetParameterValue;
        parameterId = parameter.Id;
        parameterName = parameter.name;
        parameterValue = parameter.Value;
        if (parameter.TryGetComponent<CubismDisplayInfoParameterName>(out var displayParameterName))
        {
            displayName = displayParameterName.Name;
        }
    }
    public ModelParameter(string parameterId,string parameterName,float parameterValue)
    {
        this.parameterId = parameterId;
        this.parameterName = parameterName;
        this.parameterValue = parameterValue;
    }

    public void SetParameter(CubismParameter target)
    {
        if (parameter != null)
        {
            parameter.OnSetValue -= SetParameterValue;
        }
        parameter = target;
        parameter.Value = parameterValue;
        if (parameter.TryGetComponent<CubismDisplayInfoParameterName>(out var displayParameterName))
        {
            displayName = displayParameterName.Name;
        }
        parameter.OnSetValue += SetParameterValue;
    }

    private void SetParameterValue(float value)
    {
        parameterValue = value;
    }
    /// <summary>
    /// 将参数重置为默认值
    /// </summary>
    public void ResetToDefault()
    {
        if (parameter == null || Mathf.Approximately(parameter.Value, parameter.DefaultValue)) return;
        parameter.Value = parameter.DefaultValue;
    }
}
/// <summary>
/// 模型动作存储
/// </summary>
[Serializable]
public class ModelMotion
{
    public string motionName;
    public string motionNickname;
    public bool motionOn;
}
/// <summary>
/// 模型表情存储
/// </summary>
[Serializable]
public class ModelExp : IPageListItem
{
    public CubismExp3Json exp3Json;
    public string expName;
    public string expNickname;
    public bool expOn;
    [Header("0:默认 1:自定义")]
    public int type = 0;
    [NonSerialized]
    public List<TmpExpParameter> tempParameters = new ();
    
    public ModelExp(CubismExp3Json parameter,string targetExpName,string targetNickName,bool expOn)
    {
        exp3Json = parameter;
        expName = targetExpName;
        expNickname = targetNickName;
        this.expOn = expOn;
    }
    public ModelExp()
    {
        type = 1;
        exp3Json = new CubismExp3Json();
        expName = "";
        expNickname = "";
        expOn = true;
    }

    public void SetNickName(string taget)
    {
        expNickname = taget;
    }

    public void AddTmpExpParameter(ModelParameter target)
    {
        var flag = false;
        foreach (var tmp in tempParameters)
        {
            if (tmp.parameterId == target.parameterId)
            {
                tmp.value = target.parameterValue;
                flag = true;
                break;
            }
        }

        if (!flag)
        {
            tempParameters.Add(new TmpExpParameter
            {
                parameterId = target.parameterId,
                parameterDisplayName = target.displayName,
                value = target.parameterValue
            });
        }
    }

    public void RemoveTmpExpParameter(ModelParameter target)
    {
        foreach (var tmp in tempParameters)
        {
            if (tmp.parameterId == target.parameterId)
            {
                tempParameters.Remove(tmp);
                break;
            }
        }
    }
    public void RemoveTmpExpParameter(TmpExpParameter target)
    {
        foreach (var tmp in tempParameters)
        {
            if (tmp.parameterId == target.parameterId)
            {
                tempParameters.Remove(tmp);
                break;
            }
        }
    }
    public class TmpExpParameter : IPageListItem
    {
        public string parameterId;
        public string parameterDisplayName;
        public float value;
    }
}