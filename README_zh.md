# VoxelTa

中文版 | [English](README.md)

一个基于 Unity 开发的桌面虚拟聊天伴侣项目，让你可以在桌面上拥有一个 Live2d 角色，并与 Ta 进行互动聊天。

## 项目特色

- 可以导入任意 Live2d 虚拟角色
- 可自定义的角色表情，并根据对话更改
- 智能对话系统
- 桌面伴侣模式
- 支持本地文字转语音

## 如何使用

### 普通用户 - 直接使用软件
如果你只是想体验这个桌面伴侣，无需安装 Unity 和各种依赖：

**[点击此处下载最新版本](https://github.com/HXDBK/VoxelTa/releases)**

下载后解压即可直接运行！

---

### 开发者 - 源码开发
如果你想修改项目或参与开发，请按照下面的完整步骤：

## 环境要求

- **Unity 版本**：2022.3.x LTS
- **支持平台**：
  - Windows（完整功能，包括桌面模式）
  
## 依赖插件

本项目依赖以下第三方插件，需要你手动下载并导入：

### 必需插件

| 插件名称 | 类型 | 获取方式 |
|---------|------|----------|
| [DOTween](https://dotween.demigiant.com/) | 免费 | Unity Asset Store |
| [Easy Save 3](https://assetstore.unity.com/packages/tools/utilities/easy-save-the-complete-save-data-serialization-asset-768) | 付费 | Unity Asset Store |

> **重要提醒**：这些插件不会包含在仓库中，请在克隆项目后自行下载并导入。

## 快速开始

### 1. 获取项目代码
```bash
git clone https://github.com/HXDBK/VoxelTa.git
cd VoxelTa
```

### 2. 打开项目
使用 Unity 2022.3.x LTS 打开项目文件夹

### 3. 导入依赖插件

#### DOTween 设置
1. 从 Unity Asset Store 下载并导入 DOTween
2. 在 Unity 菜单中执行以下步骤：
   ```
   Tools → Demigiant → DOTween Utility Panel → Setup DOTween
   ```
3. 按照面板提示完成设置

#### Easy Save 3 设置
1. 从 Unity Asset Store 购买并导入 Easy Save 3
2. 等待 Unity 自动完成编译

### 4. 运行项目
1. 等待所有依赖编译完成
2. 进入 Scene Main 场景
3. 点击 Unity 编辑器中的 **Play** 按钮

## 未来功能

- [ ] 第三方在线语音转文字支持
- [ ] 语音转文字支持
- [ ] 自定义动画支持
- [ ] 当前桌面识别支持

## 开源协议

- **项目代码**：[MIT License](LICENSE)
- **第三方插件**：请遵循各自的许可证要求

## 第三方声明

本项目使用了多个第三方库和插件，详细的许可信息请查看 [ThirdPartyNotices.md](ThirdPartyNotices.md)。