using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;


public class Chunk : MonoBehaviour
{
    public int2 chunkCoordinate;
    public BlockType[,,] blocks;

    //constants for better accessing
    public const int CHUNK_SIZE_X = 32;
    public const int CHUNK_SIZE_Y = 128;
    public const int CHUNK_SIZE_Z = 32;


    [HideInInspector]
    public TerrainGenerator terrainGenerator;

    private bool isInitialized = false;

    private MeshFilter meshFilter; //holds the geometry of the mesh
    private MeshRenderer meshRenderer; //renders the mesh
    private Mesh mesh; //the mesh itself lol
    private Mesh portalMesh; //mesh for the portal blocks

    private Material bedrockMaterial;
    private Material stoneMaterial;
    private Material dirtMaterial;
    private Material grassSideMaterial;
    private Material grassTopMaterial;
    private Material obsidianMaterial;
    private Material portalCoreMaterial;

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
        blocks = new BlockType[CHUNK_SIZE_X, CHUNK_SIZE_Y, CHUNK_SIZE_Z]; // This defines the size of the chunk




        // create a 32x32 grid of bedrock blocks
        for (int x = 0; x < CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < CHUNK_SIZE_Z; z++)
            {

                float worldX = x + chunkCoordinate.x * CHUNK_SIZE_X;
                float worldZ = z + chunkCoordinate.y * CHUNK_SIZE_Z;

                int terrainHeight = terrainGenerator.GetTerrainHeight(worldX, worldZ);
                int bedrockHeight = terrainGenerator.GetBedrockHeight(worldX, worldZ);

                // these two are part of the mechanism to "smoothen" the grass pattern at the top (make the surface "flatter")
                bool grassLayerPlaced = false; // did we add the grass layer?
                bool hasGrass = false; // is there grass?

                for (int y = 0; y <= terrainHeight; y++)
                {
                    if (y < bedrockHeight)
                    {
                        blocks[x, y, z] = BlockType.Bedrock; // Place bedrock based on weighted depth
                    }
                    else if (y < terrainHeight - 4)
                    {
                        blocks[x, y, z] = BlockType.Stone; // Stone layer underground
                    }
                    else if (y < terrainHeight)
                    {
                        blocks[x, y, z] = BlockType.Dirt; // Dirt near the surface
                    }
                    else if (y == terrainHeight)
                    {
                        blocks[x, y, z] = BlockType.Grass;
                        hasGrass = true;
                    }
                }

                // Now, remove excess grass from the top
                if (hasGrass)
                {
                    for (int y = terrainHeight; y >= 0; y--)
                    {
                        if (blocks[x, y, z] == BlockType.Grass)
                        {
                            if (!grassLayerPlaced)
                            {
                                // Keep the first (lowest) grass block
                                grassLayerPlaced = true;
                            }
                            else
                            {
                                // Remove any grass blocks above the first one
                                blocks[x, y, z] = BlockType.Air;
                            }
                        }
                    }

                }
            }
        }

        // Second pass: Remove isolated grass blocks
        for (int x = 0; x < CHUNK_SIZE_X; x++)
        {
            for (int z = 0; z < CHUNK_SIZE_Z; z++)
            {
                for (int y = 1; y < CHUNK_SIZE_Y - 1; y++) // Avoid out-of-bounds issues
                {
                    if (blocks[x, y, z] == BlockType.Grass)
                    {
                        // Check if all surrounding blocks are air
                        bool isIsolated = true;
                        int[] dx = { -1, 1, 0, 0, 0, 0 };
                        int[] dy = { 0, 0, -1, 1, 0, 0 };
                        int[] dz = { 0, 0, 0, 0, -1, 1 };

                        for (int i = 0; i < 6; i++)
                        {
                            int nx = x + dx[i];
                            int ny = y + dy[i];
                            int nz = z + dz[i];

                            if (nx >= 0 && nx < CHUNK_SIZE_X &&
                                ny >= 0 && ny < CHUNK_SIZE_Y &&
                                nz >= 0 && nz < CHUNK_SIZE_Z)
                            {
                                if (blocks[nx, ny, nz] != BlockType.Air)
                                {
                                    isIsolated = false;
                                    break;
                                }
                            }
                        }

                        if (isIsolated)
                        {
                            blocks[x, y, z] = BlockType.Air; // Remove the isolated grass block
                        }
                    }
                }
            }
        }

        //third iteration to carve out caves
        for (int x = 0; x < CHUNK_SIZE_X; x++)
        {
            for (int y = 0; y < CHUNK_SIZE_Y; y++)
            {
                for (int z = 0; z < CHUNK_SIZE_Z; z++)
                {
                    float worldX = x + chunkCoordinate.x * CHUNK_SIZE_X;
                    float worldY = y;
                    float worldZ = z + chunkCoordinate.y * CHUNK_SIZE_Z;

                    //check if this block should be cave or not
                    if (blocks[x, y, z] != BlockType.Air && terrainGenerator.IsCaveBlock(worldX, worldY, worldZ))
                    {
                        blocks[x, y, z] = BlockType.Air;
                    }
                }
            }
        }

        GeneratePortal();

        isInitialized = true;
    }

    private Material CreateLitObsidianMaterial()
    {
        // Create a new shader for lit obsidian
        Shader litObsidianShader = Shader.Find("Custom/litObsidian");
        if (litObsidianShader == null)
            litObsidianShader = Shader.Find("litObsidian");
        Material litObsidianMaterial = new Material(litObsidianShader);

        // Generate a procedural heightmap for normal generation
        Texture2D heightMap = GenerateProceduralHeightmap(128, 128);

        // Calculate normals using finite differencing
        Texture2D normalMap = CalculateNormalsFromHeightmap(heightMap);

        litObsidianMaterial.SetTexture("_HeightMap", heightMap);
        litObsidianMaterial.SetTexture("_NormalMap", normalMap);
        litObsidianMaterial.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.3f, 1f)); // Deep purple-blue
        litObsidianMaterial.SetFloat("_Metallic", 0.5f);
        litObsidianMaterial.SetFloat("_Smoothness", 0.7f);

        return litObsidianMaterial;
    }

    private Texture2D GenerateProceduralHeightmap(int width, int height)
    {
        Texture2D heightMap = new Texture2D(width, height, TextureFormat.RFloat, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Use multiple octaves of Perlin noise for more interesting terrain
                float frequency = 4f;
                float amplitude = 1f;
                float noiseValue = 0f;

                for (int octave = 0; octave < 4; octave++)
                {
                    float sampleX = x / (float)width * frequency;
                    float sampleY = y / (float)height * frequency;

                    noiseValue += Mathf.PerlinNoise(sampleX, sampleY) * amplitude;

                    frequency *= 2f;
                    amplitude *= 0.5f;
                }

                heightMap.SetPixel(x, y, new Color(noiseValue, noiseValue, noiseValue, 1f));
            }
        }

        heightMap.Apply();
        return heightMap;
    }

    private Texture2D CalculateNormalsFromHeightmap(Texture2D heightMap)
    {
        int width = heightMap.width;
        int height = heightMap.height;
        Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGB24, false);

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                // Calculate finite differences for normal generation
                float left = heightMap.GetPixel(x - 1, y).r;
                float right = heightMap.GetPixel(x + 1, y).r;
                float top = heightMap.GetPixel(x, y - 1).r;
                float bottom = heightMap.GetPixel(x, y + 1).r;

                Vector3 normal = new Vector3(left - right, bottom - top, 2f).normalized;

                // Convert normal from [-1, 1] to [0, 1] color range
                Color normalColor = new Color(
                    (normal.x + 1f) * 0.5f,
                    (normal.y + 1f) * 0.5f,
                    (normal.z + 1f) * 0.5f
                );

                normalMap.SetPixel(x, y, normalColor);
            }
        }

        normalMap.Apply();
        return normalMap;
    }

    private Material CreatePortalCoreShader()
    {
        // Use the provided shader
        Shader portalCoreShader = Shader.Find("Custom/portalCore");
        Material portalCoreMaterial = new Material(portalCoreShader);

        // Customize shader properties
        portalCoreMaterial.SetColor("_MainColor", new Color(0.5f, 0.1f, 0.8f, 0.8f));
        portalCoreMaterial.SetColor("_SecondaryColor", new Color(0.7f, 0.3f, 1f, 0.6f));
        portalCoreMaterial.SetFloat("_Intensity", 1.2f);
        portalCoreMaterial.SetFloat("_Speed", 3f);
        portalCoreMaterial.SetFloat("_Transparency", 0.8f);

        return portalCoreMaterial;
    }



    private Vector2 GetPortalLocationInChunk(int2 chunkCoordinate)
    {
        // Start from 10 blocks below the top to avoid checking empty air space
        int startY = Mathf.Min(CHUNK_SIZE_Y - 10, CHUNK_SIZE_Y - 1);

        // Add debugging to track progress
        int candidatesChecked = 0;
        int flatAreasFound = 0;

        for (int y = startY; y >= 1; y--) // Start at 1 to avoid checking y-1 < 0
        {
            for (int x = 6; x < CHUNK_SIZE_X - 7; x++)
            {
                for (int z = 6; z < CHUNK_SIZE_Z - 7; z++)
                {
                    candidatesChecked++;

                    // Skip if this isn't even a grass block
                    if (blocks[x, y, z] != BlockType.Grass)
                        continue;

                    bool isFlatGrassPatch = true;

                    // Check 2x4 area for grass blocks
                    for (int dx = 0; dx < 2 && isFlatGrassPatch; dx++)
                    {
                        for (int dz = 0; dz < 4 && isFlatGrassPatch; dz++)
                        {
                            // Ensure all blocks are grass at the same height
                            if (blocks[x + dx, y, z + dz] != BlockType.Grass)
                            {
                                isFlatGrassPatch = false;
                            }
                        }
                    }

                    // If we found a flat area, check surroundings with additional safeguards
                    if (isFlatGrassPatch)
                    {
                        flatAreasFound++;
                        bool surroundingsValid = true;

                        // Ensure blocks below are solid
                        for (int dx = 0; dx < 2 && surroundingsValid; dx++)
                        {
                            for (int dz = 0; dz < 4 && surroundingsValid; dz++)
                            {
                                if (y > 0 && !IsBlockSolid(x + dx, y - 1, z + dz))
                                {
                                    surroundingsValid = false;
                                }
                            }
                        }

                        // Ensure there's space above (no blocks in the way)
                        if (surroundingsValid)
                        {
                            for (int dx = 0; dx < 2 && surroundingsValid; dx++)
                            {
                                for (int dz = 0; dz < 4 && surroundingsValid; dz++)
                                {
                                    for (int h = 1; h <= 5 && surroundingsValid; h++) // Check 5 blocks high
                                    {
                                        if (y + h < CHUNK_SIZE_Y && IsBlockSolid(x + dx, y + h, z + dz))
                                        {
                                            surroundingsValid = false;
                                        }
                                    }
                                }
                            }
                        }
                        //no check for blocks around, only below and above. add that

                        // If surroundings are valid, we've found our spot!
                        if (surroundingsValid)
                        {
                            Debug.Log($"Found portal location at ({x},{y},{z}) after checking {candidatesChecked} candidates and {flatAreasFound} flat areas");
                            return new Vector2(x, z);
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"No suitable portal location found in chunk {chunkCoordinate.x},{chunkCoordinate.y} after checking {candidatesChecked} candidates and {flatAreasFound} flat areas");

        // As a fallback, place portal at center of chunk if possible
        int centerX = CHUNK_SIZE_X / 2;
        int centerZ = CHUNK_SIZE_Z / 2;

        // Try to find suitable Y for center placement
        for (int y = startY; y >= 1; y--)
        {
            if (blocks[centerX, y, centerZ] == BlockType.Grass)
            {
                Debug.Log($"Using fallback portal location at center: ({centerX},{y},{centerZ})");
                return new Vector2(centerX, centerZ);
            }
        }

        // If all else fails, return center anyway
        return new Vector2(CHUNK_SIZE_X / 2, CHUNK_SIZE_Z / 2);
    }


    public void GeneratePortal()
    {
        if (terrainGenerator.ShouldGeneratePortal(chunkCoordinate))
        {
            Debug.Log($"Portal generation started in chunk {chunkCoordinate.x},{chunkCoordinate.y}");
            Vector2 portalLocation = GetPortalLocationInChunk(chunkCoordinate);
            int portalX = Mathf.FloorToInt(portalLocation.x);
            int portalZ = Mathf.FloorToInt(portalLocation.y);
            Debug.Log($"Portal location: X={portalX}, Z={portalZ}");

            int portalY = terrainGenerator.GetTerrainHeight(portalLocation.x + chunkCoordinate.x * CHUNK_SIZE_X, portalLocation.y + chunkCoordinate.y * CHUNK_SIZE_Z);

            Debug.Log($"Portal Y position: {portalY}");

            // Create 4x5 portal frame of obsidian
            for (int x = portalX - 1; x <= portalX + 2; x++)
            {
                for (int y = portalY; y < portalY + 5; y++)
                {
                    for (int z = portalZ - 1; z <= portalZ - 1; z++)
                    {

                        // Check if we're within chunk bounds first
                        if (x < 0 || x >= CHUNK_SIZE_X ||
                            y < 0 || y >= CHUNK_SIZE_Y ||
                            z < 0 || z >= CHUNK_SIZE_Z)
                        {
                            continue; // Skip this block if it's outside the chunk
                        }

                        // Check if this is a frame block
                        if (x == portalX - 1 || x == portalX + 2 ||
                        y == portalY || y == portalY + 4)
                        {
                            blocks[x, y, z] = BlockType.Obsidian;
                            Debug.Log($"Set obsidian at {x},{y},{z}");
                        }

                        // Create portal core
                        else if (x > portalX - 1 && x < portalX + 2 && y > portalY && y < portalY + 4)
                        {
                            blocks[x, y, z] = BlockType.PortalCore;
                        }
                    }
                }
            }
        }
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

        bedrockMaterial = new Material(Shader.Find("Unlit/Texture"));
        stoneMaterial = new Material(Shader.Find("Unlit/Texture"));
        dirtMaterial = new Material(Shader.Find("Unlit/Texture"));
        grassSideMaterial = new Material(Shader.Find("Unlit/Texture"));
        grassTopMaterial = new Material(Shader.Find("Unlit/Texture"));
        obsidianMaterial = CreateLitObsidianMaterial();
        portalCoreMaterial = CreatePortalCoreShader();


        Texture2D bedrockTexture = Resources.Load<Texture2D>("proper_bedrock");
        Texture2D stoneTexture = Resources.Load<Texture2D>("proper_stone");
        Texture2D dirtTexture = Resources.Load<Texture2D>("proper_dirt");
        Texture2D grassSideTexture = Resources.Load<Texture2D>("proper_grass_side");
        Texture2D grassTopTexture = Resources.Load<Texture2D>("proper_grass_top");
        Texture2D obsidianTexture = Resources.Load<Texture2D>("final_obsidian");


        if (bedrockTexture == null || stoneTexture == null || dirtTexture == null | grassSideTexture == null || grassTopTexture == null || obsidianTexture == null)
        {
            Debug.LogError("Failed to load textures!");
        }
        else
        {
            bedrockMaterial.mainTexture = bedrockTexture;
            stoneMaterial.mainTexture = stoneTexture;
            dirtMaterial.mainTexture = dirtTexture;
            grassSideMaterial.mainTexture = grassSideTexture;
            grassTopMaterial.mainTexture = grassTopTexture;
            obsidianMaterial.mainTexture = obsidianTexture;
        }


        mesh = new Mesh();
        portalMesh = new Mesh();
        //added this because of that issue with rightmost blocks on some chunks not rendering properly; turns out I had too many vertices
        //to be rendered on each chunk, so I had to increase the max amount of vertices on each chunk
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh = mesh;

        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesBedrock = new List<int>();  // Separate lists for bedrock, stone, dirt, grass & obsidian
        List<int> trianglesStone = new List<int>();
        List<int> trianglesDirt = new List<int>();
        List<int> trianglesGrassSide = new List<int>();
        List<int> trianglesGrassTop = new List<int>();
        List<int> trianglesObsidian = new List<int>();
        List<int> trianglesPortal = new List<int>();
        List<Vector2> uvsList = new List<Vector2>();

        int vertexOffset = 0;

        for (int x = 0; x < CHUNK_SIZE_X; x++)
        {
            for (int y = 0; y < CHUNK_SIZE_Y; y++)
            {
                for (int z = 0; z < CHUNK_SIZE_Z; z++)
                {
                    if (blocks[x, y, z] == BlockType.Bedrock)
                    {
                        AddBlockMesh(x, y, z, verticesList, trianglesBedrock, uvsList, ref vertexOffset);
                    }
                    else if (blocks[x, y, z] == BlockType.Stone)
                    {
                        AddBlockMesh(x, y, z, verticesList, trianglesStone, uvsList, ref vertexOffset);
                    }
                    else if (blocks[x, y, z] == BlockType.Dirt)
                    {
                        AddBlockMesh(x, y, z, verticesList, trianglesDirt, uvsList, ref vertexOffset);
                    }
                    else if (blocks[x, y, z] == BlockType.Grass)
                    {
                        AddGrassBlockMesh(x, y, z, verticesList, trianglesGrassSide, trianglesGrassTop, trianglesDirt, uvsList, ref vertexOffset);
                    }
                    else if (blocks[x, y, z] == BlockType.Obsidian)
                    {
                        AddBlockMesh(x, y, z, verticesList, trianglesObsidian, uvsList, ref vertexOffset);
                        Debug.Log($"Generating obsidian block mesh at {x},{y},{z}");
                    }
                    else if (blocks[x, y, z] == BlockType.PortalCore)
                    {
                        AddBlockMesh(x, y, z, verticesList, trianglesPortal, uvsList, ref vertexOffset);
                    }

                }
            }
        }

        mesh.vertices = verticesList.ToArray();
        mesh.subMeshCount = 7; // One submesh for bedrock, one for stone, one for dirt, two for grass (side & top faces), one for obsidian
        mesh.SetTriangles(trianglesBedrock.ToArray(), 0); // First submesh is bedrock
        mesh.SetTriangles(trianglesStone.ToArray(), 1);   // Second submesh is stone
        mesh.SetTriangles(trianglesDirt.ToArray(), 2);    // Third for dirt
        mesh.SetTriangles(trianglesGrassSide.ToArray(), 3); // Fourth for grass side
        mesh.SetTriangles(trianglesGrassTop.ToArray(), 4); // Fifth for grass top
        mesh.SetTriangles(trianglesObsidian.ToArray(), 5); // Sixth for obsidian
        mesh.SetTriangles(trianglesPortal.ToArray(), 6); // Seventh for portal core
        mesh.uv = uvsList.ToArray();
        mesh.RecalculateNormals();

        // Assign all materials to the renderer
        Material[] materials = new Material[7] { bedrockMaterial, stoneMaterial, dirtMaterial, grassSideMaterial, grassTopMaterial, obsidianMaterial, portalCoreMaterial };
        meshRenderer.materials = materials;
    }



    public void SetPosition()
    {
        // Set the chunk's world position based on its chunk coordinates
        transform.position = new Vector3(chunkCoordinate.x * 32, 0, chunkCoordinate.y * 32);
    }

    private void AddGrassBlockMesh(int x, int y, int z, List<Vector3> verticesList,
                               List<int> trianglesGrassSide, List<int> trianglesGrassTop,
                               List<int> trianglesDirt, List<Vector2> uvsList, ref int vertexOffset)
    {
        Vector3[] vertices = new Vector3[]{
        new Vector3(x, y, z),           // 0, front bottom left
        new Vector3(x + 1, y, z),       // 1, front bottom right
        new Vector3(x + 1, y + 1, z),   // 2, front top right
        new Vector3(x, y + 1, z),       // 3, front top left
        new Vector3(x, y + 1, z + 1),   // 4, back top left
        new Vector3(x, y, z + 1),       // 5, back bottom left
        new Vector3(x + 1, y, z + 1),   // 6, back bottom right
        new Vector3(x + 1, y + 1, z + 1)// 7, back top right
    };

        // Check if block has neighbors
        bool hasFrontNeighbor = z > 0 ? IsBlockSolid(x, y, z - 1) : false;
        bool hasBackNeighbor = z < CHUNK_SIZE_Z - 1 ? IsBlockSolid(x, y, z + 1) : false;
        bool hasLeftNeighbor = x > 0 ? IsBlockSolid(x - 1, y, z) : false;
        bool hasRightNeighbor = x < CHUNK_SIZE_X - 1 ? IsBlockSolid(x + 1, y, z) : false;
        bool hasTopNeighbor = y < CHUNK_SIZE_Y - 1 ? IsBlockSolid(x, y + 1, z) : false;
        bool hasBottomNeighbor = y > 0 ? IsBlockSolid(x, y - 1, z) : false;


        // Add faces that are visible
        // Front face (grass side)
        if (!hasFrontNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 0, 1, 2, 3 }, verticesList, trianglesGrassSide, uvsList, ref vertexOffset, "front");

        // Left face (grass side)
        if (!hasLeftNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 5, 0, 3, 4 }, verticesList, trianglesGrassSide, uvsList, ref vertexOffset, "left");

        // Right face (grass side)
        if (!hasRightNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 1, 6, 7, 2 }, verticesList, trianglesGrassSide, uvsList, ref vertexOffset, "right");

        // Top face (grass top)
        if (!hasTopNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 3, 2, 7, 4 }, verticesList, trianglesGrassTop, uvsList, ref vertexOffset, "top");

        // Bottom face (dirt)
        if (!hasBottomNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 0, 5, 6, 1 }, verticesList, trianglesDirt, uvsList, ref vertexOffset, "bottom");

        // Back face (grass side)
        if (!hasBackNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 5, 4, 7, 6 }, verticesList, trianglesGrassSide, uvsList, ref vertexOffset, "back");
    }


    private void AddBlockMesh(int x, int y, int z, List<Vector3> verticesList, List<int> trianglesList, List<Vector2> uvsList, ref int vertexOffset)
    {
        // Don't need to set material here anymore as we're using submeshes

        //made it to accept positions, so I can render blocks anywhere with the same relative positions of each vertex
        Vector3[] vertices = new Vector3[]{ //this part is the equivalent of making a vertex buffer
            //this is equal to (0, 0, 0)
            new Vector3(x, y, z), //0, front bottom left
            //this is equal to (1, 0, 0)
            new Vector3(x + 1, y, z), //1, front bottom right
            //and so on
            new Vector3(x + 1, y + 1, z), //2, front top right
            new Vector3(x, y + 1, z), //3, front top left
            new Vector3(x, y + 1, z + 1), //4, back top left
            new Vector3(x, y, z + 1), //5, back bottom left
            new Vector3(x + 1, y, z + 1), //6, back bottom right
            new Vector3(x + 1, y + 1, z + 1) //7, back top right
        };

        // For edges of the chunk, ALWAYS render the face
        bool hasFrontNeighbor = z > 0 ? IsBlockSolid(x, y, z - 1) : false;
        bool hasBackNeighbor = z < CHUNK_SIZE_Z - 1 ? IsBlockSolid(x, y, z + 1) : false;
        bool hasLeftNeighbor = x > 0 ? IsBlockSolid(x - 1, y, z) : false;
        bool hasRightNeighbor = x < CHUNK_SIZE_X - 1 ? IsBlockSolid(x + 1, y, z) : false;
        bool hasTopNeighbor = y < CHUNK_SIZE_Y - 1 ? IsBlockSolid(x, y + 1, z) : false;
        bool hasBottomNeighbor = y > 0 ? IsBlockSolid(x, y - 1, z) : false;


        // Only add faces that are visible (not covered by other blocks)
        if (!hasFrontNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 0, 1, 2, 3 }, verticesList, trianglesList, uvsList, ref vertexOffset, "front");

        if (!hasLeftNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 5, 0, 3, 4 }, verticesList, trianglesList, uvsList, ref vertexOffset, "left");

        if (!hasRightNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 1, 6, 7, 2 }, verticesList, trianglesList, uvsList, ref vertexOffset, "right");

        if (!hasTopNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 3, 2, 7, 4 }, verticesList, trianglesList, uvsList, ref vertexOffset, "top");

        if (!hasBottomNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 0, 5, 6, 1 }, verticesList, trianglesList, uvsList, ref vertexOffset, "bottom");

        if (!hasBackNeighbor)
            AddFaceWithConsistentUVs(vertices, new int[] { 5, 4, 7, 6 }, verticesList, trianglesList, uvsList, ref vertexOffset, "back");
    }

    private bool IsBlockSolid(int x, int y, int z)
    {
        // Check if coordinates are within bounds of this chunk
        if (x >= 0 && x < CHUNK_SIZE_X && y >= 0 && y < CHUNK_SIZE_Y && z >= 0 && z < CHUNK_SIZE_Z)
        {
            // Check if this block position contains a solid block
            return blocks[x, y, z] == BlockType.Bedrock ||
                   blocks[x, y, z] == BlockType.Stone ||
                   blocks[x, y, z] == BlockType.Dirt ||
                   blocks[x, y, z] == BlockType.Grass ||
                   blocks[x, y, z] == BlockType.Obsidian;
        }

        // For coordinates outside this chunk, consider the edge of the world as having no neighbors
        // This will make the outer edge blocks render their faces
        // In a complete solution, you would check neighboring chunks here
        return false;

    }


    private void AddFaceWithConsistentUVs(Vector3[] cubeVertices, int[] faceIndices, List<Vector3> verticesList,
                                         List<int> trianglesList, List<Vector2> uvsList, ref int vertexOffset, string faceType)
    {
        // Add the 4 vertices for this face
        for (int i = 0; i < 4; i++)
        {
            verticesList.Add(cubeVertices[faceIndices[i]]);
        }

        // all faces use standard UV mapping, except the grass back face, which needs rotating
        Vector2[] standardUVs = new Vector2[]
        {
            new Vector2(0, 0), // Bottom left
            new Vector2(1, 0), // Bottom right
            new Vector2(1, 1), // Top right
            new Vector2(0, 1)  // Top left
        };

        // Modify for back face, so the texture is upright instead of to the side
        if (faceType == "back")
        {
            standardUVs = new Vector2[]
            {
                new Vector2(1, 0), // Bottom right
                new Vector2(1, 1), // Top right
                new Vector2(0, 1), // Top left
                new Vector2(0, 0)  // Bottom left
            };
        }

        // for all other types of face, just add regular UVs
        uvsList.AddRange(standardUVs);

        // Add triangles for each face
        trianglesList.Add(vertexOffset);
        trianglesList.Add(vertexOffset + 2);
        trianglesList.Add(vertexOffset + 1);

        trianglesList.Add(vertexOffset);
        trianglesList.Add(vertexOffset + 3);
        trianglesList.Add(vertexOffset + 2);

        // Increment offset by 4 for the next face
        vertexOffset += 4;
    }
}