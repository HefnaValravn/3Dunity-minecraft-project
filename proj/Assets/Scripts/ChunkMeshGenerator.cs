using UnityEngine;
using System.Collections.Generic;

public class ChunkMeshGenerator
{
    private BlockType[,,] blocks;
    private int sizeX, sizeY, sizeZ;
    
    private Material bedrockMaterial;
    private Material stoneMaterial;
    private Material dirtMaterial;
    private Material grassSideMaterial;
    private Material grassTopMaterial;
    private Material obsidianMaterial;

    public ChunkMeshGenerator(BlockType[,,] blocks, int sizeX, int sizeY, int sizeZ)
    {
        this.blocks = blocks;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        bedrockMaterial = new Material(Shader.Find("Unlit/Texture"));
        stoneMaterial = new Material(Shader.Find("Unlit/Texture"));
        dirtMaterial = new Material(Shader.Find("Unlit/Texture"));
        grassSideMaterial = new Material(Shader.Find("Unlit/Texture"));
        grassTopMaterial = new Material(Shader.Find("Unlit/Texture"));
        obsidianMaterial = CreateUnlitObsidianMaterial();

        Texture2D bedrockTexture = Resources.Load<Texture2D>("proper_bedrock");
        Texture2D stoneTexture = Resources.Load<Texture2D>("proper_stone");
        Texture2D dirtTexture = Resources.Load<Texture2D>("proper_dirt");
        Texture2D grassSideTexture = Resources.Load<Texture2D>("proper_grass_side");
        Texture2D grassTopTexture = Resources.Load<Texture2D>("proper_grass_top");
        Texture2D obsidianTexture = Resources.Load<Texture2D>("final_obsidian");

        if (bedrockTexture == null || stoneTexture == null || dirtTexture == null || 
            grassSideTexture == null || grassTopTexture == null || obsidianTexture == null)
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
    }

    public Mesh GenerateMesh(out Material[] materials)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesBedrock = new List<int>();
        List<int> trianglesStone = new List<int>();
        List<int> trianglesDirt = new List<int>();
        List<int> trianglesGrassSide = new List<int>();
        List<int> trianglesGrassTop = new List<int>();
        List<int> trianglesObsidian = new List<int>();
        List<Vector2> uvsList = new List<Vector2>();

        int vertexOffset = 0;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
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
                    }
                }
            }
        }

        mesh.vertices = verticesList.ToArray();
        mesh.subMeshCount = 6; // Separate submeshes for each material
        mesh.SetTriangles(trianglesBedrock.ToArray(), 0); // Bedrock
        mesh.SetTriangles(trianglesStone.ToArray(), 1);   // Stone
        mesh.SetTriangles(trianglesDirt.ToArray(), 2);    // Dirt
        mesh.SetTriangles(trianglesGrassSide.ToArray(), 3); // Grass side
        mesh.SetTriangles(trianglesGrassTop.ToArray(), 4); // Grass top
        mesh.SetTriangles(trianglesObsidian.ToArray(), 5); // Obsidian
        mesh.uv = uvsList.ToArray();
        mesh.RecalculateNormals();

        // Assign materials
        materials = new Material[6] { 
            bedrockMaterial, 
            stoneMaterial, 
            dirtMaterial, 
            grassSideMaterial, 
            grassTopMaterial, 
            obsidianMaterial 
        };
        
        return mesh;
    }

    private void AddBlockMesh(int x, int y, int z, List<Vector3> verticesList, List<int> trianglesList, List<Vector2> uvsList, ref int vertexOffset)
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
        bool hasBackNeighbor = z < sizeZ - 1 ? IsBlockSolid(x, y, z + 1) : false;
        bool hasLeftNeighbor = x > 0 ? IsBlockSolid(x - 1, y, z) : false;
        bool hasRightNeighbor = x < sizeX - 1 ? IsBlockSolid(x + 1, y, z) : false;
        bool hasTopNeighbor = y < sizeY - 1 ? IsBlockSolid(x, y + 1, z) : false;
        bool hasBottomNeighbor = y > 0 ? IsBlockSolid(x, y - 1, z) : false;

        // Only add faces that are visible
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
        bool hasBackNeighbor = z < sizeZ - 1 ? IsBlockSolid(x, y, z + 1) : false;
        bool hasLeftNeighbor = x > 0 ? IsBlockSolid(x - 1, y, z) : false;
        bool hasRightNeighbor = x < sizeX - 1 ? IsBlockSolid(x + 1, y, z) : false;
        bool hasTopNeighbor = y < sizeY - 1 ? IsBlockSolid(x, y + 1, z) : false;
        bool hasBottomNeighbor = y > 0 ? IsBlockSolid(x, y - 1, z) : false;

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

    private void AddFaceWithConsistentUVs(Vector3[] cubeVertices, int[] faceIndices, List<Vector3> verticesList,
                                        List<int> trianglesList, List<Vector2> uvsList, ref int vertexOffset, string faceType)
    {
        // Add the 4 vertices for this face
        for (int i = 0; i < 4; i++)
        {
            verticesList.Add(cubeVertices[faceIndices[i]]);
        }

        // Standard UV mapping
        Vector2[] standardUVs = new Vector2[]
        {
            new Vector2(0, 0), // Bottom left
            new Vector2(1, 0), // Bottom right
            new Vector2(1, 1), // Top right
            new Vector2(0, 1)  // Top left
        };

        // Modify for back face, so the texture is upright
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

        // Add UVs
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

    private bool IsBlockSolid(int x, int y, int z)
    {
        return BlockUtility.IsBlockSolid(blocks, x, y, z, sizeX, sizeY, sizeZ);
    }

    private Material CreateUnlitObsidianMaterial()
    {
        Shader unlitObsidianShader = Shader.Find("Custom/unlitObsidian");
        if (unlitObsidianShader == null)
            unlitObsidianShader = Shader.Find("unlitObsidian");
            
        Material unlitObsidianMaterial = new Material(unlitObsidianShader);

        // Generate dispMap for normals
        Texture2D dispMap = GenerateProceduraldispMap(128, 128);
        Texture2D normalMap = CalculateNormalsFromdispMap(dispMap);

        unlitObsidianMaterial.SetTexture("_dispMap", dispMap);
        unlitObsidianMaterial.SetTexture("_NormalMap", normalMap);
        unlitObsidianMaterial.SetColor("_BaseColor", new Color(0.2f, 0.2f, 0.3f, 1f));
        unlitObsidianMaterial.SetFloat("_Metallic", 0.5f);
        unlitObsidianMaterial.SetFloat("_Smoothness", 0.7f);

        return unlitObsidianMaterial;
    }

    private Texture2D GenerateProceduraldispMap(int width, int height)
    {
        Texture2D dispMap = new Texture2D(width, height, TextureFormat.RFloat, false);

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

                dispMap.SetPixel(x, y, new Color(noiseValue, noiseValue, noiseValue, 1f));
            }
        }

        dispMap.Apply();
        return dispMap;
    }

    private Texture2D CalculateNormalsFromdispMap(Texture2D dispMap)
    {
        int width = dispMap.width;
        int height = dispMap.height;
        Texture2D normalMap = new Texture2D(width, height, TextureFormat.RGB24, false);

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                // Calculate finite differences for normal generation
                float left = dispMap.GetPixel(x - 1, y).r;
                float right = dispMap.GetPixel(x + 1, y).r;
                float top = dispMap.GetPixel(x, y - 1).r;
                float bottom = dispMap.GetPixel(x, y + 1).r;

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
}