using UnityEngine;

public class WaterGenerator : MonoBehaviour
{
    [Header("Water Settings")]
    public Material waterMaterial;
    public int waterLevel = 62; // Height at which water will be placed
    public float waterAmplitude = 0.1f; // How much the vertices move up/down
    public float waterFrequency = 1.0f; // Speed of water animation
    public float finiteDifferenceDelta = 0.01f; //for finite differencing

    private Mesh waterMesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Vector3[] originalVertices;
    private Vector3[] worldPositions; //positions of water planes
    private float[] heightOffsets;
    private Vector3[] normals;


    private void Start()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            // Set water to render after terrain
            meshRenderer.material.renderQueue = 3000; // Transparent queue
        }
    }

    public void Initialize(Vector3 position, int sizeX, int sizeZ, int xSquares, int zSquares)
    {
        // Create mesh components if they don't exist
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Generate the tesselated plane
        waterMesh = GenerateTesselatedPlane(sizeX, sizeZ, xSquares, zSquares);
        meshFilter.mesh = waterMesh;

        // Store original vertices for animation
        originalVertices = waterMesh.vertices;
        normals = new Vector3[originalVertices.Length];

        // Calculate and store world positions for each vertex
        worldPositions = new Vector3[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            // Transform to world space and store
            worldPositions[i] = transform.TransformPoint(originalVertices[i]);
        }

        // Position the water
        transform.position = position + new Vector3(0, waterLevel, 0);

        // Set material
        if (waterMaterial != null)
        {
            meshRenderer.material = waterMaterial;
        }
        else
        {
            // Create a basic blue material if none is assigned
            waterMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            waterMaterial.color = new Color(0.0f, 0.4f, 0.7f, 0.6f);
            waterMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            waterMaterial.renderQueue = 3000; // Transparent rendering queue
            meshRenderer.material = waterMaterial;
        }

    }

    public Mesh GenerateTesselatedPlane(int sizeX, int sizeZ, int xSquares, int zSquares)
    {
        Mesh mesh = new Mesh();

        // Calculate vertices
        Vector3[] vertices = new Vector3[(xSquares + 1) * (zSquares + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];

        float xStep = sizeX / (float)xSquares;
        float zStep = sizeZ / (float)zSquares;

        // Create vertices and UV coordinates
        for (int z = 0; z <= zSquares; z++)
        {
            for (int x = 0; x <= xSquares; x++)
            {
                int index = z * (xSquares + 1) + x;
                vertices[index] = new Vector3(x * xStep, 0, z * zStep);
                uvs[index] = new Vector2(x * xStep, z * zStep);
            }
        }

        // Create triangles
        int[] triangles = new int[xSquares * zSquares * 6]; // 2 triangles per square, 3 vertices per triangle

        int triangleIndex = 0;
        for (int z = 0; z < zSquares; z++)
        {
            for (int x = 0; x < xSquares; x++)
            {
                int vertexIndex = z * (xSquares + 1) + x;

                // First triangle
                triangles[triangleIndex++] = vertexIndex;
                triangles[triangleIndex++] = vertexIndex + xSquares + 1;
                triangles[triangleIndex++] = vertexIndex + 1;

                // Second triangle
                triangles[triangleIndex++] = vertexIndex + 1;
                triangles[triangleIndex++] = vertexIndex + xSquares + 1;
                triangles[triangleIndex++] = vertexIndex + xSquares + 2;
            }
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }

    // Calculate height at a specific world position using our wave function
    private float CalculateHeight(float x, float z, float time)
    {
        float xCoord = x * 0.3f;
        float zCoord = z * 0.3f;

        // Multiple combined sin functions for more interesting wave effects
        float height =
            Mathf.Sin(xCoord + time) * 0.3f +
            Mathf.Sin(xCoord * 2.0f + time * 1.1f) * 0.2f +
            Mathf.Sin(zCoord * 0.8f + time * 1.2f) * 0.3f +
            Mathf.Sin(zCoord * 1.6f + time * 0.9f) * 0.15f +
            Mathf.Sin((xCoord + zCoord) * 0.5f + time * 0.8f) * 0.2f +
            Mathf.Sin((xCoord - zCoord) * 0.7f + time * 1.3f) * 0.1f;

        height *= waterAmplitude;
        return height;
    }

    // Calculate normal using finite differencing as specified in the instructions
    private Vector3 CalculateNormal(float x, float z, float height, float time)
    {
        // Calculate points using finite differencing
        Vector3 pointA = new Vector3(x, height, z); // Current point (x, z, f(x, z))

        // Point B = (x+e, z, f(x+e, z))
        float heightB = CalculateHeight(x + finiteDifferenceDelta, z, time);
        Vector3 pointB = new Vector3(x + finiteDifferenceDelta, heightB, z);

        // Point C = (x, z+e, f(x, z+e))
        float heightC = CalculateHeight(x, z + finiteDifferenceDelta, time);
        Vector3 pointC = new Vector3(x, heightC, z + finiteDifferenceDelta);

        // Calculate vectors along the surface
        Vector3 vectorAB = pointB - pointA;
        Vector3 vectorAC = pointC - pointA;

        // Calculate normal using cross product
        Vector3 normal = Vector3.Cross(vectorAB, vectorAC).normalized;

        // Ensure normal points upward (for water surface)
        if (normal.y < 0)
            normal = -normal;

        return normal;
    }

    private void Update()
    {
        if (waterMesh == null || originalVertices == null)
            return;

        // Animate water surface
        Vector3[] vertices = new Vector3[originalVertices.Length];
        Vector3[] updatedNormals = new Vector3[originalVertices.Length];

        float time = Time.time * waterFrequency;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = originalVertices[i];

            // Get world position for consistent waves across chunks
            Vector3 worldPos = worldPositions[i];

            float height = CalculateHeight(worldPos.x, worldPos.z, time);

            // Apply the height offset if we have it
            if (heightOffsets != null && i < heightOffsets.Length)
            {
                height += heightOffsets[i];
            }

            vertices[i].y = height;
            updatedNormals[i] = CalculateNormal(worldPos.x, worldPos.z, height, time);
        }

        waterMesh.vertices = vertices;
        waterMesh.normals = updatedNormals;
    }

    public void AdjustForTerrain(BlockType[,,] blocks, int chunkSizeX, int chunkSizeY, int chunkSizeZ)
    {
        // Update world positions when adjusting for terrain
        if (waterMesh != null && originalVertices != null)
        {
            worldPositions = new Vector3[originalVertices.Length];
            for (int i = 0; i < originalVertices.Length; i++)
            {
                worldPositions[i] = transform.TransformPoint(originalVertices[i]);
            }
        }


        // Create a new array that will store height offsets
        heightOffsets = new float[originalVertices.Length];

        for (int i = 0; i < originalVertices.Length; i++)
        {
            // Get the vertex in world coordinates
            Vector3 worldPos = transform.TransformPoint(originalVertices[i]);

            // Convert to block coordinates
            int blockX = Mathf.FloorToInt(worldPos.x);
            int blockZ = Mathf.FloorToInt(worldPos.z);

            // Convert to local coordinates within this chunk
            int localX = blockX % chunkSizeX;
            if (localX < 0) localX += chunkSizeX;  // Handle negative coordinates

            int localZ = blockZ % chunkSizeZ;
            if (localZ < 0) localZ += chunkSizeZ;  // Handle negative coordinates

            // Verify we're within bounds
            if (localX >= 0 && localX < chunkSizeX && localZ >= 0 && localZ < chunkSizeZ)
            {
                // Find terrain height
                int terrainHeight = -1;
                for (int y = waterLevel + 2; y >= 0; y--)  // Check a bit above water level
                {
                    if (y < chunkSizeY && blocks[localX, y, localZ] != BlockType.Air)
                    {
                        terrainHeight = y;
                        break;
                    }
                }


                if (terrainHeight >= waterLevel - 5 && terrainHeight <= waterLevel + 2)
                {
                    // If terrain is at or above water level, make water higher here
                    if (terrainHeight >= waterLevel - 1)
                    {
                        heightOffsets[i] = 0.2f;  // Raise water slightly
                    }
                    // If terrain is just below water level, create a shore gradient
                    else if (terrainHeight >= waterLevel - 3)
                    {
                        // Create a gradient effect for shore
                        heightOffsets[i] = 0.1f;
                    }
                }
            }

        }
    }
}