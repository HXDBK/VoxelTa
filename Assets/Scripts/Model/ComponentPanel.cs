using System.Collections.Generic;
using System.Globalization;
using SFB;
using TMPro;
using UnityEngine;
using WUI;

namespace Model
{
    public class ComponentPanel : UIPanel
    {
        private CharacterData _character;
        private CustomModel _model;
        private List<ModelComponent> _components = new ();
        public WPageList componentsList;
        private ModelComponent _curComponent;
        
        public TMP_InputField componentNameInput;
        public TMP_InputField componentSourcePathInput;
        public TMP_InputField componentPositionXInput;
        public TMP_InputField componentPositionYInput;
        public TMP_InputField componentRotationInput;
        public TMP_InputField componentScaleXInput;
        public TMP_InputField componentScaleYInput;
        public TMP_InputField componentParentNameInput;
        public WPageList subComponentsList;

        protected override void Start()
        {
            base.Start();
            componentNameInput.onValueChanged.AddListener((value)=>{SetComponentData();});
            componentSourcePathInput.onValueChanged.AddListener((value)=>{SetComponentData();});
            componentPositionXInput.onValueChanged.AddListener((value)=>{SetComponentData();});
            componentPositionYInput.onValueChanged.AddListener((value)=>{SetComponentData();});
            componentRotationInput.onValueChanged.AddListener((value)=>{SetComponentData();});
            componentScaleXInput.onValueChanged.AddListener((value)=>{SetComponentData();});
            componentScaleYInput.onValueChanged.AddListener((value)=>{SetComponentData();});
        }

        public void Show(CharacterData data,GeneralModel model)
        {
            _character = data;
            if (model == null)
            {
                _model = new GameObject().AddComponent<CustomModel>();
                _model.SetModelData(new CustomModelData());
            }
            else
            {
                _model = model as CustomModel;
            }
            Show();
        }

        public override void Show()
        {
            base.Show();
            LoadComponents();
        }

        public void AddModelComponent()
        {
            var spriteRenderer = new GameObject($"component_{_components.Count}").AddComponent<SpriteRenderer>();
            var newComponent = _model.AddModelComponent(spriteRenderer);
            spriteRenderer.transform.SetParent(_model.transform);
            componentsList.SetData(_components);
            ShowComponentDetail(newComponent);
            Debug.Log(_model.modelComponents.Count);
            Debug.Log(_model.customModelData.components.Count);
        }
        private void LoadComponents()
        {
            _components = _model.modelComponents;
            componentsList.SetData(_components);
        }
        
        public void ShowComponentDetail(ModelComponent component)
        {
            componentsList.GotoItem(component);
            _curComponent = component;
            componentNameInput.text = _curComponent.componentName;
            componentSourcePathInput.text = _curComponent.sourcePath;
            componentPositionXInput.text = _curComponent.position.x.ToString("F2");
            componentPositionYInput.text = _curComponent.position.y.ToString("F2");
            componentRotationInput.text = _curComponent.rotation.z.ToString("F2");
            componentScaleXInput.text = _curComponent.scale.x.ToString("F2");
            componentScaleYInput.text = _curComponent.scale.y.ToString("F2");
        }

        public void SetComponentSourcePath()
        {
            var extension = new ExtensionFilter("Image Files", "png", "jpg", "jpeg");
            string[] paths = StandaloneFileBrowser.OpenFilePanel("选择图片","", new []{extension}, false);

            if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
            {
            }
            else
            {
                componentSourcePathInput.text = paths[0];
            }
        }
        private void SetComponentData()
        {
            if(_curComponent == null) return;

            var flag = false;
            _curComponent.componentName = componentNameInput.text;
            if (_curComponent.sourcePath != componentSourcePathInput.text)
            {
                flag = true;
                _curComponent.sourcePath = componentSourcePathInput.text;
            }
            _curComponent.position = new Vector3(float.Parse(componentPositionXInput.text), float.Parse(componentPositionYInput.text), 0);
            _curComponent.rotation = new Vector3(0, 0, float.Parse(componentRotationInput.text));
            _curComponent.scale = new Vector3(float.Parse(componentScaleXInput.text), float.Parse(componentScaleYInput.text), 1);

            var actor = _curComponent.actor;
            if (actor != null)
            {
                actor.name = _curComponent.componentName;
                if (flag)
                {
                    GameManager.instance.LoadSprite(_curComponent.sourcePath, actor);
                }
                actor.transform.localPosition = _curComponent.position;
                actor.transform.localEulerAngles = _curComponent.rotation;
                actor.transform.localScale = _curComponent.scale;
            }
        }

        public void SaveData()
        {
            Debug.Log(_character.live2dPath is { Length: > 0 });
            if (_character.live2dPath is { Length: > 0 })
            {
                ES3.Save("CustomModelData",_model.customModelData,_character.live2dPath);
            }
            else
            {
                var extension = new ExtensionFilter("Model File", "vtam");
                string path = StandaloneFileBrowser.SaveFilePanel("保存模型", "", _character.characterTitle,  new[] { extension });

                if (!string.IsNullOrEmpty(path))
                {
                    _character.live2dPath = path;
                    ES3.Save("CustomModelData",_model.customModelData,path);
                    Debug.Log("角色数据已保存到: " + path);
                }
                else
                {
                    Debug.Log("用户取消了保存操作。");
                }
            }
        }
    }
}
