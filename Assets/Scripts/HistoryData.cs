using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "新配置", menuName = "配置和历史", order = 1)]
public class HistoryData : ScriptableObject
{
    [TextArea(5,30)]
    public string settingInstruction;
    public string aiCallName;
    public string playerCallName;
    public string apiKey;
    public int maxCharacterCount = 5000;
    public string apiUrl;
    public string modelName;
    public string ruleName;
    public int modelInx;
    public int sceneInx;
    public int timeInx;
    public bool talkAudioIsOn;
    
    [Header("人物立绘")]
    public string characterImagePath;  // 用于保存立绘的路径
    public string headImagePath;  // 用于保存立绘的路径
    [Header("声音相关")]
    public string audioAPI;
    public string audioReferPath;
    public string audioReferText;
    
    [Serializable]
    public class Message
    {
        public string role;
        [TextArea(5,30)]
        public string content;
        public string time; // ISO8601 字符串
        public string additional; 
        [TextArea(5,30)]
        public string think;
    }
    [Serializable]
    public class SceneData
    {
        public string sceneName;
        public string scenePicUrl;
    }
    public List<Message> conversationHistory = new List<Message>();
    public List<Memory> memories = new List<Memory>();
    public List<SceneData> scenes = new List<SceneData>();
}
