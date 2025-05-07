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
        bedrockMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        stoneMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        dirtMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        grassSideMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        grassTopMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        obsidianMaterial = CreateObsidianMaterial();

        bedrockMaterial.SetFloat("_Smoothness", 0.1f);
        stoneMaterial.SetFloat("_Smoothness", 0.1f);
        dirtMaterial.SetFloat("_Smoothness", 0.0f);
        grassSideMaterial.SetFloat("_Smoothness", 0.1f);
        grassTopMaterial.SetFloat("_Smoothness", 0.1f);

        bedrockMaterial.SetFloat("_Metallic", 0.0f);
        stoneMaterial.SetFloat("_Metallic", 0.0f);
        dirtMaterial.SetFloat("_Metallic", 0.0f);
        grassSideMaterial.SetFloat("_Metallic", 0.0f);
        grassTopMaterial.SetFloat("_Metallic", 0.0f);

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

    private Material CreateObsidianMaterial()
    {
        Shader ObsidianShader = Shader.Find("Custom/Obsidian");
        if (ObsidianShader == null)
            ObsidianShader = Shader.Find("Obsidian");

        Material ObsidianMaterial = new Material(ObsidianShader);

        // Generate dispMap for normals
        Texture2D dispMap = GenerateProceduraldispMap(128, 128);
        Texture2D normalMap = CalculateNormalsFromdispMap(dispMap);

        ObsidianMaterial.SetTexture("_NormalMapTex", normalMap);
        ObsidianMaterial.SetColor("_Color", new Color(0.3f, 0.3f, 0.4f, 1f));
        ObsidianMaterial.SetFloat("_Metallic", 0.9f);
        ObsidianMaterial.SetFloat("_Smoothness", 0.7f);
        ObsidianMaterial.SetFloat("_DispMapBlend", 0.661f); // 66% blend between procedural and texture
        ObsidianMaterial.SetFloat("_DispTexScale", 0.7f);  // Scale the texture sampling
        ObsidianMaterial.SetFloat("_HeightScale", 0.3f);

        return ObsidianMaterial;
    }

    private Texture2D GenerateProceduraldispMap(int width, int height)
    {
        Texture2D dispMap = new Texture2D(width, height, TextureFormat.RFloat, false);

        // Seed for variation
        float seedX = Random.Range(0f, 100f);
        float seedY = Random.Range(0f, 100f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Use multiple octaves of Perlin noise for more interesting terrain
                float frequency = 3.5f;
                float amplitude = 0.8f;
                float noiseValue = 0f;
                float totalAmplitude = 0f;

                // More octaves for finer detail
                for (int octave = 0; octave < 6; octave++)
                {
                    float sampleX = (x / (float)width * frequency) + seedX;
                    float sampleY = (y / (float)height * frequency) + seedY;

                    // Add some domain warping for more natural patterns
                    float warp = Mathf.PerlinNoise(sampleX * 2f, sampleY * 2f) * 0.1f;
                    sampleX += warp;
                    sampleY += warp;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleY);

                    // Add some ridged noise patterns by modifying the Perlin output
                    if (octave % 2 == 1)
                        perlin = 1f - Mathf.Abs((perlin * 2f) - 1f); // Ridged noise

                    noiseValue += perlin * amplitude;
                    totalAmplitude += amplitude;

                    frequency *= 2.1f;
                    amplitude *= 0.45f;
                }

                // Normalize and add a subtle bias toward darker values
                noiseValue = noiseValue / totalAmplitude;
                noiseValue = Mathf.Pow(noiseValue, 1.2f);

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

        float normalStrength = 2.5f;

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                // Use wrapping for seamless textures
                int xPrev = (x - 1 + width) % width;
                int xNext = (x + 1) % width;
                int yPrev = (y - 1 + height) % height;
                int yNext = (y + 1) % height;

                // Calculate finite differences for normal generation
                float heightLeft = dispMap.GetPixel(xPrev, y).r;
                float heightRight = dispMap.GetPixel(xNext, y).r;
                float heightUp = dispMap.GetPixel(x, yPrev).r;
                float heightDown = dispMap.GetPixel(x, yNext).r;

                // Calculate slopes with adjustable strength
                float dX = (heightLeft - heightRight) * normalStrength;
                float dY = (heightUp - heightDown) * normalStrength;

                // Create normal vector - the Z component affects how pronounced the normal is
                Vector3 normal = new Vector3(dX, dY, 1.0f).normalized;

                // Convert normal from [-1, 1] to [0, 1] color range
                Color normalColor = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f,
                    1.0f
                );

                normalMap.SetPixel(x, y, normalColor);
            }
        }

        normalMap.Apply();
        return normalMap;
    }
}