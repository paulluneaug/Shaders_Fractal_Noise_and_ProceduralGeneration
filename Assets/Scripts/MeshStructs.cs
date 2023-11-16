using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;


public static class Constants
{
    public const int CHUNK_SIZE = 16;
    public const int CHUNK_VOLUME = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;
}

public static class MeshStructs
{
    public interface IMesh
    {
        Vector3[] GetVertices();
        int[] GetTriangles();

        Mesh GetMesh();
    }

    public unsafe class MeshStructsUtils
    {
        public static Vector3[] GetVertices(IntPtr verticesPtr, int verticesCount, bool trimMinusOne)
        {
            float[] rawValues = new float[3 * verticesCount];
            Vector3[] result;
            Marshal.Copy(verticesPtr, rawValues, 0, 3 * verticesCount);
            if (trimMinusOne)
            {

                int size = 0;
                for (; size < verticesCount; ++size)
                {
                    if (rawValues[size * 3] == -1)
                    {
                        break;
                    }
                }

                result = new Vector3[size];

                if (size == 0)
                {
                    return result;
                }

                fixed (Vector3* resultRawPtr = &result[0])
                {
                    IntPtr resultPtr = new IntPtr(resultRawPtr);
                    Marshal.Copy(rawValues, 0, resultPtr, size * 3);
                }
            }
            else
            {
                result = new Vector3[verticesCount];
                fixed (Vector3* resultRawPtr = &result[0])
                {
                    IntPtr resultPtr = new IntPtr(resultRawPtr);
                    Marshal.Copy(rawValues, 0, resultPtr, verticesCount * 3);
                }
            }
            return result;
        }
        public static NativeArray<Vector3> GetVerticesNativeArray(IntPtr verticesPtr, int verticesCount, bool trimMinusOne)
        {
            NativeArray<float> rawValues = new NativeArray<float>(3 * verticesCount, Allocator.Temp);
            NativeArray<Vector3> result;
            UnsafeUtility.MemCpy(rawValues.GetUnsafePtr(), verticesPtr.ToPointer(), 3 * verticesCount * sizeof(float));
            if (trimMinusOne)
            {

                int size = 0;
                for (; size < verticesCount; ++size)
                {
                    if (rawValues[size * 3] == -1)
                    {
                        break;
                    }
                }

                result = new NativeArray<Vector3>(size, Allocator.Persistent);

                if (size == 0)
                {
                    return result;
                }

                UnsafeUtility.MemCpy(result.GetUnsafePtr(), rawValues.GetUnsafePtr(), 3 * size * sizeof(float));
            }
            else
            {
                result = new NativeArray<Vector3>(verticesCount, Allocator.Persistent);
                UnsafeUtility.MemCpy(result.GetUnsafePtr(), rawValues.GetUnsafePtr(), 3 * verticesCount * sizeof(float));
            }
            return result;
        }

        public static int[] GetTriangles(IntPtr trianglesPtr, int trianglesCount, bool trimMinusOne)
        {
            int[] rawValues = new int[trianglesCount];

            Marshal.Copy(trianglesPtr, rawValues, 0, trianglesCount);
            if (trimMinusOne)
            {
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
            else
            {
                return rawValues;
            }
        }

        public static NativeArray<int> GetTrianglesNativeArray(IntPtr trianglesPtr, int trianglesCount, bool trimMinusOne)
        {
            NativeArray<int> rawValues = new NativeArray<int>(trianglesCount, Allocator.Persistent);

            UnsafeUtility.MemCpy(rawValues.GetUnsafePtr(), trianglesPtr.ToPointer(), trianglesCount * sizeof(int));
            if (trimMinusOne)
            {
                int size = 0;
                for (; size < trianglesCount; size++)
                {
                    if (rawValues[size] == -1)
                    {
                        break;
                    }
                }

                NativeArray<int> result = new NativeArray<int>(size, Allocator.Persistent); 
                UnsafeUtility.MemCpy(result.GetUnsafePtr(), rawValues.GetUnsafePtr(), size * sizeof(int));
                //Buffer.BlockCopy(rawValues, 0, result, 0, size * sizeof(int));

                return result;
            }
            else
            {
                return rawValues;
            }
        }

        public static Mesh GetMesh(IMesh meshStruct) 
        {
            return new Mesh
            {
                vertices = meshStruct.GetVertices(),
                triangles = meshStruct.GetTriangles()
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CellMesh : IMesh
    {
        private const int VERTICES_COUNT = 12;
        private const int TRIANGLES_COUNT = 12;

        public fixed float Vertices[VERTICES_COUNT * 3];
        public fixed int Triangles[TRIANGLES_COUNT];

        public Vector3[] GetVertices()
        {
            fixed (float* verticesRawPrt = Vertices)
            {
                IntPtr verticesPtr = new IntPtr(verticesRawPrt);
                {
                    return MeshStructsUtils.GetVertices(verticesPtr, VERTICES_COUNT, false);
                }
            }
        }

        public NativeArray<Vector3> GetVerticesNativeArray()
        {
            fixed (float* verticesRawPrt = Vertices)
            {
                IntPtr verticesPtr = new IntPtr(verticesRawPrt);
                {
                    return MeshStructsUtils.GetVerticesNativeArray(verticesPtr, VERTICES_COUNT, false);
                }
            }
        }

        public int[] GetTriangles()
        {
            fixed (int* trianglesRawPtr = Triangles)
            {
                IntPtr trianglesPtr = new IntPtr(trianglesRawPtr);
                {
                    return MeshStructsUtils.GetTriangles(trianglesPtr, TRIANGLES_COUNT, false);
                }
            }
        }
        public NativeArray<int> GetTrianglesNativeArray()
        {
            fixed (int* trianglesRawPtr = Triangles)
            {
                IntPtr trianglesPtr = new IntPtr(trianglesRawPtr);
                {
                    return MeshStructsUtils.GetTrianglesNativeArray(trianglesPtr, TRIANGLES_COUNT, false);
                }
            }
        }

        public readonly Mesh GetMesh()
        {
            return MeshStructsUtils.GetMesh(this);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ChunkMesh : IMesh
    {
        private const int VERTICES_COUNT = 12 * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE;
        private const int TRIANGLES_COUNT = 12 * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE * Constants.CHUNK_SIZE;

        private NativeArray<Vector3> Vertices;
        private NativeArray<int> Triangles;

        public ChunkMesh(NativeArray<Vector3> vertices, NativeArray<int> triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }

        public readonly int[] GetTriangles()
        {
            return Triangles.ToArray();
        }

        public readonly Vector3[] GetVertices()
        {
            return Vertices.ToArray();
        }
        public readonly Mesh GetMesh()
        {
            return MeshStructsUtils.GetMesh(this);
        }
    }
}


