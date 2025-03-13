using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

public class Chunk : MonoBehaviour
{
    public int2 chunkCoordinate;
    public BlockType[,,] blocks;

    private MeshFilter meshFilter; //holds the geometry of the mesh
    private MeshRenderer meshRenderer; //renders the mesh
    private Mesh mesh; //the mesh itself lol

    private Material bedrockMaterial;
    private Material stoneMaterial;
    private Material dirtMaterial;

    private void Start()
    {
        InitializeChunk();
        SetPosition();
        GenerateMesh();
    }

    private void InitializeChunk()
    {
        blocks = new BlockType[32, 128, 32]; //this defines the size of the chunk (how many blocks in each direction)

        //IMPORTANT: WHATEVER DIMENSIONS YOU SET HERE HAVE TO MATCH THE DIMENSIONS IN THE GenerateMesh() FUNCTION
        // create a 32x32 grid of bedrock blocks at y = 0
        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                int bedrockDepth = GetBedrockRandomDepth(); // Get a weighted random depth

                for (int y = 0; y < bedrockDepth; y++) // Place bedrock up to this depth
                {
                    blocks[x, y, z] = BlockType.Bedrock;
                }

                // Place stone blocks above the bedrock layer
                for (int y = bedrockDepth; y < bedrockDepth + 10; y++) // 3 layers of stone above bedrock
                {
                    blocks[x, y, z] = BlockType.Stone;
                }

                for (int y = bedrockDepth + 10; y < bedrockDepth + 15; y++)
                {
                    blocks[x, y, z] = BlockType.Dirt;
                }
            }
        }
    }

    private void GenerateMesh()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        bedrockMaterial = new Material(Shader.Find("Unlit/Texture"));
        stoneMaterial = new Material(Shader.Find("Unlit/Texture"));
        dirtMaterial = new Material(Shader.Find("Unlit/Texture"));

        Texture2D bedrockTexture = Resources.Load<Texture2D>("bedrock");
        Texture2D stoneTexture = Resources.Load<Texture2D>("stone");
        Texture2D dirtTexture = Resources.Load<Texture2D>("dirt");

        if (bedrockTexture == null || stoneTexture == null || dirtTexture == null)
        {
            Debug.LogError("Failed to load textures!");
        }
        else
        {
            bedrockMaterial.mainTexture = bedrockTexture;
            stoneMaterial.mainTexture = stoneTexture;
            dirtMaterial.mainTexture = dirtTexture;
        }


        mesh = new Mesh();
        meshFilter.mesh = mesh;

        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesBedrock = new List<int>();  // Separate lists for bedrock, stone & dirt
        List<int> trianglesStone = new List<int>();
        List<int> trianglesDirt = new List<int>();
        List<Vector2> uvsList = new List<Vector2>();

        int vertexOffset = 0;

        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                for (int z = 0; z < 32; z++)
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
                }
            }
        }

        mesh.vertices = verticesList.ToArray();
        mesh.subMeshCount = 3; // One submesh for bedrock, one for stone, one for dirt
        mesh.SetTriangles(trianglesBedrock.ToArray(), 0); // First submesh is bedrock
        mesh.SetTriangles(trianglesStone.ToArray(), 1);   // Second submesh is stone
        mesh.SetTriangles(trianglesDirt.ToArray(), 2);    // Third for dirt
        mesh.uv = uvsList.ToArray();
        mesh.RecalculateNormals();

        // Assign all materials to the renderer
        Material[] materials = new Material[3] { bedrockMaterial, stoneMaterial, dirtMaterial };
        meshRenderer.materials = materials;
    }

    // Returns a weighted random bedrock depth. This makes it so the bedrock layer is most likely to be 2 cubes deep, but can also be 1 or 3 deep
    private int GetBedrockRandomDepth()
    {
        float randomValue = UnityEngine.Random.value; // Generates a value between 0.0 and 1.0

        if (randomValue < 0.2f) return 1; // 20% chance for 1 block deep
        if (randomValue < 0.8f) return 2; // 60% chance for 2 blocks deep
        return 3; // 20% chance for 3 blocks deep
    }

    private void SetPosition()
    {
        // Set the chunk's world position based on its chunk coordinates
        transform.position = new Vector3(chunkCoordinate.x * 32, 0, chunkCoordinate.y * 32);
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

        // Check if block has neighbors and only add faces when needed
        bool hasFrontNeighbor = z > 0 && IsBlockSolid(x, y, z - 1);
        bool hasBackNeighbor = z < 31 && IsBlockSolid(x, y, z + 1);
        bool hasLeftNeighbor = x > 0 && IsBlockSolid(x - 1, y, z);
        bool hasRightNeighbor = x < 31 && IsBlockSolid(x + 1, y, z);
        bool hasTopNeighbor = y < 127 && IsBlockSolid(x, y + 1, z);
        bool hasBottomNeighbor = y > 0 && IsBlockSolid(x, y - 1, z);

        // Only add faces that are visible (not covered by other blocks)
        if (!hasFrontNeighbor)
            AddFaceWithUVs(vertices, new int[] { 0, 1, 2, 3 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        if (!hasLeftNeighbor)
            AddFaceWithUVs(vertices, new int[] { 5, 0, 3, 4 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        if (!hasRightNeighbor)
            AddFaceWithUVs(vertices, new int[] { 1, 6, 7, 2 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        if (!hasTopNeighbor)
            AddFaceWithUVs(vertices, new int[] { 3, 2, 7, 4 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        if (!hasBottomNeighbor)
            AddFaceWithUVs(vertices, new int[] { 0, 5, 6, 1 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        if (!hasBackNeighbor)
            AddFaceWithUVs(vertices, new int[] { 5, 4, 7, 6 }, verticesList, trianglesList, uvsList, ref vertexOffset);
    }

    private bool IsBlockSolid(int x, int y, int z)
    {
        // Check if this block position contains a solid block (either bedrock or stone)
        return blocks[x, y, z] == BlockType.Bedrock || blocks[x, y, z] == BlockType.Stone || blocks[x, y, z] == BlockType.Dirt;
    }

    private void AddFaceWithUVs(Vector3[] cubeVertices, int[] faceIndices, List<Vector3> verticesList, List<int> trianglesList, List<Vector2> uvsList, ref int vertexOffset)
    {
        // Add the 4 vertices for this face
        for (int i = 0; i < 4; i++)
        {
            verticesList.Add(cubeVertices[faceIndices[i]]);
        }

        // Add UV coordinates for the 4 vertices (mapping a full texture to this face)
        uvsList.Add(new Vector2(0, 0)); // Bottom left
        uvsList.Add(new Vector2(1, 0)); // Bottom right
        uvsList.Add(new Vector2(1, 1)); // Top right
        uvsList.Add(new Vector2(0, 1)); // Top left

        // Add two triangles to make a quad (face)
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