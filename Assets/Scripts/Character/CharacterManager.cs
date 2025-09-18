using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dialog;
using Live2D;
using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using Live2D.Cubism.Framework.Json;
using Model;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WUI;

namespace Character
{
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager instance;
        public UIPanel characterPanel;
        public UIPanel detailPanel;
        public Live2dDetailPanel live2dDetailPanel;
        
        [Header("角色列表")]
        public WPageList characterPageList;
        [HideInInspector]
        public List<CharacterData> characterDatas;
        public WButton addButton;
        [HideInInspector] 
        public CharacterData curCharacter;
        public GameObject noCharacterPanel;

        [Header("角色立绘")] 
        private bool _needShowLive2d;
        public RectTransform targetUIRect;
        public GeneralModel curModel;
        public Live2DModelLoader live2DModelLoader;
        public CustomModelLoader customModelLoader;
        public GameObject loadingPanel;
        public TMP_Text loadText;
        public WButton loadButton;
        
        public Toggle isBreathToggle;
        public Toggle isBlinkToggle;
        public Toggle isLookMouseToggle;
        public GameObject btns;
        
        //移动
        private Vector2 _moveOffset;
        private bool _isMove;
        //旋转
        private bool _isRotating;
        private float _startAngleOffset;
        
        //缩放
        public Slider uiSizeSlider;
        private float _startScaleMagnitude;
        private Vector3 _originalScale;
        private float _scroll;
        private readonly float _scrollSaveDelay = 0.5f;
        private float _scrollSaveTimer = 0f;
        private bool _scaleChanged = false;
        //设置
        public RingMenu ringMenu;
        
        [Header("头像")]
        public Image headImage;
        public Sprite defHeadIcon;
        
        [Header("角色详情输入框")]
        public TMP_InputField characterDescriptionInput;
        public TMP_InputField usernameInput;
        public TMP_InputField characterNameInput;
        public TMP_InputField characterTitleInput;

        [Header("记忆模块")]
        public WPageList memoryPageList;
        public UIPanel memoryPanel;
        public TMP_InputField memoryTitleInput;
        public TMP_InputField memoryContentInput;
        private Memory _curMemory;
        
        [Header("组件和自定义模型")]
        public ComponentPanel componentPanel;

        [Header("保存相关")] 

        private bool _isChangeData;
        public WButton saveButton;

        private Camera _camera;

        public event Action<CharacterData> OnSetCharacterData;
        public event Action<CharacterData> OnHideCharacterPanel;
        
        private void Awake()
        {
            instance = this;
            string folderPath = Application.persistentDataPath + "/Characters"; // 你存放.vta文件的路径
            List<string> vtaFiles = new List<string>();

            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "*.vta", SearchOption.TopDirectoryOnly);
                vtaFiles.AddRange(files);
            }
            foreach (var file in vtaFiles)
            {
                Debug.Log("Found vta file: " + file);
                characterDatas.Add(ES3.Load<CharacterData>("CharacterData",file));
            }

            characterDatas = characterDatas.OrderByDescending(x => x.createTime).ToList();
            if (characterDatas.Count > 0)
            {
                SetCurCharacter(characterDatas[0]);
            }
            else
            {
                curCharacter = null;
                OnSetCharacterData?.Invoke(null);
                noCharacterPanel.SetActive(true);
                ClearCharacterPanel();
            }
        }

        void Start()
        {
            _camera = Camera.main;
            GameManager.instance.OnChangeMode += ChangeMode;
            
            addButton.onPointerClick.AddListener(AddCharacter);

            // 监听文本输入完成事件，更新角色数据
            characterDescriptionInput.onValueChanged.AddListener(_ => SetData());
            usernameInput.onValueChanged.AddListener(_ => SetData());
            characterNameInput.onValueChanged.AddListener(_ => SetData());
            characterTitleInput.onValueChanged.AddListener(_ => SetData());
            
            isBreathToggle.onValueChanged.AddListener(_=>SetData());
            isBlinkToggle.onValueChanged.AddListener(_=>SetData());
            isLookMouseToggle.onValueChanged.AddListener(_=>SetData());
            
            uiSizeSlider.onValueChanged.AddListener(ChangeUIScale);
            
            // moveButton.onPointerDown.AddListener(StartMoveFurniture);
            // moveButton.onPointerUp.AddListener(EndMoveFurniture);
            
            // scaleButton.onPointerDown.AddListener(StartScaleFurniture);
            // scaleButton.onPointerUp.AddListener(EndScaleFurniture);

            DialogManager.instance.OnMessageReceived += CheckExpAndMotion;
            DialogManager.instance.OnMessageReceived += MouthTalk;
        }
        void Update()
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
            // else if (_isSettingCenter)
            // {
            //     if (Input.GetMouseButtonDown(0))
            //     {
            //         curModel.autoLookAtCenter.transform.position = _camera.ScreenToWorldPoint(Input.mousePosition);
            //         EndSetCenter();
            //     }
            // }

            if (ringMenu.isShow && Input.GetMouseButtonUp(1))
            {
                ringMenu.Hide();
            }
            if (!characterPanel.isShow)
            {
                _scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Input.GetMouseButtonDown(0))
                {
                    // ✅ 跳过 UI 点击
                    if (EventSystem.current && EventSystem.current.IsPointerOverGameObject())
                        return;
                    Vector2 mouseWorldPos = _camera!.ScreenToWorldPoint(Input.mousePosition);
                    Collider2D col = Physics2D.OverlapPoint(mouseWorldPos);
                    if (col && col.CompareTag("Character"))
                    {
                        StartMoveFurniture();
                    }
                }else if (Input.GetMouseButtonDown(1) && GameManager.instance.CurMode == GameMode.Desktop)
                {
                    Vector2 mouseWorldPos = _camera!.ScreenToWorldPoint(Input.mousePosition);
                    Collider2D col = Physics2D.OverlapPoint(mouseWorldPos);
                    if (col && col.CompareTag("Character"))
                    {
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            ringMenu.transform.parent as RectTransform, 
                            Input.mousePosition, 
                            _camera, 
                            out Vector2 localPos
                        );
                        ringMenu.GetComponent<RectTransform>().localPosition = localPos;
                        ringMenu.Show();
                    }
                }else if (_scroll != 0)
                {
                    Vector2 mouseWorldPos = _camera!.ScreenToWorldPoint(Input.mousePosition);
                    Collider2D col = Physics2D.OverlapPoint(mouseWorldPos);
                    if (col && col.CompareTag("Character"))
                    {
                        float sensitivity = 0.2f;        // 调下灵敏度
                        float factor = 1f + _scroll * sensitivity;

                        // 避免 factor 过小或为负数导致翻转
                        factor = Mathf.Clamp(factor, 0.5f, 20f);

                        Vector3 newScale = curModel.transform.localScale * factor;

                        // 统一按比例限制范围，防止太小或太大
                        float minS = 0.05f;
                        float maxS = 20f;
                        float clamped = Mathf.Clamp(newScale.x, minS, maxS);
                        newScale = Vector3.one * clamped;

                        curModel.transform.localScale = newScale;

                        _scaleChanged = true;
                        _scrollSaveTimer = 0f;
                    }
                }
                if (_scaleChanged)
                {
                    _scrollSaveTimer += Time.deltaTime;
                    if (_scrollSaveTimer >= _scrollSaveDelay)
                    {
                        _scrollSaveTimer = 0;
                        EndScaleFurniture();
                        _scaleChanged = false;
                    }
                }
            }
            else
            {
                if (_scaleChanged)
                {
                    _scrollSaveTimer += Time.deltaTime;
                    if (_scrollSaveTimer >= _scrollSaveDelay)
                    {
                        SaveData(false);
                        _scrollSaveTimer = 0;
                        _scaleChanged = false;
                    }
                }
            }
        }

        /// <summary>
        /// 检测对话中的表情
        /// NEEDREMOVE
        /// </summary>
        /// <param name="entry"></param>
        private void CheckExpAndMotion(DialogueEntry entry)
        {
            curModel.CheckExp(entry);
            curModel.CheckMotion(entry);
        }

        public void ShowPanel()
        {
            characterPanel.Show();
            if (curCharacter == null)
            {
                SetCurCharacter(characterDatas[0]);
            }
            else 
            {
                SetCurCharacter(curCharacter);
            }
            saveButton.gameObject.SetActive(false);
            UpdateModelShow();
        }
        
        public void HidePanel()
        {
            if (_isChangeData)
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("是否保存","有未保存的数据，是否需要保存?", () =>
                        {
                            SaveData();
                            DoHidePanel();
                        },()=>
                        {
                            SetDefData();
                            DoHidePanel();
                        },"保存","不保存");
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Save?","There is unsaved data. Do you want to save it?", () =>
                        {
                            SaveData();
                            DoHidePanel();
                        },()=>
                        {
                            SetDefData();
                            DoHidePanel();
                        },"Save","Don’t Save");
                        break;
                }
            }
            else
            {
                DoHidePanel();
            }

        }

        private void DoHidePanel()
        {
            UpdateModelShow();
            characterPanel.Hide();
            OnHideCharacterPanel?.Invoke(curCharacter);
        }

        public void ShowDetailPanel()
        {
            if (curModel.GetType() == typeof(ImageModel))
            {
                MessageManager.instance.ShowMessage("图片类型的立绘无法设置参数");
            }
            else
            {
                live2dDetailPanel.Show(curCharacter,curModel);
                // detailPanel.Show();
                UpdateModelShow();
            }
        }
        
        /// <summary>
        /// 把UI的数据同步到实例
        /// </summary>
        private void SetData()
        {
            if (curCharacter == null) return;
            _isChangeData = true;
            saveButton.gameObject.SetActive(true);
            curCharacter.characterDescription = characterDescriptionInput.text;
            curCharacter.userName = usernameInput.text;
            curCharacter.characterName = characterNameInput.text;
            curCharacter.characterTitle = characterTitleInput.text;
            //NEEDREMOVE

            if (_curMemory != null)
            {
                _curMemory.title = memoryTitleInput.text;
                _curMemory.content = memoryContentInput.text;
            }
        }
        public void SaveData( bool showMsg = true)
        {
            if (showMsg)
            {
                MessageManager.instance.ShowMessage("已保存",MessageType.Success);
            }
            string folderPath = Path.Combine(Application.persistentDataPath, "Characters");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, curCharacter.id + ".vta");
            ES3.Save("CharacterData",curCharacter,filePath);
            _isChangeData = false;
            saveButton.gameObject.SetActive(false);
        }

        private void SetDefData()
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "Characters");
            string filePath = Path.Combine(folderPath, curCharacter.id + ".vta");

            if (File.Exists(filePath))
            {
                Debug.Log("已删除: " + filePath);
            }
            else
            {
                Debug.LogWarning("文件不存在: " + filePath);
            }
            var tmp = ES3.Load<CharacterData>("CharacterData",filePath);
            var inx = characterDatas.IndexOf(curCharacter);
            characterDatas[inx] = tmp;
            curCharacter = characterDatas[inx];
            _isChangeData = false;
            saveButton.gameObject.SetActive(false);
            // if (characterDatas == null)
            // {
            //     characterDatas = tmp;
            //     OnSetCharacterData?.Invoke(null);
            //     noCharacterPanel.SetActive(true);
            //     ClearCharacterPanel();
            //     _isChangeData = false;
            //     saveButton.gameObject.SetActive(false);
            // }
            // else
            // {
            //     curCharacter = tmp;
            //     _isChangeData = false;
            //     saveButton.gameObject.SetActive(false);
            // }
        }

        public void Changed()
        {
            _isChangeData = true;
            saveButton.gameObject.SetActive(true);
        }
        #region 模型控制相关

        #region ----------缩放控制----------
        private void EndScaleFurniture()
        {
            switch (GameManager.instance.CurMode)
            {
                case GameMode.Talk:
                    break;
                case GameMode.ModeTalk:
                    curCharacter.pos = curModel.transform.position;
                    curCharacter.scale = curModel.transform.localScale;
                    break;
                case GameMode.Desktop:
                    curCharacter.deskPos = curModel.transform.position;
                    curCharacter.deskScale = curModel.transform.localScale;
                    break;
            }
            SaveData(false);
        }

        private void ChangeUIScale(float coefficient)
        {
            if (curModel == null) return;
            curModel.transform.localScale = coefficient * curCharacter.uiScale;
            curCharacter.uiScaleCoefficient = coefficient;
            _scaleChanged = true;
        }
        public void ResetUIScale()
        {
            if (curModel == null) return;
            curModel.transform.localScale = curCharacter.uiScale;
            curCharacter.uiScaleCoefficient = 1;
            _scaleChanged = true;
        }
        #endregion
        #region ----------移动控制----------
        private void StartMoveFurniture()
        {
            // FitModelToWord();
            _isMove = true;
            characterPanel.gameObject.SetActive(false);
            Vector2 mouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            _moveOffset = (Vector2)curModel.transform.position - mouseWorldPos;
        }
        private void MoveFurniture()
        {
            Vector2 mouseWorldPos =_camera.ScreenToWorldPoint(Input.mousePosition);
            curModel.transform.position = mouseWorldPos + _moveOffset;
        }
        private void EndMoveFurniture()
        {
            switch (GameManager.instance.CurMode)
            {
                case GameMode.Talk:
                    break;
                case GameMode.ModeTalk:
                    curCharacter.pos = curModel.transform.position;
                    curCharacter.scale = curModel.transform.localScale/curCharacter.uiScaleCoefficient;
                    break;
                case GameMode.Desktop:
                    curCharacter.deskPos = curModel.transform.position;
                    curCharacter.deskScale = curModel.transform.localScale/curCharacter.uiScaleCoefficient;;
                    break;
            }
            SaveData(false);
            _isMove = false;
        }
        #endregion

        /// <summary>
        /// 重置模型数据
        /// </summary>
        public void ResetScaleAndPos()
        {
            if (curCharacter.pos == Vector3.zero && curCharacter.scale == Vector3.one && curCharacter.deskPos == Vector3.zero && curCharacter.deskScale == Vector3.one)
            {
                return;
            }
            curCharacter.pos = Vector3.zero;
            curCharacter.scale = Vector3.one;
            curCharacter.deskPos = Vector3.zero;
            curCharacter.deskScale = Vector3.one;

            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    MessageManager.instance.ShowMessage("立绘模型和大小已重置");
                    break;
                case "en":
                    MessageManager.instance.ShowMessage("Model and size have been reset");
                    break;
            }
        }
        #endregion
        
        #region 角色相关

        /// <summary>
        /// 新增角色
        /// </summary>
        public void AddCharacter()
        {
            if (!characterPanel.isShow)
            {
                characterPanel.Show();
            }

            string baseTitle = "Untitled";
            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    baseTitle = "未设置";
                    break;
                case "en":
                    baseTitle = "Untitled";
                    break;
            }

            // 查找现有最大序号
            int maxIndex = 0;
            foreach (var data in characterDatas)
            {
                if (data.characterTitle.StartsWith(baseTitle))
                {
                    // 截取 baseTitle 后的数字部分
                    var suffix = data.characterTitle.Substring(baseTitle.Length);
                    if (int.TryParse(suffix, out int num))
                    {
                        if (num > maxIndex) maxIndex = num;
                    }
                }
            }

            // 下一个序号
            int newIndex = maxIndex + 1;
            string title = baseTitle + newIndex;

            var characterData = new CharacterData { characterTitle = title };
            characterDatas.Insert(0, characterData);
            SetCurCharacter(characterData);
            SaveData();
        }
        /// <summary>
        /// 移除指定角色
        /// </summary>
        /// <param name="target"></param>
        public void RemoveCharacter(CharacterData target)
        {
            characterDatas.Remove(target);
            string folderPath = Path.Combine(Application.persistentDataPath, "Characters");
            string filePath = Path.Combine(folderPath, target.id + ".vta");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("已删除: " + filePath);
            }
            else
            {
                Debug.LogWarning("文件不存在: " + filePath);
            }
            // 处理当前选中角色被删除的情况
            if (curCharacter == target)
            {
                curCharacter = characterDatas.Count > 0 ? characterDatas[0] : null;
            }
            UpdateCharacterPanel();
        }
        /// <summary>
        /// 复制指定角色
        /// </summary>
        /// <param name="target"></param>
        public void CopyCharacter(CharacterData target)
        {
            var newData = target.Clone();
            newData.characterTitle += " 副本";
            characterDatas.Add(newData);
            UpdateCharacterPanel();
            SetCurCharacter(newData);
            SetData();
        }
        /// <summary>
        /// 更新界面UI
        /// </summary>
        private void UpdateCharacterPanel()
        {
            if (curCharacter == null)
            {
                OnSetCharacterData?.Invoke(null);
                noCharacterPanel.SetActive(true);
                ClearCharacterPanel();
                return;
            }
            noCharacterPanel.SetActive(false);
            characterPageList.SetData(characterDatas);
            characterPageList.GotoItem(curCharacter);
            UpdateCurCharacterPanel();
        }

        private void ClearCharacterPanel()
        {
            characterDescriptionInput.text = "";
            usernameInput.text = "";
            characterNameInput.text = "";
            characterTitleInput.text = "";
            memoryPageList.Clear();
            characterPageList.Clear();
            GameManager.instance.LoadImage("",headImage,defHeadIcon);
            UpdateModelShow();
            // SaveData(false);
            characterPanel.Hide();
        }
        /// <summary>
        /// 更新角色相关界面
        /// </summary>
        private void UpdateCurCharacterPanel()
        {
            //解除绑定
            characterDescriptionInput.onValueChanged.RemoveAllListeners();
            usernameInput.onValueChanged.RemoveAllListeners();
            characterNameInput.onValueChanged.RemoveAllListeners();
            characterTitleInput.onValueChanged.RemoveAllListeners();
            
            isBreathToggle.onValueChanged.RemoveAllListeners();
            isBlinkToggle.onValueChanged.RemoveAllListeners();
            isLookMouseToggle.onValueChanged.RemoveAllListeners();
            uiSizeSlider.onValueChanged.RemoveAllListeners();

            //UI更新
            characterDescriptionInput.text = curCharacter.characterDescription;
            usernameInput.text = curCharacter.userName;
            characterNameInput.text = curCharacter.characterName;
            characterTitleInput.text = curCharacter.characterTitle;
            isBlinkToggle.isOn = curCharacter.isBlink;
            isBreathToggle.isOn = curCharacter.isBreath;
            isLookMouseToggle.isOn = curCharacter.isLookAt;
            uiSizeSlider.value = curCharacter.uiScaleCoefficient;
            //重新绑定
            characterDescriptionInput.onValueChanged.AddListener(_ => SetData());
            usernameInput.onValueChanged.AddListener(_ => SetData());
            characterNameInput.onValueChanged.AddListener(_ => SetData());
            characterTitleInput.onValueChanged.AddListener(_ => SetData());
            
            isBreathToggle.onValueChanged.AddListener(_=>SetData());
            isBlinkToggle.onValueChanged.AddListener(_=>SetData());
            isLookMouseToggle.onValueChanged.AddListener(_=>SetData());
            uiSizeSlider.onValueChanged.AddListener(ChangeUIScale);

            // 同步记忆数据到 UI
            // UIPanel.BindList(curCharacter.memories,
            //     memoryLineList,
            //     memoryLinePeb,
            //     memoryParent,
            //     (line, data, index) => line.SetData(data));
            memoryPageList.SetData(curCharacter.memories);
            
            //更新live2d
            UpdateModelShow();
            // _isChangeData = false;
            // saveButton.gameObject.SetActive(false);
            GameManager.instance.LoadImage(curCharacter.iconPath,headImage,defHeadIcon);
        }
        /// <summary>
        /// 设置焦点角色,并更新UI
        /// </summary>
        /// <param name="characterData"></param>
        public void SetCurCharacter(CharacterData characterData)
        {
            if (_isChangeData)
            {
                Debug.Log("VAR");
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("是否保存","有未保存的数据，是否需要保存?", () =>
                        {
                            SaveData();
                            DoHidePanel();
                        },()=>
                        {
                            SetDefData();
                            DoHidePanel();
                        },"保存","不保存");
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Save?","There is unsaved data. Do you want to save it?", () =>
                        {
                            SaveData();
                            DoHidePanel();
                        },()=>
                        {
                            SetDefData();
                            DoHidePanel();
                        },"Save","Don’t Save");
                        break;
                }
            }
            else
            {
                curCharacter = characterData;
                OnSetCharacterData?.Invoke(curCharacter);
                UpdateCharacterPanel();
            }
        }
        /// <summary>
        /// 导出角色
        /// </summary>
        /// <param name="characterData"></param>
        public void ExportCharacter(CharacterData characterData)
        {
            var extension = new ExtensionFilter("Character File", "vta");
            string path = StandaloneFileBrowser.SaveFilePanel("导出角色", "", characterData.characterTitle,  new[] { extension });

            if (!string.IsNullOrEmpty(path))
            {
                var dataToExport = characterData.Clone();
                dataToExport.backgroundPath = "";
                dataToExport.iconPath = "";
                dataToExport.live2dPath = "";
                ES3.Save("CharacterData", dataToExport, path);
                Debug.Log("角色数据已保存到: " + path);
            }
            else
            {
                Debug.Log("用户取消了保存操作。");
            }
        }
        /// <summary>
        /// 导入角色
        /// </summary>
        public void ImportCharacterData()
        {
            var extension = new ExtensionFilter("Character File", "vta");
            string[] paths = StandaloneFileBrowser.OpenFilePanel("加载角色", "", new[] { extension }, false);

            if (paths.Length > 0 && File.Exists(paths[0]))
            {
                var newCharacterData = ES3.Load<CharacterData>(paths[0]);

                // 检查是否重名
                string baseTitle = newCharacterData.characterTitle;
                string finalTitle = baseTitle;
                int index = 1;
                while (characterDatas.Any(c => c.characterTitle == finalTitle))
                {
                    finalTitle = $"{baseTitle}{index}";
                    index++;
                }
                newCharacterData.characterTitle = finalTitle;

                characterDatas.Add(newCharacterData);
                SetCurCharacter(newCharacterData);
                SaveData();
            }
        }

        #endregion
        
        #region  立绘相关

        private void LoadModel()
        {
            string ext = Path.GetExtension(curCharacter.live2dPath).ToLowerInvariant();
            if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
            {
                LoadLive2d();
            }
            else if (ext == ".json")
            {
                LoadLive2d();
            }
            else if(ext == ".vtam")
            {
                LoadCustomModel();
            }
        }
        private void LoadLive2d()
        {
            Debug.Log("LoadLive2d");
            StartCoroutine(LoadLive2dIE());
        }
        IEnumerator LoadLive2dIE()
        {
            loadingPanel.SetActive(true);
            yield return null;
            live2DModelLoader.LoadModelFromFile(curCharacter, curCharacter.live2dPath,OnModelLoadSuccess);
        }
        private void LoadCustomModel()
        {
            Debug.Log("LoadCustomModel");
            StartCoroutine(LoadCustomModelIE());
        }
        IEnumerator LoadCustomModelIE()
        {
            loadingPanel.SetActive(true);
            yield return null;
            customModelLoader.LoadModelFromFile(curCharacter, curCharacter.live2dPath,OnModelLoadSuccess);
        }
        /// <summary>
        /// 模型成功加载后
        /// NEEDCHANGE
        /// </summary>
        /// <param name="model"></param>
        void OnModelLoadSuccess(GeneralModel model)
        {
            if (model == null)
            {
                loadingPanel.SetActive(false);
                curCharacter.live2dPath = "";
                UpdateModelShow();
                return;
            }
            loadingPanel.SetActive(false);
            curModel = model;
            if (curModel.GetType() == typeof(Live2DController) || curModel.GetType() == typeof(ImageModel))
            {
                curModel.OnLoadSuccess(curCharacter,live2DModelLoader);
                if (live2DModelLoader.modelData == curCharacter)
                {
                    curModel.SetLayer(101);
                    curModel.SetColor(curCharacter.backgroundLight);
                    SetModelToUI();
                    FitModelToUI();
                    loadButton.gameObject.SetActive(false);
                }
                else
                {
                    curModel.gameObject.SetActive(false);
                }
            }
            else
            {
                curModel.OnLoadSuccess(curCharacter,customModelLoader);
                curModel.SetLayer(101);
                curModel.SetColor(curCharacter.backgroundLight);
                SetModelToUI();
                FitModelToUI();
                loadButton.gameObject.SetActive(false);
            }
            // curModel.autoLookAtCenter.localPosition = curCharacter.lookCenter;

            UpdateModelShow();
            // SetParametersValue();
            // SetMotionValue();
            // SetExpValue();
        }
        /// <summary>
        /// 设置角色的live2d
        /// </summary>
        private void SetLive2d()
        {
            StartCoroutine(GetLive2dPath());
        }
        /// <summary>
        /// 设置角色的自定义模型
        /// </summary>
        private void SetCustomModel()
        {
            StartCoroutine(GetCustomModelPath());
        }
        IEnumerator GetLive2dPath()
        {
            loadingPanel.SetActive(true);
            var extensions = new[]
            {
                new ExtensionFilter("Live2D Model", "model3.json"),
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg"),
            };
            var title = "选择 Live2D 模型文件";
            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    title = "选择 Live2D 模型文件";
                    break;
                case "en":
                    title = "Choose Live2D Model File";
                    break;
            }
            string[] paths = StandaloneFileBrowser.OpenFilePanel(title, "", extensions, false);

            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            {
                Debug.LogWarning("未选择模型文件。");
                loadingPanel.SetActive(false);
                yield break;
            }
            string modelJsonPath = paths[0];
            live2DModelLoader.LoadModelFromFile(curCharacter,modelJsonPath, model =>
            {
                curCharacter.live2dPath = modelJsonPath;
                SetData();
                loadingPanel.SetActive(false);
                curModel=model;
                if (curModel)
                {
                    if (live2DModelLoader.modelData == curCharacter)
                    {
                        curModel.gameObject.SetActive(_needShowLive2d);
                        curModel.SetLayer(101);
                    }
                    else
                    {
                        curModel.gameObject.SetActive(false);
                    }
                }
                OnModelLoadSuccess(model);
            });
        }
        IEnumerator GetCustomModelPath()
        {
            loadingPanel.SetActive(true);
            var extensions = new[]
            {
                new ExtensionFilter("Model File", "vtam"),
            };
            var title = "选择模型文件";
            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    title = "选择模型文件";
                    break;
                case "en":
                    title = "Choose Model File";
                    break;
            }
            string[] paths = StandaloneFileBrowser.OpenFilePanel(title, "", extensions, false);

            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            {
                Debug.LogWarning("未选择模型文件。");
                loadingPanel.SetActive(false);
                yield break;
            }
            string modelJsonPath = paths[0];
            customModelLoader.LoadModelFromFile(curCharacter,modelJsonPath, model =>
            {
                curCharacter.live2dPath = modelJsonPath;
                SetData();
                loadingPanel.SetActive(false);
                curModel=model;
                if (curModel)
                {
                    curModel.gameObject.SetActive(_needShowLive2d);
                    curModel.SetLayer(101);
                }
                OnModelLoadSuccess(model);
            });
        }
        public void ResetModel()
        {
            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    MessageManager.instance.ShowPropUpMessage("确认",$"确认清空<b>{curCharacter.characterTitle}</b>的人物模型吗？",DoResetModel);
                    break;
                case "en":
                    MessageManager.instance.ShowPropUpMessage("Confirm","Are you sure you want to clear the character illustration of <b>{curCharacter.characterTitle}</b>?",DoResetModel);
                    break;
            }
        }
        private void DoResetModel()
        {
            curModel = null;
            curCharacter.live2dPath = "";
            curCharacter.pos = Vector3.zero;
            curCharacter.scale = Vector3.one;
            curCharacter.deskPos = Vector3.zero;
            curCharacter.deskScale = Vector3.one;
            curCharacter.uiScale = Vector3.one;
            curCharacter.lookCenter = Vector3.zero;
            curCharacter.uiScaleCoefficient = 1;
            
            curCharacter.modelParameters.Clear();
            curCharacter.modelExps.Clear();
            curCharacter.modelMotions.Clear();
            curCharacter.activeModelExps.Clear();
            live2DModelLoader.RemoveCurModel();
            SetData();
            UpdateModelShow();
        }
        private void FitModelToUI()
        {
            // 6. 设置模型位置和缩放
            curModel.transform.localPosition = curCharacter.uiPos;
            curModel.transform.localScale = curCharacter.uiScale * curCharacter.uiScaleCoefficient;
            curModel.SetLayer(201);
            curModel.SetColor(Color.white);
        }

        private void FitModelToWord()
        {
            FitBoxColliderToModel();
            curModel.SetColor(curCharacter.backgroundLight);
            switch (GameManager.instance.CurMode)
            {
                case GameMode.Talk:
                    break;
                case GameMode.ModeTalk:
                    curModel.transform.localPosition = curCharacter.pos;
                    curModel.transform.localScale = curCharacter.scale;
                    break;
                case GameMode.Desktop:
                    curModel.transform.localPosition = curCharacter.deskPos;
                    curModel.transform.localScale = curCharacter.deskScale;
                    break;
            }

            curModel.SetLayer(0);
        }
        private void SetModelToUI()
        {
            if (targetUIRect == null || curModel == null) return;
            if(curCharacter.uiScale != Vector3.one){return;}
            // 1. 获取 UI 区域的世界四个角
            Vector3[] corners = new Vector3[4];
            targetUIRect.GetWorldCorners(corners);
            Vector3 bottomLeft = corners[0];
            Vector3 topRight = corners[2];

            // 2. UI 区域中心点 & 宽高
            Vector3 centerWorld = (bottomLeft + topRight) / 2f;
            float uiWidth = Vector3.Distance(corners[0], corners[3]); // x方向
            float uiHeight = Vector3.Distance(corners[0], corners[1]); // y方向

            // 3. 获取模型的原始尺寸
            var bounds = curModel.GetBounds();
            float modelWidth = bounds.size.x;
            float modelHeight = bounds.size.y;

            // 4. 将中心点转换到模型父节点的本地坐标
            Vector3 localCenter = curModel.transform.parent != null
                ? curModel.transform.parent.InverseTransformPoint(centerWorld)
                : centerWorld;
            // 5. 缩放比例（保持纵横比填满）
            float scaleX = uiWidth / modelWidth;
            float scaleY = uiHeight / modelHeight;
            float scale = Mathf.Min(scaleX, scaleY); // 等比缩放
            if (scale > 1000)
            {
                scale = 1;
            }
            // 6. 设置模型位置和缩放
            curModel.transform.localPosition = localCenter;
            curModel.transform.localScale = Vector3.one * (scale * curCharacter.uiScaleCoefficient);
            
            curCharacter.uiPos = curModel.transform.localPosition;
            curCharacter.uiScale = curModel.transform.localScale / curCharacter.uiScaleCoefficient;;
            _scaleChanged = true;
            Debug.Log(curCharacter.uiScale);
        }
        private void FitBoxColliderToModel()
        {
            if (!curModel) return;

            // 获取包围盒（在模型的本地空间中）
            var bounds = curModel.GetBounds();;
            Vector3 localCenter = curModel.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = curModel.transform.InverseTransformVector(bounds.size);
            // 获取 BoxCollider2D，如果没有则添加
            var boxCollider2d = curModel.boxCollider2d;
            
            // 设置碰撞体尺寸和偏移
            boxCollider2d.size = localSize;
            boxCollider2d.offset = localCenter;
        }
        /// <summary>
        /// 设置模型是否展示
        /// </summary>
        private void UpdateModelShow()
        {
            if (curCharacter == null)
            {
                loadButton.gameObject.SetActive(false);
                if (curModel != null)
                {
                    Destroy(curModel.gameObject);
                    curModel=null;
                }
                btns.gameObject.SetActive(false);
                return;
            }
            if (characterPanel.isShow)
            {
                if (curModel != null)
                {
                    if (live2DModelLoader.modelData == curCharacter)
                    {
                        curModel.gameObject.SetActive(true);
                        FitModelToUI();
                        loadButton.gameObject.SetActive(false);
                        btns.gameObject.SetActive(true);
                    }
                    else
                    {
                        curModel.gameObject.SetActive(false); 
                        loadButton.gameObject.SetActive(true);
                        if (curCharacter.live2dPath is { Length: > 0 })
                        {
                            switch (LocalizerManager.GetCode())
                            {
                                case "zh-Hans":
                                    loadText.text = "角色模型已设置，点击加载";
                                    break;
                                case "en":
                                    loadText.text = "Role model has been set, click to load";
                                    break;
                            }
                            loadButton.onPointerClick.RemoveAllListeners();
                            loadButton.onPointerClick.AddListener(LoadModel);
                        }
                        else
                        {
                            switch (LocalizerManager.GetCode())
                            {
                                case "zh-Hans":
                                    loadText.text = "角色模型未设置，点击选择角色模型";
                                    break;
                                case "en":
                                    loadText.text = "Role model is not set, click to select role model";
                                    break;
                            }
                            loadButton.onPointerClick.RemoveAllListeners();
                            loadButton.onPointerClick.AddListener(SetLive2d);
                        }
                        btns.gameObject.SetActive(false);
                    }

                }else if (curCharacter.live2dPath is { Length: > 0 })
                {
                    loadButton.gameObject.SetActive(true);
                    switch (LocalizerManager.GetCode())
                    {
                        case "zh-Hans":
                            loadText.text = "角色模型已设置，点击加载";
                            break;
                        case "en":
                            loadText.text = "Role model has been set, click to load";
                            break;
                    }
                    loadButton.onPointerClick.RemoveAllListeners();
                    loadButton.onPointerClick.AddListener(LoadModel);
                    btns.gameObject.SetActive(false);
                }
                else
                {
                    loadButton.gameObject.SetActive(true);
                    switch (LocalizerManager.GetCode())
                    {
                        case "zh-Hans":
                            loadText.text = "角色模型未设置，点击选择角色模型";
                            break;
                        case "en":
                            loadText.text = "Role model is not set, click to select role model";
                            break;
                    }
                    loadButton.onPointerClick.RemoveAllListeners();
                    loadButton.onPointerClick.AddListener(SetLive2d);
                    btns.gameObject.SetActive(false);
                }
            }
            else
            {
                if (_needShowLive2d)
                {
                    if (curModel != null && live2DModelLoader.modelData == curCharacter && curCharacter.live2dPath is { Length: > 0 })
                    {
                        curModel.gameObject.SetActive(true);
                        FitModelToWord();
                        btns.gameObject.SetActive(true);
                    }
                    else if(curCharacter.live2dPath is { Length: > 0 })
                    {
                        LoadLive2d();
                        btns.gameObject.SetActive(true);
                    }
                    else if(curModel != null)
                    {
                        Destroy(curModel.gameObject);
                        curModel=null;
                        btns.gameObject.SetActive(false);
                    }
                }
                else if(curModel != null)
                {
                    Destroy(curModel.gameObject);
                    curModel=null;
                    btns.gameObject.SetActive(true);
                }
            }
        }
        
        /// <summary>
        /// 播放指定动画组
        /// </summary>
        /// <param name="target"></param>
        public void PlayMotion(ModelMotion target)
        {
            if (curModel !=null && curModel.characterData == curCharacter)
            {
                curModel.PlayMotion(target);
            }
        }
        /// <summary>
        /// 播放指定表情
        /// </summary>
        /// <param name="target"></param>
        /// <param name="isPlay"></param>
        public void SetExpression(CubismExp3Json target,bool isPlay)
        {
            if (curModel.characterData == curCharacter)
            {
                if (isPlay)
                {
                    curModel.SetExpression(target);
                }
                else
                {
                    curModel.CancelExpression(target);
                }
            }
        }
        private void MouthTalk(DialogueEntry entry)
        {
            if (!curCharacter.SettingData.ttsIson && curModel != null && curModel.characterData == curCharacter)
            {
                curModel.FakeTalk(3);
            }
        }

        /// <summary>
        /// 打开组件界面
        /// </summary>
        public void ShowComponentPanel()
        {
            componentPanel.Show(curCharacter,curModel);
        }
        public void CreateModel()
        {
            var model = new GameObject().AddComponent<CustomModel>();
            model.SetModelData(new CustomModelData());
            OnModelLoadSuccess(model);
            componentPanel.Show(curCharacter,curModel);
        }
        #endregion
        
        #region 记忆相关

        /// <summary>
        /// 展示指定记忆的详情
        /// </summary>
        public void ShowMemory(MemoryLine target)
        {
            _curMemory = target.data;
            memoryTitleInput.text = target.data.title;
            memoryContentInput.text = target.data.content;
            memoryTitleInput.onValueChanged.RemoveAllListeners();
            memoryTitleInput.onValueChanged.AddListener(_ => SetData());
            memoryContentInput.onValueChanged.RemoveAllListeners();
            memoryContentInput.onValueChanged.AddListener(_ => SetData());
            
            memoryPanel.Show();
        }

        /// <summary>
        /// 添加新的记忆
        /// </summary>
        public void AddNewMemory()
        {
            var target = new Memory();
            curCharacter.memories.Add(target);
            memoryPageList.SetData(curCharacter.memories);
            // UpdateCurCharacterPanel();
        }

        public void HideMemoryPanel()
        {
            memoryTitleInput.onValueChanged.RemoveAllListeners();
            memoryContentInput.onValueChanged.RemoveAllListeners();
            _curMemory = null;
            memoryPanel.Hide();
            memoryPageList.SetData(curCharacter.memories);
            // UpdateCurCharacterPanel();
        }
        /// <summary>
        /// 移除当前正在编辑的记忆
        /// </summary>
        public void RemoveMemory()
        {
            if (_curMemory == null || curCharacter == null) return;
            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    MessageManager.instance.ShowPropUpMessage("确认",$"确认删除记忆 {_curMemory.title} 吗？",()=>
                    {
                        curCharacter.memories.Remove(_curMemory);
                        memoryPageList.SetData(curCharacter.memories);
                        HideMemoryPanel();
                    });
                    break;
                case "en":
                    MessageManager.instance.ShowPropUpMessage("Confirm",$"Are you sure you want to delete the memory {_curMemory.title}?",()=>
                    {
                        curCharacter.memories.Remove(_curMemory);
                        memoryPageList.SetData(curCharacter.memories);
                        HideMemoryPanel();
                    });
                    break;
            }

        }

        #endregion
        
        #region 头像相关

        public void SetHeadIcon()
        {
            // 打开文件选择器
            string[] paths = StandaloneFileBrowser.OpenFilePanel("选择图片", "", "", false);

            if (paths.Length > 0)
            {
                GameManager.instance.LoadImage(paths[0],headImage,defHeadIcon);
                curCharacter.iconPath = paths[0];
            }
            SetData();
        }
        public void ResetHeadIcon()
        {
            // 打开文件选择器
            GameManager.instance.LoadImage(null,headImage);
            curCharacter.iconPath = "";
            SetData();
        }

        public Sprite GetHeadIcon()
        {
            return headImage.sprite;
        }
        #endregion

        private void ChangeMode(int targetInx)
        {
            switch (targetInx)
            {
                case 0:
                    _needShowLive2d = false;
                    UpdateModelShow();
                    break;
                case 1:
                    _needShowLive2d = true;
                    UpdateModelShow();
                    break;
                case 2:
                    _needShowLive2d = true;
                    UpdateModelShow();
                    break;
            }
        }
    }
}
public class NaturalSortComparer : IComparer<string>
{
    public int Compare(string x, string y)
    {
        return String
            .Compare(Regex.Replace(x ?? "", @"\d+", m => m.Value.PadLeft(10, '0')), Regex.Replace(y ?? "", @"\d+", m => m.Value.PadLeft(10, '0')), StringComparison.Ordinal);
    }
}