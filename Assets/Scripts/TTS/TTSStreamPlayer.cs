using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Threading;

[RequireComponent(typeof(AudioSource))]
public class TTSStreamPlayer : MonoBehaviour
{
    public string ttsUrl = "http://localhost:9880/tts?text=大人，诱惑的话语有很多种，但最重要的是保持尊重和得体。如果您是在寻找一些浪漫或者引人入胜的表达，我可以提供一些诗意的描述，比如：“在繁星点点的夜空下，每一颗星星都像是在诉说着我们未完的故事。”希望这样的表达能够满足您的需求。如果您有其他问题或者需要更多的帮助，请随时告诉我。" +
                           "&text_lang=zh&ref_audio_path=E:\\AI\\GPT-SoVITS-V2Pro\\GPT-SoVITS-v2pro-20250604\\output\\slicer_opt\\ssxj1\\让老师猜猜，打游戏了？.wav" +
                           "&prompt_text=让老师猜猜，打游戏了？&prompt_lang=zh&media_type=raw&streaming_mode=true";
    public int sampleRate = 32000;

    private AudioSource audioSource;
    private AudioClip streamingClip;

    private Queue<float> audioBuffer = new Queue<float>();
    private object bufferLock = new object();
    private bool isStreaming = false;
    private bool isDone = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(StartTTS());
    }

    IEnumerator StartTTS()
    {
        isStreaming = true;
        isDone = false;

        // 创建 1 秒缓冲大小的流式 AudioClip
        streamingClip = AudioClip.Create("TTSStreamingClip", sampleRate, 1, sampleRate, true, OnAudioRead);
        audioSource.clip = streamingClip;
        audioSource.loop = false;

        // 启动后台线程读取字节流
        Thread streamThread = new Thread(() => StreamPCM(ttsUrl));
        streamThread.Start();

        yield return new WaitForSeconds(0.2f); // 稍微等一会让缓冲填充
        audioSource.Play();
    }

    void OnAudioRead(float[] data)
    {
        lock (bufferLock)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (audioBuffer.Count > 0)
                    data[i] = audioBuffer.Dequeue();
                else
                    data[i] = 0f; // 没数据就静音
            }
        }
    }

    void StreamPCM(string url)
    {
        try
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";

            using var res = req.GetResponse();
            using var stream = res.GetResponseStream();
            byte[] buffer = new byte[2048];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                lock (bufferLock)
                {
                    for (int i = 0; i < bytesRead; i += 2)
                    {
                        if (i + 1 >= bytesRead) break;
                        short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
                        float pcm = sample / 32768f;
                        audioBuffer.Enqueue(pcm);
                    }
                }
            }

            isDone = true;
            Debug.Log("TTS PCM stream finished.");
        }
        catch (Exception ex)
        {
            Debug.LogError("TTS Streaming failed: " + ex.Message);
        }
    }
}
