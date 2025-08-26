using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TransparentWindow : MonoBehaviour
{

    [DllImport("user32.dll")]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    const uint SWP_NOMOVE        = 0x0002;
    const uint SWP_NOSIZE        = 0x0001;
    const uint SWP_NOACTIVATE    = 0x0010;
    const uint SWP_NOOWNERZORDER = 0x0200;
    const uint SWP_NOSENDCHANGING= 0x0400;
    
    private uint originalStyle;
    private struct MARGINS {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [DllImport("Dwmapi.dll")]
    private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    const int GWL_EXSTYLE = -20;

    const uint WS_EX_LAYERED = 0x00080000;
    const uint WS_EX_TRANSPARENT = 0x00000020;

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    const uint LWA_COLORKEY = 0x00000001;

    private IntPtr hWnd;

    [Header("拖个包含 GraphicRaycaster 的 Canvas 进来")]
    public List<Canvas> uiCanvases = new(); // 原本是 Canvas uiCanvas;

    private readonly List<GraphicRaycaster> _raycasters = new();
    private PointerEventData _pointerEventData;
    private readonly List<RaycastResult> _results = new();
    
    public bool allowClickThrough = false;
    private void Start() {
        //MessageBox(new IntPtr(0), "Hello World!", "Hello Dialog", 0);
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0, 0, 0, 0); // 透明背景
#if !UNITY_EDITOR
        hWnd = GetActiveWindow();
        originalStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);

        if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
        {
            EnableTransparentMode();
        }
#endif

        Application.runInBackground = true;
        // 初始化手动 UI 射线检测
        foreach (var uiCanvas in uiCanvases)
        {
            var raycaster = uiCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster)
            {
                _raycasters.Add(raycaster);
            }
        }
        _pointerEventData = new PointerEventData(EventSystem.current);
    }

    private void Update() {
        
        _pointerEventData.position = Input.mousePosition;
        _results.Clear();
        foreach (var raycaster in _raycasters)
        {
            raycaster.Raycast(_pointerEventData, _results);
            bool pointerOnUI = _results.Count > 0;
            // GameManager.Instance.testLog.text = $"{_results.Count}";
            // 判断鼠标是否指向UI
            // bool pointerOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            // 鼠标位置转世界位置
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = 0;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

            // 判断2D物体是否被点击
            bool pointerOn2DObject = Physics2D.OverlapPoint(worldPos) != null;

            // 只在鼠标没有点中UI和2D物体时才开启穿透
            if (allowClickThrough)
            {
                SetClickThrough(!(pointerOnUI || pointerOn2DObject));
            }
        }
        
    }
    /// <summary>
    /// 设置鼠标点击穿透
    /// </summary>
    /// <param name="clickThrough"></param>
    private void SetClickThrough(bool clickThrough) {
        if (clickThrough) {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        } else {
            SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED);
        }
    }
    IEnumerator DelayedSetTopmost()
    {
        yield return new WaitForSeconds(0.5f);
        SetWindowTopmost();
    }
    private void SetWindowTopmost()
    {
        SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE |
            SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
    }
    public void EnableTransparentMode()
    {
        allowClickThrough = true;
#if !UNITY_EDITOR
    MARGINS margins = new MARGINS { cxLeftWidth = -1 };
    DwmExtendFrameIntoClientArea(hWnd, ref margins);

    SetWindowLong(hWnd, GWL_EXSTYLE, WS_EX_LAYERED | WS_EX_TRANSPARENT);
    // SetLayeredWindowAttributes(hWnd, 0, 0, LWA_COLORKEY);

    SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
         SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE |
         SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
    StartCoroutine(DelayedSetTopmost());
#endif
    }

    public void DisableTransparentMode()
    {
        allowClickThrough = false;
#if !UNITY_EDITOR
    SetWindowLong(hWnd, GWL_EXSTYLE, originalStyle);
    // SetLayeredWindowAttributes(hWnd, 0, 255, LWA_COLORKEY);
    SetWindowPos(hWnd, new IntPtr(0), 0, 0, 0, 0,
         SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE |
         SWP_NOOWNERZORDER | SWP_NOSENDCHANGING);
#endif
    }
}
