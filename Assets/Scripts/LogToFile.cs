using System.IO;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug; // 注意：需要添加此命名空间（Windows 平台）

public class LogToFile : MonoBehaviour
{
    private string _logFilePath;

    void Awake()
    {
        _logFilePath = Path.Combine(Application.persistentDataPath, "unity_error_log.txt");
        Application.logMessageReceived += HandleLog;

        if (!File.Exists(_logFilePath))
        {
            File.WriteAllText(_logFilePath, "");
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            string errorLog = $"[{System.DateTime.Now}] {logString}\n{stackTrace}\n\n";
            File.AppendAllText(_logFilePath, errorLog);
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    /// <summary>
    /// 打开日志文件（Windows/macOS 有效）
    /// </summary>
    public void OpenLogFile()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        Process.Start(new ProcessStartInfo()
        {
            FileName = _logFilePath,
            UseShellExecute = true // 让它用系统默认方式打开文件
        });
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    Process.Start("open", _logFilePath);
#else
    MessageManager.instance.ShowMessage("请手动查找: " + _logFilePath, MessageType.Warning);
#endif
    }

}