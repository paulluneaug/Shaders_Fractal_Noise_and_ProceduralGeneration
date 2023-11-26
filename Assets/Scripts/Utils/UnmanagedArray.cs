using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using Unity.Collections.LowLevel.Unsafe;
using System.Linq;

public readonly unsafe struct UnmanagedArray<T> : IDisposable where T : unmanaged
{
    private readonly IntPtr m_arrayPtr;
    private readonly int m_length;

    private static int SizeofT { get { return UnmagagedSizeOf<T>(); } }

    public readonly T this[int i]
    {
        get
        {
            return (T)Marshal.PtrToStructure(m_arrayPtr + i * SizeofT, typeof(T));
        }
        set
        {
            Marshal.StructureToPtr(value, m_arrayPtr + i * SizeofT, false);
        }
    }

    public UnmanagedArray(int length)
    {
        m_length = length;
        m_arrayPtr = Marshal.AllocHGlobal(SizeofT * length);
    }

    public UnmanagedArray(int length, IntPtr sourceArray, int sourceLength) : this(length)
    {
        UnsafeUtility.MemCpy(m_arrayPtr.ToPointer(), sourceArray.ToPointer(), Math.Min(SizeofT * m_length, sourceLength));
    }

    public static UnmanagedArray<T> FromArray(T[] array)
    {
        fixed (T* ptr = &array[0])
        {
            return new UnmanagedArray<T>(array.Length, new IntPtr(ptr), SizeofT * array.Length);
        }
    }

    public static UnmanagedArray<T> FromEnumerable(IEnumerable<T> enumerable)
    {
        T[] array = enumerable.ToArray();
        return FromArray(array);
    }

    public readonly void Dispose()
    {
        Marshal.FreeHGlobal(m_arrayPtr);
    }

    public readonly T[] ToArray()
    {
        T[] result = new T[m_length];
        fixed (T* ptr = &result[0])
        {
            UnsafeUtility.MemCpy(ptr, m_arrayPtr.ToPointer(), SizeofT * m_length);
        }
        return result;
    }

    public static unsafe int UnmagagedSizeOf<Type>() where Type : unmanaged
    {
        Span<Type> span0 = stackalloc Type[1];
        Span<Type> span1 = stackalloc Type[1];
        fixed (Type* ptr0 = span0)
        {
            fixed (Type* ptr1 = span1)
            {
                return (int)ptr1 - (int)ptr0;
            }
        }
    }
}