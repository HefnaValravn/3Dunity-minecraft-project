using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ChunkManager : MonoBehaviour
{
    public Transform player;
    public Camera playerCamera;
    public int viewDistance = 3; // Number of chunks visible around the player (2 means a 5x5 grid)
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
        UpdateChunks();
    }

    void Update()
    {
        UpdateChunks();
    }

    private void UpdateChunks()
    {
        int2 playerChunkCoord = GetChunkCoord(player.position);
        HashSet<int2> requiredChunks = GetRequiredChunks(playerChunkCoord);



        if (prioritizeViewDirection)
        {
            PrioritizeChunksByViewDirection(requiredChunks);
            // Process the prioritized chunks
            foreach (int2 coord in prioritizedChunks)
            {
                if (!activeChunks.ContainsKey(coord) && IsChunkVisible(coord.x, coord.y))
                {
                    LoadChunk(coord);
                }
            }
        }
        else
        {
            // Reuse or create new chunks if necessary
            foreach (int2 coord in requiredChunks)
            {
                if (!activeChunks.ContainsKey(coord) && IsChunkVisible(coord.x, coord.y))
                {
                    LoadChunk(coord);
                }
            }
        }

        // Remove chunks that are no longer needed
        List<int2> chunksToRemove = new List<int2>();

        foreach (var coord in activeChunks.Keys)
        {
            if (!requiredChunks.Contains(coord))
            {
                chunksToRemove.Add(coord);
            }
        }

        foreach (int2 coord in chunksToRemove)
        {
            UnloadChunk(coord);
        }
    }


    private void PrioritizeChunksByViewDirection(HashSet<int2> requiredChunks)
    {
         // Clear the previous prioritized list
        prioritizedChunks.Clear();
        
        // Add all required chunks to the prioritized list
        prioritizedChunks.AddRange(requiredChunks);
        
        // Get view direction as a 2D vector (xz plane)
        Vector3 viewDir = playerCamera.transform.forward;
        Vector2 viewDir2D = new Vector2(viewDir.x, viewDir.z).normalized;
        
        // Get player position
        Vector3 playerPos = player.position;
        
        // Sort chunks by relevance to the player's view direction
        prioritizedChunks.Sort((a, b) => {
            // Calculate center positions of chunks
            Vector2 aCenter = new Vector2((a.x * chunkSize) + (chunkSize/2), (a.y * chunkSize) + (chunkSize/2));
            Vector2 bCenter = new Vector2((b.x * chunkSize) + (chunkSize/2), (b.y * chunkSize) + (chunkSize/2));
            
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
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Bounds chunkBounds = new Bounds(new Vector3(chunkX * Chunk.CHUNK_SIZE_X + Chunk.CHUNK_SIZE_X/2, 
                      Chunk.CHUNK_SIZE_Y/2, 
                      chunkZ * Chunk.CHUNK_SIZE_Z + Chunk.CHUNK_SIZE_Z/2), 
            new Vector3(Chunk.CHUNK_SIZE_X, Chunk.CHUNK_SIZE_Y, Chunk.CHUNK_SIZE_Z)
        );

        return GeometryUtility.TestPlanesAABB(planes, chunkBounds);
    }


    private void LoadChunk(int2 coord)
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
        chunk.InitializeChunk();
        chunk.SetPosition();
        chunk.GenerateMesh();
        activeChunks[coord] = chunk;
    }

    private void UnloadChunk(int2 coord)
    {
        if (activeChunks.TryGetValue(coord, out Chunk chunk))
        {
            chunk.gameObject.SetActive(false);
            chunkPool.Enqueue(chunk);
            activeChunks.Remove(coord);
        }
    }

    private int2 GetChunkCoord(Vector3 position)
    {
        return new int2(Mathf.FloorToInt(position.x / chunkSize), Mathf.FloorToInt(position.z / chunkSize));
    }

    private HashSet<int2> GetRequiredChunks(int2 playerChunk)
    {
        HashSet<int2> requiredChunks = new HashSet<int2>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int y = -viewDistance; y <= viewDistance; y++)
            {
                requiredChunks.Add(new int2(playerChunk.x + x, playerChunk.y + y));
            }
        }

        return requiredChunks;
    }
}
