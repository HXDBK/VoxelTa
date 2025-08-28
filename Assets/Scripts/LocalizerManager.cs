using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class LocalizerManager : MonoBehaviour
{
    public static LocalizerManager instance;
    public List<LocalizationItem> localizationItems = new List<LocalizationItem>
    {
    new LocalizationItem { key = "请先为当前对话设置模型", enValue = "Please set a model for the current dialogue first" },
    new LocalizationItem { key = "背景图片已删除", enValue = "Background image deleted" },
    new LocalizationItem { key = "背景图片已设置", enValue = "Background image set successfully" },
    new LocalizationItem { key = "背景图片大小已重置", enValue = "Background image size reset" },
    new LocalizationItem { key = "背景图片位置已重置", enValue = "Background image position reset" },
    new LocalizationItem { key = "已保存", enValue = "Saved successfully" },
    new LocalizationItem { key = "请输入表情名称", enValue = "Please enter an expression name" },
    new LocalizationItem { key = "请输入表情识别名称", enValue = "Please enter an expression recognition name" },
    new LocalizationItem { key = "请输入表情识别标识", enValue = "Please enter an expression recognition ID" },
    new LocalizationItem { key = "请输入表情识别关键字", enValue = "Please enter an expression recognition keyword" },
    new LocalizationItem { key = "请输入淡入时长", enValue = "Please enter fade-in duration" },
    new LocalizationItem { key = "请输入淡出时长", enValue = "Please enter fade-out duration" },
    new LocalizationItem { key = "请至少添加一个表情参数", enValue = "Please add at least one expression parameter" },
    new LocalizationItem { key = "请先选择模型", enValue = "Please select a model first" },
    new LocalizationItem { key = "请先选择模型信息", enValue = "Please select model information first" },
    new LocalizationItem { key = "请先选择模型路径信息", enValue = "Please select model path information first" },
    new LocalizationItem { key = "请先选择表情识别信息", enValue = "Please select expression recognition information first" },
    new LocalizationItem { key = "请先选择表情参数信息", enValue = "Please select expression parameter information first" },
    new LocalizationItem { key = "请先选择背景信息", enValue = "Please select background information first" },
    new LocalizationItem { key = "请先选择语音模块信息", enValue = "Please select voice module information first" },
    new LocalizationItem { key = "请先输入用户信息", enValue = "Please enter user information first" },
    new LocalizationItem { key = "请先输入用户昵称", enValue = "Please enter user nickname first" },
    new LocalizationItem { key = "请先输入用户称呼", enValue = "Please enter user display name first" },
    new LocalizationItem { key = "请先输入对话信息", enValue = "Please enter dialogue information first" },
    new LocalizationItem { key = "请先输入模型路径信息", enValue = "Please enter model path information first" },
    new LocalizationItem { key = "请先输入表情识别信息", enValue = "Please enter expression recognition information first" },
    new LocalizationItem { key = "请先输入表情参数信息", enValue = "Please enter expression parameter information first" },
    new LocalizationItem { key = "请先输入背景信息", enValue = "Please enter background information first" },
    new LocalizationItem { key = "请先输入语音模块信息", enValue = "Please enter voice module information first" },
    new LocalizationItem { key = "请先输入语音模块路径信息", enValue = "Please enter voice module path information first" },
    new LocalizationItem { key = "背景图片上传失败", enValue = "Background image upload failed" },
    new LocalizationItem { key = "请选择一个模型", enValue = "Please select a model" },
    new LocalizationItem { key = "请选择一个语音模块", enValue = "Please select a voice module" },
    new LocalizationItem { key = "请选择一个背景", enValue = "Please select a background" },
    new LocalizationItem { key = "保存成功", enValue = "Saved successfully" },
    new LocalizationItem { key = "保存失败", enValue = "Save failed" },
    new LocalizationItem { key = "语音播放失败", enValue = "Voice playback failed" }
};

    public Toggle chineseToggle;
    public Toggle enmToggle;
    public string curLanguageCode = "zh-Hans";
    private void Awake()
    {
        instance = this;
    }

    IEnumerator Start()
    {
        var init = LocalizationSettings.InitializationOperation;
        if (!init.IsDone) yield return init;
        curLanguageCode = ES3.Load("language",defaultValue:"zh-Hans");
        chineseToggle.isOn = curLanguageCode == "zh-Hans";
        enmToggle.isOn = curLanguageCode != "zh-Hans";
        Debug.Log(chineseToggle.isOn);
        SetByCode(curLanguageCode);
        chineseToggle.onValueChanged.AddListener(ChangeLanguage);
    }

    private void ChangeLanguage(bool isChinese)
    {
        if (isChinese)
        {
            curLanguageCode = "zh-Hans";
            ES3.Save("language",curLanguageCode);
            SetByCode(curLanguageCode);
        }
        else
        {
            curLanguageCode = "en";
            ES3.Save("language",curLanguageCode);
            SetByCode(curLanguageCode);
        }
    }
    
    public void ChangeLanguage(string languageCode)
    {
        curLanguageCode = languageCode;
        ES3.Save("language",curLanguageCode);
        SetByCode(curLanguageCode);
        chineseToggle.onValueChanged.RemoveListener(ChangeLanguage);
        chineseToggle.isOn = curLanguageCode == "zh-Hans";
        chineseToggle.onValueChanged.AddListener(ChangeLanguage);
    }
    
    public static void SetByCode(string code)
    {
        var loc = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (loc != null)
            LocalizationSettings.SelectedLocale = loc; // 触发全部 UI 刷新
    }
    
    /// <summary>
    /// 翻译
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetValue(string key)
    {
        switch (LocalizationSettings.SelectedLocale.Identifier.Code)
        {
            case "zh-Hans":
                return key;
            case "en":
                foreach (var item in instance.localizationItems)
                {
                    if (item.key == key)
                    {
                        Debug.Log(item.enValue);
                        return item.enValue;
                    }
                }
                break;
        }

        return key;
    }
    /// <summary>
    /// 当前使用语言
    /// </summary>
    /// <returns></returns>
    public static string GetCode()
    {
        return LocalizationSettings.SelectedLocale.Identifier.Code;
    }
    [Serializable]
    public class LocalizationItem
    {
        public string key;
        public string enValue;
    }
}