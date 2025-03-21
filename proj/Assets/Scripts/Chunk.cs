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

    private Material bedrockMaterial;
    private Material stoneMaterial;
    private Material dirtMaterial;
    private Material grassSideMaterial;
    private Material grassTopMaterial;

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
                        blocks[x, y, z] = BlockType.Grass; //Grass layer on top, only 1 block thick
                    }
                }
            }
        }

        //second iteration to carve out caves
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

        bedrockMaterial = new Material(Shader.Find("Unlit/Texture"));
        stoneMaterial = new Material(Shader.Find("Unlit/Texture"));
        dirtMaterial = new Material(Shader.Find("Unlit/Texture"));
        grassSideMaterial = new Material(Shader.Find("Unlit/Texture"));
        grassTopMaterial = new Material(Shader.Find("Unlit/Texture"));

        Texture2D bedrockTexture = Resources.Load<Texture2D>("bedrock");
        Texture2D stoneTexture = Resources.Load<Texture2D>("stone");
        Texture2D dirtTexture = Resources.Load<Texture2D>("dirt");
        Texture2D grassSideTexture = Resources.Load<Texture2D>("gud_grass_side");
        Texture2D grassTopTexture = Resources.Load<Texture2D>("gud_grass_top");

        if (bedrockTexture == null || stoneTexture == null || dirtTexture == null | grassSideTexture == null || grassTopTexture == null)
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
        }


        mesh = new Mesh();
        //added this because of that issue with rightmost blocks on some chunks not rendering properly; turns out I had too many vertices
        //to be rendered on each chunk, so I had to increase the max amount of vertices on each chunk
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        meshFilter.mesh = mesh;

        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesBedrock = new List<int>();  // Separate lists for bedrock, stone, dirt & grass
        List<int> trianglesStone = new List<int>();
        List<int> trianglesDirt = new List<int>();
        List<int> trianglesGrassSide = new List<int>();
        List<int> trianglesGrassTop = new List<int>();
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
                }
            }
        }

        mesh.vertices = verticesList.ToArray();
        mesh.subMeshCount = 5; // One submesh for bedrock, one for stone, one for dirt, two for grass (side & top faces)
        mesh.SetTriangles(trianglesBedrock.ToArray(), 0); // First submesh is bedrock
        mesh.SetTriangles(trianglesStone.ToArray(), 1);   // Second submesh is stone
        mesh.SetTriangles(trianglesDirt.ToArray(), 2);    // Third for dirt
        mesh.SetTriangles(trianglesGrassSide.ToArray(), 3); // Fourth for grass side
        mesh.SetTriangles(trianglesGrassTop.ToArray(), 4); // Fifth for grass top
        mesh.uv = uvsList.ToArray();
        mesh.RecalculateNormals();

        // Assign all materials to the renderer
        Material[] materials = new Material[5] { bedrockMaterial, stoneMaterial, dirtMaterial, grassSideMaterial, grassTopMaterial };
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
                   blocks[x, y, z] == BlockType.Grass;
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