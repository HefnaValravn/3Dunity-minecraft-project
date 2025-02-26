using Unity.Mathematics;
using UnityEngine;

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
        GenerateMesh();
    }

    private void InitializeChunk()
    {
        blocks = new BlockType[32, 128, 32]; //this defines the size of the chunk (how many blocks in each direction)

        blocks[0, 0, 0] = BlockType.Bedrock; //the block we're gonna generate at (0,0,0) will be Bedrock
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

        GenerateBlockMesh();
    }


    private void GenerateBlockMesh()
    {
        Vector3[] vertices = new Vector3[]{ //this part is the equivalent of making a vertex buffer
            new Vector3(0, 0, 0), //0, front bottom left
            new Vector3(1, 0, 0), //1, front bottom right
            new Vector3(1, 1, 0), //2, front top right
            new Vector3(0, 1, 0), //3, front top left
            new Vector3(0, 1, 1), //4, back top left
            new Vector3(0, 0, 1), //5, back bottom left
            new Vector3(1, 0, 1), //6, back bottom right
            new Vector3(1, 1, 1) //7, back top right
        };


        int[] trinagles = new int[]{ //and THIS is the equivalent of making an index buffer, which describes,
                                     //given the vertex buffer, the coordinates of all the triangles to be drawn
                                     //IMPORTANT, REMEMBER: CLOCKWISE MEANS SEEN, COUNTER-CLOCKWISE UNSEEN
            //Front two triangles
            0, 2, 1, 0, 3, 2,

            //Left triangles
            5, 3, 0, 5, 4, 3,

            //Right triangles
            1, 7, 6, 1, 2, 7,

            //Top triangles
            3, 7, 2, 3, 4, 7,

            //Bottom triangles
            0, 6, 5, 0, 1, 6,

            //Back triangles
            5, 6, 7, 5, 7, 4
        };

        mesh.vertices = vertices;
        mesh.triangles = trinagles;
        mesh.RecalculateNormals(); //automatically creates normal for the generated cube
    }

}
