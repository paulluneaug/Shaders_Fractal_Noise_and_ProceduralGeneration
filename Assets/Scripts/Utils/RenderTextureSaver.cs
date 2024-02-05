using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

# if UNITY_EDITOR
using UnityEditor;
#endif

public static class RenderTextureSaver
{
    private static NativeArray<float> m_rawTextureData;

    public static bool SaveRenderTexture(this RenderTexture tex, string path)
    {
        int width = tex.width;
        int height = tex.height;
        int depth = tex.volumeDepth;


        m_rawTextureData = new NativeArray<float>(width * height * depth * sizeof(float), Allocator.Persistent);
        AsyncGPUReadback.RequestIntoNativeArray(ref m_rawTextureData, tex, 0, (AsyncGPUReadbackRequest request) => OnRequestEnded(request, tex, width, height, depth, path));

        return true;
    }

    private static void OnRequestEnded(AsyncGPUReadbackRequest request, RenderTexture tex, int width, int height, int depth, string path)
    {
# if UNITY_EDITOR
        Texture output = null;
        switch (tex.dimension)
        {
            case TextureDimension.Unknown:
            case TextureDimension.None:
            case TextureDimension.Any:
            case TextureDimension.Cube:
            case TextureDimension.CubeArray:
                break;

            case TextureDimension.Tex2D:
                Texture2D output2D = new Texture2D(width, height, tex.graphicsFormat, TextureCreationFlags.None);
                //Graphics.CopyTexture(tex, output2D);
                output2D.SetPixelData(m_rawTextureData, 0);
                //output2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                output = output2D;
                break;

            case TextureDimension.Tex3D:
                Texture3D output3D = new Texture3D(width, height, depth, tex.graphicsFormat, TextureCreationFlags.None);
                output3D.SetPixelData(m_rawTextureData, 0);
                //Graphics.CopyTexture(tex, output3D);
                output3D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                output = output3D;
                break;

            case TextureDimension.Tex2DArray:
                break;

        }
        m_rawTextureData.Dispose();
        if (output == null)
        {
            return;
        }

        AssetDatabase.CreateAsset(output, $"Assets/{path}.asset");
        AssetDatabase.SaveAssetIfDirty(output);
# endif
    }
}
