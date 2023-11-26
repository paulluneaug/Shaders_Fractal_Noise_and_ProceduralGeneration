
using static MeshStructs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile(CompileSynchronously = true)]
struct ChunkifyCellsJob : IJobFor
{
    [ReadOnly]
    private NativeArray<CellMesh> m_cells;

    [WriteOnly]
    public NativeArray<UnmanagedChunkMesh> GeneratedMeshes;

    public ChunkifyCellsJob(NativeArray<CellMesh> cells, NativeArray<UnmanagedChunkMesh> generatedMeshes)
    {
        m_cells = cells;
        GeneratedMeshes = generatedMeshes;
    }

    public void Execute(int chunkIndex)
    {
        //GeneratedMeshes[chunkIndex] = MarchingCubeGenerator.ChunkifyCellsForChunk<UnmanagedChunkMesh>(m_cells, chunkIndex);
    }
}