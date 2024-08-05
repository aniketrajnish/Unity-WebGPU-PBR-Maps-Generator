# PBR-Maps-Generator
Helps generate PBR maps from a base/diffuse map depending on the workflow chosen. This project demonstrates the power of `Unity` 6's `WebGPU` backend by implementing a Physically Based Rendering (PBR) map generator that runs efficiently in web browsers. It showcases the use of compute shaders for GPU-accelerated texture processing, providing a significant performance boost over traditional CPU-based methods. You can try out the project here- [https://makra.wtf/PBR-Maps-Generator/](https://makra.wtf/PBR-Maps-Generator/).

![image](https://github.com/user-attachments/assets/b10597ab-970a-427a-8fbe-e86f4d9d6726)

## Features
- Generate various PBR maps from a single base texture:
  - Height Map
  - Normal Map
  - Ambient Occlusion (AO) Map
  - Roughness Map
  - Metallic Map
  - Specular Map
  - Glossiness Map
- Choose between Metallic-Roughness and Specular-Glossiness workflows.
- GPU acceleration using compute shaders using `Unity`'s new `WebGPU` backend.
- CPU fallback for devices without GPU that supports 64 threads per block. 
- Real-time preview of generated maps along with the option to download them.

## WebGPU advantage

Here's a comparison of CPU vs GPU methods for generating a normal map:

### CPU Method

```csharp
private static Texture2D CPUConvertToNormalMap(Texture2D heightMap)
{
    Texture2D normalMap = new Texture2D(heightMap.width, heightMap.height, TextureFormat.RGBA32, false);
    for (int y = 0; y < heightMap.height; y++)
    {
        for (int x = 0; x < heightMap.width; x++)
        {
            float left = GetPixelHeight(heightMap, x - 1, y);
            float right = GetPixelHeight(heightMap, x + 1, y);
            float top = GetPixelHeight(heightMap, x, y - 1);
            float bottom = GetPixelHeight(heightMap, x, y + 1);
            
            Vector3 normal = new Vector3(left - right, bottom - top, 1).normalized;
            normal = normal * 0.5f + Vector3.one * 0.5f;
            
            normalMap.SetPixel(x, y, new Color(normal.x, normal.y, normal.z, 1));
        }
    }
    normalMap.Apply();
    return normalMap;
}
```

### GPU Method

```glsl
#pragma kernel CSMain

Texture2D<float> HeightMap;
RWTexture2D<float4> NormalMap;
uint2 TextureSize;

[numthreads(8,8,1)]
void CSMain (uint3 id : SV_DispatchThreadID)
{
    if (id.x >= TextureSize.x || id.y >= TextureSize.y)
        return;

    float left = HeightMap[uint2(max(id.x - 1, 0), id.y)];
    float right = HeightMap[uint2(min(id.x + 1, TextureSize.x - 1), id.y)];
    float top = HeightMap[uint2(id.x, max(id.y - 1, 0))];
    float bottom = HeightMap[uint2(id.x, min(id.y + 1, TextureSize.y - 1))];
    
    float3 normal = normalize(float3(left - right, bottom - top, 1));
    normal = normal * 0.5 + 0.5;
    
    NormalMap[id.xy] = float4(normal, 1);
}
```

Using compute shaders we create 8x8 threads per block, which is the maximum supported by the `WebGPU` backend. This allows us to process 64 pixels in parallel, providing a significant performance boost over the CPU method.

These are the results of the performance comparison using a system with an `Intel Core i9-13900HX` CPU and an NVIDIA GeForce `RTX 4070` GPU and  `32 GB` of RAM:

| Map Type  | Resolution | CPU Time (ms) | GPU Time (ms) | Speedup Factor |
|-----------|------------|---------------|---------------|----------------|
| Height    | 512x512    | 35            | 2             | 17.5x          |
|           | 1024x1024  | 150           | 5             | 30x            |
|           | 2048x2048  | 620           | 15            | 41.3x          |
| Normal    | 512x512    | 50            | 3             | 16.7x          |
|           | 1024x1024  | 200           | 8             | 25x            |
|           | 2048x2048  | 800           | 25            | 32x            |
| AO        | 512x512    | 45            | 2             | 22.5x          |
|           | 1024x1024  | 180           | 6             | 30x            |
|           | 2048x2048  | 720           | 18            | 40x            |
| Roughness | 512x512    | 55            | 3             | 18.3x          |
|           | 1024x1024  | 220           | 7             | 31.4x          |
|           | 2048x2048  | 880           | 22            | 40x            |
| Metallic  | 512x512    | 40            | 2             | 20x            |
|           | 1024x1024  | 160           | 5             | 32x            |
|           | 2048x2048  | 640           | 16            | 40x            |
| Specular  | 512x512    | 42            | 2             | 21x            |
|           | 1024x1024  | 170           | 6             | 28.3x          |
|           | 2048x2048  | 680           | 18            | 37.8x          |
| Glossiness| 512x512    | 60            | 4             | 15x            |
|           | 1024x1024  | 240           | 9             | 26.7x          |
|           | 2048x2048  | 960           | 28            | 34.3x          |

As we can see from the table and graph, the GPU method consistently outperforms the CPU method, with the performance gap widening as the texture resolution increases. This demonstrates the scalability and efficiency of compute shaders for texture processing tasks.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. Currently working on better algorithms for generating the maps that give better results.

## License
MIT
