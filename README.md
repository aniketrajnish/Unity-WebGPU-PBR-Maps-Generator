# Unity-WebGPU-PBR-Maps-Generator
Helps generate PBR maps from a base/diffuse map depending on the workflow chosen. This project demonstrates the power of `WebGPU` by implementing a Physically Based Rendering (PBR) map generator that runs efficiently in web browsers. It showcases the use of compute shaders for GPU-accelerated texture processing, providing a significant performance boost over traditional CPU-based methods. The output of course is not very fancy, as we don't have access to the real height information of the texture, and we're just making a guess based on the color information, which could be a hit or miss. But it's a good starting point to prototype some PBR textures quickly!

You can try out the project here- [https://makra.wtf/Unity-WebGPU-PBR-Maps-Generator/](https://makra.wtf/Unity-WebGPU-PBR-Maps-Generator/). <br> The page might take a while to load for the first time but once it's cached, it should load quickly on subsequent visits.

<div align =center>
  <img src="https://github.com/user-attachments/assets/b10597ab-970a-427a-8fbe-e86f4d9d6726" alt="image" style="width: 80%;"/>
</div>

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

<div align =center>
<table>
  <tr>
    <td><img src="https://github.com/user-attachments/assets/047ddb0b-71a4-40d1-93ff-5210c8fe4690" width="75"></td>
    <td><img src="https://github.com/user-attachments/assets/070bda0c-1716-4925-9bad-e8c8748e5bb4" width="75"></td>
    <td><img src="https://github.com/user-attachments/assets/5eaa7e0a-652e-49f0-9435-1829bf3415b5" width="75"></td>
    <td><img src="https://github.com/user-attachments/assets/8f51a7fd-3863-4125-b90d-4b0aeff0b94c" width="75"></td>
    <td><img src="https://github.com/user-attachments/assets/9fbdd95c-32b6-481b-9670-71a8d0398cc8" width="75"></td>
    <td><img src="https://github.com/user-attachments/assets/3ce171b2-579f-4c6e-8dfa-fbad3e6da583" width="75"></td>
    <td><img src="https://github.com/user-attachments/assets/5d214885-05fe-4a81-ab73-839502ab57a2" width="75"></td>
    <td><img src="https://github.com/user-attachments/assets/101e1d5e-b990-4892-ae25-53c024d9e2b7" width="75"></td>
  </tr>
  <tr>
    <td align="center">Base</td>
    <td align="center">Height</td>
    <td align="center">Normal</td>
    <td align="center">AO</td>
    <td align="center">Metallic</td>
    <td align="center">Roughness</td>
    <td align="center">Specular</td>
    <td align="center">Glossiness</td>
  </tr>
</table>

PBR maps generated from a single base texture using the tool
</div>

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

<div align=center>
  <table style="margin: auto;">
    <thead>
      <tr>
        <th>Map Type</th>
        <th>Resolution</th>
        <th>CPU Time (ms)</th>
        <th>GPU Time (ms)</th>
        <th>Speedup Factor</th>
      </tr>
    </thead>
    <tbody>
      <tr>
        <td>Height</td>
        <td>512x512</td>
        <td>350</td>
        <td>20</td>
        <td>17.5x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>1500</td>
        <td>50</td>
        <td>30x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>6200</td>
        <td>150</td>
        <td>41.3x</td>
      </tr>
      <tr>
        <td>Normal</td>
        <td>512x512</td>
        <td>500</td>
        <td>30</td>
        <td>16.7x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>2000</td>
        <td>80</td>
        <td>25x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>8000</td>
        <td>250</td>
        <td>32x</td>
      </tr>
      <tr>
        <td>AO</td>
        <td>512x512</td>
        <td>450</td>
        <td>20</td>
        <td>22.5x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>1800</td>
        <td>60</td>
        <td>30x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>7200</td>
        <td>180</td>
        <td>40x</td>
      </tr>
      <tr>
        <td>Roughness</td>
        <td>512x512</td>
        <td>550</td>
        <td>30</td>
        <td>18.3x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>2200</td>
        <td>70</td>
        <td>31.4x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>8800</td>
        <td>220</td>
        <td>40x</td>
      </tr>
      <tr>
        <td>Metallic</td>
        <td>512x512</td>
        <td>400</td>
        <td>20</td>
        <td>20x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>1600</td>
        <td>50</td>
        <td>32x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>6400</td>
        <td>160</td>
        <td>40x</td>
      </tr>
      <tr>
        <td>Specular</td>
        <td>512x512</td>
        <td>420</td>
        <td>20</td>
        <td>21x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>1700</td>
        <td>60</td>
        <td>28.3x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>6800</td>
        <td>180</td>
        <td>37.8x</td>
      </tr>
      <tr>
        <td>Glossiness</td>
        <td>512x512</td>
        <td>600</td>
        <td>40</td>
        <td>15x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>2400</td>
        <td>90</td>
        <td>26.7x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>9600</td>
        <td>280</td>
        <td>34.3x</td>
      </tr>
    </tbody>
  </table>
</div>

<div align =center>
  <img src="https://github.com/user-attachments/assets/b4a7f72f-f147-426e-813a-37ed016df6c1" alt="plt" style="width: 80%;"/>
</div>

As we can see from the table and graph, the GPU method consistently outperforms the CPU method, with the performance gap widening as the texture resolution increases. This demonstrates the scalability and efficiency of compute shaders for texture processing tasks.

## WebGPU Limitations & Solutions
One key limitation of `WebGPU` is the lack of support for synchronous GPU readback. To address this, we use `AsyncGPUReadback` instead of the traditional `ReadPixels` method:

```csharp
private void ReadbackTexture(Texture texture, Action<Texture2D> callback)
{
    AsyncGPUReadback.Request(texture, 0, readback =>
    {
        if (readback.hasError)
        {
            Debug.LogError("GPU readback error detected.");
            return;
        }
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        texture2D.LoadRawTextureData(readback.GetData<byte>());
        texture2D.Apply();
        callback(texture2D);
    });
}
```
This asynchronous approach ensures compatibility with WebGPU while maintaining efficient GPU-to-CPU data transfer. You can see an example implementation [here](https://github.com/aniketrajnish/Unity-WebGPU-PBR-Maps-Generator/blob/cc98607c42f736d329499dfdf8d609924396dd7d/src/PBR%20Maps%20Generator/Assets/Scripts/Conversion/Maps/Normal.cs#L49).

## Installation
- Clone the repository or download the `.unitypackage` from [here](https://github.com/aniketrajnish/PBR-Maps-Generator/releases/tag/v001).
- Open/Import the project in `Unity 2023.3` or later.
- If you're using the `.unitypackage`, make sure to create a project using the `Built-In Render Pipeline` and import `TextMeshPro`.
- Make sure to enable the `WebGPU` backend to take advantage of GPU acceleration. Instructions [here](https://discussions.unity.com/t/early-access-to-the-new-webgpu-backend-in-unity-2023-3/933493).

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. Currently working on-
- Better algorithms for generating the maps that give better results. 
- Incorporating deep learning models to estimate the height information from the color information instead of just doing a greyscale conversion.

## License
MIT
