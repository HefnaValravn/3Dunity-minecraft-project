using Unity.Mathematics;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    // Public properties
    public int2 chunkCoordinate;
    public BlockType[,,] blocks;

    // Constants
    public const int CHUNK_SIZE_X = 32;
    public const int CHUNK_SIZE_Y = 128;
    public const int CHUNK_SIZE_Z = 32;

    [HideInInspector]
    public TerrainGenerator terrainGenerator;

    // Private fields
    private bool isInitialized = false;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    // Helper components
    private ChunkTerrain terrainGenerator_;
    private ChunkMeshGenerator meshGenerator;
    private ChunkPortalGenerator portalGenerator;

    private void Start()
    {
        // Only run if we haven't been explicitly initialized
        if (!isInitialized && terrainGenerator != null)
        {
            InitializeChunk();
            SetPosition();
            GenerateMesh();
        }
    }

    public void InitializeChunk()
    {
        // Verify we have a terrain generator
        if (terrainGenerator == null)
        {
            Debug.LogError("Terrain generator not assigned to chunk!");
            return;
        }

        // Initialize block array
        blocks = new BlockType[CHUNK_SIZE_X, CHUNK_SIZE_Y, CHUNK_SIZE_Z];

        // Initialize helpers
        terrainGenerator_ = new ChunkTerrain(blocks, terrainGenerator, chunkCoordinate, 
                                          CHUNK_SIZE_X, CHUNK_SIZE_Y, CHUNK_SIZE_Z);
        
        // Generate terrain blocks
        terrainGenerator_.GenerateTerrainBlocks();
        terrainGenerator_.GenerateCaves();

        // Get water level and convert grass to riverbed
        int waterLevel = GetWaterLevel();
        terrainGenerator_.ConvertGrassToRiverbed(waterLevel);

        // Generate portal
        portalGenerator = new ChunkPortalGenerator(blocks, terrainGenerator, chunkCoordinate, 
                                                 transform, CHUNK_SIZE_X, CHUNK_SIZE_Y, CHUNK_SIZE_Z);
        portalGenerator.GeneratePortal();
        portalGenerator.GeneratePortalPlane();

        isInitialized = true;
    }

    public void GenerateMesh()
    {
        // Only generate mesh if we have blocks initialized
        if (blocks == null)
        {
            Debug.LogError("Cannot generate mesh - blocks not initialized!");
            return;
        }

        // Add components if they don't exist
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Create mesh generator and generate mesh
        meshGenerator = new ChunkMeshGenerator(blocks, CHUNK_SIZE_X, CHUNK_SIZE_Y, CHUNK_SIZE_Z);
        Material[] materials;
        mesh = meshGenerator.GenerateMesh(out materials);
        
        // Assign mesh and materials
        meshFilter.mesh = mesh;
        meshRenderer.materials = materials;
    }

    public void SetPosition()
    {
        // Set the chunk's world position based on its chunk coordinates
        transform.position = new Vector3(chunkCoordinate.x * CHUNK_SIZE_X, 0, chunkCoordinate.y * CHUNK_SIZE_Z);
    }

    private int GetWaterLevel()
    {
        int waterLevel = 62; // Default value
        ChunkManager chunkManager = FindFirstObjectByType<ChunkManager>();
        if (chunkManager != null)
        {
            waterLevel = chunkManager.waterLevel;
        }
        return waterLevel;
    }
}