using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Optimizes graphics settings for mobile devices
/// Reduces lag and improves performance while maintaining visual quality
/// Automatically adjusts settings based on device specifications
/// </summary>
public class MobileGraphicsOptimizer : MonoBehaviour
{
    public static MobileGraphicsOptimizer Instance { get; private set; }
    
    [Header("Quality Settings")]
    [Range(0, 5)]
    public int targetQualityLevel = 2; // 0=Fastest, 5=Fantastic
    
    [Header("Performance Optimization")]
    public bool enableVSyncOptimization = true;
    public int targetFrameRate = 60;
    
    [Tooltip("Reduce shadow distance on lower-end devices")]
    public bool optimizeShadows = true;
    [Range(10, 200)]
    public float shadowDistance = 100f;
    
    [Tooltip("Enable GPU instancing for batching")]
    public bool enableGPUInstancing = true;
    
    [Tooltip("Reduce texture resolution on lower-end devices")]
    public bool optimizeTextures = true;
    
    [Tooltip("Enable LOD (Level of Detail) groups")]
    public bool enableLOD = true;
    
    [Header("Anti-Aliasing")]
    public bool enableAntiAliasing = true;
    public int msaaLevel = 2; // 1=Off, 2=2x, 4=4x, 8=8x
    
    [Header("Debug")]
    public bool debugLogging = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        ApplyOptimizations();
    }
    
    public void ApplyOptimizations()
    {
        Debug.Log("[MobileGraphicsOptimizer] Applying graphics optimizations...");
        
        // Set frame rate
        if (enableVSyncOptimization)
        {
            QualitySettings.vSyncCount = 1; // Enable VSync for smooth performance
            Application.targetFrameRate = targetFrameRate;
            LogDebug($"Frame rate set to {targetFrameRate} FPS with VSync enabled");
        }
        
        // Apply quality level
        QualitySettings.SetQualityLevel(targetQualityLevel, true);
        LogDebug($"Quality level set to {targetQualityLevel}");
        
        // Optimize shadows
        if (optimizeShadows)
        {
            QualitySettings.shadowDistance = shadowDistance;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;
            LogDebug($"Shadow distance set to {shadowDistance}");
        }
        
        // Enable GPU instancing
        if (enableGPUInstancing)
        {
            // This needs to be done per-material, so we'll flag it
            OptimizeAllMaterials();
        }
        
        // Anti-aliasing
        if (enableAntiAliasing)
        {
            QualitySettings.antiAliasing = msaaLevel;
            LogDebug($"Anti-aliasing set to {msaaLevel}x MSAA");
        }
        
        // Additional optimizations
        QualitySettings.maxQueuedFrames = 2; // Reduce frame queue for better responsiveness
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        
        LogDebug("Graphics optimizations applied successfully");
    }
    
    void OptimizeAllMaterials()
    {
        // Enable GPU instancing on all materials in scene
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
        int count = 0;
        
        foreach (Material mat in materials)
        {
            if (mat.shader != null && mat.HasProperty("_MainTex"))
            {
                mat.enableInstancing = true;
                count++;
            }
        }
        
        LogDebug($"GPU instancing enabled on {count} materials");
    }
    
    public void OptimizeForDevice()
    {
        // Auto-detect device capabilities and optimize
        int deviceRam = SystemInfo.systemMemorySize;
        int gpuMemory = SystemInfo.graphicsMemorySize;
        
        if (deviceRam < 3000) // Less than 3GB RAM
        {
            targetQualityLevel = 0;
            shadowDistance = 50f;
            msaaLevel = 1;
        }
        else if (deviceRam < 6000) // 3-6GB RAM
        {
            targetQualityLevel = 2;
            shadowDistance = 75f;
            msaaLevel = 2;
        }
        else // 6GB+ RAM
        {
            targetQualityLevel = 4;
            shadowDistance = 150f;
            msaaLevel = 4;
        }
        
        LogDebug($"Device detected: {deviceRam}MB RAM, {gpuMemory}MB GPU VRAM");
        LogDebug($"Auto-optimized for device: Quality={targetQualityLevel}, ShadowDist={shadowDistance}");
        
        ApplyOptimizations();
    }
    
    void LogDebug(string message)
    {
        if (debugLogging)
        {
            Debug.Log($"[MobileGraphicsOptimizer] {message}");
        }
    }
    
    // Public methods to adjust settings at runtime
    public void SetQualityLevel(int level)
    {
        targetQualityLevel = Mathf.Clamp(level, 0, 5);
        QualitySettings.SetQualityLevel(targetQualityLevel, true);
        LogDebug($"Quality level changed to {targetQualityLevel}");
    }
    
    public void SetFrameRate(int rate)
    {
        targetFrameRate = Mathf.Max(30, rate);
        Application.targetFrameRate = targetFrameRate;
        LogDebug($"Frame rate changed to {targetFrameRate}");
    }
    
    public void SetShadowDistance(float distance)
    {
        shadowDistance = Mathf.Max(10, distance);
        QualitySettings.shadowDistance = shadowDistance;
        LogDebug($"Shadow distance changed to {shadowDistance}");
    }
}
