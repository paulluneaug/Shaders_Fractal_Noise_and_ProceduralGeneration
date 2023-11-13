using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct FixedSizeArray<TVal>
{
    [SerializeField] private TVal m_val0;
    [SerializeField] private TVal m_val1;
    [SerializeField] private TVal m_val2;
    [SerializeField] private TVal m_val3;
    [SerializeField] private TVal m_val4;
    [SerializeField] private TVal m_val5;
    [SerializeField] private TVal m_val6;
    [SerializeField] private TVal m_val7;
    [SerializeField] private TVal m_val8;
    [SerializeField] private TVal m_val9;
    [SerializeField] private TVal m_val10;
    [SerializeField] private TVal m_val11;


    public TVal this[int i]
    {
        get { return GetValue(i); }
        set { SetValue(i, value); }
    }

    public TVal[] GetArray()
    {
        return new TVal[12]
        {
                m_val0,
                m_val1,
                m_val2,
                m_val3,
                m_val4,
                m_val5,
                m_val6,
                m_val7,
                m_val8,
                m_val9,
                m_val10,
                m_val11
        };
    }

    private TVal GetValue(int i)
    {
        return i switch
        {
            0 => m_val0,
            1 => m_val1,
            2 => m_val2,
            3 => m_val3,
            4 => m_val4,
            5 => m_val5,
            6 => m_val6,
            7 => m_val7,
            8 => m_val8,
            9 => m_val9,
            10 => m_val10,
            11 => m_val11,
            _ => throw new IndexOutOfRangeException(),
        };
    }

    private void SetValue(int i, TVal val)
    {
        switch (i)
        {
            case 0:
                m_val0 = val;
                break;
            case 1:
                m_val1 = val;
                break;
            case 2:
                m_val2 = val;
                break;
            case 3:
                m_val3 = val;
                break;
            case 4:
                m_val4 = val;
                break;
            case 5:
                m_val5 = val;
                break;
            case 6:
                m_val6 = val;
                break;
            case 7:
                m_val7 = val;
                break;
            case 8:
                m_val8 = val;
                break;
            case 9:
                m_val9 = val;
                break;
            case 10:
                m_val10 = val;
                break;
            case 11:
                m_val11 = val;
                break;
            default: throw new IndexOutOfRangeException();
        }
    }
}
