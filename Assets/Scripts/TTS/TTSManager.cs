using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
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

        public UIPanel audioIcon;
        public AudioSource audioSource;

        public event Action<AudioClip,DialogueEntry> OnGetAudio;
        
        private CharacterManager _characterManager;
        private AudioClip _lastClip;
        private void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            _characterManager = CharacterManager.instance;
            DialogManager.instance.OnMessageReceived += StartTextToSpeech;
        }

        public void StartTextToSpeech(DialogueEntry entry)
        {
            if(!_characterManager.curCharacter.SettingData.ttsIson){return;}
            string cleanText = RemoveParenthesesContent(entry.content);
            StartCoroutine(PostTTSRequest(cleanText,entry));
        }

        private string RemoveParenthesesContent(string target)
        {
            string pattern = @"[\(（].*?[\)）]";
            return Regex.Replace(target, pattern, "");
        }

        private IEnumerator PostTTSRequest(string cleanText,DialogueEntry entry = null)
        {
            audioIcon.targetImage.color = MyColor.Blue;
            audioIcon.Show();

            string apiUrl = _characterManager.curCharacter.SettingData.ttsApiUrl.TrimEnd('/') + "/tts";
#if UNITY_EDITOR
            Debug.Log(apiUrl);
#endif
            // 构造 JSON 请求体
            TTSRequest request = new TTSRequest
            {
                text = cleanText,
                text_lang = "zh",
                ref_audio_path = _characterManager.curCharacter.SettingData.ttsReferPath,
                aux_ref_audio_paths = new string[] {}, // 可留空
                prompt_lang = "zh",
                prompt_text = _characterManager.curCharacter.SettingData.ttsReferText
            };
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
                audioIcon.targetImage.color = MyColor.Red;
                Debug.LogError("语音生成失败: " + www.error);
                MessageManager.instance.ShowMessage("语音生成失败: " + www.error,MessageType.Warning);
                yield return new WaitForSeconds(1);
                audioIcon.Hide();
            }
            else
            {
                _lastClip = DownloadHandlerAudioClip.GetContent(www);
                if (_lastClip != null)
                {
                    OnGetAudio?.Invoke(_lastClip,entry);
                    audioSource.Stop();
                    audioSource.clip = _lastClip;
                    audioSource.Play();
                    audioIcon.targetImage.color = MyColor.Green;
                }
                else
                {
                    audioIcon.targetImage.color = MyColor.Red;
                    Debug.LogError("音频Clip为空");
                }
                yield return new WaitForSeconds(1);
                audioIcon.Hide();
            }
        }
        /// <summary>
        /// 播放指定路径的音频
        /// </summary>
        /// <param name="filePath"></param>
        public void PlayLocalAudio(string filePath)
        {
            StartCoroutine(PlayLocalAudioIE(filePath));
        }

        public void PlayLastClip()
        {
            if (_lastClip != null)
            {
                audioSource.Stop();
                audioSource.clip = _lastClip;
                audioSource.Play();
            }
        }
        IEnumerator PlayLocalAudioIE(string filePath)
        {
            string url = "file:///" + filePath.Replace("\\", "/"); // Windows 文件路径兼容处理
            using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                MessageManager.instance.ShowMessage("音频加载失败",MessageType.Warning);
                Debug.LogError("音频加载失败: " + www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("播放音频成功");
            }
        }
        [System.Serializable]
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
