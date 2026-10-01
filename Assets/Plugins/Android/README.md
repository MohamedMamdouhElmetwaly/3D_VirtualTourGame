# Android File Picker

The Android picker is implemented in C# in:

`Assets/Scripts/UI/AndroidFilePicker.cs`

It uses Android's system document picker through Unity's `AndroidJavaObject` and
`AndroidJavaProxy` APIs. `GlbFileHelper.java` performs the selected URI-to-cache
stream copy on Android so binary data is transferred correctly and efficiently.
No custom activity override or storage permission is required.

## Flow

1. The user taps Browse.
2. Unity opens Android's real file picker with `ActivityResultContracts.OpenDocument`.
3. The user selects a `.glb` file from Downloads, internal storage, Drive, or another provider.
4. Unity receives the selected `content://` URI.
5. The file is copied into Unity's temporary cache folder.
6. The existing runtime model loader receives a normal local file path.

## Permissions

Storage permissions are not required for this flow. Android grants access to the
selected document URI through the system picker.

`INTERNET` is still needed for the app's Load from URL option.

## Disabled Files

`AndroidManifest.xml.DISABLED` is intentionally disabled and is not used by Unity.
Do not rename it back to `AndroidManifest.xml` unless you specifically need a
custom manifest for another reason.
