using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TTS
{
    /// <summary>
    /// GPT-SoVITS TTS 流式音频播放器
    /// </summary>
    public class TTSStreamingPlayer : MonoBehaviour
    {
        [Header("UI")]
        public AudioIcon audioIcon;
        
        [Header("API Settings")]
        [SerializeField] private int sampleRate = 32000; // 默认采样率
        [SerializeField] private int channels = 1; // 单声道
    
        [Header("Audio Settings")]
        [SerializeField] private float bufferTime = 0.5f; // 缓冲时间（秒）
        [SerializeField] private int audioClipLength = 10; // AudioClip长度（秒）
    
        private AudioSource audioSource;
        private float[] audioBuffer;
        private int writePosition = 0;
        private int readPosition = 0;
        private bool isStreaming = false;
        private bool hasWavHeader = false;
    
        private Queue<float[]> audioQueue = new Queue<float[]>();
        private object queueLock = new object();
        
        // 添加协程管理
        private Coroutine currentStreamingCoroutine;
        private UnityWebRequest currentRequest;
        
        // 添加播放状态标志
        private bool isPreparedToPlay = false;
        private int sessionId = 0; // 用于标识每个播放会话

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // 初始化但不立即设置AudioClip
            InitializeAudioClip();
        }
        
        private void InitializeAudioClip()
        {
            // 创建循环播放的AudioClip
            int clipSamples = sampleRate * audioClipLength;
            AudioClip clip = AudioClip.Create("TTSStream", clipSamples, channels, sampleRate, true, OnAudioRead);
            audioSource.clip = clip;
            audioSource.loop = true;
        
            // 初始化音频缓冲区
            int bufferSize = (int)(sampleRate * bufferTime * channels);
            audioBuffer = new float[clipSamples * channels];
        }

        /// <summary>
        /// 开始流式TTS
        /// </summary>
        public void StartStreaming(TTSManager.TTSRequest request, string url)
        {
            // 先彻底停止之前的播放
            ForceStopStreaming();
            
            // 增加会话ID，用于区分不同的播放请求
            sessionId++;
            
            // 延迟一帧再开始新的播放，确保之前的状态完全清理
            StartCoroutine(DelayedStartStreaming(request, url, sessionId));
        }
        
        private IEnumerator DelayedStartStreaming(TTSManager.TTSRequest request, string url, int currentSessionId)
        {
            // 等待一帧，确保之前的操作完成
            yield return null;
            
            // 检查是否仍然是当前会话
            if (currentSessionId != sessionId)
            {
                yield break;
            }
            
            // 重新初始化AudioClip（如果采样率改变了）
            InitializeAudioClip();
            
            // 开始新的流式播放
            audioIcon.LoadingAndPlay();
            request.streaming_mode = true;
            request.media_type = "wav";
            
            isStreaming = true;
            isPreparedToPlay = false;
            hasWavHeader = false;
            
            currentStreamingCoroutine = StartCoroutine(StreamTTS(request, url, currentSessionId));
        }
    
        /// <summary>
        /// 强制停止流式播放
        /// </summary>
        private void ForceStopStreaming()
        {
            isStreaming = false;
            isPreparedToPlay = false;
            
            // 停止当前的流式协程
            if (currentStreamingCoroutine != null)
            {
                StopCoroutine(currentStreamingCoroutine);
                currentStreamingCoroutine = null;
            }
            
            // 取消当前的网络请求
            if (currentRequest != null)
            {
                currentRequest.Abort();
                currentRequest.Dispose();
                currentRequest = null;
            }
            
            // 停止音频播放
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        
            // 清理队列
            lock (queueLock)
            {
                audioQueue.Clear();
            }
        
            // 重置所有状态
            writePosition = 0;
            readPosition = 0;
            hasWavHeader = false;
            
            if (audioIcon != null)
            {
                audioIcon.Stop();
            }
            
            // 清空缓冲区
            if (audioBuffer != null)
            {
                Array.Clear(audioBuffer, 0, audioBuffer.Length);
            }
        }
        
        /// <summary>
        /// 停止流式播放（公共接口）
        /// </summary>
        public void StopStreaming()
        {
            ForceStopStreaming();
        }
    
        /// <summary>
        /// 流式请求协程
        /// </summary>
        private IEnumerator StreamTTS(TTSManager.TTSRequest request, string apiUrl, int currentSessionId)
        {
            // 构建请求URL
            string url = BuildUrl(request, apiUrl);
        
            currentRequest = UnityWebRequest.Get(url);
            currentRequest.downloadHandler = new StreamingDownloadHandler(this, currentSessionId);
            
            yield return currentRequest.SendWebRequest();
            
            // 检查是否仍然是当前会话
            if (currentSessionId != sessionId)
            {
                yield break;
            }
            
            if (currentRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"TTS请求失败: {currentRequest.error}");
                isStreaming = false;
                audioIcon.Stop();
            }
            else
            {
                // 等待所有音频播放完毕
                while (audioQueue.Count > 0 && currentSessionId == sessionId)
                {
                    yield return null;
                }
            }
            
            // 清理
            if (currentSessionId == sessionId)
            {
                isStreaming = false;
                currentRequest = null;
            }
        }
    
        /// <summary>
        /// 处理接收到的音频数据
        /// </summary>
        public void ProcessAudioData(byte[] data, int currentSessionId)
        {
            // 检查是否是当前会话的数据
            if (currentSessionId != sessionId || !isStreaming)
            {
                return;
            }
        
            float[] audioData = null;
        
            if (!hasWavHeader && data.Length >= 44)
            {
                // 第一个数据块包含WAV头，需要解析
                audioData = ParseWavData(data);
                hasWavHeader = true;
            
                // 从WAV头获取实际的采样率
                int wavSampleRate = BitConverter.ToInt32(data, 24);
                if (wavSampleRate != sampleRate)
                {
                    Debug.Log($"更新采样率: {sampleRate} -> {wavSampleRate}");
                    sampleRate = wavSampleRate;
                    // 重新创建AudioClip
                    InitializeAudioClip();
                }
            }
            else if (hasWavHeader)
            {
                // 后续数据块是原始PCM数据
                audioData = ConvertPCMToFloat(data);
            }
        
            if (audioData != null && audioData.Length > 0)
            {
                lock (queueLock)
                {
                    audioQueue.Enqueue(audioData);
                }
                
                // 缓冲足够的数据后开始播放
                if (!isPreparedToPlay && audioQueue.Count >= 2)
                {
                    isPreparedToPlay = true;
                }
            
                // 开始播放
                if (!audioSource.isPlaying && isPreparedToPlay && currentSessionId == sessionId)
                {
                    audioSource.Play();
                }
            }
        }
    
        /// <summary>
        /// 解析WAV数据（跳过头部）
        /// </summary>
        private float[] ParseWavData(byte[] wavData)
        {
            if (wavData.Length <= 44) return new float[0];
        
            // WAV头部通常是44字节，跳过它
            int dataStart = 44;
        
            // 查找"data"块
            for (int i = 36; i < wavData.Length - 4; i++)
            {
                if (wavData[i] == 'd' && wavData[i + 1] == 'a' && 
                    wavData[i + 2] == 't' && wavData[i + 3] == 'a')
                {
                    dataStart = i + 8; // 跳过"data"标识和大小字段
                    break;
                }
            }
        
            int pcmLength = wavData.Length - dataStart;
            if (pcmLength <= 0) return new float[0];
            
            byte[] pcmData = new byte[pcmLength];
            Array.Copy(wavData, dataStart, pcmData, 0, pcmLength);
        
            return ConvertPCMToFloat(pcmData);
        }
    
        /// <summary>
        /// 将16位PCM数据转换为Unity的float格式
        /// </summary>
        private float[] ConvertPCMToFloat(byte[] pcmData)
        {
            int sampleCount = pcmData.Length / 2; // 16位 = 2字节
            float[] floatData = new float[sampleCount];
        
            for (int i = 0; i < sampleCount; i++)
            {
                // 小端序读取16位有符号整数
                short sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
                // 转换为-1.0到1.0的浮点数
                floatData[i] = sample / 32768f;
            }
        
            return floatData;
        }
    
        /// <summary>
        /// Unity音频回调
        /// </summary>
        private void OnAudioRead(float[] data)
        {
            int samplesNeeded = data.Length;
            int samplesFilled = 0;
        
            lock (queueLock)
            {
                while (samplesFilled < samplesNeeded && audioQueue.Count > 0)
                {
                    float[] currentBuffer = audioQueue.Peek();
                    int samplesAvailable = currentBuffer.Length - readPosition;
                    int samplesToRead = Mathf.Min(samplesNeeded - samplesFilled, samplesAvailable);
                
                    Array.Copy(currentBuffer, readPosition, data, samplesFilled, samplesToRead);
                
                    samplesFilled += samplesToRead;
                    readPosition += samplesToRead;
                
                    if (readPosition >= currentBuffer.Length)
                    {
                        audioQueue.Dequeue();
                        readPosition = 0;
                    }
                }
            }
        
            // 如果没有足够的数据，填充静音
            for (int i = samplesFilled; i < samplesNeeded; i++)
            {
                data[i] = 0f;
            }
        }
    
        /// <summary>
        /// 构建请求URL
        /// </summary>
        private string BuildUrl(TTSManager.TTSRequest request, string apiUrl)
        {
            StringBuilder sb = new StringBuilder(apiUrl);
            sb.Append("?");
            sb.Append($"text={UnityWebRequest.EscapeURL(request.text)}");
            sb.Append($"&text_lang={request.text_lang}");
            sb.Append($"&ref_audio_path={UnityWebRequest.EscapeURL(request.ref_audio_path)}");
            sb.Append($"&prompt_lang={request.prompt_lang}");
            sb.Append($"&prompt_text={UnityWebRequest.EscapeURL(request.prompt_text)}");
            sb.Append($"&streaming_mode={request.streaming_mode.ToString().ToLower()}");
            sb.Append($"&media_type={request.media_type}");
            sb.Append($"&text_split_method={request.text_split_method}");
            sb.Append($"&batch_size={request.batch_size}");
            sb.Append($"&speed_factor={request.speed_factor}");
        
            return sb.ToString();
        }
        
        void OnDestroy()
        {
            ForceStopStreaming();
        }
    }

    /// <summary>
    /// 自定义流式下载处理器
    /// </summary>
    public class StreamingDownloadHandler : DownloadHandlerScript
    {
        private TTSStreamingPlayer player;
        private MemoryStream stream;
        private int sessionId;
    
        public StreamingDownloadHandler(TTSStreamingPlayer player, int sessionId) : base(new byte[1024])
        {
            this.player = player;
            this.sessionId = sessionId;
            this.stream = new MemoryStream();
        }
    
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            if (data == null || dataLength == 0)
                return true;
        
            // 将数据复制出来处理
            byte[] audioData = new byte[dataLength];
            Array.Copy(data, audioData, dataLength);
        
            // 处理音频数据，传递会话ID
            player.ProcessAudioData(audioData, sessionId);
        
            return true;
        }
    
        protected override void CompleteContent()
        {
            stream?.Dispose();
        }
    
        protected override void ReceiveContentLengthHeader(ulong contentLength)
        {
            // 可选：处理内容长度
        }
    }
}