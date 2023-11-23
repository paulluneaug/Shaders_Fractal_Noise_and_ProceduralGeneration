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

    [NonSerialized] private Vector3Int m_lastFrameContainingSuperChunk = Vector3Int.zero;

    [NonSerialized] private Dictionary<Vector3Int, SuperChunk> m_superChunks = new Dictionary<Vector3Int, SuperChunk>();
    [NonSerialized] private bool m_isGenerating = false;
    [NonSerialized] private Queue<Vector3Int> m_generationQueue = new Queue<Vector3Int>();
     
    private void Start()
    {
        m_lastFrameContainingSuperChunk = GetRefPointContainingSuperChunk();
        IEnumerable<Vector3Int> startSuperChunks = GetSuperChunksToActivate(m_lastFrameContainingSuperChunk);
        foreach(Vector3Int chunkPos in startSuperChunks)
        {
            AddNewSuperChunkToGenerate(chunkPos);
        }
    }

    private void Update()
    {
        Vector3Int currentContainingSuperChunk = GetRefPointContainingSuperChunk();
        if (currentContainingSuperChunk != m_lastFrameContainingSuperChunk)
        {
            DisableAllSuperChunks();

            IEnumerable<Vector3Int> startSuperChunks = GetSuperChunksToActivate(currentContainingSuperChunk);
            foreach (Vector3Int chunkPos in startSuperChunks)
            {
                if (m_superChunks.TryGetValue(chunkPos, out SuperChunk superChunk))
                {
                    superChunk.gameObject.SetActive(true);
                }
                else
                {
                    AddNewSuperChunkToGenerate(chunkPos);
                }
            }
            m_lastFrameContainingSuperChunk = currentContainingSuperChunk;
        }
    }

    private void DisableAllSuperChunks()
    {
        foreach (SuperChunk superChunk in m_superChunks.Values)
        {
            superChunk.gameObject.SetActive(false);
        }
    }

    private Vector3Int GetRefPointContainingSuperChunk()
    {
        Vector3 refPointPosition = m_refPoint.position;
        refPointPosition = new Vector3(
            Math.Sign(refPointPosition.x) == -1 ? refPointPosition.x - 1 : refPointPosition.x,
            Math.Sign(refPointPosition.y) == -1 ? refPointPosition.y - 1 : refPointPosition.y,
            Math.Sign(refPointPosition.z) == -1 ? refPointPosition.z - 1 : refPointPosition.z);


        return new Vector3Int(
            (int)refPointPosition.x / SUPER_CHUNK_SIZE,
            (int)refPointPosition.y / SUPER_CHUNK_SIZE,
            (int)refPointPosition.z / SUPER_CHUNK_SIZE);
    }

    private IEnumerable<Vector3Int> GetSuperChunksToActivate(Vector3Int containingSuperChunk)
    {
        for (int x = -1; x <= 1; ++x)
        {
            for (int y = -1; y <= 1; ++y)
            {
                for (int z = -1; z <= 1; ++z)
                {
                    yield return containingSuperChunk + new Vector3Int(x, y, z);
                }
            }
        }
    }

    private void GenerateSuperChunk(Vector3Int superChunkPosition)
    {
        m_isGenerating = true;

        GameObject superGo = new GameObject($"SuperChunk_{superChunkPosition}");
        Transform superTransform = superGo.transform;
        superTransform.position = superChunkPosition * CHUNK_SIZE * SUPER_CHUNK_CHUNK_SIZE;
        superTransform.parent = transform;

        SuperChunk superChunk = superGo.AddComponent<SuperChunk>();
        superChunk.SuperChunkPosition = superChunkPosition;

        void onGenerationComplete(Chunk[] chunks)
        {
            superChunk.SetChunks(chunks);
            m_isGenerating = false;
            OnSuperChunkGenerationComplete(superChunk);
            GenerateNextSuperChunk();
        }

        m_generator.GenerateZone(Vector3Int.one * SUPER_CHUNK_CHUNK_SIZE, superChunkPosition * SUPER_CHUNK_CHUNK_SIZE, onGenerationComplete);
    }

    private void AddNewSuperChunkToGenerate(Vector3Int position)
    {
        if (m_generationQueue.Contains(position))
        {
            return;
        }
        m_generationQueue.Enqueue(position);
        if (!m_isGenerating)
        {
            GenerateNextSuperChunk();
        }
    }

    private void GenerateNextSuperChunk()
    {
        if (m_generationQueue.Count == 0)
        {
            return;
        }
        GenerateSuperChunk(m_generationQueue.Dequeue());
    }

    private void OnSuperChunkGenerationComplete(SuperChunk generatedSuperChunk)
    {
        if (!m_superChunks.TryAdd(generatedSuperChunk.SuperChunkPosition, generatedSuperChunk))
        {
            Debug.LogError($"Chunk at position {generatedSuperChunk.SuperChunkPosition} was already generated");
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
