using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ChunkManager : MonoBehaviour
{
    public Transform player;
    public Camera firstPlayerCamera;
    public Camera thirdPlayerCamera;
    private Camera activeCamera;
    public int viewDistance = 5; // Number of chunks visible around the player (2 means a 5x5 grid)
    //this is because there are 2 to the player's left side, 2 to the right side, and the one the player is on
    //which makes a 5x5 grid when viewDistance = 2, or a 7x7 grid when viewDistance = 3
    //or a 11x11 grid if viewDistance = 5
    public bool prioritizeViewDirection = true; //prioritize rendering chunks within view

    [Header("Terrain Settings")]
    public TerrainGenerator terrainGenerator;
    private Dictionary<int2, Chunk> activeChunks = new Dictionary<int2, Chunk>();
    private Queue<Chunk> chunkPool = new Queue<Chunk>(); // Stores reusable chunks

    private int chunkSize = 32;
    private List<int2> prioritizedChunks = new List<int2>(); //direction based loading


    [Header("Water Settings")]
    public Material waterMaterial;
    public int waterLevel = 50;
    public int waterTesselation = 8;
    private Dictionary<int2, GameObject> waterObjects = new Dictionary<int2, GameObject>();
    public Cubemap skybox;


    [Header("Performance Settings")]
    public int chunksPerFrame = 3;
    public float generationDelay = 0.02f;
    private Queue<int2> chunkGenerationQueue = new Queue<int2>();
    private HashSet<int2> queuedChunks = new HashSet<int2>(); // Track what's already queued
    private bool isGeneratingChunks = false;
    private System.Collections.IEnumerator currentGenerationRoutine;
    public bool useOcclusionCulling = true;


    // Cache frequently accessed values
    private float viewDistanceSquaredCache;
    private float maxViewDistSqrCache;
    private int2 lastPlayerChunkCoord;
    private bool playerChunkChanged = true;




    void Start()
    {
        // Create terrain generator if not assigned
        if (terrainGenerator == null)
        {
            terrainGenerator = gameObject.AddComponent<TerrainGenerator>();
            Debug.Log("TerrainGenerator component created");
        }

        // Set chunk size from Chunk constants
        chunkSize = Chunk.CHUNK_SIZE_X;
        lastPlayerChunkCoord = GetChunkCoord(player.position);
        UpdateCachedValues();
        UpdateActiveCamera();
        UpdateChunks();
    }

    void Update()
    {
        UpdateActiveCamera();
        UpdateChunks();
    }

    private void UpdateActiveCamera()
    {
        // Determine which camera is currently active in the scene
        if (firstPlayerCamera.gameObject.activeInHierarchy)
        {
            activeCamera = firstPlayerCamera;
        }
        else if (thirdPlayerCamera.gameObject.activeInHierarchy)
        {
            activeCamera = thirdPlayerCamera;
        }
        else
        {
            // Fallback to main camera if neither is active
            activeCamera = Camera.main;
            Debug.LogWarning("Neither first-person nor third-person camera is active. Using Camera.main as fallback.");
        }
    }

    private void LoadWater(int2 coord)
    {
        // Check if water already exists at this coordinate
        if (waterObjects.ContainsKey(coord))
            return;

        // Create water GameObject
        GameObject waterObject = new GameObject($"Water {coord.x}, {coord.y}");
        waterObject.transform.parent = transform;

        // Add WaterGenerator component
        WaterGenerator waterGenerator = waterObject.AddComponent<WaterGenerator>();
        waterGenerator.properSkybox = skybox;
        waterGenerator.waterMaterial = waterMaterial;
        waterGenerator.waterLevel = waterLevel;

        // Initialize water
        Vector3 chunkPosition = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        waterGenerator.Initialize(chunkPosition, chunkSize, chunkSize, waterTesselation, waterTesselation);

        // Store reference to water object
        waterObjects[coord] = waterObject;
    }

    private void UnloadWater(int2 coord)
    {
        if (waterObjects.TryGetValue(coord, out GameObject waterObject))
        {
            Destroy(waterObject);
            waterObjects.Remove(coord);
        }
    }

    private void UpdateCachedValues()
    {
        viewDistanceSquaredCache = viewDistance * viewDistance;
        maxViewDistSqrCache = viewDistance * viewDistance * chunkSize * chunkSize;
    }

    private void UpdateChunks()
    {
        int2 playerChunkCoord = GetChunkCoord(player.position);
        
        // Check if player moved to a different chunk
        if (!playerChunkCoord.Equals(lastPlayerChunkCoord))
        {
            lastPlayerChunkCoord = playerChunkCoord;
            playerChunkChanged = true;
        }
        
        // Only do expensive chunk calculations if player moved chunks
        if (!playerChunkChanged && chunkGenerationQueue.Count == 0)
            return;
            
        playerChunkChanged = false;

        HashSet<int2> requiredChunks = GetRequiredChunks(playerChunkCoord);

        // Find all chunks that need to be loaded
        List<int2> chunksToLoad = new List<int2>();
        foreach (int2 coord in requiredChunks)
        {
            if (!activeChunks.ContainsKey(coord) && IsChunkVisible(coord.x, coord.y))
            {
                chunksToLoad.Add(coord);
            }
        }

        if (prioritizeViewDirection && chunksToLoad.Count > 0)
        {
            PrioritizeChunksByViewDirection(chunksToLoad);

            // Queue the prioritized chunks
            foreach (int2 coord in prioritizedChunks)
            {
                if (!queuedChunks.Contains(coord))
                {
                    chunkGenerationQueue.Enqueue(coord);
                    queuedChunks.Add(coord);
                }
            }
        }
        else
        {
            // Queue all chunks that need to be loaded
            foreach (int2 coord in chunksToLoad)
            {
                if (!queuedChunks.Contains(coord))
                {
                    chunkGenerationQueue.Enqueue(coord);
                    queuedChunks.Add(coord);
                }
            }
        }

        // Start the chunk generation coroutine if it's not already running
        if (!isGeneratingChunks && chunkGenerationQueue.Count > 0)
        {
            currentGenerationRoutine = GenerateChunksOverTime();
            StartCoroutine(currentGenerationRoutine);
        }

        // Remove chunks that are no longer needed - one at a time to avoid lag spikes
        List<int2> chunksToRemove = new List<int2>();
        foreach (var coord in activeChunks.Keys)
        {
            if (!requiredChunks.Contains(coord))
            {
                chunksToRemove.Add(coord);
                break; // Just remove one per frame to reduce lag
            }
        }

        foreach (int2 coord in chunksToRemove)
        {
            UnloadChunk(coord);
        }
    }

    private System.Collections.IEnumerator GenerateChunksOverTime()
    {
        isGeneratingChunks = true;

        while (chunkGenerationQueue.Count > 0)
        {
            // Process a limited number of chunks per frame
            int chunksToProcess = Mathf.Min(chunksPerFrame, chunkGenerationQueue.Count);

            for (int i = 0; i < chunksToProcess; i++)
            {
                int2 coord = chunkGenerationQueue.Dequeue();
                queuedChunks.Remove(coord); // Remove from tracking set

                // Skip if the chunk has already been created (could happen if player moves back and forth)
                if (activeChunks.ContainsKey(coord))
                    continue;

                // Load the chunk
                LoadChunkImmediate(coord);

                // Wait a tiny bit to spread CPU usage within the frame
                yield return null;
            }

            // Wait for the next frame or specified delay
            if (generationDelay > 0)
                yield return new WaitForSeconds(generationDelay);
            else
                yield return null;
        }

        isGeneratingChunks = false;
    }


    private void PrioritizeChunksByViewDirection(List<int2> chunksToLoad)
    {
        // Clear the previous prioritized list
        prioritizedChunks.Clear();

        // Add all required chunks to the prioritized list
        prioritizedChunks.AddRange(chunksToLoad);

        // Get view direction as a 2D vector (xz plane)
        Vector3 viewDir = activeCamera.transform.forward;
        Vector2 viewDir2D = new Vector2(viewDir.x, viewDir.z).normalized;

        // Get player position
        Vector3 playerPos = player.position;

        // Sort chunks by relevance to the player's view direction
        prioritizedChunks.Sort((a, b) =>
        {
            // Calculate center positions of chunks
            Vector2 aCenter = new Vector2((a.x * chunkSize) + (chunkSize / 2), (a.y * chunkSize) + (chunkSize / 2));
            Vector2 bCenter = new Vector2((b.x * chunkSize) + (chunkSize / 2), (b.y * chunkSize) + (chunkSize / 2));

            // Calculate direction vectors from player to chunk centers
            Vector2 aDir = new Vector2(aCenter.x - playerPos.x, aCenter.y - playerPos.z).normalized;
            Vector2 bDir = new Vector2(bCenter.x - playerPos.x, bCenter.y - playerPos.z).normalized;

            // Calculate dot product to determine alignment with view direction
            float aDot = Vector2.Dot(viewDir2D, aDir);
            float bDot = Vector2.Dot(viewDir2D, bDir);

            // Sort by dot product (higher means more aligned with view direction)
            return bDot.CompareTo(aDot);
        });
    }



    private bool IsChunkVisible(int chunkX, int chunkZ)
    {
        // First, do a distance check using cached value (very fast)
        Vector3 chunkCenter = new Vector3(
            chunkX * Chunk.CHUNK_SIZE_X + Chunk.CHUNK_SIZE_X / 2,
            Chunk.CHUNK_SIZE_Y / 2,
            chunkZ * Chunk.CHUNK_SIZE_Z + Chunk.CHUNK_SIZE_Z / 2
        );

        float distanceSqr = (chunkCenter - player.position).sqrMagnitude;

        if (distanceSqr > maxViewDistSqrCache)
            return false;

        // Next, check frustum planes (a bit more expensive)
        Bounds chunkBounds = new Bounds(chunkCenter,
            new Vector3(Chunk.CHUNK_SIZE_X, Chunk.CHUNK_SIZE_Y, Chunk.CHUNK_SIZE_Z));

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(activeCamera);
        if (!GeometryUtility.TestPlanesAABB(planes, chunkBounds))
            return false;

        // Finally, check occlusion if enabled (most expensive)
        if (useOcclusionCulling && distanceSqr > chunkSize * chunkSize * 3)
        {
            Vector3 dirToChunk = (chunkCenter - player.position).normalized;
            if (Physics.Raycast(player.position, dirToChunk, out RaycastHit hit, Mathf.Sqrt(distanceSqr) - chunkSize))
            {
                // Something is blocking the view to this chunk
                return false;
            }
        }

        return true;
    }


    // Rename the current LoadChunk to LoadChunkImmediate
    private void LoadChunkImmediate(int2 coord)
    {
        Chunk chunk;

        if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Dequeue(); // Reuse old chunk
            chunk.gameObject.SetActive(true);
            chunk.chunkCoordinate = coord;
        }
        else
        {
            GameObject chunkObject = new GameObject($"Chunk {coord.x}, {coord.y}");
            chunkObject.transform.parent = transform;
            chunk = chunkObject.AddComponent<Chunk>();
            chunk.chunkCoordinate = coord;
        }

        chunk.terrainGenerator = terrainGenerator;

        // Start a coroutine for multi-phase chunk generation
        StartCoroutine(GenerateChunkPhases(chunk, coord));
    }

    // Add a multi-phase chunk generation coroutine
    private System.Collections.IEnumerator GenerateChunkPhases(Chunk chunk, int2 coord)
    {
        // Phase 1: Initialize terrain data
        chunk.InitializeChunk();
        chunk.SetPosition();
        activeChunks[coord] = chunk;
        
        yield return null; // Wait one frame

        // Phase 2: Generate mesh
        chunk.GenerateMesh();
        yield return null; // Wait another frame

        // Phase 3: Generate water
        LoadWater(coord);
    }

    private void UnloadChunk(int2 coord)
    {
        if (activeChunks.TryGetValue(coord, out Chunk chunk))
        {
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
            activeChunks.Remove(coord);
        }

        UnloadWater(coord);
    }

    private int2 GetChunkCoord(Vector3 position)
    {
        return new int2(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.z / chunkSize));
    }

    private HashSet<int2> GetRequiredChunks(int2 playerChunk)
    {
        HashSet<int2> requiredChunks = new HashSet<int2>();

        // Use cached squared view distance for faster distance calculations
        // Check all chunks in a square boundary and filter by circular distance
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                // Calculate squared distance from player's chunk
                float distanceSquared = x * x + z * z;

                // Only include chunks within the circular boundary using cached value
                if (distanceSquared <= viewDistanceSquaredCache)
                {
                    requiredChunks.Add(new int2(playerChunk.x + x, playerChunk.y + z));
                }
            }
        }

        return requiredChunks;
    }

    void OnValidate()
    {
        // Update cached values when view distance is changed in inspector
        if (Application.isPlaying)
        {
            UpdateCachedValues();
        }
    }
}