using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Character;
using DG.Tweening;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public SettingData SettingData=>CharacterManager.instance.curCharacter.SettingData;
    public TMP_Dropdown modeDropdown;
    [Header("默认错误图片")]
    public Sprite defaultSprite;
    
    [Header("liv2d编辑")]
    public GameObject editObjs;
    [Header("<窗口管理器>")]
    public TransparentWindow transparentWindow; // 拖到管理脚本里

    public GameObject toolMenu;
    public event Action<int> OnChangeMode;
    public GameMode CurMode => (GameMode)SettingData.modeIndex;

    private Vector2Int _windowSize = new Vector2Int(1920,1080);
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        modeDropdown.value = SettingData.modeIndex;
        modeDropdown.RefreshShownValue();
        modeDropdown.onValueChanged.AddListener(ChangeMode);
        if (CharacterManager.instance.curCharacter != null)
        {
            StartCoroutine(ChangeModeIE());
        }
        CharacterManager.instance.OnHideCharacterPanel += SetModeAsCurCharacter;
    }

    IEnumerator ChangeModeIE()
    {
        yield return new WaitForSeconds(0.5f);
        ChangeMode(SettingData.modeIndex);
    }
    public void SaveData()
    {
        ES3.Save("characterDatas",CharacterManager.instance.characterDatas);
        Debug.Log($"SaveTalkData");
    }
    public void SaveSettingData()
    {
        ES3.Save("characterDatas",CharacterManager.instance.characterDatas);
        Debug.Log($"============SaveSettingData==============");
    }
    
    /// <summary>
    /// 改变对话模式
    /// </summary>
    /// <param name="modeInx"></param>
    private void ChangeMode(int modeInx)
    {
        if (modeInx == 2 && string.IsNullOrEmpty(CharacterManager.instance.curCharacter.live2dPath))
        {
            MessageManager.instance.ShowMessage("请先为人物设置模型，再开启桌面模式", MessageType.Warning);
            modeDropdown.onValueChanged.RemoveAllListeners();
            modeDropdown.value = SettingData.modeIndex;
            modeDropdown.onValueChanged.AddListener(ChangeMode);
            return;
        }
        SettingData.modeIndex = modeInx;
        if (modeInx == 2)
        {
            SetFullScreenWindow();
            toolMenu.SetActive(false);
        }
        else
        {
            SetWindowed();
            toolMenu.SetActive(true);
        }
        SaveSettingData();
        OnChangeMode?.Invoke(modeInx);
    }
    public void SetMode(int modeInx)
    {
        modeDropdown.value = modeInx;
    }
    public void SetModeAsCurCharacter(CharacterData data)
    {
        if (data == null)
        {
            return;
        }
        else
        {
            modeDropdown.onValueChanged.RemoveAllListeners();
            modeDropdown.value = data.SettingData.modeIndex;
            modeDropdown.onValueChanged.AddListener(ChangeMode);
            if (modeDropdown.value == data.SettingData.modeIndex)
            {
                ChangeMode(modeDropdown.value);
            }
        }
    }
    /// <summary>
    /// 加载图片
    /// </summary>
    /// <param name="path"></param>
    /// <param name="target"></param>
    /// <param name="defSprite"></param>
    public void LoadImage(string path, Image target,Sprite defSprite = null)
    {
        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                byte[] fileData = System.IO.File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2);
                
                if (texture.LoadImage(fileData))
                {
                    Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    target.sprite = newSprite;
                    target.color = Color.white;
                }
                else
                {
                    Debug.LogWarning("加载图片失败：" + path);
                    MessageManager.instance.ShowMessage("加载图片失败：" + path,MessageType.Warning);
                    target.sprite = defaultSprite;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("读取图片文件异常：" + ex.Message);
                MessageManager.instance.ShowMessage("读取图片文件异常：" + ex.Message,MessageType.Warning);
                target.sprite = defaultSprite;
            }
        }
        else
        {
            target.sprite = defSprite==null?defaultSprite:defSprite;
            target.color = Color.white;
        }
    }
    public void SetWindowed()
    {
        transparentWindow.DisableTransparentMode();
        Screen.SetResolution(_windowSize.x, _windowSize.y, FullScreenMode.Windowed);
        Debug.Log("切换到窗口模式");
    }
    public void SetFullScreenWindow()
    {
        _windowSize = new Vector2Int(Screen.width, Screen.height);
        transparentWindow.EnableTransparentMode();
        Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        Debug.Log("切换到窗口全屏模式");
    }
}

public enum GameMode
{
    Talk = 0,
    ModeTalk = 1,
    Desktop = 2,
}