using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

using static Constants;

public class MarchingCubeController : MonoBehaviour
{
    private Vector3Int CellsToGenerateSize => m_chunkZoneSizeToGenerate * CHUNK_SIZE;

    [SerializeField] private MarchingCubeGenerator m_generator;
    [SerializeField] private Transform m_refPoint;

    [SerializeField] private Vector3Int m_chunkOffset = Vector3Int.zero;
    [SerializeField] private Vector3Int m_chunkZoneSizeToGenerate = Vector3Int.one * 4;

    [Header("Gizmos")]
    [SerializeField] private Axis m_axisToDrawChunkBorders = Axis.X | Axis.Y | Axis.Z;
    [SerializeField] private Color m_chunksBorderColor = Color.red;
    [SerializeField] private Axis m_axisToDrawCellBorders = Axis.X | Axis.Y | Axis.Z;
    [SerializeField] private Color m_cellsBorderColor = Color.yellow;

    [NonSerialized] private Dictionary<Vector3Int, Chunk> m_chunks = new Dictionary<Vector3Int, Chunk>();

    private void Start()
    {
    }

    [ContextMenu("Generate Zone")]
    private void GenerateZone()
    {
        m_generator.GenerateZone(m_chunkZoneSizeToGenerate, m_chunkOffset, OnGenerationComplete);
    }

    [ContextMenu("Clear")]
    private void Clear()
    {
        foreach (var chunk in m_chunks.Values)
        {
            Destroy(chunk.gameObject);
        }
        m_chunks.Clear();
    }



    private void OnGenerationComplete(Chunk[] generatedChunks)
    {
        foreach (Chunk chunk in generatedChunks)
        {
            if (!m_chunks.TryAdd(chunk.ChunkPosition, chunk))
            {
                Debug.LogError($"Chunk at position {chunk.ChunkPosition} was already generated");
            }
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3Int offset = m_chunkOffset * CHUNK_SIZE;
        Gizmos.color = m_cellsBorderColor;
        if ((m_axisToDrawCellBorders & Axis.X) == Axis.X)
        {
            for (int xy_x = 1; xy_x < CellsToGenerateSize.x; ++xy_x)
            {
                for (int xy_y = 1; xy_y < CellsToGenerateSize.y; ++xy_y)
                {
                    DrawGizmoLineInLocalSpace(offset + new Vector3(xy_x, xy_y, 0), offset + new Vector3(xy_x, xy_y, CellsToGenerateSize.z));
                }
            }

        }

        if ((m_axisToDrawCellBorders & Axis.Y) == Axis.Y)
        {
            for (int xz_x = 1; xz_x < CellsToGenerateSize.x; ++xz_x)
            {
                for (int xz_z = 1; xz_z < CellsToGenerateSize.z; ++xz_z)
                {
                    DrawGizmoLineInLocalSpace(offset + new Vector3(xz_x, 0, xz_z), offset + new Vector3(xz_x, CellsToGenerateSize.y, xz_z));
                }
            }

        }

        if ((m_axisToDrawCellBorders & Axis.Z) == Axis.Z)
        {
            for (int yz_y = 1; yz_y < CellsToGenerateSize.y; ++yz_y)
            {
                for (int yz_z = 1; yz_z < CellsToGenerateSize.z; ++yz_z)
                {
                    DrawGizmoLineInLocalSpace(offset + new Vector3(0, yz_y, yz_z), offset + new Vector3(CellsToGenerateSize.x, yz_y, yz_z));
                }
            }
        }

        Gizmos.color = m_chunksBorderColor;
        if ((m_axisToDrawChunkBorders & Axis.X) == Axis.X)
        {
            for (int xy_x = 0; xy_x < m_chunkZoneSizeToGenerate.x + 1; ++xy_x)
            {
                for (int xy_y = 0; xy_y < m_chunkZoneSizeToGenerate.y + 1; ++xy_y)
                {
                    DrawGizmoLineInLocalSpace(offset + new Vector3(xy_x, xy_y, 0) * CHUNK_SIZE, offset + new Vector3(xy_x, xy_y, m_chunkZoneSizeToGenerate.z) * CHUNK_SIZE);
                }
            }

        }

        if ((m_axisToDrawChunkBorders & Axis.Y) == Axis.Y)
        {
            for (int xz_x = 0; xz_x < m_chunkZoneSizeToGenerate.x + 1; ++xz_x)
            {
                for (int xz_z = 0; xz_z < m_chunkZoneSizeToGenerate.z + 1; ++xz_z)
                {
                    DrawGizmoLineInLocalSpace(offset + new Vector3(xz_x, 0, xz_z) * CHUNK_SIZE, offset + new Vector3(xz_x, m_chunkZoneSizeToGenerate.y, xz_z) * CHUNK_SIZE);
                }
            }

        }

        if ((m_axisToDrawChunkBorders & Axis.Z) == Axis.Z)
        {
            for (int yz_y = 0; yz_y < m_chunkZoneSizeToGenerate.y + 1; ++yz_y)
            {
                for (int yz_z = 0; yz_z < m_chunkZoneSizeToGenerate.z + 1; ++yz_z)
                {
                    DrawGizmoLineInLocalSpace(offset + new Vector3(0, yz_y, yz_z) * CHUNK_SIZE, offset + new Vector3(m_chunkZoneSizeToGenerate.x, yz_y, yz_z) * CHUNK_SIZE);
                }
            }
        }
    }

    private void DrawGizmoLineInLocalSpace(Vector3 from, Vector3 to)
    {
        Gizmos.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to));
    }
#endif

}
