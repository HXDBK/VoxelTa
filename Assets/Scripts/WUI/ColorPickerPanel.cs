using System;
using UnityEngine;
using UnityEngine.UI;

namespace WUI
{
    public class ColorPickerPanel : UIPanel
    {
        public static ColorPickerPanel instance;
        public ColorPicker colorPicker;
        public Image previewImage;
        public Image baseImage;

        private Action _onHide;
        private void Awake()
        {
            instance = this;
        }

        protected override void Start()
        {
            base.Start();
            Hide();
        }

        public void SetColor(Color baseColor, Action<Color> colorAction,Vector3 pos,Action onHide = null)
        {
            transform.position = pos;
            Show();
            baseImage.color = baseColor;
            previewImage.color = baseColor;
            colorPicker.color = baseColor;
            colorPicker.onColorChanged = null;
            colorPicker.onColorChanged += SetPreviewImage;
            colorPicker.onColorChanged += colorAction;
            _onHide = onHide;
        }

        private void SetPreviewImage(Color color)
        {
            previewImage.color = color;

        }

        public override void Hide()
        {
            base.Hide();
            _onHide?.Invoke();
            colorPicker.onColorChanged = null;
        }
    }
}
