using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using WUI;

[Serializable]
[ES3Serializable]
public class TalkData
{
    public List<DialogueEntry> dialogueEntries = new ();
    /// <summary>
    /// 构建 JSON 请求体，传递历史消息
    /// </summary>
    /// <returns></returns>
    public List<Message> GetMessages()
    {
        StringBuilder strB = new StringBuilder();
        // 用于生成请求体的消息列表
        List<Message> messages = new List<Message>();
        // 将截断后的对话历史添加进请求体
        List<DialogueEntry> trimmedHistory = TrimHistoryToMaxCharacters(dialogueEntries);
        foreach (var entry in trimmedHistory)
        {
            messages.Add(new Message { role = entry.role, content = entry.content+(entry.additional is {Length:>0}?"附加信息:"+entry.additional:"") });
        }
        return messages;
    }
    /// <summary>
    /// 解析存储的历史数据
    /// </summary>
    /// <param name="targetHistory"></param>
    /// <returns></returns>
    List<DialogueEntry> TrimHistoryToMaxCharacters(List<DialogueEntry> targetHistory)
    {
        int totalLength = 0;
        var trimmed = new List<DialogueEntry>();
        var maxChar = GameManager.instance.SettingData.maxCharCount;
        for (int i = targetHistory.Count - 1; i >= 0; i--)
        {
            totalLength += targetHistory[i].content.Length;
            
            if (totalLength > maxChar) break;
            trimmed.Insert(0, targetHistory[i]);
        }

        return trimmed;
    }
    /// <summary>
    /// 增加对话
    /// </summary>
    /// <param name="entry"></param>
    public void AddDialogue(DialogueEntry entry)
    {
        dialogueEntries.Add(entry);
    }
    /// <summary>
    /// 移除对话
    /// </summary>
    /// <param name="entry"></param>
    public void RemoveDialogue(DialogueEntry entry)
    {
        dialogueEntries.Remove(entry);
    }
    public TalkData Clone()
    {
        var clone = new TalkData();
        foreach (var entry in this.dialogueEntries)
        {
            clone.dialogueEntries.Add(new DialogueEntry
            {
                role = entry.role,
                content = entry.content,
                think = entry.think,
                additional = entry.additional,
                time = entry.time,
                getRequest = entry.getRequest
            });
        }
        return clone;
    }
}
[Serializable]
public class Message
{
    public string role;
    public string content;
}
[Serializable]
public class DialogueEntry : IPageListItem
{
    public string role;
    public string content;
    public string think;
    public string additional;
    public DateTime time;
    public bool getRequest = true;
}