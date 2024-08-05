using UnityEngine.Rendering;
using UnityEngine;

/// <summary>
/// This class is used to check if the device has a GPU and if it is available for use.
/// It also has a static property to set if the GPU should be used by the user.
/// </summary>
public static class GPUUtility
{    
    public static bool isGPUAvailable =>    
        SystemInfo.supportsComputeShaders && SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
    public static bool IsGPUAvailable() => isGPUAvailable;
    public static bool useGPU {  get; private set; }
    static GPUUtility() => useGPU = isGPUAvailable;

    public static void SetUseGPU(bool use) => useGPU = use && isGPUAvailable;    
}