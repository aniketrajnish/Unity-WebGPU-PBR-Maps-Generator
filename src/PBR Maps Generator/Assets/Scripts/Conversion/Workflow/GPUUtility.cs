using UnityEngine.Rendering;
using UnityEngine;

public static class GPUUtility
{
    public static bool IsGPUComputeAvailable() =>    
        SystemInfo.supportsComputeShaders && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
    
}