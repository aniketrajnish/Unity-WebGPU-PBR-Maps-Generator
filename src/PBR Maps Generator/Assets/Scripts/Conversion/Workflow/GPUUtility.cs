using UnityEngine.Rendering;
using UnityEngine;

public static class GPUUtility
{
    public static bool isGPUAvailable =>    
        SystemInfo.supportsComputeShaders && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
    public static bool IsGPUAvailable() => isGPUAvailable;
    public static bool useGPU {  get; private set; }
    static GPUUtility() => useGPU = isGPUAvailable;

    public static void SetUseGPU(bool use) => useGPU = use && isGPUAvailable;    
}