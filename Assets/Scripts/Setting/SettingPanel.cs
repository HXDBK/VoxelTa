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
         private SettingData _settingData;
         public WButton showOrHideKeyButton;
         public Image showOrHideKeyImage;
         public Sprite showSprite;
         public Sprite hideSprite;
         
         //tts
         public Toggle ttsToggle;
         public Toggle isShowDiagOnDeskToggle;
         public TMP_InputField audioAPIInput;
         public TMP_InputField audioReferPathInput;
         public TMP_InputField audioReferTextInput;

         public WButton saveButton;
         private bool _isChangeData;

         protected override void Start()
         {
             // _settingData = GameManager.instance.settingData;
            base.Start();
             modeDropdown.onValueChanged.AddListener(SetModel);
             apiUrlInput.onValueChanged.AddListener(str=>SetData());
             modelNameInput.onValueChanged.AddListener(str=>SetData());
             roleNameInput.onValueChanged.AddListener(str=>SetData());
             apiKeyInput.onValueChanged.AddListener(str=>SetData());
             maxCharInput.onValueChanged.AddListener(str=>SetData());
             ttsToggle.onValueChanged.AddListener(str=>SetData());
             audioAPIInput.onValueChanged.AddListener(str=>SetData());
             audioReferPathInput.onValueChanged.AddListener(str=>SetData());
             audioReferTextInput.onValueChanged.AddListener(str=>SetData());
             isShowDiagOnDeskToggle.onValueChanged.AddListener(str=>SetData());
             
             saveButton.gameObject.SetActive(false);
             showOrHideKeyButton.onPointerClick.AddListener(ShowApiKey);
         }

         public override void Show()
         {
             if (CharacterManager.instance.curCharacter != null)
             {
                 base.Show();
                 _settingData = CharacterManager.instance.curCharacter.SettingData;
                 LoadUIFromData();
             }
             else
             {
                 MessageManager.instance.ShowMessage("请先选择一个对话",MessageType.Warning);
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
             // 反映当前设置数据到UI控件
             int modeIndex = modeDropdown.options.FindIndex(opt => opt.text == _settingData.modelType);
             modeDropdown.value = (modeIndex >= 0) ? modeIndex : 0;
             switch (modeIndex)
             {
                 case 0:
                     apiUrlInput.text = "https://api.deepseek.com/chat/completions";
                     apiUrlInput.readOnly = true;
                     modelNameInput.text = _settingData.modelName;
                     roleNameInput.text = "system";
                     roleNameInput.readOnly = true;
                     apiKeyInput.text = _settingData.apiKey;
                     break;
                 case 1:
                     apiUrlInput.text = "https://api.openai.com/v1/chat/completions";
                     apiUrlInput.readOnly = true;
                     modelNameInput.text = _settingData.modelName;
                     roleNameInput.text = "developer";
                     roleNameInput.readOnly = true;
                     apiKeyInput.text = _settingData.apiKey;
                     break;
                 case 2:
                     apiUrlInput.text = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
                     apiUrlInput.readOnly = true;
                     modelNameInput.text = _settingData.modelName;
                     roleNameInput.text = "developer";
                     roleNameInput.readOnly = true;
                     apiKeyInput.text = _settingData.apiKey;
                     break;
                 case 3:
                     apiUrlInput.text = _settingData.apiUrl;
                     apiUrlInput.readOnly = false;
                     modelNameInput.text = _settingData.modelName;
                     roleNameInput.text = _settingData.roleName;
                     roleNameInput.readOnly = false;
                     apiKeyInput.text = _settingData.apiKey;
                     break;
             }
             maxCharInput.text = _settingData.maxCharCount.ToString();
             audioAPIInput.text = _settingData.ttsApiUrl;
             audioReferPathInput.text = _settingData.ttsReferPath;
             audioReferTextInput.text = _settingData.ttsReferText;
             ttsToggle.isOn = _settingData.ttsIson;
             isShowDiagOnDeskToggle.isOn = _settingData.isHideDiagOnDesk;
         }

         public void SetData()
         {
             saveButton.gameObject.SetActive(true);
             _isChangeData = true;
             _settingData.modelType = modeDropdown.options[modeDropdown.value].text;
             _settingData.apiUrl = apiUrlInput.text;
             _settingData.modelName = modelNameInput.text;
             _settingData.roleName = roleNameInput.text;
             _settingData.apiKey = apiKeyInput.text;
             _settingData.maxCharCount = int.Parse(maxCharInput.text);

             _settingData.ttsApiUrl = audioAPIInput.text;
             _settingData.ttsReferPath = audioReferPathInput.text;
             _settingData.ttsReferText = audioReferTextInput.text;
             _settingData.ttsIson = ttsToggle.isOn;
             _settingData.isHideDiagOnDesk = isShowDiagOnDeskToggle.isOn;
         }

         public void Changed()
         {
             saveButton.gameObject.SetActive(true);
             _isChangeData = true;
         }
         private void SetModel(int inx)
         {
             switch (inx)
             {
                 case 0:
                     apiUrlInput.text = "https://api.deepseek.com/chat/completions";
                     apiUrlInput.readOnly = true;
                     roleNameInput.text = "system";
                     roleNameInput.readOnly = true;
                     break;
                 case 1:
                     apiUrlInput.text = "https://api.openai.com/v1/chat/completions";
                     apiUrlInput.readOnly = true;
                     roleNameInput.text = "developer";
                     roleNameInput.readOnly = true;
                     break;
                 case 2:
                     apiUrlInput.text = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
                     apiUrlInput.readOnly = true;
                     roleNameInput.text = "developer";
                     roleNameInput.readOnly = true;
                     break;
                 case 3:
                     apiUrlInput.readOnly = false;
                     roleNameInput.readOnly = false;
                     break;
             }
             SetData();
         }
         private void ChangeBgmVolume(float vol)
         {
             // bgmAudioSource.volume = vol;
         }
         public void SaveData()
         {
             MessageManager.instance.ShowMessage("已保存",MessageType.Success);
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
                 MessageManager.instance.ShowPropUpMessage("是否保存","有未保存的数据，是否需要保存", () =>
                 {
                     SaveData();
                     DoHidePanel();
                 },()=>
                 {
                     SetDefData();
                     DoHidePanel();
                 },"保存","不保存");
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