using UnityEngine;
using UnityEngine.Rendering;
using System;

/// <summary>
/// Generates a normal map from a height map.
/// Initializes the compute shader if the device supports it. (has a GPU that can support 8x8 threads per block)
/// </summary>
public static class NormalMap
{
    private static ComputeShader _normalComp;
    private static int _kernelIdx;

    /// <summary>
    /// When the class is initialized, check if the device has a GPU and initialize the compute shader.
    /// <!-- This is a static constructor and will only be called once. -->
    /// </summary>
    static NormalMap()
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_useGPU)
            InitializeComputeShader();        
    }

    /// <summary>
    /// Load the Normal compute shader
    /// </summary>
    public static void InitializeComputeShader()
    {
        _normalComp = Resources.Load<ComputeShader>("NormalCompute");
        if (_normalComp == null)
        {
            Debug.LogError("Failed to load Normal compute shader. Make sure 'NormalCompute.compute' is in a Resources folder.");
            return;
        }
        _kernelIdx = _normalComp.FindKernel("CSMain");
        if (_kernelIdx < 0)
            Debug.LogError("Failed to find 'CSMain' kernel in Normal compute shader.");
    }

    /// <summary>
    /// Perfrom the Normal conversion on the GPU if available, else use the CPU.
    /// Uses AsyncGPUReadback as WebGPU does not support ReadPixels.
    /// We divide the texture into 64 pixel blocks and process them in parallel.
    /// </summary>
    /// <!-- This is an async operation and will return the Normal map in the callback. -->
    /// <param name="baseMap">The base map (Texture2D) to convert to Normal.</param>
    /// <param name="callback">The callback to return the Normal map.</param>
    public static void GPUConvertToNormalMap(Texture2D baseMap, Action<Texture2D> callback)
    {
        bool _useGPU = GPUUtility.useGPU;
        if (_normalComp == null || !_useGPU)
        {
            callback(CPUConvertToNormalMap(baseMap));
            return;
        }

        HeightMap.GPUConvertToHeightMap(baseMap, (heightMap) =>
        {
            if (heightMap == null)
            {
                Debug.LogError("Failed to generate height map for normal map generation.");
                callback(null);
                return;
            }

            int w = baseMap.width;
            int h = baseMap.height;
            RenderTexture heightRT = new RenderTexture(w, h, 0, RenderTextureFormat.RFloat);
            heightRT.enableRandomWrite = true;
            Graphics.Blit(heightMap, heightRT);
            RenderTexture normalRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            normalRT.enableRandomWrite = true;
            normalRT.Create();
            _normalComp.SetTexture(_kernelIdx, "HeightMap", heightRT);
            _normalComp.SetTexture(_kernelIdx, "NormalMap", normalRT);
            _normalComp.SetInts("TextureSize", w, h);
            _normalComp.SetBool("FlipY", Application.platform == RuntimePlatform.WebGLPlayer);
            int thrdX = Mathf.CeilToInt(w / 8.0f);
            int thrdY = Mathf.CeilToInt(h / 8.0f);
            _normalComp.Dispatch(_kernelIdx, thrdX, thrdY, 1);

            AsyncGPUReadback.Request(normalRT, 0, TextureFormat.RGBA32, (request) =>
            {
                if (request.hasError)
                {
                    Debug.LogError("GPU readback error detected.");
                    callback(null);
                    heightRT.Release();
                    normalRT.Release();
                    return;
                }

                Texture2D normalMap = new Texture2D(w, h, TextureFormat.RGBA32, false);
                normalMap.LoadRawTextureData(request.GetData<byte>());
                normalMap.Apply();

                heightRT.Release();
                normalRT.Release();

                callback(normalMap);
            });
        });
    }

    /// <summary>
    /// Take a base map, iterate through each pixel, 
    /// get the height value of the surrounding pixels by sampling the height map, 
    /// and calculate the normal from the height values.
    /// We invert the Y axis for WebGPU as it is renderes on render texture upside down.
    /// <!-- This is a CPU bound operation and is not recommended for large textures. -->
    /// </summary>
    /// <param name="baseMap">The base map to convert to Normal.</param>
    /// <returns>
    /// The Normal map (Texture2D) generated from the base map.
    /// </returns>
    private static Texture2D CPUConvertToNormalMap(Texture2D baseMap)
    {
        Texture2D heightMap = HeightMap.CPUConvertToHeightMap(baseMap);
        Texture2D normalMap = new Texture2D(baseMap.width, baseMap.height, TextureFormat.RGBA32, true);
        for (int y = 0; y < heightMap.height; y++)
        {
            for (int x = 0; x < heightMap.width; x++)
            {
                float left = GetPixelHeight(heightMap, x - 1, y);
                float right = GetPixelHeight(heightMap, x + 1, y);
                float up = GetPixelHeight(heightMap, x, y - 1);
                float down = GetPixelHeight(heightMap, x, y + 1);
                Vector3 normal = new Vector3(left - right, up - down, 2f).normalized * 5f;
                normal = (normal + Vector3.one) * .5f;
                normalMap.SetPixel(x, y, new Color(normal.x, normal.y, normal.z, 1f));
            }
        }
        normalMap.Apply();
        return normalMap;
    }
    /// <summary>
    /// Get the grayscale value of a pixel in a texture.
    /// </summary>
    /// <param name="tex">The texture to sample.</param>
    /// <param name="x">The x position of the pixel.</param>
    /// <param name="y">The y position of the pixel.</param>
    /// <returns>
    /// The grayscale value of the pixel.
    /// </returns>
    private static float GetPixelHeight(Texture2D tex, int x, int y)
    {
        x = Mathf.Clamp(x, 0, tex.width - 1);
        y = Mathf.Clamp(y, 0, tex.height - 1);
        return tex.GetPixel(x, y).grayscale;
    }
}