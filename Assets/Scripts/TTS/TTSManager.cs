using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Character;
using Dialog;
using Other;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WUI;

namespace TTS
{
    public class TTSManager : MonoBehaviour
    {
        public static TTSManager instance;

        [Header("UI")]
        public AudioIcon audioIcon;
        public AudioSource audioSource;

        public bool UseStreamingMode => _characterManager.curCharacter.settingData.isStreaming; // 是否使用流式模式

        [Header("流式模式设置")] 
        public TTSStreamingPlayer streamingPlayer;

        public event Action<AudioClip, DialogueEntry> OnGetAudio;

        // 私有成员
        private CharacterManager _characterManager;
        private AudioClip _lastClip;
        
        // 流式播放相关
        private List<float> _streamBuffer;
        private readonly object _bufferLock = new object();
        private CancellationTokenSource _streamCancellationToken;
        private byte _pendingByte;
        private readonly long _totalSamplesReceived = 0;
        private readonly long _totalSamplesPlayed = 0;
        private AudioClip _streamClip;

        private void Awake()
        {
            instance = this;
            Application.runInBackground = true;
        }

        private void Start()
        {
            _characterManager = CharacterManager.instance;
            DialogManager.instance.OnMessageReceived += StartTextToSpeech;
        }
        
        public void StartTextToSpeech(DialogueEntry entry)
        {
            if (!_characterManager.curCharacter.SettingData.ttsIson) return;
            string cleanText = CleanForTTS(entry.content);
            
            // 根据模式选择不同的处理方式
            if (UseStreamingMode)
            {
                StartStreamingTTS(cleanText, entry);
            }
            else
            {
                StartCoroutine(PostTTSRequest(cleanText, entry));
            }
        }
        private string CleanForTTS(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (_characterManager.curCharacter.settingData.onlyQuotationMarks)
            {
                input = ExtractQuotedContent(input);
            }

            if (_characterManager.curCharacter.settingData.removeBracket)
            {
                input = RemoveBracketsContent(input);
            }
            // 保留中文、英文、数字、常见标点（逗号句号问号感叹号顿号引号冒号分号括号等）
            string pattern = @"[^a-zA-Z0-9\u4e00-\u9fff，。！？、：“”；（）《》〈〉『』「」…—\- ]";
        
            // 去掉不在范围内的字符
            string result = Regex.Replace(input, pattern, "");

            // 把多余的空格压缩成一个
            result = Regex.Replace(result, @"\s{2,}", " ");

            // 去掉首尾空格
            return result.Trim();
        }
        /// <summary>
        /// 只保留引号中的内容，多个引号片段会合并成一个字符串。
        /// 支持英文引号 " " 和中文引号 “ ”。
        /// </summary>
        private string ExtractQuotedContent(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // 匹配 "xxx" 或 “xxx”
            var matches = Regex.Matches(input, "[\"“](.*?)[\"”]");
            List<string> results = new List<string>();

            foreach (Match m in matches)
            {
                if (m.Groups.Count > 1)
                {
                    results.Add(m.Groups[1].Value);
                }
            }

            // 多个引号内容合并，中间用空格隔开
            return string.Join(" ", results);
        }
        /// <summary>
        /// 去除所有括号（中英文的圆括号、方括号、花括号）以及其中的内容。
        /// </summary>
        private static string RemoveBracketsContent(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // 正则模式: 匹配一对括号以及里面的内容（非贪婪）
            string pattern = @"(\(.*?\)|（.*?）|\[.*?\]|【.*?】|\{.*?\}|｛.*?｝)";
            string result = Regex.Replace(input, pattern, "");

            // 去掉多余空格
            result = Regex.Replace(result, @"\s{2,}", " ");
            return result.Trim();
        }
        #region 普通模式
        
        private IEnumerator PostTTSRequest(string cleanText, DialogueEntry entry = null)
        {
            audioIcon.Loading();
            
            string apiUrl = _characterManager.curCharacter.SettingData.ttsApiUrl.TrimEnd('/') + "/tts";
            
            // 构造请求体
            TTSRequest request = CreateTTSRequest(cleanText, false);
            string json = JsonUtility.ToJson(request);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
            
            using UnityWebRequest www = new UnityWebRequest(apiUrl, "POST");
            www.uploadHandler = new UploadHandlerRaw(jsonBytes);
            www.downloadHandler = new DownloadHandlerAudioClip(apiUrl, AudioType.WAV);
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 180;

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                audioIcon.Error();
#if UNITY_EDITOR
                Debug.LogError("语音生成失败: " + www.error);
#endif
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowMessage("语音生成失败: " + www.error, MessageType.Warning);
                        break;
                    case "en":
                        MessageManager.instance.ShowMessage("Failed to generate voice: " + www.error, MessageType.Warning);
                        break;
                }
                yield return new WaitForSeconds(1);
            }
            else
            {
                _lastClip = DownloadHandlerAudioClip.GetContent(www);
                if (_lastClip != null)
                {
                    OnGetAudio?.Invoke(_lastClip, entry);
                    audioSource.Stop();
                    audioSource.clip = _lastClip;
                    audioSource.Play();
                    audioIcon.Play();
                }
                else
                {
                    audioIcon.Play();
#if UNITY_EDITOR
                    Debug.LogError("音频Clip为空");
#endif
                }
                yield return new WaitForSeconds(1);
                audioIcon.Stop();
            }
        }
        
        #endregion

        #region 流式模式
        
        private void StartStreamingTTS(string cleanText, DialogueEntry entry)
        {
            audioIcon.Loading();
            var request = CreateTTSRequest(cleanText, true);
            string apiUrl = _characterManager.curCharacter.SettingData.ttsApiUrl.TrimEnd('/') + "/tts";

            streamingPlayer.StartStreaming(request,apiUrl);
        }
        
        private void StopStreaming()
        {
            streamingPlayer.StopStreaming();
        }
        
        #endregion

        #region 公共方法
        
        private TTSRequest CreateTTSRequest(string text, bool streaming)
        {
            var setting = _characterManager.curCharacter.SettingData;
            return new TTSRequest
            {
                text = text,
                text_lang = setting.textLang,
                ref_audio_path = setting.ttsReferPath,
                aux_ref_audio_paths = new string[] {},
                prompt_lang = setting.promptLang,
                prompt_text = setting.ttsReferText,
                streaming_mode = streaming,
                media_type = "wav"
            };
        }

        public void PlayLocalAudio(string filePath)
        {
            StartCoroutine(PlayLocalAudioIE(filePath));
        }

        public void PlayLastClip()
        {
            if (_lastClip != null)
            {
                StopStreaming(); // 停止流式播放
                audioSource.Stop();
                audioSource.clip = _lastClip;
                audioSource.loop = false;
                audioSource.Play();
            }
        }

        IEnumerator PlayLocalAudioIE(string filePath)
        {
            string url = "file:///" + filePath.Replace("\\", "/");
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                MessageManager.instance.ShowMessage("音频加载失败", MessageType.Warning);
                Debug.LogError("音频加载失败: " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                StopStreaming(); // 停止流式播放
                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.loop = false;
                audioSource.Play();
            }
        }
        
        private void OnDestroy()
        {
            StopStreaming();
#if UNITY_EDITOR
            Debug.Log($"[TTS] 销毁 - 总接收: {_totalSamplesReceived}, 总播放: {_totalSamplesPlayed}");
#endif
        }
        
        #endregion

        [Serializable]
        public class TTSRequest
        {
            public string text;
            public string text_lang;
            public string ref_audio_path;
            public string[] aux_ref_audio_paths;
            public string prompt_lang;
            public string prompt_text;
            public int top_k = 5;
            public float top_p = 1f;
            public float temperature = 1f;
            public string text_split_method = "cut5";
            public int batch_size = 1;
            public float batch_threshold = 0.75f;
            public bool split_bucket = true;
            public float speed_factor = 1f;
            public float fragment_interval = 0.3f;
            public int seed = -1;
            public string media_type = "wav";
            public bool streaming_mode = false;
            public bool parallel_infer = true;
            public float repetition_penalty = 1.35f;
            public int sample_steps = 32;
            public bool super_sampling = false;
        }
    }
}