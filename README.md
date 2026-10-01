# 3D Virtual Tour Game

An interactive 3D Virtual Tour and Real Estate Walkthrough application built with **Unity 6** and the **Universal Render Pipeline (URP)**. Designed for responsive mobile (Android) experiences with runtime 3D model loading and Flutter integration support.

---

## 🌟 Key Features

- **Dual-View Camera System**
  - **First-Person Virtual Tour**: Smooth locomotion, touch drag looking, pitch/yaw limits, and collision avoidance.
  - **Top-Down Overview Mode**: Bird's-eye perspective with smooth camera transitions and pinch-to-zoom.
  - **Presets**: Configurable camera behavior presets via ScriptableObjects (`CameraAppSettingsPreset`).

- **Intuitive Mobile Touch Controls**
  - Responsive **Virtual Joystick** for navigation.
  - Left-hand vertical drag movement support.
  - Two-finger pinch zoom and touch look navigation.
  - **SafeArea Handling** and dynamic UI scaling across various screen aspect ratios and notches.
  - Guide tooltips and toggleable control guides.

- **Runtime 3D Model Loading**
  - Load custom **GLB** and **OBJ** models directly at runtime.
  - Native **Android System File Picker** integration with file validation.
  - Automatic model scaling, centering, and camera alignment.

- **Interactive Environment**
  - Interactive doors with smooth opening/closing animations.
  - Object selection manager (`SelectionManager`) and interactable object interface (`IInteractable`).
  - Runtime color customization on designated scene objects.

- **Flutter Integration**
  - Bidirectional communication layer (`SendToFlutter`, `UnityMessageManager`) to embed Unity seamlessly into Flutter applications.

- **Mobile Performance Optimizations**
  - Custom `MobileGraphicsOptimizer` to maintain high frame rates on mobile hardware.
  - URP optimized shaders and rendering pipeline settings.

---

## 📁 Project Structure

```text
Assets/
├── Animations/       # Door and interaction animations
├── Plugins/          # Android native helpers (Java file helper, AndroidManifest)
├── Scenes/           # Main scene: "Apartment Virtual Tour.unity"
├── Scripts/
│   ├── Camera/       # Camera controllers and settings presets
│   ├── Core/         # GameManager, model scaler, mobile graphics optimizer
│   ├── Editor/       # Custom editor tools and build helpers
│   ├── FlutterIntegration/ # Flutter messaging bridge
│   ├── Interaction/  # Object selection, interactables, and door controllers
│   ├── ModelLoading/ # Runtime GLB and OBJ loaders
│   └── UI/           # Virtual joystick, safe area, file picker, and HUD
├── Settings/         # URP profiles, camera presets, and build configurations
├── Sprites/          # UI icons and graphics
└── UI/               # Joystick textures and control sprites
```

---

## 🛠️ Getting Started

### Prerequisites
- **Unity Editor**: `6000.4.6f1` (Unity 6) or compatible.
- **Render Pipeline**: Universal Render Pipeline (URP).
- **Target Platform**: Android (ARM64 recommended) / Desktop.

### Setup Instructions
1. Clone the repository:
   ```bash
   git clone https://github.com/MohamedMamdouhElmetwaly/3D_VirtualTourGame.git
   ```
2. Open **Unity Hub** and click **Add** > **Add project from disk**.
3. Select the cloned repository folder.
4. Open the project with **Unity 6000.4.6f1** (or compatible Unity 6 release).
5. In the Project window, navigate to `Assets/Scenes/` and open `Apartment Virtual Tour.unity`.
6. Press **Play** in the Unity Editor to test controls and navigation.

---

## 📱 Build Instructions (Android)

1. Go to **File** > **Build Profiles** (or **Build Settings**).
2. Select **Android** and click **Switch Platform** if not already active.
3. Ensure `Assets/Scenes/Apartment Virtual Tour.unity` is checked in the scene list.
4. In **Player Settings**:
   - Target Architecture: **ARM64**.
   - Scripting Backend: **IL2CPP**.
   - Minimum API Level: Android 7.0 (API level 24) or higher.
5. Click **Build** to generate the APK or export as an Android project for Flutter embedding.

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details if applicable.
