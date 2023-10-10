using UnityEngine;

public class ChunkifierUtils
{
    public int ChunkVolume => m_chunkVolume;

    private int m_chunkSize;
    private int m_chunkVolume;
    private (int x, int y, int z) m_chunkSpan;

    public ChunkifierUtils(int chunkSize, (int x, int y, int z) chunkSpan)
    {
        m_chunkSize = chunkSize;
        m_chunkSpan = chunkSpan;
        m_chunkVolume = chunkSize * chunkSize * chunkSize;
    }
    public static int NextMultipleOf(int n, int m)
    {
        return n + ((n % m != 0) ? 1 : 0) * m;
    }

    public static int NextPowerOfTwoExposant(int n)
    {
        int shift = 0;
        bool a = false;
        bool isPowerOfTwo = n > 0;
        while (n != 0)
        {
            if (a)
            {
                isPowerOfTwo = false;
            }
            a = (n & 1) == 1;


            shift += 1;
            n >>= 1;
        }
        return shift - (isPowerOfTwo ? 1 : 0);
    }

    public int CoordinatesToIndex((int x, int y, int z) coords)
    {
        int chunkIndex = CoordinatesToChunkIndex(coords);
        int chunkOffset = chunkIndex * m_chunkSize * m_chunkSize * m_chunkSize;
        (int lx, int ly, int lz) = CoordinatesToLocalCoordinates(coords);

        return chunkOffset + LocalCoordinatesToLocalOffset((lx, ly, lz));
    }

    //private Vector3Int IndexToCoordinates(int index)
    //{
    //    int z = index / (m_datasSize.x * m_datasSize.y);
    //    int indexY = index % (m_datasSize.x * m_datasSize.y);
    //    int y = indexY / m_datasSize.x;
    //    int x = indexY % m_datasSize.x;
    //    return new Vector3Int(x, y, z);
    //}

    public int CoordinatesToChunkIndex((int x, int y, int z) coords)
    {
        Vector3Int chunkOrigin = new Vector3Int(
            coords.x / m_chunkSize,
            coords.y / m_chunkSize,
            coords.z / m_chunkSize);

        return chunkOrigin.x + chunkOrigin.y * m_chunkSpan.x + chunkOrigin.z * m_chunkSpan.x * m_chunkSpan.y;
    }

    public (int lx, int ly, int lz) CoordinatesToLocalCoordinates((int x, int y, int z) coords)
    {
        return (
            coords.x % m_chunkSize,
            coords.y % m_chunkSize,
            coords.z % m_chunkSize);
    }

    public int LocalCoordinatesToLocalOffset((int x, int y, int z) localCoords)
    {
        int cubeDim = NextPowerOfTwoExposant(Mathf.Max(localCoords.x, localCoords.y, localCoords.z) + 1);
        int offset = 0;
        (int x, int y, int z) savedCoords = localCoords;
        while (cubeDim > 0)
        {
            ((int x, int y, int z) localCoordinates, (int x, int y, int z) remainder) = CoordinatesToLocalCoordinatesInCubeOfDim(localCoords, cubeDim);
            localCoords = remainder;
            int offsetInUnitCube = CoordinatesToOffsetInUnitCube(localCoordinates);
            int addedOffset = offsetInUnitCube * (1 << ((cubeDim - 1) * 3));
            Debug.Log($"Cube at {savedCoords.CoordsToString()} <=> {localCoordinates.CoordsToString()} in cube of dim {cubeDim} | Offset In Unit cube = {offsetInUnitCube} | Added Offset = {addedOffset}");
            offset += addedOffset;
            --cubeDim;
        }
        Debug.Log($"Cube at {savedCoords.CoordsToString()} => Offset : {offset} (CubeDim = {(Mathf.Max(savedCoords.x, savedCoords.y, savedCoords.z) + 1)}.NextPowerOfTwoExposant() = {NextPowerOfTwoExposant(Mathf.Max(savedCoords.x, savedCoords.y, savedCoords.z) + 1)}");
        return offset;
    }

    public (int x, int y, int z) LocalOffsetToLocalCoordinates(int offset)
    {
        int x = 0;
        int y = 0;
        int z = 0;

        int pow = NextMultipleOf(NextPowerOfTwoExposant(offset), 3) / 3;
        while (pow-- >= 0)
        {
            (int localOffset, int remainder) = OffsetToLocalOffsetInCubeOfDim(offset, pow);
            (int x, int y, int z) coordsInUnitCube = OffsetToCoordinatesInUnitCube(localOffset);
            x += coordsInUnitCube.x * (1 << pow);
            y += coordsInUnitCube.y * (1 << pow);
            z += coordsInUnitCube.z * (1 << pow);
            offset = remainder;
        }
        return (x, y, z);
    }

    private (int localOffset, int remainder) OffsetToLocalOffsetInCubeOfDim(int offset, int dim)
    {
        int cubeVolume = 1 << (3 * dim);
        return (offset / cubeVolume, offset % cubeVolume);
    }

    private ((int x, int y, int z) localCoordinates, (int x, int y, int z) remainder) CoordinatesToLocalCoordinatesInCubeOfDim((int x, int y, int z) coordinates, int dim)
    {
        int div = 1 << (dim - 1);
        return (
            (coordinates.x / div, coordinates.y / div, coordinates.z / div),
            (coordinates.x % div, coordinates.y % div, coordinates.z % div));
    }

    private (int x, int y, int z) OffsetToCoordinatesInUnitCube(int offset)
    {
        int z = offset / 4;
        offset %= 4;
        return (offset % 2, offset / 2, z);
    }

    private int CoordinatesToOffsetInUnitCube((int x, int y, int z) coordinates)
    {
        return coordinates.x + coordinates.y * 2 + coordinates.z * 4;
    }
}

public static class IntTupleExtension
{

    public static string CoordsToString(this (int x, int y, int z) coords)
    {
        return $"({coords.x}, {coords.y}, {coords.z})";
    }
}
