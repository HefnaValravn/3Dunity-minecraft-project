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
        //add a meshFilter to the cube object
        meshFilter = gameObject.AddComponent<MeshFilter>();
        //add a meshRenderer to the cube object
        meshRenderer = gameObject.AddComponent<MeshRenderer>();

        //create materials
        bedrockMaterial = new Material(Shader.Find("Unlit/Texture"));
        stoneMaterial = new Material(Shader.Find("Unlit/Texture"));

        //load textures
        Texture2D bedrockTexture = Resources.Load<Texture2D>("bedrock");
        Texture2D stoneTexture = Resources.Load<Texture2D>("stone");

        if (bedrockTexture == null || stoneTexture == null)
        {
            Debug.LogError("Failed to load textures!");
        }
        else
        {
            //assign textures to materials
            bedrockMaterial.mainTexture = bedrockTexture;
            stoneMaterial.mainTexture = stoneTexture;
            Debug.Log("Bedrock texture applied successfully: " + bedrockTexture.name);
            Debug.Log("Stone texture applied successfully: " + stoneTexture.name);
        }

        //create a new mesh and assign it to the meshFilter
        mesh = new Mesh();
        meshFilter.mesh = mesh;

        List<Vector3> verticesList = new List<Vector3>(); //vertices on each block
        List<int> trianglesList = new List<int>(); //triangles made with said vertices
        List<Vector2> uvsList = new List<Vector2>(); // UVs for texture mapping

        int vertexOffset = 0;

        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                for (int z = 0; z < 32; z++)
                {
                    //optional logic to only generate mesh for a certain type of terrain
                    if (blocks[x, y, z] == BlockType.Bedrock)
                    {
                        meshRenderer.material = bedrockMaterial;
                        // Add the block's mesh to the chunk's mesh
                        AddBlockMesh(x, y, z, verticesList, trianglesList, uvsList, ref vertexOffset);
                    }
                    else if (blocks[x, y, z] == BlockType.Stone)
                    {
                        meshRenderer.material = stoneMaterial;
                        // Add the block's mesh to the chunk's mesh
                        AddBlockMesh(x, y, z, verticesList, trianglesList, uvsList, ref vertexOffset);
                    }
                }
            }
        }

        mesh.vertices = verticesList.ToArray();
        mesh.triangles = trianglesList.ToArray();
        mesh.uv = uvsList.ToArray(); // Apply the UV coordinates
        mesh.RecalculateNormals();
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

    private void AddBlockMesh(int x, int y, int z, List<Vector3> verticesList, List<int> trianglesList, List<Vector2> uvsList, ref int vertexOffset)
    {
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
