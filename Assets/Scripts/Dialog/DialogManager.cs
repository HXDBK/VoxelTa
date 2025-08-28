using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Character;
using DG.Tweening;
using Newtonsoft.Json;
using Setting;
using TMPro;
using TTS;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WUI;

namespace Dialog
{
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager instance;
        private CharacterManager _characterManager;
        
        [Header("UI")] 
        public SettingPanel settingPanel;
        [SerializeField]
        private Canvas canvas;
        public TMP_InputField userMessageInput;
        public WButton submitButton;
        public Image submitButtonIcon;
        public WButton clearButton;
        public UIPanel dialogPanel;
        public Sprite loadSprite, submitSprite;
        
        public TMP_InputField easyMessageInput;
        public UIPanel easyMessagePanel;
        public WButton easySubmitButton;
        public Image easySubmitButtonIcon;
        public UIPanel easyDialogPanel;
        public TMP_Text easyDialogText;
        public GameObject easyDialogAudioPlay;
        private Vector3 _shift;
        private Tween _delayedCallTween;
        
        public WScrollList scrollList;
        //声音
        public DialogueEntry hasAudioEntry;
        private TalkData CurTalkData => CharacterManager.instance.curCharacter.talkData;
        public event Action<DialogueEntry> OnMessageReceived;
        private void Awake()
        {
            instance = this;
        }

        void Start()
        {
            _characterManager = CharacterManager.instance;
            
            submitButton.onPointerClick.RemoveAllListeners();
            clearButton.onPointerClick.RemoveAllListeners();
            submitButton.onPointerClick.AddListener(Submit);
            easySubmitButton.onPointerClick.AddListener(EasySubmit);
            clearButton.onPointerClick.AddListener(Clear);
            
            _characterManager.OnSetCharacterData += OnChangeCharacter;
            GameManager.instance.OnChangeMode += OnChangeMode;
            userMessageInput.onValidateInput += FilterNewLines;
            easyMessageInput.onValidateInput += FilterNewLines;
            TTSManager.instance.OnGetAudio += OnGetAudio;
            
            ShowDialogPanel();
            
        }
        private char FilterNewLines(string text, int charIndex, char addedChar)
        {
            // 如果不是按住 Ctrl，就禁止插入换行
            if ((addedChar == '\n' || addedChar == '\r') &&
                !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                return '\0'; // 阻止字符插入
            }
            return addedChar; // 允许其他字符
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                RectTransform rt = scrollList.GetComponent<RectTransform>(); 
                Debug.Log(rt.offsetMax);
                Debug.Log(rt.offsetMin);
            }
            // 仅在输入框聚焦时处理
            if (userMessageInput.isFocused)
            {
                if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                {
                    // 没按 Ctrl，表示“提交”
                    if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                    {
                        Submit();

                        // 防止光标被取消
                        userMessageInput.ActivateInputField();
                    }
                    // Ctrl+Enter → 系统允许插入换行，FilterNewLines 会放行
                }
            }else if (easyMessageInput.isFocused)
            {
                if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
                {
                    // 没按 Ctrl，表示“提交”
                    if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                    {
                        EasySubmit();

                        // 防止光标被取消
                        easyMessageInput.ActivateInputField();
                    }
                    // Ctrl+Enter → 系统允许插入换行，FilterNewLines 会放行
                }
            }
        }

        public void ShowDialogPanel()
        {
            dialogPanel.Show();
        }

        public void HideDialogPanel()
        {
            dialogPanel.Hide();
        }
        private void Submit()
        {
            if (string.IsNullOrEmpty(userMessageInput.text)) return;
            if (_characterManager.curCharacter.SettingData.apiUrl is not { Length: > 0 })
            {
                settingPanel.Show();
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            if (_characterManager.curCharacter.SettingData.apiKey is not { Length: > 0 })
            {
                settingPanel.Show();
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            if (_characterManager.curCharacter.SettingData.modelName is not { Length: > 0 })
            {
                settingPanel.Show();
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            if (_characterManager.curCharacter.SettingData.roleName is not { Length: > 0 })
            {
                settingPanel.Show();
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            submitButtonIcon.sprite = loadSprite;

            StartCoroutine(SendWebMessage());
        }
        /// <summary>
        /// 桌面模式下的提交
        /// </summary>
        private void EasySubmit()
        {
            if (string.IsNullOrEmpty(easyMessageInput.text)) return;
            if (_characterManager.curCharacter.SettingData.apiUrl is not { Length: > 0 })
            {
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            if (_characterManager.curCharacter.SettingData.apiKey is not { Length: > 0 })
            {
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            if (_characterManager.curCharacter.SettingData.modelName is not { Length: > 0 })
            {
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            if (_characterManager.curCharacter.SettingData.roleName is not { Length: > 0 })
            {
                MessageManager.instance.ShowMessage("请完善模型接口信息",MessageType.Warning);
                return;
            }
            easyDialogAudioPlay.SetActive(false);
            easySubmitButtonIcon.sprite = loadSprite;
            StartCoroutine(EasySendWebMessage());
        }
        private void Clear()
        {
            userMessageInput.text = "";
        }

        private void OnChangeCharacter(CharacterData target)
        {
            if (target == null)
            {
                ClearData();
            }
            else
            {
                target.talkData ??= new TalkData();
            }
        }
        /// <summary>
        /// 更新对话历史显示
        /// </summary>
        private void UpdateDialogPanel()
        {
            // 获取截断后的对话历史数据
            List<DialogueEntry> trimmedHistory = CurTalkData.dialogueEntries;
            scrollList.SetData(trimmedHistory);
            scrollList.ScrollToBottom();
        }
        private void OnGetAudio(AudioClip target,DialogueEntry entry)
        {
            hasAudioEntry = entry;
            var targetLine = scrollList.GetItem(entry) as DialogLine;
            if (targetLine != null)
            {
                targetLine.playAudioButton.SetActive(true);
            }
            easyDialogAudioPlay.SetActive(true);
        }
        /// <summary>
        /// 发送消息并更新历史
        /// </summary>
        /// <returns></returns>
        IEnumerator SendWebMessage()
        {
            submitButton.Interactable = false;
            userMessageInput.interactable = false;

            var userMessage = userMessageInput.text.TrimEnd('\n');
            var userMessageEnty = new DialogueEntry
            {
                role = "user",
                content = userMessage,
                time = DateTime.Now
            };
            //添加用户消息
            CurTalkData.AddDialogue(userMessageEnty);
            UpdateDialogPanel();
            string jsonBody = GetJsonRequest();
            Debug.Log(jsonBody);
            using (UnityWebRequest request = new UnityWebRequest(CharacterManager.instance.curCharacter.SettingData.apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + CharacterManager.instance.curCharacter.SettingData.apiKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    userMessageEnty.getRequest = true;
                    // Debug.Log(request.downloadHandler.text);
                    (string modelReply, string modelThink) = ExtractReply(request.downloadHandler.text);
                    var entry = new DialogueEntry
                    {
                        role = "assistant",
                        content = modelReply,
                        think = modelThink,
                        // additional = json,
                        time = DateTime.Now
                    };
                    //添加回复消息
                    CurTalkData.AddDialogue(entry);
                    OnMessageReceived?.Invoke(entry);
                    submitButtonIcon.sprite = submitSprite;
                    UpdateDialogPanel();
                    GameManager.instance.SaveData();
                }
                else
                {
                    userMessageEnty.getRequest = false;
                    Debug.LogError("网络请求错误：" + request.error + "\n响应内容：" + request.downloadHandler.text);
                    submitButtonIcon.sprite = submitSprite;
                }
            }

            userMessageInput.interactable = true;
            submitButton.Interactable = true;
            userMessageInput.text = "";
            userMessageInput.ActivateInputField();
        }
        /// <summary>
        /// 桌面模式发送消息并更新历史
        /// </summary>
        /// <returns></returns>
        IEnumerator EasySendWebMessage()
        {
            submitButton.Interactable = false;
            userMessageInput.interactable = false;
            easyMessageInput.interactable = false;
            easySubmitButton.Interactable = false;
            HideEasyDialog();
            
            var userMessage = easyMessageInput.text.TrimEnd('\n');
            var userMessageEnty = new DialogueEntry
            {
                role = "user",
                content = userMessage,
                time = DateTime.Now
            };
            //添加用户消息
            CurTalkData.AddDialogue(userMessageEnty);
            UpdateDialogPanel();
            string jsonBody = GetJsonRequest();
            Debug.Log(jsonBody);
            using (UnityWebRequest request = new UnityWebRequest(CharacterManager.instance.curCharacter.SettingData.apiUrl, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + CharacterManager.instance.curCharacter.SettingData.apiKey);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    userMessageEnty.getRequest = true;
                    Debug.Log(request.downloadHandler.text);
                    (string modelReply, string modelThink) = ExtractReply(request.downloadHandler.text);
                    var entry = new DialogueEntry
                    {
                        role = "assistant",
                        content = modelReply,
                        think = modelThink,
                        // additional = json,
                        time = DateTime.Now
                    };
                    //添加回复消息
                    CurTalkData.AddDialogue(entry);
                    OnMessageReceived?.Invoke(entry);
                    if (!_characterManager.curCharacter.SettingData.isHideDiagOnDesk)
                    {
                        easyDialogPanel.Show();
                    }
                    easySubmitButtonIcon.sprite = submitSprite;
                    SetEasyDialogPanelSize(modelReply);
                    _delayedCallTween?.Kill();
                    _delayedCallTween = DOVirtual.DelayedCall(0.3f*modelReply.Length, () =>
                    {
                        easyDialogPanel.Hide();
                    });
                    GameManager.instance.SaveData();
                }
                else
                {
                    userMessageEnty.getRequest = false;
                    Debug.LogError("网络请求错误：" + request.error + "\n响应内容：" + request.downloadHandler.text);
                    easySubmitButtonIcon.sprite = submitSprite;
                }
            }

            userMessageInput.interactable = true;
            submitButton.Interactable = true;
            easyMessageInput.interactable = true;
            easySubmitButton.Interactable = true;
            
            easyMessageInput.text = "";
            easyMessageInput.ActivateInputField();
        }

        void SetEasyDialogPanelSize(string content)
        {
            easyDialogText.text = ConvertMarkdownToTMP(content);

            // 每个字符估算宽度（你可以根据字体调整）
            const int charWidth = 15;
            const int maxLinePixelWidth = 1000;
            const int lineHeight = 20;
            const int padding = 60;

            string[] lines = content.Split('\n');
            int totalLineCount = 0;
            int maxCharsInLine = 0;

            foreach (string line in lines)
            {
                int lineCharCount = line.Length;
                maxCharsInLine = Mathf.Max(maxCharsInLine, lineCharCount);

                // 估算该行所需行数（包括自动换行）
                int wrappedLines = Mathf.CeilToInt((float)(lineCharCount * charWidth) / maxLinePixelWidth);
                totalLineCount += Mathf.Max(1, wrappedLines); // 至少一行
            }
            float preferredWidth = Mathf.Min(maxCharsInLine * charWidth + padding, maxLinePixelWidth + padding);
            float preferredHeight = totalLineCount * lineHeight + padding;

            // 应用到 text 和 panel
            var size = new Vector2(preferredWidth, preferredHeight);
            easyDialogText.rectTransform.sizeDelta = size;
            easyDialogPanel.GetComponent<RectTransform>().sizeDelta = size + new Vector2(30,0);
        }
        /// <summary>
        /// 根据历史数据构建json
        /// </summary>
        /// <returns></returns>
        private string GetJsonRequest()
        {
            // 构建系统指令
            var systemInstructionBuilder = new StringBuilder();
            
            systemInstructionBuilder.Append(_characterManager.curCharacter.characterDescription);
            Debug.Log(_characterManager.curCharacter.characterDescription);
            // if (!string.IsNullOrEmpty(_characterManager.curCharacter.userName))
            // {
            //     systemInstructionBuilder.Append($"请称呼用户为{_characterManager.curCharacter.userName}。");
            // }
            // if (!string.IsNullOrEmpty(_characterManager.curCharacter.characterName))
            // {
            //     systemInstructionBuilder.Append($"你将被称为{_characterManager.curCharacter.userName}。");
            // }
            // 添加表情信息
            // systemInstructionBuilder.Append(_characterManager.GetCharacterModelStr());
            // 添加记忆内容
            if (_characterManager.curCharacter.HasMemory)
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        systemInstructionBuilder.Append("以下是你的一些重要记忆：");
                        break;
                    case "en":
                        systemInstructionBuilder.Append("Here are some important memories for you:");
                        break;
                }
                systemInstructionBuilder.Append(_characterManager.curCharacter.GetMemorise());
            }
            
            string systemInstruction = systemInstructionBuilder.ToString();

            // 用于生成请求体的消息列表
            List<Message> messages = new List<Message>();
            if (systemInstruction.Length>0)
            {
                messages.Add(new() {
                    role = CharacterManager.instance.curCharacter.SettingData.roleName,
                    content = systemInstruction,
                } );
            }        
            var talkMessages = CurTalkData.GetMessages();
            messages.AddRange(talkMessages);
            // 利用对象序列化生成最终 JSON 字符串
            var requestBody = new
            {
                model = CharacterManager.instance.curCharacter.SettingData.modelName, messages
            };

            return JsonConvert.SerializeObject(requestBody);
        }
        /// <summary>
        /// 从响应中提取模型的回复
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        (string, string) ExtractReply(string response)
        {
            try
            {
                var responseObj = JsonConvert.DeserializeObject<ChatCompletionResponse>(response);
                if (responseObj is { choices: { Count: > 0 } })
                {
                    return (responseObj.choices[0].message.content, responseObj.choices[0].message.reasoning_content);
                }
                else
                {
                    Debug.LogWarning("无法提取模型回复：choices 为空或解析结果为 null");
                    return (string.Empty, string.Empty);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("JSON 解析失败：" + ex.Message);
                // 如果需要记录更详细的错误，确保全局日志回调能捕获当前的 Debug.LogError
                return (string.Empty, string.Empty);
            }
        }
        /// <summary>
        /// 从文本中提取 附加信息:{...} 字段并自动设置动作
        /// </summary>
        private (string, string) ApplyEmbeddedLive2DAction(string text)
        {
            var match = Regex.Match(text, @"附加信息\s*:\s*(\{.*?\})", RegexOptions.IgnoreCase);
            string json = "";
            if (match.Success)
            {
                json = match.Groups[1].Value;
                Debug.Log($"提取到嵌入 附加信息 JSON：{json}");
            }
            else
            {
                Debug.Log("文本中未找到 附加信息 JSON。");
            }

            return (json, Regex.Replace(text, @"附加信息\s*:\s*\{.*?\}", "").Trim());
        }
        public void RemoveLine(DialogueEntry targetEntry)
        {
            int index = CurTalkData.dialogueEntries.IndexOf(targetEntry);
            if (index == -1)
            {
                Debug.LogWarning("未找到指定的对话条目，无法移除后续内容。");
                return;
            }

            // 移除数据模型中 targetEntry 之后的所有条目（不包括自身）
            int countToRemove = CurTalkData.dialogueEntries.Count - index;
            if (countToRemove > 0)
            {
                CurTalkData.dialogueEntries.RemoveRange(index, countToRemove);
            }
            GameManager.instance.SaveData();
            // 重新刷新 UI（UpdateDialogPanel 会根据 CurTalkData 重建 UI）
            UpdateDialogPanel();
        }
        public void EditLine(DialogueEntry targetEntry, string newTalk,string newAdditional="")
        {
            // 修改数据
            targetEntry.content = RemoveRichText(newTalk);
            if (newAdditional != "")
            {
                targetEntry.additional = newAdditional;
            }
            GameManager.instance.SaveData();
            // 刷新 UI
            UpdateDialogPanel();
        }
        /// <summary>
        /// 移除字符串中的 Unity 富文本标签
        /// </summary>
        private string RemoveRichText(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // 匹配 <...> 和 </...>
            return Regex.Replace(input, @"<\/?[^>]+?>", string.Empty);
        }
        private void OnChangeMode(int targetInx)
        {
            RectTransform rt = scrollList.GetComponent<RectTransform>();
            switch (targetInx)
            {
                case 0:
                    dialogPanel.Show();
                    easyDialogPanel.Hide();
                    easyMessagePanel.Hide();
                    // 先直接设置锚点和 pivot，保证动画前参数正确
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(20, 266);
                    rt.offsetMax = new Vector2(-20, -65);
                    UpdateDialogPanel();
                    scrollList.ScrollToBottom();
                    break;
                case 1:
                    dialogPanel.Show();
                    easyDialogPanel.Hide();
                    easyMessagePanel.Hide();
                    // Canvas tmp = GetComponentInParent<Canvas>();
                    float uiHalfWidth = Screen.width / canvas.scaleFactor / 2f;
                    var size = new Vector2(uiHalfWidth,rt.rect.height);
                    // 设置锚点和 pivot：右对齐并上下拉伸
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 0.5f);
                    rt.anchoredPosition = new Vector2(-20, rt.anchoredPosition.y);
                    rt.sizeDelta = size;
                    rt.offsetMin = new Vector2(rt.offsetMin.x, 266);
                    rt.offsetMax = new Vector2(rt.offsetMax.x, -65);
                    UpdateDialogPanel();
                    scrollList.ScrollToBottom();
                    break;
                case 2:
                    dialogPanel.Hide();
                    break;
            }
        }
        public void SaveEasyInputPos()
        {
            CharacterManager.instance.curCharacter.easyInputPos = easyMessagePanel.transform.position;
            CharacterManager.instance.curCharacter.easyDialogPos = easyDialogPanel.transform.position;
            GameManager.instance.SaveData();
        }
        public void ShowEasyInput()
        {
            SetPanelPosition(_characterManager.curModel.transform.position);
            easyMessagePanel.Show();
            easyMessageInput.ActivateInputField();
        }
        public void HideEasyInput()
        {
            easyMessagePanel.Hide();
        }
        public void ShowEasyDialog()
        {
            SetPanelPosition(_characterManager.curModel.transform.position);
            if (CurTalkData.dialogueEntries.Count > 0 && CurTalkData.dialogueEntries[^1].role == "assistant")
            {
                easyDialogText.text = ConvertMarkdownToTMP(CurTalkData.dialogueEntries[^1].content);
                Debug.Log(easyDialogText.text);
                SetEasyDialogPanelSize(CurTalkData.dialogueEntries[^1].content);
                _delayedCallTween?.Kill();
                _delayedCallTween = DOVirtual.DelayedCall(0.1f * CurTalkData.dialogueEntries[^1].content.Length, () =>
                {
                    easyDialogPanel.Hide();
                });
                // OnMessageReceived?.Invoke(CurTalkData.dialogueEntries[^1].content);
                easyDialogPanel.Show();
            }else if(CurTalkData.dialogueEntries.Count > 1 && CurTalkData.dialogueEntries[^2].role == CharacterManager.instance.curCharacter.SettingData.roleName)
            {
                easyDialogText.text = ConvertMarkdownToTMP(CurTalkData.dialogueEntries[^2].content);
                SetEasyDialogPanelSize(CurTalkData.dialogueEntries[^2].content);
                _delayedCallTween?.Kill();
                _delayedCallTween = DOVirtual.DelayedCall(0.1f*CurTalkData.dialogueEntries[^2].content.Length, () =>
                {
                    easyDialogPanel.Hide();
                });
                // OnMessageReceived?.Invoke(CurTalkData.dialogueEntries[^1].content);
                easyDialogPanel.Show();
            }
        }
        private void HideEasyDialog()
        {
            easyDialogPanel.Hide();
            _delayedCallTween?.Kill();
            _delayedCallTween = DOVirtual.DelayedCall(5f, () =>
            {
                easyDialogPanel.Hide();
            });
        }
        public void HideEasyDialogNow()
        {
            _delayedCallTween?.Kill();
            easyDialogPanel.Hide();
        }
        private void SetPanelPosition(Vector3 worldPosition)
        {
            if (CharacterManager.instance.curCharacter.easyInputPos == Vector3.zero)
            {
                // 将世界坐标转为屏幕坐标
                Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);

                // 再将屏幕坐标转为 UI 局部坐标（针对 Canvas）
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screenPos,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
                    out Vector2 localPos
                );

                // 设置位置
                easyMessagePanel.GetComponent<RectTransform>().anchoredPosition = localPos;
                easyDialogPanel.GetComponent<RectTransform>().anchoredPosition = localPos;
            }
            else
            {
                easyMessagePanel.transform.position = CharacterManager.instance.curCharacter.easyInputPos;
                easyDialogPanel.transform.position = CharacterManager.instance.curCharacter.easyDialogPos;
            }
        }
        /// <summary>
        /// 解析markdown
        /// </summary>
        /// <param name="markdown"></param>
        /// <returns></returns>
        private string ConvertMarkdownToTMP(string markdown)
        {
            string result = markdown;

            // 多级标题 # ## ### #### #####
            result = Regex.Replace(result, @"^(######) (.+)$", "<size=110%><b>$2</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^(#####) (.+)$", "<size=120%><b>$2</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^(####) (.+)$", "<size=140%><b>$2</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^(###) (.+)$", "<size=160%><b>$2</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^(##) (.+)$", "<size=180%><b>$2</b></size>", RegexOptions.Multiline);
            result = Regex.Replace(result, @"^(#) (.+)$", "<size=200%><b>$2</b></size>", RegexOptions.Multiline);

            // 加粗 **
            result = Regex.Replace(result, @"\*\*(.+?)\*\*", "<b>$1</b>");

            // 斜体 *
            result = Regex.Replace(result, @"\*(.+?)\*", "<i>$1</i>");

            // 列表项 - 
            result = Regex.Replace(result, @"^- (.+)$", "• $1", RegexOptions.Multiline);

            // 水平线 ---
            result = Regex.Replace(result, @"^\s*---\s*$", "<color=#000000>————————————</color></size>", RegexOptions.Multiline);

            // 对话解析（支持中文引号 “” 和英文引号 ""）
            // 示例：“你好” → <b><color=#FF5555>“你好”</color></b>
            string dialogColor = "#294A70"; // 自定义颜色（如橙色）
            result = Regex.Replace(result, @"[“""](.+?)[”""]", $"<b><color={dialogColor}>“$1”</color></b>");

            // 换行处理
            result = result.Replace("\r\n", "\n").Replace("\r", "\n");

            return result;
        }
        public void Refresh()
        {
            scrollList.Refresh();
            scrollList.ScrollToBottom();
        }

        public void ClearData()
        {
            scrollList.Clear();
        }
    }
    [Serializable]
    public class ChatCompletionResponse
    {
        public List<Choice> choices;
    }

    [Serializable]
    public class Choice
    {
        public ResponseMessage message;
    }

    [Serializable]
    public class ResponseMessage
    {
        public string role;
        public string content;

        // ReSharper disable once InconsistentNaming
        public string reasoning_content;
    }
}
