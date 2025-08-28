<p align="center">
  <img src="images/pic7.png" width="700">
</p>

<h1 align="center">VoxelTa</h1>

<div align="center">

[中文版](README_CN.md) | English

A Unity-based desktop virtual chat companion project. Create the 'Ta' of your dreams.  
Ta — means he, she, it, or they.

</div>

## ⭐ Project Features

- Import any Live2D virtual character
- Customizable character expressions that change based on conversation
- Intelligent dialogue system
- Desktop companion mode
- Local text-to-speech support
- Currently only supports Windows

## How to Use

### Regular Users - Use the Software Directly
If you just want to experience this desktop companion without installing Unity and various dependencies:

**[Click here to download the latest version](https://github.com/HXDBK/VoxelTa/releases)**

Simply extract and run after downloading!

## 🚀 Quick Start

### 1. Run VoxelTa

After downloading, please extract the folder first, then double-click **VoxelTa.exe** in the extracted folder to run the software.

### 2. Create Your First Character

[<img src="images/pic8.png" width="500">](images/pic8.png)

If this is your first time entering VoxelTa, this interface will pop up. You can select your language and click the **Add Character** button to create your first character.

[<img src="images/pic9.png" width="500">](images/pic9.png)

The character interface is divided into 3 main sections. The leftmost is the character dialogue selection section. In VoxelTa, you can create multiple characters. Through the left list, you can **create new characters**, **export characters**, **import characters**, **copy characters**, and **delete characters**. After selecting a character from the left character list, you can start configuring this character (items marked with * are required).

#### Character Settings
**Dialogue Title**: Displayed in the left character list.  
**Character Avatar**: Avatar shown in the dialogue list.  
**Character Name**: Character name displayed in the dialogue list.  
**User Name**: User name displayed in the dialogue list.  
**Character Setting***: [Important] This character's settings, including basic information like name, age, gender, background settings, reply rules, etc. The data filled here determines who your character is.  
**Memory**: The character's long-term memory (there's no functional difference between filling this here and directly in **Character Settings**, it's mainly for easier management).  

#### Portrait Settings
If you haven't set a Live2D model for the current character, click this area to select a Live2D model for your character. You need to select the file ending with .model3.json in the Live2D files. If there are no issues, your Live2D model will be loaded.

[<img src="images/pic10.png" width="500">](images/pic10.png)

Click the save button in the upper right corner and close the character settings page.  
After the Live2D model is set up, new buttons will appear in the upper right corner of the portrait area. Subsequently, clicking the **gear** button allows further configuration of Live2D. For details, see [Portrait Settings and Custom Expressions](#portrait-settings-and-custom-expressions).

### 3. Set Up Large Language Model
Click the **gear** icon in the upper right corner of the screen to open the dialogue settings interface.

[<img src="images/pic11.png" width="500">](images/pic11.png)

To chat with your companion, you need to set up a large language model for the character. You can configure the model to use in the left column of the interface:  
**Model Selection**: VoxelTa provides some default model configurations (DeepSeek, ChatGPT, Gemini). You can also choose custom to configure other third-party models.  
**API URL**: Only needed when selecting custom, the address for API calls.  
**Model Name**: The same large model may have different versions, for example, DeepSeek has deepseek-chat and deepseek-reasoner models. For specific differences between models, check the model's official website.  
**Rule Name**: Only needed when selecting custom, the name used when sending system rules.  
**API Key**: (Important) This input field requires your obtained API key for the model.  
**Maximum Characters**: Limits the maximum context character count. Dialogues exceeding this character count will be truncated.  

After configuration, please click the save button in the upper right corner and close the dialogue settings page.  
For other settings interfaces if needed, please refer to:  
[Set Background Image](#set-background-image)  
[Use Text-to-Speech](#use-text-to-speech)

### 4. Dialogue

[<img src="images/pic12.png" width="500">](images/pic12.png)

After returning to the dialogue interface, you can try sending your first message.  
There are three modes below the dialogue interface:  
**Dialogue Only**: Only displays dialogue text.  
**Character Dialogue**: Displays the character with dialogue text shown on the left. Drag on the character to change position, scroll the wheel to change character size.  
**Desktop Mode**: Displayed as a floating window. Drag on the character to change position, scroll the wheel to change character size, right-click and hold on the character to bring up the menu.  

### Portrait Settings and Custom Expressions

[<img src="images/pic13.png" width="500">](images/pic13.png)

**Model Parameter Adjustment**: All parameters of your Live2D model will be displayed on the left side of the interface. You can directly adjust these parameters to modify the Live2D model's appearance.  
**Model Auto Behaviors**: You can enable or disable some automatic behaviors of the model, including breathing, blinking, and mouse tracking.  
**Model Expressions**: The far right side of the interface shows all expressions owned by this model (some models come with expressions).  
**Custom Expressions**: You can add your custom expressions through the **+** button in the upper right corner of the interface.  

[<img src="images/pic14.png" width="500">](images/pic14.png)

Custom expressions have the following settings:  
**Expression Recognition Identifier***: When this identifier appears in the character's reply, the character will make this expression until the next reply (supports regex).  
**Expression Name***: Name displayed in the expression list.  
**Fade In Duration**: Fade-in duration when playing this expression.  
**Fade Out Duration**: Fade-out duration when playing this expression.  
**Expression Parameter List***: After opening the custom expression interface, a **+** sign appears in the upper right corner of each parameter in the leftmost parameter list. After adjusting any parameter, click the **+** sign to add that parameter to the custom expression.  
After configuration, click **Save Expression** to save the custom expression.  

### Set Background Image
In the settings interface, you can set the dialogue background (background won't be displayed in desktop mode).  
**Select Image**: Choose a local image as the background.  
**Hold Button Below to Drag Background**: Hold this button and drag to set background position.  
**Hold Button Below to Scale Background**: Hold this button and drag to set background size.  
**Background Color**: Set the color of solid background.  
**Background Light**: Set the character's color to better blend the character with the background.  
**Character Name Color**: The character's name color in the dialogue interface.  

### Use Text-to-Speech
Currently, VoxelTa's text-to-speech only supports local API calls for [GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS).  
**Voice Module API**: Local API address for [GPT-SoVITS](https://github.com/RVC-Boss/GPT-SoVITS).  
**Reference Audio File Path**: File path for reference audio during generation. You can directly click the **folder** icon on the right to select.  
**Reference Audio Text**: Text reference for the reference audio.  
**Show Bubbles in Desktop Mode**: In desktop mode, if you only want to hear the voice, you can uncheck this option.  

---

### Developers - Source Code Development
If you want to modify the project or participate in development, please follow these complete steps:

## Environment Requirements

- **Unity Version**: 2022.3.x LTS
- **Supported Platforms**:
  - Windows (full functionality, including desktop mode)
  
## Required Plugins

This project depends on the following third-party plugins, which you need to manually download and import:

### Required Plugins

| Plugin Name | Type | How to Get |
|------------|------|------------|
| [DOTween](https://dotween.demigiant.com/) | Free | Unity Asset Store |
| [Easy Save 3](https://assetstore.unity.com/packages/tools/utilities/easy-save-the-complete-save-data-serialization-asset-768) | Paid | Unity Asset Store |

> **Important Notice**: These plugins are not included in the repository. Please download and import them yourself after cloning the project.

## Import Project

### 1. Get Project Code
```bash
git clone https://github.com/HXDBK/VoxelTa.git
cd VoxelTa
```

### 2. Open Project
Open the project folder using Unity 2022.3.x LTS

### 3. Import Required Plugins

#### DOTween Setup
1. Download and import DOTween from Unity Asset Store
2. Execute the following steps in Unity menu:
   ```
   Tools → Demigiant → DOTween Utility Panel → Setup DOTween
   ```
3. Follow the panel instructions to complete setup

#### Easy Save 3 Setup
1. Purchase and import Easy Save 3 from Unity Asset Store
2. Wait for Unity to complete compilation automatically

### 4. Run Project
1. Wait for all dependencies to finish compiling
2. Enter the Scene Main scene
3. Click the **Play** button in Unity Editor

## Future Features

- [ ] Third-party online speech-to-text support
- [ ] Speech-to-text support
- [ ] Custom animation support
- [ ] Current desktop recognition support

## License

- **Project Code**: [MIT License](LICENSE)
- **Third-Party Plugins**: Please follow their respective license requirements

## Third-Party Notice

This project uses multiple third-party libraries and plugins. For detailed license information, please see [ThirdPartyNotices.md](ThirdPartyNotices.md).