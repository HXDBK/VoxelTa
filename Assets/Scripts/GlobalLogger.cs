using System;
using System.IO;
using UnityEngine;

public class GlobalLogger : MonoBehaviour
{
    private string logFilePath;

    private void Awake()
    {
#if UNITY_EDITOR
        return;
#endif
        // 指定日志文件存储路径，例如 persistentDataPath
        logFilePath = Path.Combine(Application.persistentDataPath, "runtime_log.txt");
        // 订阅日志消息回调
        Application.logMessageReceived += HandleLog;
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        return;
#endif
        // 取消订阅，避免内存泄漏或重复记录
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        try
        {
            // 构建日志文本，包含时间戳和日志类型
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{type}] {logString}\n{stackTrace}\n\n";
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception ex)
        {
            // 当日志写入失败时，也可以输出错误信息（尽量不要让记录日志失败再次引发异常）
            Debug.LogError("日志写入失败：" + ex.Message);
        }
    }
}