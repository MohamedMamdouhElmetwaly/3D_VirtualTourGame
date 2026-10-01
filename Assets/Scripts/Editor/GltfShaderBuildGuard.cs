#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Prevents glTFast's runtime-only shaders from being stripped from players.
/// Runtime GLB materials have no scene-time material reference, so Unity's
/// normal dependency scan cannot discover these shaders on its own.
/// </summary>
[InitializeOnLoad]
public sealed class GltfShaderBuildGuard : IPreprocessBuildWithReport
{
    private const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";

    private static readonly string[] ShaderPaths =
    {
        "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-pbrMetallicRoughness.shadergraph",
        "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-pbrSpecularGlossiness.shadergraph",
        "Packages/com.unity.cloud.gltfast/Runtime/Shader/glTF-unlit.shadergraph",
        "Packages/com.unity.cloud.gltfast/Runtime/Shader/URP/glTF-pbrMetallicRoughness-Clearcoat.shadergraph"
    };

    public int callbackOrder => -1000;

    static GltfShaderBuildGuard()
    {
        EditorApplication.delayCall += EnsureShadersIncluded;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureShadersIncluded();
    }

    [MenuItem("Tools/GLB/Ensure Runtime Shaders Are Included")]
    public static void EnsureShadersIncluded()
    {
        var graphicsSettings = GraphicsSettings.GetGraphicsSettings();
        if (graphicsSettings == null)
        {
            throw new BuildFailedException(
                $"Could not load {GraphicsSettingsPath}; glTF runtime shaders cannot be protected from stripping.");
        }

        var serializedSettings = new SerializedObject(graphicsSettings);
        var includedShaders = serializedSettings.FindProperty("m_AlwaysIncludedShaders");
        if (includedShaders == null || !includedShaders.isArray)
        {
            throw new BuildFailedException(
                "Unity Graphics Settings does not expose m_AlwaysIncludedShaders.");
        }

        // Remove broken references left by package upgrades or manual YAML edits.
        for (int i = includedShaders.arraySize - 1; i >= 0; i--)
        {
            if (includedShaders.GetArrayElementAtIndex(i).objectReferenceValue == null)
                includedShaders.DeleteArrayElementAtIndex(i);
        }

        int addedCount = 0;
        foreach (string shaderPath in ShaderPaths)
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
                throw new BuildFailedException($"Required glTF shader was not found: {shaderPath}");

            if (ContainsShader(includedShaders, shader))
                continue;

            int index = includedShaders.arraySize;
            includedShaders.InsertArrayElementAtIndex(index);
            includedShaders.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            addedCount++;
        }

        if (serializedSettings.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(graphicsSettings);
            AssetDatabase.SaveAssets();
        }

        Debug.Log(
            $"[GltfShaderBuildGuard] Verified {ShaderPaths.Length} glTF runtime shaders " +
            $"in Always Included Shaders ({addedCount} added). These shaders and variants will be kept in player builds.");
    }

    private static bool ContainsShader(SerializedProperty includedShaders, Shader shader)
    {
        for (int i = 0; i < includedShaders.arraySize; i++)
        {
            if (includedShaders.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                return true;
        }

        return false;
    }
}
#endif
