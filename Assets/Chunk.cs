using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Vector3Int ChunkPosition;
}

public class SuperChunk : MonoBehaviour
{
    public Vector3Int SuperChunkPosition;
    private Dictionary<Vector3Int, Chunk> m_chunks;

    private void Awake()
    {
        m_chunks = new Dictionary<Vector3Int, Chunk>();
    }

    public void SetChunks(Chunk[] chunks)
    {
        foreach (Chunk chunk in chunks)
        {
            m_chunks.Add(Mod(chunk.ChunkPosition, Constants.SUPER_CHUNK_CHUNK_SIZE), chunk);
            chunk.transform.parent = transform;
        }
    }

    public Vector3Int Mod(Vector3Int v, int mod)
    {
        return new Vector3Int(v.x % mod, v.y % mod, v.z % mod);
    }
}
