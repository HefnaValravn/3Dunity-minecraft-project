using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class ChunkManager : MonoBehaviour
{
    public Transform player;
    public int viewDistance = 5; // Number of chunks visible around the player (2 means a 5x5 grid)
    //this is because there are 2 to the player's left side, 2 to the right side, and the one the player is on
    //which makes a 5x5 grid when viewDistance = 2, or a 7x7 grid when viewDistance = 3
    //or a 11x11 grid if viewDistance = 5
    private Dictionary<int2, Chunk> activeChunks = new Dictionary<int2, Chunk>();
    private Queue<Chunk> chunkPool = new Queue<Chunk>(); // Stores reusable chunks

    private int chunkSize = 32;

    void Start()
    {
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

        // Reuse or create new chunks if necessary
        foreach (int2 coord in requiredChunks)
        {
            if (!activeChunks.ContainsKey(coord) && IsChunkVisible(coord.x, coord.y))
            {
                LoadChunk(coord);
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

    private bool IsChunkVisible(int chunkX, int chunkZ)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Bounds chunkBounds = new Bounds(new Vector3(chunkX * 32 + 16, 64, chunkZ * 32 + 16), new Vector3(32, 128, 32));

        return GeometryUtility.TestPlanesAABB(planes, chunkBounds);
    }


    private void LoadChunk(int2 coord)
    {
        Chunk chunk;

        if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Dequeue(); // Reuse old chunk
            chunk.gameObject.SetActive(true);
        }
        else
        {
            GameObject chunkObject = new GameObject($"Chunk {coord.x}, {coord.y}");
            chunk = chunkObject.AddComponent<Chunk>();
        }

        chunk.chunkCoordinate = coord;
        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunk.InitializeChunk();
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
