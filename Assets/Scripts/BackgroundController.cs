using System;
using System.Collections;
using Character;
using Dialog;
using Setting;
using SFB;
using UnityEngine;
using UnityEngine.UI;
using WUI;

public class BackgroundController : MonoBehaviour
{
    public Image backgroundImage;
    public Image backgroundBaseImage;
    public SettingPanel settingPanel;
    public WButton backgroundColorBtn;
    public WButton lightColorBtn;
    public WButton nameColorBtn;
    
    private bool _isMove;
    private Camera _camera;
    private Vector2 _moveOffset;

    private bool _isScaling = false;
    private Vector2 _scaleStartMousePos;
    private Vector3 _originalScale;

    IEnumerator Start()
    {
        yield return null;
        _camera = Camera.main;
        SetCurCharacterBackground(CharacterManager.instance.curCharacter);
        CharacterManager.instance.OnHideCharacterPanel += SetCurCharacterBackground;
        backgroundColorBtn.onPointerClick.AddListener(() =>
        {
            ColorPickerPanel.instance.SetColor(backgroundColorBtn.Image.color, SetBaseColor, backgroundColorBtn.transform.position,
                () =>
                {
                    settingPanel.Show();
                });
            settingPanel.gameObject.SetActive(false);
        });
        lightColorBtn.onPointerClick.AddListener(() =>
        {
            if (CharacterManager.instance.curModel == null)
            {
                MessageManager.instance.ShowMessage("请先为当前对话设置模型",MessageType.Warning);
                return;
            }
            ColorPickerPanel.instance.SetColor(lightColorBtn.Image.color, SetLight, lightColorBtn.transform.position,
                () =>
                {
                    settingPanel.Show();
                });
            settingPanel.gameObject.SetActive(false);
        });
        nameColorBtn.onPointerClick.AddListener(() =>
        {
            ColorPickerPanel.instance.SetColor(nameColorBtn.Image.color, SetNameLight, nameColorBtn.transform.position);
        });
    }

    private void SetCurCharacterBackground(CharacterData target)
    {
        if (target == null)
        {
            backgroundImage.color = Color.black;
            backgroundImage.sprite = null;
            return;
        }
        backgroundBaseImage.color = target.backgroundColor;
        backgroundColorBtn.Image.color = target.backgroundColor;
        lightColorBtn.Image.color = target.backgroundLight;
        nameColorBtn.Image.color = target.aiNameColor;
        if (target.backgroundPath is { Length: > 0 })
        {
            GameManager.instance.LoadImage(target.backgroundPath,backgroundImage);
            backgroundImage.transform.localScale = target.backgroundScale;
            backgroundImage.transform.localPosition = target.backgroundPos;
            backgroundImage.gameObject.SetActive(true);
        }
        else
        {
            backgroundImage.gameObject.SetActive(false);
        }
    }
    private void SetBaseColor(Color color)
    {
        if (color != backgroundBaseImage.color)
        {
            backgroundBaseImage.color = color;
            backgroundColorBtn.Image.color = color;
            CharacterManager.instance.curCharacter.backgroundColor = color;
            settingPanel.Changed();
        }
    }

    private void SetLight(Color color)
    {
        if (color != lightColorBtn.Image.color)
        {
            lightColorBtn.Image.color = color;
            CharacterManager.instance.curModel.SetColor(color);
            CharacterManager.instance.curCharacter.backgroundLight = color;
            settingPanel.Changed();
        }
    }

    private void SetNameLight(Color color)
    {
        if (color != nameColorBtn.Image.color)
        {
            nameColorBtn.Image.color = color;
            CharacterManager.instance.curCharacter.aiNameColor = color;
            settingPanel.Changed();
        }
    }
    
    public void RemoveBackground()
    {
        switch (LocalizerManager.GetCode())
        {
            case "zh-Hans":
                MessageManager.instance.ShowPropUpMessage("确认","确认要移除背景图片吗？",DoRemoveBackground);
                break;
            case "en":
                MessageManager.instance.ShowPropUpMessage("Confirm","Are you sure you want to remove the background image?",DoRemoveBackground);
                break;
            default:
                MessageManager.instance.ShowPropUpMessage("Confirm","Are you sure you want to remove the background image?",DoRemoveBackground);
                break;
        }
    }

    private void DoRemoveBackground()
    {
        backgroundImage.gameObject.SetActive(false);
        MessageManager.instance.ShowMessage("背景图片已清除");
        CharacterManager.instance.curCharacter.backgroundPath = "";
        settingPanel.Changed();
    }
    private void Update()
    {
        if (_isMove)
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndMoveFurniture();
            }
            else
            {
                MoveFurniture();
            }
        }
        
        if (_isScaling)
        {
            if (Input.GetMouseButtonUp(0))
            {
                EndScaleFurniture();
            }
            else
            {
                ScaleFurniture();
            }
        }
    }
    public void SetImage()
    {
        // 打开文件选择器
        string[] paths = StandaloneFileBrowser.OpenFilePanel("选择图片", "", "", false);

        if (paths.Length > 0)
        {
            GameManager.instance.LoadImage(paths[0],backgroundImage);
            CharacterManager.instance.curCharacter.backgroundPath = paths[0];
            backgroundImage.gameObject.SetActive(true);
            MessageManager.instance.ShowMessage("背景图片已设置",MessageType.Success);
            settingPanel.Changed();
        }
    }
    public void StartMoveFurniture()
    {
        _isMove = true;
        settingPanel.gameObject.SetActive(false);
        Vector2 mouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
        _moveOffset = (Vector2)backgroundImage.transform.position - mouseWorldPos;
    }
    private void MoveFurniture()
    {
        float distanceToCamera = Mathf.Abs(_camera.transform.position.z - backgroundImage.transform.position.z);

        Vector3 screenPos = Input.mousePosition;
        screenPos.z = distanceToCamera;
        Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(screenPos);

        Vector3 tmp = mouseWorldPos + (Vector3)_moveOffset;
        backgroundImage.transform.position = tmp;

    }
    private void EndMoveFurniture()
    {
        _isMove = false;
        settingPanel.gameObject.SetActive(true);
        CharacterManager.instance.curCharacter.backgroundPos = backgroundImage.transform.localPosition;
        settingPanel.Changed();
    }
    public void StartScaleFurniture()
    {
        _isScaling = true;
        settingPanel.gameObject.SetActive(false);
        _scaleStartMousePos = Input.mousePosition;
        _originalScale = backgroundImage.transform.localScale;
    }
    private void ScaleFurniture()
    {
        if (!_isScaling) return;

        Vector2 currentMousePos = Input.mousePosition;
        float delta = (currentMousePos - _scaleStartMousePos).magnitude;
        float direction = Mathf.Sign((currentMousePos - _scaleStartMousePos).x);

        float scaleFactor = 1 + direction * delta / 500f; // 控制缩放速度
        scaleFactor = Mathf.Clamp(scaleFactor, 0.2f, 5f); // 限制缩放范围

        backgroundImage.transform.localScale = _originalScale * scaleFactor;
    }
    private void EndScaleFurniture()
    {
        _isScaling = false;
        settingPanel.gameObject.SetActive(true);
        CharacterManager.instance.curCharacter.backgroundScale = backgroundImage.transform.localScale;
        settingPanel.Changed();
    }

    public void ResetBackgroundSize()
    {
        MessageManager.instance.ShowMessage("背景图片大小已重置");
        backgroundImage.transform.localScale = new Vector3(1, 1, 1);
        CharacterManager.instance.curCharacter.backgroundScale = new Vector3(1, 1, 1);
    }
    public void ResetBackgroundPos()
    {
        MessageManager.instance.ShowMessage("背景图片位置已重置");
        backgroundImage.transform.localPosition = new Vector3(0, 0, 0);
        CharacterManager.instance.curCharacter.backgroundPos = new Vector3(0, 0, 0);
    }
}