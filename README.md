# PBR-Maps-Generator
Helps generate PBR maps from a base/diffuse map depending on the workflow chosen. This project demonstrates the power of `Unity` 6's `WebGPU` backend by implementing a Physically Based Rendering (PBR) map generator that runs efficiently in web browsers. It showcases the use of compute shaders for GPU-accelerated texture processing, providing a significant performance boost over traditional CPU-based methods. You can try out the project here- [https://makra.wtf/PBR-Maps-Generator/](https://makra.wtf/PBR-Maps-Generator/).

<div style="text-align: center;">
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

<div style="text-align: center;">
  <table style="width: 80%; margin: auto;">
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
        <td>35</td>
        <td>2</td>
        <td>17.5x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>150</td>
        <td>5</td>
        <td>30x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>620</td>
        <td>15</td>
        <td>41.3x</td>
      </tr>
      <tr>
        <td>Normal</td>
        <td>512x512</td>
        <td>50</td>
        <td>3</td>
        <td>16.7x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>200</td>
        <td>8</td>
        <td>25x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>800</td>
        <td>25</td>
        <td>32x</td>
      </tr>
      <tr>
        <td>AO</td>
        <td>512x512</td>
        <td>45</td>
        <td>2</td>
        <td>22.5x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>180</td>
        <td>6</td>
        <td>30x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>720</td>
        <td>18</td>
        <td>40x</td>
      </tr>
      <tr>
        <td>Roughness</td>
        <td>512x512</td>
        <td>55</td>
        <td>3</td>
        <td>18.3x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>220</td>
        <td>7</td>
        <td>31.4x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>880</td>
        <td>22</td>
        <td>40x</td>
      </tr>
      <tr>
        <td>Metallic</td>
        <td>512x512</td>
        <td>40</td>
        <td>2</td>
        <td>20x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>160</td>
        <td>5</td>
        <td>32x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>640</td>
        <td>16</td>
        <td>40x</td>
      </tr>
      <tr>
        <td>Specular</td>
        <td>512x512</td>
        <td>42</td>
        <td>2</td>
        <td>21x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>170</td>
        <td>6</td>
        <td>28.3x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>680</td>
        <td>18</td>
        <td>37.8x</td>
      </tr>
      <tr>
        <td>Glossiness</td>
        <td>512x512</td>
        <td>60</td>
        <td>4</td>
        <td>15x</td>
      </tr>
      <tr>
        <td></td>
        <td>1024x1024</td>
        <td>240</td>
        <td>9</td>
        <td>26.7x</td>
      </tr>
      <tr>
        <td></td>
        <td>2048x2048</td>
        <td>960</td>
        <td>28</td>
        <td>34.3x</td>
      </tr>
    </tbody>
  </table>
</div>

<div style="text-align: center;">
  <img src="https://github.com/user-attachments/assets/c8262bd7-1e78-480d-b820-724c1b5cae8d" alt="plt" style="width: 80%;"/>
</div>

As we can see from the table and graph, the GPU method consistently outperforms the CPU method, with the performance gap widening as the texture resolution increases. This demonstrates the scalability and efficiency of compute shaders for texture processing tasks.

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change. Currently working on better algorithms for generating the maps that give better results.

## License
MIT
