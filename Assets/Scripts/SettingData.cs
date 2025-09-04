public class SettingData
{
    public string modelType = "自定义";
    public string apiUrl;
    public string modelName;
    public string roleName;
    public string apiKey;
    public int maxCharCount = 5000;
    public float bgmVolume;
    public bool isTextStreaming;

    public bool ttsIson;
    public string ttsApiUrl;
    public string ttsReferPath;
    public string ttsReferText;
    public bool isStreaming;
    public string textLang="zh";
    public string promptLang ="zh";
    public bool onlyQuotationMarks;
    public bool removeBracket;

    public bool isHideDiagOnDesk;

    public int modeIndex;

    public SettingData Clone()
    {
        return (SettingData)MemberwiseClone();
    }
}