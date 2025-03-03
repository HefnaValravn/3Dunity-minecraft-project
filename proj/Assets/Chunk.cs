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
        // create a 20x20 grid of bedrock blocks at y = 0
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
                for (int y = bedrockDepth; y < bedrockDepth + 3; y++) // 3 layers of stone above bedrock
                {
                    blocks[x, y, z] = BlockType.Stone;
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

    Texture2D bedrockTexture = Resources.Load<Texture2D>("bedrock");
    Texture2D stoneTexture = Resources.Load<Texture2D>("stone");

    if (bedrockTexture == null || stoneTexture == null)
    {
        Debug.LogError("Failed to load textures!");
    }
    else
    {
        bedrockMaterial.mainTexture = bedrockTexture;
        stoneMaterial.mainTexture = stoneTexture;
    }

    mesh = new Mesh();
    meshFilter.mesh = mesh;

    List<Vector3> verticesList = new List<Vector3>();
    List<int> trianglesBedrock = new List<int>();  // Separate lists for bedrock & stone
    List<int> trianglesStone = new List<int>();
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
                    AddBlockMesh(x, y, z, verticesList, trianglesBedrock, uvsList, ref vertexOffset, bedrockMaterial);
                }
                else if (blocks[x, y, z] == BlockType.Stone)
                {
                    AddBlockMesh(x, y, z, verticesList, trianglesStone, uvsList, ref vertexOffset, stoneMaterial);
                }
            }
        }
    }

    mesh.vertices = verticesList.ToArray();
    mesh.subMeshCount = 2; // One submesh for bedrock, one for stone
    mesh.SetTriangles(trianglesBedrock, 0);
    mesh.SetTriangles(trianglesStone, 1);
    mesh.uv = uvsList.ToArray();
    mesh.RecalculateNormals();

    meshRenderer.materials = new Material[] { bedrockMaterial, stoneMaterial };
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
        transform.position = new Vector3(chunkCoordinate.x * 32, chunkCoordinate.y * 128, 0);
    }

    private void AddBlockMesh(int x, int y, int z, List<Vector3> verticesList, List<int> trianglesList, List<Vector2> uvsList, ref int vertexOffset, Material material)
    {

        meshRenderer.material = material;

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

        // Define UV coordinates for each vertex
        // For a cube, we need to define UVs for each face separately

        // Front face
        AddFaceWithUVs(vertices, new int[] { 0, 1, 2, 3 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        // Left face
        AddFaceWithUVs(vertices, new int[] { 5, 0, 3, 4 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        // Right face
        AddFaceWithUVs(vertices, new int[] { 1, 6, 7, 2 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        // Top face
        AddFaceWithUVs(vertices, new int[] { 3, 2, 7, 4 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        // Bottom face
        AddFaceWithUVs(vertices, new int[] { 0, 5, 6, 1 }, verticesList, trianglesList, uvsList, ref vertexOffset);

        // Back face
        AddFaceWithUVs(vertices, new int[] { 5, 4, 7, 6 }, verticesList, trianglesList, uvsList, ref vertexOffset);
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
