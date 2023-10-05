using System.Collections;
using System.Collections.Generic;
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
        return 
            localCoords.x +
            localCoords.y * m_chunkSize + 
            localCoords.z * m_chunkSize * m_chunkSize;
    }
}
