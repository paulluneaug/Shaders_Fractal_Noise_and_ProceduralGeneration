using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

using static Constants;

public class MarchingCubeController : MonoBehaviour
{
    [SerializeField] private MarchingCubeGenerator m_generator;
    [SerializeField] private Transform m_refPoint;

    // Cache
    [NonSerialized] private Vector3Int m_lastFrameContainingSuperChunk = Vector3Int.zero;

    [NonSerialized] private Dictionary<Vector3Int, SuperChunk> m_superChunks = new Dictionary<Vector3Int, SuperChunk>();
    [NonSerialized] private bool m_isGenerating = false;
    [NonSerialized] private Vector3Int? m_chunkBeingGenerated = null;
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

        Vector3Int superChunkPos = new Vector3Int(
            (int)refPointPosition.x / SUPER_CHUNK_SIZE,
            (int)refPointPosition.y / SUPER_CHUNK_SIZE,
            (int)refPointPosition.z / SUPER_CHUNK_SIZE);

        return new Vector3Int(
            Math.Sign(refPointPosition.x) == -1 ? superChunkPos.x - 1 : superChunkPos.x,
            Math.Sign(refPointPosition.y) == -1 ? superChunkPos.y - 1 : superChunkPos.y,
            Math.Sign(refPointPosition.z) == -1 ? superChunkPos.z - 1 : superChunkPos.z);
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
            m_chunkBeingGenerated = null;
            OnSuperChunkGenerationComplete(superChunk);
            GenerateNextSuperChunk();
        }

        m_generator.GenerateZone(Vector3Int.one * SUPER_CHUNK_CHUNK_SIZE, superChunkPosition * SUPER_CHUNK_CHUNK_SIZE, onGenerationComplete);
    }

    private void AddNewSuperChunkToGenerate(Vector3Int position)
    {
        if (m_chunkBeingGenerated.HasValue && m_chunkBeingGenerated.Value == position)
        {
            return;
        }
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
        m_chunkBeingGenerated = m_generationQueue.Dequeue();
        GenerateSuperChunk(m_chunkBeingGenerated.Value);
    }

    private void OnSuperChunkGenerationComplete(SuperChunk generatedSuperChunk)
    {
        if (!m_superChunks.TryAdd(generatedSuperChunk.SuperChunkPosition, generatedSuperChunk))
        {
            Debug.LogError($"Chunk at position {generatedSuperChunk.SuperChunkPosition} was already generated");
        }
    }
}
