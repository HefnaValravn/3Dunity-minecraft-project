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
            }
        }
    }

    private void GenerateMesh()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();


        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.black;
        meshRenderer.material = mat;

        mesh = new Mesh();
        meshFilter.mesh = mesh;

        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesList = new List<int>();

        int vertexOffset = 0;


        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 128; y++)
            {
                for (int z = 0; z < 32; z++)
                {
                    //optional logic to only generate mesh for a certain type of terrain
                    // 
                    if (blocks[x, y, z] == BlockType.Bedrock)
                    {
                        // Add the block's mesh to the chunk's mesh
                        AddBlockMesh(x, y, z, verticesList, trianglesList, ref vertexOffset);
                    }
                }
            }
        }

        mesh.vertices = verticesList.ToArray();
        mesh.triangles = trianglesList.ToArray();
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


    private void AddBlockMesh(int x, int y, int z, List<Vector3> verticesList, List<int> trianglesList, ref int vertexOffset)
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

        //same concept as above; instead of using 0, 2, 1 to construct a triangle, you use the offset
        int[] triangles = new int[]{ //and THIS is the equivalent of making an index buffer, which describes,
                                     //given the vertex buffer, the coordinates of all the triangles to be drawn
                                     //IMPORTANT, REMEMBER: CLOCKWISE MEANS SEEN, COUNTER-CLOCKWISE UNSEEN
            //Front two triangles
            //equal to 0, 2, 1, 0, 3, 2
            vertexOffset, vertexOffset + 2, vertexOffset + 1, vertexOffset, vertexOffset + 3, vertexOffset + 2,

            //Left triangles
            vertexOffset + 5, vertexOffset + 3, vertexOffset, vertexOffset + 5, vertexOffset + 4, vertexOffset + 3,

            //Right triangles
            vertexOffset + 1, vertexOffset + 7, vertexOffset + 6, vertexOffset + 1, vertexOffset + 2, vertexOffset + 7,

            //Top triangles
            vertexOffset + 3, vertexOffset + 7, vertexOffset + 2, vertexOffset + 3, vertexOffset + 4, vertexOffset + 7,

            //Bottom triangles
            vertexOffset, vertexOffset + 6, vertexOffset + 5, vertexOffset, vertexOffset + 1, vertexOffset + 6,

            //Back triangles
            vertexOffset + 5, vertexOffset + 6, vertexOffset + 7, vertexOffset + 5, vertexOffset + 7, vertexOffset + 4
        };

        verticesList.AddRange(vertices);
        trianglesList.AddRange(triangles);

        //increment offset by 8 blocks so you can place next block
        vertexOffset += 8;
    }

}
