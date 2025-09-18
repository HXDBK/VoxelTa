using System;
using System.Collections;
using System.IO;
using Live2D;
using Live2D.Cubism.Framework.Json;
using UnityEngine;

namespace Model
{
    public class CustomModelLoader : ModelLoader
    {
        private CustomModel _model;

        public void LoadModelFromFile(CharacterData target, string modelPath, Action<CustomModel> onComplete = null)
        {
            Debug.Log("loading model from file");
            StartCoroutine(LoadModelFromFileIE(target, modelPath, onComplete));
        }

        private IEnumerator LoadModelFromFileIE(CharacterData target,string modelPath, Action<CustomModel> onComplete = null)
        {
            if (_model != null)
            {
                Destroy(_model.gameObject);
            }

            if (!File.Exists(modelPath))
            {
                Debug.LogError($"模型文件不存在: {modelPath}");
                MessageManager.instance.ShowMessage($"Model file not found: {modelPath}", MessageType.Warning);
                onComplete?.Invoke(null);
                yield break;
            }
            var modelData = ES3.Load<CustomModelData>("CustomModelData", modelPath);
            var go = new GameObject(target.characterTitle);

            _model = go.AddComponent<CustomModel>();
            _model.SetModelData(modelData);
            
            go.tag = "Character";

            onComplete?.Invoke(_model);
            yield break;
        }
    }
}