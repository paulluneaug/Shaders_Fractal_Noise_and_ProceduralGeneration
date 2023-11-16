using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public static class ArrayExtension
{
    public unsafe static T[] Resize<T>(this T[] array, int size) where T : unmanaged
    {
        T[] result = new T[size];

        if (size == 0 || array.Length == 0)
        {
            return result;
        }

        fixed (T* arrayRawPtr = &array[0])
        {
            fixed (T* resultRawPtr = &result[0])
            {
                Buffer.MemoryCopy(arrayRawPtr, resultRawPtr, size * sizeof(T), Math.Min(array.Length, size) * sizeof(T));
            }
        }

        return result;
    }

    public unsafe static T[] Resize<T>(this Span<T> array, int size) where T : unmanaged
    {
        T[] result = new T[size];

        if (size == 0 || array.Length == 0)
        {
            return result;
        }

        fixed (T* arrayRawPtr = &array[0])
        {
            fixed (T* resultRawPtr = &result[0])
            {
                Buffer.MemoryCopy(arrayRawPtr, resultRawPtr, size * sizeof(T), Math.Min(array.Length, size) * sizeof(T));
            }
        }

        return result;
    }

    public unsafe static NativeArray<T> Resize<T>(this NativeArray<T> array, int size) where T : unmanaged
    {
        NativeArray<T> result = new NativeArray<T>(size, Allocator.Persistent);
        
        if (size == 0 || array.Length == 0)
        {
            return result;
        }

        UnsafeUtility.MemCpy(result.GetUnsafePtr(), array.GetUnsafePtr(), Math.Min(array.Length, size) * sizeof(T));

        return result;
    }

    public unsafe static void Insert<T>(this T[] array, T[] other, int offset) where T : unmanaged
    {
        int otherLen = other.Length;
        if (offset + otherLen > array.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (T* arrayRawPtr = &array[offset])
        {
            fixed (T* otherRawPtr = &other[0])
            {
                Buffer.MemoryCopy(otherRawPtr, arrayRawPtr, otherLen * sizeof(T), otherLen * sizeof(T));
            }
        }
    }

    public unsafe static void Insert<T>(this T[] array, T other, int offset) where T : unmanaged
    {
        if (offset >= array.Length)
        {
            throw new IndexOutOfRangeException();
        }

        fixed (T* arrayRawPtr = &array[offset])
        {
            T* otherRawPtr = &other;
            Buffer.MemoryCopy(otherRawPtr, arrayRawPtr, sizeof(T), sizeof(T));
        }
    }

}
