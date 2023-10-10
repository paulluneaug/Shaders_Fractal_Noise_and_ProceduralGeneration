using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class Constants
{
    public const int CHUNK_SIZE = 16;
}

public static class MeshStructs
{
    public interface IMesh
    {
        Vector3[] GetVertices();
        int[] GetTriangles();
    }

    public unsafe class MeshStructsUtils
    {
        public static Vector3[] GetVertices(IntPtr verticesPtr, int verticesCount)
        {
            float[] rawValues = new float[3 * verticesCount];

            Marshal.Copy(verticesPtr, rawValues, 0, 3 * verticesCount);

            int size = 0;
            for (; size < verticesCount; size++)
            {
                if (rawValues[size] == -1)
                {
                    break;
                }
            }

            int resultSize = size / 3;
            Debug.Log($"MemCopy : Size = {size} | Len = {resultSize}");
            Vector3[] result = new Vector3[resultSize];

            if (resultSize == 0)
            {
                return result;
            }

            fixed (Vector3* resultRawPtr = &result[0])
            {
                IntPtr resultPtr = new IntPtr(resultRawPtr);
                Marshal.Copy(rawValues, 0, resultPtr, resultSize * 3);
            }

            //Array.Copy(rawValues, result, resultSize * 3);

            return result;
        }

        public static int[] GetTriangles(IntPtr trianglesPtr, int trianglesCount)
        {
            int[] rawValues = new int[trianglesCount];

            Marshal.Copy(trianglesPtr, rawValues, 0, trianglesCount);

            int size = 0;
            for (; size < trianglesCount; size++)
            {
                if (rawValues[size] == -1)
                {
                    break;
                }
            }

            int[] result = new int[size];
            Array.Copy(rawValues, result, size);
            //Buffer.BlockCopy(rawValues, 0, result, 0, size * sizeof(int));

            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CubeMesh : IMesh
    {
        private const int VERTICES_COUNT = 12;
        private const int TRIANGLES_COUNT = 12;

        private fixed float Vertices[VERTICES_COUNT * 3];
        private fixed int Triangles[TRIANGLES_COUNT];

        public Vector3[] GetVertices()
        {
            fixed (float* verticesRawPrt = Vertices)
            {
                IntPtr verticesPtr = new IntPtr(verticesRawPrt);
                {
                    return MeshStructsUtils.GetVertices(verticesPtr, VERTICES_COUNT);
                }
            }
        }

        public int[] GetTriangles()
        {
            fixed (int* trianglesRawPtr = Triangles)
            {
                IntPtr trianglesPtr = new IntPtr(trianglesRawPtr);
                {
                    return MeshStructsUtils.GetTriangles(trianglesPtr, TRIANGLES_COUNT);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ChunkMesh : IMesh
    {
        private const int VERTICES_COUNT = 12 * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE;
        private const int TRIANGLES_COUNT = 12 * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE;

        private fixed float Vertices[VERTICES_COUNT * 3];
        private fixed int Triangles[TRIANGLES_COUNT];

        public Vector3[] GetVertices()
        {
            fixed (float* verticesRawPrt = Vertices)
            {
                IntPtr verticesPtr = new IntPtr(verticesRawPrt);
                {
                    return MeshStructsUtils.GetVertices(verticesPtr, VERTICES_COUNT);
                }
            }
        }

        public int[] GetTriangles()
        {
            fixed (int* trianglesRawPtr = Triangles)
            {
                IntPtr trianglesPtr = new IntPtr(trianglesRawPtr);
                {
                    return MeshStructsUtils.GetTriangles(trianglesPtr, TRIANGLES_COUNT);
                }
            }
        }
    }
}


