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
        public Live2DController curModel;
        public Live2DModelLoader live2DModelLoader;
        public GameObject loadingPanel;
        public TMP_Text loadText;
        public WButton loadButton;
        
        public Toggle isBreathToggle;
        public Toggle isBlinkToggle;
        public Toggle isLookMouseToggle;
        public WButton detailBtn;
        public WButton removeBtn;
        
        //移动
        private Vector2 _moveOffset;
        private bool _isMove;
        //旋转
        private bool _isRotating;
        private float _startAngleOffset;
        //设置模型中心
        private bool _isSettingCenter;
        public GameObject setCenterPanel;
        //缩放
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
        [Header("立绘参数")] 
        public WPageList parameterPageList;
        public TMP_InputField parameterSearchInput;
        private readonly List<ModelParameter> _needSetDef = new (); 
        private static readonly NaturalSortComparer NaturalComparer = new NaturalSortComparer();
        [Header("立绘动画")]
        public MotionLine motionLinePeb;
        [HideInInspector]
        public List<MotionLine> motionLines = new();
        public RectTransform motionLineParent;
        [Header("立绘表情")]
        [HideInInspector]
        public List<ModelExp> expPageItems = new();
        public List<ModelParameter> parameterPageItems = new();
        public WPageList expPageList;

        public Toggle isShowDisableExpToggle;
        // 自定义表情
        public UIPanel customExpPanel;
        public WPageList customExpParameterPageList;
        public TMP_InputField customExpNameInput;
        public TMP_InputField customExpNickNameInput;
        public TMP_InputField customExpFadeInInput;
        public TMP_InputField customExpFadeOutInput;
        private ModelExp _curCustomExp;
        private readonly Dictionary<ModelParameter,float> _paramsSnapshot = new();
        private bool _needUseSnapshot;
        private bool _customExpChanged;
        
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

        [Header("保存相关")] 

        private bool _isChangeData;
        public WButton saveButton;

        private Camera _camera;

        public event Action<CharacterData> OnSetCharacterData;
        public event Action<CharacterData> OnHideCharacterPanel;
        
        private void Awake()
        {
            instance = this;
            characterDatas = ES3.Load("characterDatas", new List<CharacterData>());
            if (characterDatas.Count > 0)
            {
                SetCurCharacter(characterDatas[0]);
            }
            else
            {
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
            
            // moveButton.onPointerDown.AddListener(StartMoveFurniture);
            // moveButton.onPointerUp.AddListener(EndMoveFurniture);
            
            // scaleButton.onPointerDown.AddListener(StartScaleFurniture);
            // scaleButton.onPointerUp.AddListener(EndScaleFurniture);

            DialogManager.instance.OnMessageReceived += CheckExpAndMotion;
            DialogManager.instance.OnMessageReceived += MouthTalk;
            
            parameterSearchInput.onSubmit.AddListener(_=>Search());
            isShowDisableExpToggle.onValueChanged.AddListener(IsShowDisableExps);
            
            //自定义表情相关
            customExpNameInput.onValueChanged.RemoveAllListeners();
            customExpNameInput.onValueChanged.AddListener(_ =>
            {
                _customExpChanged = true;
            });
            customExpNickNameInput.onValueChanged.RemoveAllListeners();
            customExpNickNameInput.onValueChanged.AddListener(_ =>
            {
                _customExpChanged = true;
            });
            customExpFadeInInput.onValueChanged.RemoveAllListeners();
            customExpFadeInInput.onValueChanged.AddListener(_ =>
            {
                _customExpChanged = true;
            });
            customExpFadeOutInput.onValueChanged.RemoveAllListeners();
            customExpFadeOutInput.onValueChanged.AddListener(_ =>
            {
                _customExpChanged = true;
            });
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
            else if (_isSettingCenter)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    curModel.autoLookAtCenter.transform.position = _camera.ScreenToWorldPoint(Input.mousePosition);
                    EndSetCenter();
                }
            }

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
                        curModel.transform.localScale += Vector3.one * _scroll;
                        _scaleChanged = true;
                        _scrollSaveTimer = 0f; // 重置保存计时器
                    }
                }
                if (_scaleChanged)
                {
                    _scrollSaveTimer += Time.deltaTime;
                    if (_scrollSaveTimer >= _scrollSaveDelay)
                    {
                        EndScaleFurniture();
                        _scaleChanged = false;
                    }
                }
            }
            else if(curModel!=null && !curModel.isLookMouse)
            {
                // if (Input.GetMouseButtonDown(0))
                // {
                //     Vector2 mouseWorldPos = _camera!.ScreenToWorldPoint(Input.mousePosition);
                //     Collider2D col = Physics2D.OverlapPoint(mouseWorldPos);
                //     if (col && col.CompareTag("Character"))
                //     {
                //         curModel.dragController.StartDrag();
                //     }
                // }else if (Input.GetMouseButtonUp(0))
                // {
                //     curModel.dragController.EndDrag();
                // }
            }
        }

        private void LateUpdate()
        {
            if (_needSetDef.Count > 0)
            {
                foreach (var parameterListItemData in _needSetDef)
                {
                    parameterListItemData.ResetToDefault();
                }
                _needSetDef.Clear();
            }

            if (_needUseSnapshot)
            {
                foreach (var parameterPageItem in parameterPageItems)
                {
                    parameterPageItem.parameter.Value = _paramsSnapshot[parameterPageItem];
                }
                _paramsSnapshot.Clear();
                _needUseSnapshot = false;
            }
        }

        private void CheckExpAndMotion(DialogueEntry entry)
        {
            if(curModel == null) return;
            var message = entry.content;
            List<AnimationClip> matchedMotion = new List<AnimationClip>();
            foreach (var item in motionLines)
            {
                if (!item.isOn.isOn){continue;}

                var keyStr = item.motionNameInput.text;
                try
                {
                    // 尝试将 pattern 作为正则表达式匹配 input
                    if (Regex.IsMatch(message, keyStr))
                    {
                        matchedMotion.Add(item.Clip);
                    }
                }
                catch (ArgumentException)
                {
                    // pattern 不是有效正则，退而求其次使用普通字符串匹配
                    if (message.Contains(keyStr))
                    {
                        matchedMotion.Add(item.Clip);
                    }
                }
            }
            PlayMotions(matchedMotion);
            curModel.ClearAllExpressions();
            foreach (var item in expPageItems)
            {
                if (!item.expOn){continue;}

                var keyStr = item.expName;
                try
                {
                    // 尝试将 pattern 作为正则表达式匹配 input
                    if (Regex.IsMatch(message, keyStr))
                    {
                        SetExpression(item.exp3Json,true);
                    }
                }
                catch (ArgumentException)
                {
                    // pattern 不是有效正则，退而求其次使用普通字符串匹配
                    if (message.Contains(keyStr))
                    {
                        SetExpression(item.exp3Json,true);
                    }
                }
            }
            PlayMotions(matchedMotion);
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
            detailPanel.Show();
            UpdateModelShow();
        }

        public void HideDetailPanel()
        {
            SaveParameters();
            SaveMotion();
            SaveExp();
            if (curModel)
            {
                curModel.ClearAllExpressions();
            }
            detailPanel.Hide();
            UpdateModelShow();
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
            curCharacter.isBlink = isBlinkToggle.isOn;
            curCharacter.isLookAt = isLookMouseToggle.isOn;
            curCharacter.isBreath = isBreathToggle.isOn;
            
            if (curModel != null)
            {
                curModel.SetBreath(isBlinkToggle.isOn);
                curModel.SetLookMouse(isLookMouseToggle.isOn);
                curModel.SetBlink(isBlinkToggle.isOn);
                curCharacter.lookCenter = curModel.autoLookAtCenter.localPosition;
            }

            if (_curMemory != null)
            {
                _curMemory.title = memoryTitleInput.text;
                _curMemory.content = memoryContentInput.text;
            }
        }
        /// <summary>
        /// 保存角色数据到磁盘
        /// </summary>
        public void SaveData(bool showMsg = true)
        {
            if (showMsg)
            {
                MessageManager.instance.ShowMessage("已保存",MessageType.Success);
            }
            ES3.Save("characterDatas",characterDatas);
            _isChangeData = false;
            saveButton.gameObject.SetActive(false);
        }

        private void SetDefData()
        {
            var tmp = ES3.Load("characterDatas", new List<CharacterData>());
            Debug.Log("Set Def");
            Debug.Log(tmp.Count);
            if (tmp.Count <= 0)
            {
                characterDatas = tmp;
                OnSetCharacterData?.Invoke(null);
                noCharacterPanel.SetActive(true);
                ClearCharacterPanel();
                _isChangeData = false;
                saveButton.gameObject.SetActive(false);
            }
            else
            {
                var inx = characterDatas.IndexOf(curCharacter);
                characterDatas = tmp;
                curCharacter = characterDatas[inx];
                _isChangeData = false;
                saveButton.gameObject.SetActive(false);
            }
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
                    curCharacter.scale = curModel.transform.localScale;
                    break;
                case GameMode.Desktop:
                    curCharacter.deskPos = curModel.transform.position;
                    curCharacter.deskScale = curModel.transform.localScale;
                    break;
            }
            SaveData(false);
            _isMove = false;
        }
        #endregion
        #region ----------设置中心----------

        public void StartSetCenter()
        {
            _isSettingCenter = true;
            curModel.SetLookMouse(false);
            setCenterPanel.SetActive(true);
            curModel.autoLookAtCenter.gameObject.SetActive(true);
        }

        private void EndSetCenter()
        {
            _isSettingCenter = false;
            curModel.SetLookMouse(isLookMouseToggle.isOn);
            setCenterPanel.SetActive(false);
            curModel.autoLookAtCenter.gameObject.SetActive(false);
            MessageManager.instance.ShowMessage("已设置模型视线中心点");
            SetData();
        }
        #endregion

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

            var title = "Untitled";
            switch (LocalizerManager.GetCode())
            {
                case "zh-Hans":
                    title = "未设置";
                    break;
                case "en":
                    title = "Untitled";
                    break;
            }
            var characterData = new CharacterData { characterTitle = title };
            characterDatas.Add(characterData);
            SetCurCharacter(characterData);
            SetData();
        }
        /// <summary>
        /// 移除指定角色
        /// </summary>
        /// <param name="target"></param>
        public void RemoveCharacter(CharacterData target)
        {
            characterDatas.Remove(target);
            // 处理当前选中角色被删除的情况
            if (curCharacter == target)
            {
                curCharacter = characterDatas.Count > 0 ? characterDatas[0] : null;
            }
            UpdateCharacterPanel();
            SetData();
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
            SaveData(false);
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
            //UI更新
            characterDescriptionInput.text = curCharacter.characterDescription;
            usernameInput.text = curCharacter.userName;
            characterNameInput.text = curCharacter.characterName;
            characterTitleInput.text = curCharacter.characterTitle;
            isBlinkToggle.isOn = curCharacter.isBlink;
            isBreathToggle.isOn = curCharacter.isBreath;
            isLookMouseToggle.isOn = curCharacter.isLookAt;
            //重新绑定
            characterDescriptionInput.onValueChanged.AddListener(_ => SetData());
            usernameInput.onValueChanged.AddListener(_ => SetData());
            characterNameInput.onValueChanged.AddListener(_ => SetData());
            characterTitleInput.onValueChanged.AddListener(_ => SetData());
            
            isBreathToggle.onValueChanged.AddListener(_=>SetData());
            isBlinkToggle.onValueChanged.AddListener(_=>SetData());
            isLookMouseToggle.onValueChanged.AddListener(_=>SetData());
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
            var extension = new ExtensionFilter("Character File", "character");
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
            var extension = new ExtensionFilter("Character File", "character");
            string[] paths = StandaloneFileBrowser.OpenFilePanel("加载角色", "", new[] { extension }, false);

            if (paths.Length > 0 && File.Exists(paths[0]))
            {
                var newCharacterData = ES3.Load<CharacterData>(paths[0]);
                characterDatas.Add(newCharacterData);
                SetCurCharacter(newCharacterData);
            }
        }
        #endregion
        
        #region  立绘相关

        private void LoadLive2d()
        {
            Debug.Log("LoadLive2d");
            StartCoroutine(LoadLive2dIE());
        }
        IEnumerator LoadLive2dIE()
        {
            loadingPanel.SetActive(true);
            yield return null;
            live2DModelLoader.LoadModelFromFile(curCharacter, curCharacter.live2dPath,OnLive2dLoadSuccess);
        }
        /// <summary>
        /// 模型成功加载后
        /// </summary>
        /// <param name="model"></param>
        void OnLive2dLoadSuccess(Live2DController model)
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
            curModel.characterData = curCharacter;
            curModel.autoLookAtCenter.localPosition = curCharacter.lookCenter;
            if (live2DModelLoader.modelData == curCharacter)
            {
                curModel.SetLayer(101);
                curModel.SetBreath(curCharacter.isBreath);
                curModel.SetLookMouse(curCharacter.isLookAt);
                curModel.SetBlink(curCharacter.isBlink);
                curModel.SetColor(curCharacter.backgroundLight);
                SetModelToUI();
                FitModelToUI();
                loadButton.gameObject.SetActive(false);
            }
            else
            {
                curModel.gameObject.SetActive(false);
            }
            UpdateModelShow();
            SetParametersValue();
            // SetMotionValue();
            SetExpValue();
        }
        /// <summary>
        /// 设置角色的live2d
        /// </summary>
        public void SetLive2d()
        {
            StartCoroutine(GetPath());
        }
        IEnumerator GetPath()
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
                OnLive2dLoadSuccess(model);
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
            curCharacter.modelParameters.Clear();
            curCharacter.modelExps.Clear();
            curCharacter.modelMotions.Clear();
            curCharacter.activeModelExps.Clear();
            parameterPageItems.Clear();
            expPageItems.Clear();
            live2DModelLoader.RemoveCurModel();
            SetData();
            UpdateModelShow();
        }
        private void FitModelToUI()
        {
            // 6. 设置模型位置和缩放
            curModel.transform.localPosition = curCharacter.uiPos;
            curModel.transform.localScale = curCharacter.uiScale;
            Debug.Log(curCharacter.characterTitle);
            Debug.Log(curCharacter.uiScale);
            Debug.Log(curCharacter.uiPos);
                
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
            var bounds = CalculateCubismModelBounds(curModel.modelData);
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

            // 6. 设置模型位置和缩放
            curModel.transform.localPosition = localCenter;
            curModel.transform.localScale = Vector3.one * scale;
            
            curCharacter.uiPos = curModel.transform.localPosition;
            curCharacter.uiScale = curModel.transform.localScale;
        }
        public void FitBoxColliderToModel()
        {
            if (!curModel) return;

            // 获取包围盒（在模型的本地空间中）
            var bounds = CalculateCubismModelBounds(curModel.modelData);
            Vector3 localCenter = curModel.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = curModel.transform.InverseTransformVector(bounds.size);
            // 获取 BoxCollider2D，如果没有则添加
            var boxCollider2D = curModel.boxCollider2D;
            
            // 设置碰撞体尺寸和偏移
            boxCollider2D.size = localSize;
            boxCollider2D.offset = localCenter;
        }
        /// <summary>
        /// 计算整个 Live2D 模型的包围盒（模型本地坐标空间）
        /// </summary>
        private Bounds CalculateCubismModelBounds(CubismModel model)
        {
            var drawables = model.Drawables;
            if (drawables == null || drawables.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            Bounds combinedBounds = drawables[0].GetComponent<MeshRenderer>().bounds;

            for (int i = 1; i < drawables.Length; i++)
            {
                combinedBounds.Encapsulate(drawables[i].GetComponent<MeshRenderer>().bounds);
            }

            return combinedBounds;
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
                detailBtn.gameObject.SetActive(false);
                removeBtn.gameObject.SetActive(false);
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
                        detailBtn.gameObject.SetActive(true);
                        removeBtn.gameObject.SetActive(true);
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
                            loadButton.onPointerClick.AddListener(LoadLive2d);
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
                        detailBtn.gameObject.SetActive(false);
                        removeBtn.gameObject.SetActive(false);
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
                    loadButton.onPointerClick.AddListener(LoadLive2d);
                    detailBtn.gameObject.SetActive(false);
                    removeBtn.gameObject.SetActive(false);
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
                    detailBtn.gameObject.SetActive(false);
                    removeBtn.gameObject.SetActive(false);
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
                        detailBtn.gameObject.SetActive(true);
                        removeBtn.gameObject.SetActive(true);
                    }
                    else if(curCharacter.live2dPath is { Length: > 0 })
                    {
                        LoadLive2d();
                        detailBtn.gameObject.SetActive(true);
                        removeBtn.gameObject.SetActive(true);
                    }
                    else if(curModel != null)
                    {
                        Destroy(curModel.gameObject);
                        curModel=null;
                        detailBtn.gameObject.SetActive(false);
                        removeBtn.gameObject.SetActive(false);
                    }
                }
                else if(curModel != null)
                {
                    Destroy(curModel.gameObject);
                    curModel=null;
                    detailBtn.gameObject.SetActive(true);
                    removeBtn.gameObject.SetActive(true);
                }
            }
        }
        /// <summary>
        /// 播放指定动画
        /// </summary>
        /// <param name="target"></param>
        public void PlayMotion(AnimationClip target)
        {
            if (curModel.characterData == curCharacter)
            {
                curModel.PlayMotion(target);
            }
        }
        
        /// <summary>
        /// 播放指定动画组
        /// </summary>
        /// <param name="targets"></param>
        private void PlayMotions(List<AnimationClip> targets)
        {
            if (curModel !=null && curModel.characterData == curCharacter)
            {
                curModel.PlayMotions(targets);
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
                curModel.mouthController.FakeTalk(3);
            }
        }
        #endregion
        
        #region 动画/表情相关

        /// <summary>
        /// 设置动画列表
        /// </summary>
        private void SetMotionValue()
        {
            var motions = live2DModelLoader.clips.Values.ToArray();

            for (var i = 0; i < motions.Length; i++)
            {
                if (i < motionLines.Count)
                {
                    motionLines[i].SetData(motions[i]);
                    motionLines[i].gameObject.SetActive(true);
                }
                else
                {
                    var newLine = Instantiate(motionLinePeb, motionLineParent);
                    newLine.SetData(motions[i]);
                    motionLines.Add(newLine);
                }

                foreach (var modelMotion in curCharacter.modelMotions)
                {
                    if (modelMotion.motionName == motions[i].name)
                    {
                        motionLines[i].SetData(modelMotion);
                    }
                }
            }

            for (int i = motions.Length; i < motionLines.Count; i++)
            {
                motionLines[i].gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 保存动作
        /// </summary>
        private void SaveMotion()
        {
            curCharacter.modelMotions.Clear();
            foreach (var motion in motionLines)
            {
                curCharacter.modelMotions.Add(motion.GetMotionData());
            }
        }
        /// <summary>
        /// 设置表情列表
        /// </summary>
        private void SetExpValue()
        {
            // 模型的表情数据
            var exps = live2DModelLoader.expressions;
            // curCharacter.modelExps.Clear();
            // 用户存储的表情数据
            var savedExpDict = curCharacter.modelExps.ToDictionary(e => e.expName);

            // 最终页面展示的表情数据
            expPageItems = new List<ModelExp>();

            // 记录已添加的 key，避免重复添加
            HashSet<string> addedKeys = new();

            // 遍历模型提供的表情数据，优先使用用户存储数据
            foreach (var exp in exps)
            {
                if (savedExpDict.TryGetValue(exp.Key, out var savedExp))
                {
                    if (savedExp.exp3Json.Parameters == null)
                    {
                        savedExp.exp3Json = exp.Value;
                    }

                    savedExp.type = 0;
                    expPageItems.Add(savedExp);
                }
                else
                {
                    var newItem = new ModelExp(exp.Value, exp.Key, exp.Key, true)
                    {
                        type = 0
                    };
                    expPageItems.Add(newItem);
                }
                addedKeys.Add(exp.Key);
            }

            // 添加 savedExpDict 中独有的表情（模型中没有）
            foreach (var kv in savedExpDict)
            {
                if (!addedKeys.Contains(kv.Key))
                {
                    Debug.Log(kv.Value.expName);
                    kv.Value.type = 1;
                    expPageItems.Add(kv.Value);
                }
            }
            // expScrollList.SetData(expPageItems);

            // 根据是否显示禁用表情进行过滤
            if (isShowDisableExpToggle.isOn)
            {
                expPageList.SetData(expPageItems);
            }
            else
            {
                var list = expPageItems.Where(expPageItem => expPageItem.expOn).ToList();
                expPageList.SetData(list);
            }
        }

        public void RemoveCusExp(ModelExp target)
        {
            if (expPageItems.Contains(target))
            {
                expPageItems.Remove(target);
                expPageList.SetData(expPageItems);
                SetData();
            }
        }

        /// <summary>
        /// 保存表情
        /// </summary>
        private void SaveExp()
        {
            curCharacter.modelExps.Clear();
            foreach (var exp in expPageItems)
            {
                curCharacter.modelExps.Add(exp);
            }
        }
        private void IsShowDisableExps(bool isShow)
        {
            if (isShow)
            {
                expPageList.SetData(expPageItems);
            }
            else
            {
                var list = expPageItems.Where(expPageItem => expPageItem.expOn).ToList();
                expPageList.SetData(list);
            }
        }
        public void ShowCustomExpPanel()
        {
            _curCustomExp = new ModelExp();
            TakeASnapshot();
            customExpPanel.Show();
            parameterPageList.Refresh();
            customExpNameInput.text = "";
            customExpNickNameInput.text = "";
            customExpFadeInInput.text = "0.5";
            customExpFadeOutInput.text = "0.5";
            customExpParameterPageList.Clear();
            _customExpChanged = false;
            
            curModel.SetBlink(false);
            curModel.SetBreath(false);
            curModel.SetLookMouse(false);
        }
        public void ShowCustomExpPanel(ExpLine expLine)
        {
            _curCustomExp = expLine.GetModeExp();
            TakeASnapshot();
            customExpPanel.Show();
            parameterPageList.Refresh();
            _customExpChanged = false;
            customExpNameInput.text = _curCustomExp.expName;
            customExpNickNameInput.text = _curCustomExp.expNickname;;
            customExpFadeInInput.text = _curCustomExp.exp3Json.FadeInTime.ToString(CultureInfo.InvariantCulture);
            customExpFadeOutInput.text = _curCustomExp.exp3Json.FadeOutTime.ToString(CultureInfo.InvariantCulture);
            // customExpParameterPageList.SetData();
            _curCustomExp.tempParameters.Clear();
            foreach (var jsonParameter in _curCustomExp.exp3Json.Parameters)
            {
                foreach (var parameterPageItem in parameterPageItems)
                {
                    if (parameterPageItem.parameterId == jsonParameter.Id)
                    {
                        _curCustomExp.tempParameters.Add(new ModelExp.TmpExpParameter()
                        {
                            parameterId = jsonParameter.Id,
                            parameterDisplayName = parameterPageItem.displayName,
                            value = jsonParameter.Value,
                        });
                        break;
                    }
                }


            }
            customExpParameterPageList.SetData(_curCustomExp.tempParameters);
            _customExpChanged = false;
            curModel.SetBlink(false);
            curModel.SetBreath(false);
            curModel.SetLookMouse(false);
        }

        public void AddCustomExpParameter(ModelParameter target)
        {
            _customExpChanged = true;
            _curCustomExp.AddTmpExpParameter(target);
            customExpParameterPageList.SetData(_curCustomExp.tempParameters);
        }
        public void RemoveCustomExpParameter(ModelExp.TmpExpParameter target)
        {
            _customExpChanged = true;
            _curCustomExp.RemoveTmpExpParameter(target);
            customExpParameterPageList.SetData(_curCustomExp.tempParameters);
        }
        public void HideCustomExpPanel()
        {
            if (_customExpChanged)
            {
                switch (LocalizerManager.GetCode())
                {
                    case "zh-Hans":
                        MessageManager.instance.ShowPropUpMessage("保存","是否需要保存当前的自定义表情？", SaveAndHideCustomExp, () =>
                        {
                            _curCustomExp = null;
                            customExpPanel.Hide();
                            UseSnapshot();
                            parameterPageList.Refresh();
                            curModel.SetBlink(curCharacter.isBlink);
                            curModel.SetBreath(curCharacter.isBreath);
                            curModel.SetLookMouse(curCharacter.isLookAt);
                            curModel.ClearAllExpressions();
                        });
                        break;
                    case "en":
                        MessageManager.instance.ShowPropUpMessage("Save?","Do you want to save the current custom expression?", SaveAndHideCustomExp, () =>
                        {
                            _curCustomExp = null;
                            customExpPanel.Hide();
                            UseSnapshot();
                            parameterPageList.Refresh();
                            curModel.SetBlink(curCharacter.isBlink);
                            curModel.SetBreath(curCharacter.isBreath);
                            curModel.SetLookMouse(curCharacter.isLookAt);
                            curModel.ClearAllExpressions();
                        });
                        break;
                }
            }
            else
            {
                customExpPanel.Hide();
                UseSnapshot();
                parameterPageList.Refresh();
                curModel.SetBlink(curCharacter.isBlink);
                curModel.SetBreath(curCharacter.isBreath);
                curModel.SetLookMouse(curCharacter.isLookAt);
                curModel.ClearAllExpressions();
            }
        }

        public void SaveAndHideCustomExp()
        {
            if (customExpNameInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 表情名称");
                return;
            }
            if (customExpNickNameInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 表情识别名称");
                return;
            }
            if (customExpFadeInInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 淡入时长");
                return;
            }
            if (customExpFadeOutInput.text.Length <= 0)
            {
                MessageManager.instance.ShowMessage("请填写 淡出时长");
                return;
            }
            if (_curCustomExp.tempParameters.Count <= 0)
            {
                MessageManager.instance.ShowMessage("请至少添加一个表情参数");
                return;
            }

            _customExpChanged = false;
            _curCustomExp.expName = customExpNameInput.text;
            _curCustomExp.expNickname = customExpNickNameInput.text;
            _curCustomExp.exp3Json.FadeInTime = float.Parse(customExpFadeInInput.text);
            _curCustomExp.exp3Json.FadeOutTime = float.Parse(customExpFadeOutInput.text);
            _curCustomExp.exp3Json.Parameters = new CubismExp3Json.SerializableExpressionParameter[_curCustomExp.tempParameters.Count];
            for (var i = 0; i < _curCustomExp.tempParameters.Count; i++)
            {
                var tmp = new CubismExp3Json.SerializableExpressionParameter
                {
                    Id = _curCustomExp.tempParameters[i].parameterId,
                    Value = _curCustomExp.tempParameters[i].value,
                    Blend = nameof(CubismParameterBlendMode.Override)
                };
                _curCustomExp.exp3Json.Parameters[i] = tmp;
            }

            var flag = false;
            for (var i = 0; i < expPageItems.Count; i++)
            {
                if (expPageItems[i].expName == _curCustomExp.expName)
                {
                    expPageItems[i] = _curCustomExp;
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                expPageItems.Add(_curCustomExp);
            }
            customExpPanel.Hide();
            _isChangeData = true;
            saveButton.gameObject.SetActive(true);
            UseSnapshot();
            parameterPageList.Refresh();
            expPageList.SetData(expPageItems);
            curModel.SetBlink(curCharacter.isBlink);
            curModel.SetBreath(curCharacter.isBreath);
            curModel.SetLookMouse(curCharacter.isLookAt);
        }
        #endregion
        
        #region 参数相关
        /// <summary>
        /// 为所有参数创建一个快照
        /// </summary>
        private void TakeASnapshot()
        {
            _paramsSnapshot.Clear();
            foreach (var parameterPageItem in parameterPageItems)
            {
                _paramsSnapshot.Add(parameterPageItem,parameterPageItem.parameterValue);
            }
        }
        /// <summary>
        /// 使用上一个快照
        /// </summary>
        private void UseSnapshot()
        {
            _needUseSnapshot = true;
        }
        /// <summary>
        /// 搜索参数
        /// </summary>
        public void Search()
        {
            var keyword = parameterSearchInput.text?.ToLower();
    
            if (string.IsNullOrEmpty(keyword))
            {
                // 默认排序：按 parameterId 升序
                parameterPageList.SetData(parameterPageItems);
                return;
            }

            var results = parameterPageItems
                .Select(p => new
                {
                    Item = p,
                    Score = GetMatchScore(p, keyword)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Item)
                .ToList();

            parameterPageList.SetData(results);
        }
        private int GetMatchScore(ModelParameter p, string keyword)
        {
            int score = 0;

            // Exact matches: +100
            if (string.Equals(p.parameterId, keyword, StringComparison.OrdinalIgnoreCase)) score += 100;
            else if (!string.IsNullOrEmpty(p.parameterId) && p.parameterId.ToLower().StartsWith(keyword)) score += 50;
            else if (!string.IsNullOrEmpty(p.parameterId) && p.parameterId.ToLower().Contains(keyword)) score += 20;

            if (string.Equals(p.parameterName, keyword, StringComparison.OrdinalIgnoreCase)) score += 80;
            else if (!string.IsNullOrEmpty(p.parameterName) && p.parameterName.ToLower().StartsWith(keyword)) score += 40;
            else if (!string.IsNullOrEmpty(p.parameterName) && p.parameterName.ToLower().Contains(keyword)) score += 15;

            if (string.Equals(p.displayName, keyword, StringComparison.OrdinalIgnoreCase)) score += 60;
            else if (!string.IsNullOrEmpty(p.displayName) && p.displayName.ToLower().StartsWith(keyword)) score += 30;
            else if (!string.IsNullOrEmpty(p.displayName) && p.displayName.ToLower().Contains(keyword)) score += 10;

            return score;
        }
        /// <summary>
        /// 显示和设置参数
        /// </summary>
        private void SetParametersValue()
        {
            parameterSearchInput.text = "";
            var parameters = curModel.modelData.Parameters;
            var savedParamDict = curModel.characterData.modelParameters
                .ToDictionary(p => p.parameterId);

            parameterPageItems = new List<ModelParameter>();
            foreach (var parameter in parameters)
            {
                if (savedParamDict.TryGetValue(parameter.Id, out var savedParam))
                {
                    savedParam.SetParameter(parameter);
                    parameterPageItems.Add(savedParam);
                }
                else
                {
                    parameterPageItems.Add(new ModelParameter(parameter));
                }
            }
            //排序
            // parameterPageItems = parameterPageItems
            //     .OrderBy(p => p.parameterId, NaturalComparer)
            //     .ToList();

            parameterPageList.SetData(parameterPageItems);
        }

        /// <summary>
        /// 所有参数重置
        /// </summary>
        public void SetParametersValuesDef()
        {
            foreach (var parameterItem in parameterPageList.GetData())
            {
                if (parameterItem is ModelParameter itemData)
                {
                    _needSetDef.Add(itemData);
                }
            }
        }
        public void SaveParameters()
        {
            Debug.Log("SaveParameters");
            curCharacter.modelParameters = new List<ModelParameter>();
            foreach (var parameterItem in  parameterPageItems)
            {
                curCharacter.modelParameters.Add(parameterItem);
            }
            SetData();
        }

        public void GotoTargetParam(ModelExp.TmpExpParameter target)
        {
            foreach (var parameterPageItem in parameterPageItems)
            {
                if (parameterPageItem.parameterId == target.parameterId)
                {
                    parameterPageList.GotoItem(parameterPageItem);
                    break;
                }
            }
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