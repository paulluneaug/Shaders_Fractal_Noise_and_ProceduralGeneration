using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static UnityEditor.Rendering.CameraUI;

public static class RenderTextureSaver
{
    public static bool SaveRenderTexture(this RenderTexture tex, string path)
    {
        int width = tex.width;
        int height = tex.height;
        int depth = tex.volumeDepth;

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
                Graphics.CopyTexture(tex, output2D);
                output2D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                output = output2D;
                break;

            case TextureDimension.Tex3D:
                Texture3D output3D = new Texture3D(width, height, depth, tex.graphicsFormat, TextureCreationFlags.None);
                Graphics.CopyTexture(tex, output3D);
                output3D.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                output = output3D;
                break;

            case TextureDimension.Tex2DArray:
                Texture2DArray output2DArray = new Texture2DArray(width, height, depth, tex.graphicsFormat, TextureCreationFlags.None);
                Graphics.CopyTexture(tex, output2DArray);
                output2DArray.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                output = output2DArray;
                break;

        }
        if (output == null)
        {
            return false;
        }

        AssetDatabase.CreateAsset(output, $"Assets/{path}.asset");
        AssetDatabase.SaveAssetIfDirty(output);

        return true;
    }
}
