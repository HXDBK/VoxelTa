# VoxelTa

[中文版](README_zh.md) | English

A Unity-based desktop virtual chat companion project that allows you to have a Live2d character on your desktop and interact with them through chat.

## Features

- Import any Live2d virtual character
- Customizable character expressions that change based on conversations
- Intelligent dialogue system
- Desktop companion mode
- Local text-to-speech support

## How to Use

### Regular Users - Direct Software Use
If you just want to experience this desktop companion without installing Unity and dependencies:

**[Click here to download the latest version](https://github.com/HXDBK/VoxelTa/releases)**

Simply extract and run after download!

---

### Developers - Source Code Development
If you want to modify the project or participate in development, follow the complete steps below:

## System Requirements

- **Unity Version**: 2022.3.x LTS
- **Supported Platforms**:
  - Windows (Full functionality, including desktop mode)
  
## Dependencies

This project depends on the following third-party plugins that need to be manually downloaded and imported:

### Required Plugins

| Plugin Name | Type | Source |
|-------------|------|--------|
| [DOTween](https://dotween.demigiant.com/) | Free | Unity Asset Store |
| [Easy Save 3](https://assetstore.unity.com/packages/tools/utilities/easy-save-the-complete-save-data-serialization-asset-768) | Paid | Unity Asset Store |

> **Important Note**: These plugins are not included in the repository. Please download and import them after cloning the project.

## Quick Start

### 1. Get Project Code
```bash
git clone https://github.com/HXDBK/VoxelTa.git
cd VoxelTa
```

### 2. Open Project
Open the project folder using Unity 2022.3.x LTS

### 3. Import Dependencies

#### DOTween Setup
1. Download and import DOTween from Unity Asset Store
2. In Unity menu, execute the following steps:
   ```
   Tools → Demigiant → DOTween Utility Panel → Setup DOTween
   ```
3. Complete setup following panel instructions

#### Easy Save 3 Setup
1. Purchase and import Easy Save 3 from Unity Asset Store
2. Wait for Unity to complete compilation

### 4. Run Project
1. Wait for all dependencies to compile
2. Enter Scene Main scene
3. Click the **Play** button in Unity editor

## Future Features

- [ ] Third-party online speech-to-text support
- [ ] Voice-to-text support
- [ ] Custom animation support
- [ ] Current desktop recognition support

## License

- **Project Code**: [MIT License](LICENSE)
- **Third-party Plugins**: Please follow their respective license requirements

## Third Party Notices

This project uses multiple third-party libraries and plugins. For detailed licensing information, please see [ThirdPartyNotices.md](ThirdPartyNotices.md).