using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Character;
using Dialog;
using Newtonsoft.Json;
using SFB;
using TMPro;
using TTS;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using WUI;
using Debug = UnityEngine.Debug;

namespace Setting
{
    public class SettingPanel : UIPanel
    {
        public TMP_Dropdown modeDropdown;
        public TMP_InputField apiUrlInput;
        public TMP_InputField modelNameInput;

        public TMP_InputField roleNameInput;
        public TMP_InputField apiKeyInput;
        public TMP_InputField maxCharInput;
        public Toggle isTextStreamToggle;

        private SettingData _settingData;
        public WButton showOrHideKeyButton;
        public Image showOrHideKeyImage;
        public Sprite showSprite;
        public Sprite hideSprite;
        
        //tts
        public Toggle ttsToggle;
        public Toggle isShowDiagOnDeskToggle;
        public Toggle isStreamToggle;
        public Toggle onlyQuotationMarksToggle;
        public Toggle removeBracketToggle;
        
        public TMP_InputField audioAPIInput;
        public TMP_InputField audioReferPathInput;
        public TMP_InputField audioReferTextInput;
        public TMP_Dropdown textLangDropdown;
        public TMP_Dropdown promptLangDropdown;

        public WButton saveButton;
        private bool _isChangeData;
        
        // 添加标志位防止循环触发
        private bool _isLoadingData = false;

        protected override void Start()
        { 
            base.Start();
            
            // 注册事件监听器
            RegisterEventListeners();
            
            saveButton.gameObject.SetActive(false);
            showOrHideKeyButton.onPointerClick.AddListener(ShowApiKey);
        }

        private void RegisterEventListeners()
        {
            // 使用包装方法来检查是否正在加载数据
            modeDropdown.onValueChanged.AddListener(OnModeDropdownChanged);
            apiUrlInput.onValueChanged.AddListener(OnInputFieldChanged);
            modelNameInput.onValueChanged.AddListener(OnInputFieldChanged);
            roleNameInput.onValueChanged.AddListener(OnInputFieldChanged);
            apiKeyInput.onValueChanged.AddListener(OnInputFieldChanged);
            maxCharInput.onValueChanged.AddListener(OnInputFieldChanged);
            isTextStreamToggle.onValueChanged.AddListener(OnToggleChanged);
            
            ttsToggle.onValueChanged.AddListener(OnToggleChanged);
            isShowDiagOnDeskToggle.onValueChanged.AddListener(OnToggleChanged);
            isStreamToggle.onValueChanged.AddListener(OnToggleChanged);
            onlyQuotationMarksToggle.onValueChanged.AddListener(OnToggleChanged);
            removeBracketToggle.onValueChanged.AddListener(OnToggleChanged);
            
            textLangDropdown.onValueChanged.AddListener(OnDropdownChanged);
            promptLangDropdown.onValueChanged.AddListener(OnDropdownChanged);
            
            audioAPIInput.onValueChanged.AddListener(OnInputFieldChanged);
            audioReferPathInput.onValueChanged.AddListener(OnInputFieldChanged);
            audioReferTextInput.onValueChanged.AddListener(OnInputFieldChanged);
        }

        // 包装方法：检查是否正在加载数据
        private void OnModeDropdownChanged(int value)
        {
            if (!_isLoadingData)
            {
                SetModel(value);
            }
        }

        private void OnInputFieldChanged(string value)
        {
            if (!_isLoadingData)
            {
                SetData();
            }
        }

        private void OnToggleChanged(bool value)
        {
            if (!_isLoadingData)
            {
                SetData();
            }
        }

        private void OnDropdownChanged(int value)
        {
            if (!_isLoadingData)
            {
                SetData();
            }
        }

        public override void Show()
        {
            if (CharacterManager.instance.curCharacter != null)
            {
                base.Show();
                _settingData = CharacterManager.instance.curCharacter.SettingData;
                Debug.Log(_settingData.modelName);
                LoadUIFromData();
            }
            else
            {
                MessageManager.instance.ShowMessage("请先选择一个对话", MessageType.Warning);
            }
        }
        
        public void HideApiKey()
        {
            showOrHideKeyButton.onPointerClick.RemoveAllListeners();
            showOrHideKeyButton.onPointerClick.AddListener(ShowApiKey);
            showOrHideKeyImage.sprite = showSprite;
            apiKeyInput.contentType = TMP_InputField.ContentType.Password;
            apiKeyInput.ForceLabelUpdate();
        }

        public void ShowApiKey()
        {
            showOrHideKeyButton.onPointerClick.RemoveAllListeners();
            showOrHideKeyButton.onPointerClick.AddListener(HideApiKey);
            showOrHideKeyImage.sprite = hideSprite;
            apiKeyInput.contentType = TMP_InputField.ContentType.Standard;
            apiKeyInput.ForceLabelUpdate();
        }
        
        public void SetReferAudioPath()
        {
            // 打开文件选择器
            string[] paths = StandaloneFileBrowser.OpenFilePanel("选择参考音频", "", "wav", false);

            if (paths.Length > 0)
            {
                audioReferPathInput.text = paths[0];
            }
        }

        public void PlayReferAudio()
        {
            TTSManager.instance.PlayLocalAudio(audioReferPathInput.text);
        }
        
        private void LoadUIFromData()
        {
            // 设置标志位，防止触发事件
            _isLoadingData = true;
            
            try
            {
                // 反映当前设置数据到UI控件
                int modeIndex = modeDropdown.options.FindIndex(opt => opt.text == _settingData.modelType);
                modeDropdown.SetValueWithoutNotify((modeIndex >= 0) ? modeIndex : 0);
                
                // 根据模式设置UI
                UpdateUIForMode(modeIndex);
                
                // 设置其他UI控件的值（使用SetValueWithoutNotify或SetTextWithoutNotify）
                SetInputFieldWithoutNotify(maxCharInput, _settingData.maxCharCount.ToString());
                // isTextStreamToggle.SetIsOnWithoutNotify(_settingData.isTextStreaming);
                
                SetInputFieldWithoutNotify(audioAPIInput, _settingData.ttsApiUrl);
                SetInputFieldWithoutNotify(audioReferPathInput, _settingData.ttsReferPath);
                SetInputFieldWithoutNotify(audioReferTextInput, _settingData.ttsReferText);
                ttsToggle.SetIsOnWithoutNotify(_settingData.ttsIson);
                isStreamToggle.SetIsOnWithoutNotify(_settingData.isStreaming);
                
                textLangDropdown.SetValueWithoutNotify(_settingData.textLang switch
                {
                    "zh" => 0,
                    "en" => 1,
                    "ja" => 2,
                    _ => 0
                });
                
                promptLangDropdown.SetValueWithoutNotify(_settingData.promptLang switch
                {
                    "zh" => 0,
                    "en" => 1,
                    "ja" => 2,
                    _ => 0
                });
                
                onlyQuotationMarksToggle.SetIsOnWithoutNotify(_settingData.onlyQuotationMarks);
                removeBracketToggle.SetIsOnWithoutNotify(_settingData.removeBracket);
                isShowDiagOnDeskToggle.SetIsOnWithoutNotify(_settingData.isHideDiagOnDesk);
            }
            finally
            {
                // 确保标志位被重置
                _isLoadingData = false;
            }
        }

        private void UpdateUIForMode(int modeIndex)
        {
            switch (modeIndex)
            {
                case 0:
                    SetInputFieldWithoutNotify(apiUrlInput, "https://api.deepseek.com/chat/completions");
                    apiUrlInput.readOnly = true;
                    SetInputFieldWithoutNotify(modelNameInput, _settingData.modelName);
                    SetInputFieldWithoutNotify(roleNameInput, "system");
                    roleNameInput.readOnly = true;
                    SetInputFieldWithoutNotify(apiKeyInput, _settingData.apiKey);
                    isTextStreamToggle.SetIsOnWithoutNotify(_settingData.isTextStreaming);
                    isTextStreamToggle.interactable = true;
                    break;
                case 1:
                    SetInputFieldWithoutNotify(apiUrlInput, "https://api.openai.com/v1/chat/completions");
                    apiUrlInput.readOnly = true;
                    SetInputFieldWithoutNotify(modelNameInput, _settingData.modelName);
                    SetInputFieldWithoutNotify(roleNameInput, "developer");
                    roleNameInput.readOnly = true;
                    SetInputFieldWithoutNotify(apiKeyInput, _settingData.apiKey);
                    isTextStreamToggle.SetIsOnWithoutNotify(_settingData.isTextStreaming);
                    isTextStreamToggle.interactable = true;
                    break;
                case 2:
                    SetInputFieldWithoutNotify(apiUrlInput, "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions");
                    apiUrlInput.readOnly = true;
                    SetInputFieldWithoutNotify(modelNameInput, _settingData.modelName);
                    SetInputFieldWithoutNotify(roleNameInput, "developer");
                    roleNameInput.readOnly = true;
                    SetInputFieldWithoutNotify(apiKeyInput, _settingData.apiKey);
                    isTextStreamToggle.SetIsOnWithoutNotify(false);
                    _settingData.isTextStreaming = false;
                    isTextStreamToggle.interactable = false;
                    break;
                case 3:
                    SetInputFieldWithoutNotify(apiUrlInput, _settingData.apiUrl);
                    apiUrlInput.readOnly = false;
                    SetInputFieldWithoutNotify(modelNameInput, _settingData.modelName);
                    SetInputFieldWithoutNotify(roleNameInput, _settingData.roleName);
                    roleNameInput.readOnly = false;
                    SetInputFieldWithoutNotify(apiKeyInput, _settingData.apiKey);
                    isTextStreamToggle.SetIsOnWithoutNotify(false);
                    _settingData.isTextStreaming = false;
                    isTextStreamToggle.interactable = false;
                    break;
            }
        }

        // 辅助方法：设置InputField的值而不触发事件
        private void SetInputFieldWithoutNotify(TMP_InputField inputField, string value)
        {
            inputField.text = value;
            // 强制更新显示但不触发事件
            inputField.ForceLabelUpdate();
        }

        private void SetData()
        {
            if (_settingData == null) return;
            
            saveButton.gameObject.SetActive(true);
            _isChangeData = true;
            
            _settingData.modelType = modeDropdown.options[modeDropdown.value].text;
            _settingData.apiUrl = apiUrlInput.text;
            _settingData.modelName = modelNameInput.text;
            _settingData.roleName = roleNameInput.text;
            _settingData.apiKey = apiKeyInput.text;
            
            // 添加安全检查
            if (int.TryParse(maxCharInput.text, out int maxChar))
            {
                _settingData.maxCharCount = maxChar;
            }
            
            _settingData.isTextStreaming = isTextStreamToggle.isOn;

            _settingData.ttsApiUrl = audioAPIInput.text;
            _settingData.ttsReferPath = audioReferPathInput.text;
            _settingData.ttsReferText = audioReferTextInput.text;
            _settingData.ttsIson = ttsToggle.isOn;
            _settingData.isStreaming = isStreamToggle.isOn;
            
            _settingData.textLang = textLangDropdown.value switch
            {
                0 => "zh",
                1 => "en",
                2 => "ja",
                _ => "zh"
            };
            
            _settingData.promptLang = promptLangDropdown.value switch
            {
                0 => "zh",
                1 => "en",
                2 => "ja",
                _ => "zh"
            };
            
            _settingData.onlyQuotationMarks = onlyQuotationMarksToggle.isOn;
            _settingData.removeBracket = removeBracketToggle.isOn;
            _settingData.isHideDiagOnDesk = isShowDiagOnDeskToggle.isOn;
        }

        public void Changed()
        {
            saveButton.gameObject.SetActive(true);
            _isChangeData = true;
        }
        
        private void SetModel(int inx)
        {
            // 设置标志位防止循环
            _isLoadingData = true;
            
            try
            {
                switch (inx)
                {
                    case 0:
                        SetInputFieldWithoutNotify(apiUrlInput, "https://api.deepseek.com/chat/completions");
                        apiUrlInput.readOnly = true;
                        SetInputFieldWithoutNotify(roleNameInput, "system");
                        roleNameInput.readOnly = true;
                        break;
                    case 1:
                        SetInputFieldWithoutNotify(apiUrlInput, "https://api.openai.com/v1/chat/completions");
                        apiUrlInput.readOnly = true;
                        SetInputFieldWithoutNotify(roleNameInput, "developer");
                        roleNameInput.readOnly = true;
                        break;
                    case 2:
                        SetInputFieldWithoutNotify(apiUrlInput, "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions");
                        apiUrlInput.readOnly = true;
                        SetInputFieldWithoutNotify(roleNameInput, "developer");
                        roleNameInput.readOnly = true;
                        break;
                    case 3:
                        apiUrlInput.readOnly = false;
                        roleNameInput.readOnly = false;
                        break;
                }
            }
            finally
            {
                _isLoadingData = false;
            }
            
            SetData();
        }
        
        private void ChangeBgmVolume(float vol)
        {
            // bgmAudioSource.volume = vol;
        }
        
        public void SaveData()
        {
            MessageManager.instance.ShowMessage("已保存", MessageType.Success);
            SetData();
            DialogManager.instance.Refresh();
            _isChangeData = false;
            saveButton.gameObject.SetActive(false);
            GameManager.instance.SaveSettingData();
        }
        
        public void HidePanel()
        {
            if (_isChangeData)
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("是否保存", "有未保存的数据，是否需要保存", () =>
                        {
                            SaveData();
                            DoHidePanel();
                        }, () =>
                        {
                            SetDefData();
                            DoHidePanel();
                        }, "保存", "不保存");
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Save?", "There is unsaved data. Do you want to save it?", () =>
                        {
                            SaveData();
                            DoHidePanel();
                        }, () =>
                        {
                            SetDefData();
                            DoHidePanel();
                        }, "Save", "Don't Save");
                        break;
                }
            }
            else
            {
                DoHidePanel();
            }
        }
        
        private void DoHidePanel()
        {
            Hide();
        }
        
        public void SetDefData()
        {
            _settingData = CharacterManager.instance.curCharacter.SettingData;
            _isChangeData = false;
            saveButton.gameObject.SetActive(false);
        }
        
        public void OpenSaveLocation()
        {
            string path = Application.persistentDataPath;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_STANDALONE_OSX
            Process.Start("open", path);
#elif UNITY_ANDROID
            Debug.Log("请通过设备文件管理器访问此路径: " + path);
#elif UNITY_IOS
            Debug.Log("iOS中无法直接打开此路径，请使用Xcode设备管理工具。路径: " + path);
#else
            Debug.Log("当前平台不支持自动打开保存路径");
#endif
        }
        
        [System.Serializable]
        public class Model
        {
            public string id;
            public string @object;   // 注意：object 是 C# 的关键字，用 @object 表示
            public string owned_by;
        }

        [System.Serializable]
        public class ModelsResponse
        {
            public string @object;
            public List<Model> data;
        }
    }
}