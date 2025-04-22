using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Video;

public class ChunkPortalGenerator
{
    private BlockType[,,] blocks;
    private TerrainGenerator terrainGenerator;
    private int2 chunkCoordinate;
    private int sizeX, sizeY, sizeZ;
    private Material portalCoreMaterial;
    private Vector3 portalFramePosition;
    private Transform chunkTransform;
    private Vector3 gudposition; // Position for particle effects

    public ChunkPortalGenerator(BlockType[,,] blocks, TerrainGenerator terrainGen, int2 chunkCoord, 
                               Transform chunkTransform, int sizeX, int sizeY, int sizeZ)
    {
        this.blocks = blocks;
        this.terrainGenerator = terrainGen;
        this.chunkCoordinate = chunkCoord;
        this.chunkTransform = chunkTransform;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        this.portalCoreMaterial = CreatePortalCoreShader();
    }

    public Vector3 GeneratePortal()
    {
        if (terrainGenerator.ShouldGeneratePortal(chunkCoordinate))
        {
            Debug.Log($"Portal generation started in chunk {chunkCoordinate.x},{chunkCoordinate.y}");
            Vector2 portalLocation = GetPortalLocationInChunk(chunkCoordinate);
            int portalX = Mathf.FloorToInt(portalLocation.x);
            int portalZ = Mathf.FloorToInt(portalLocation.y);
            Debug.Log($"Portal location: X={portalX}, Z={portalZ}");

            int portalY = terrainGenerator.GetTerrainHeight(
                portalLocation.x + chunkCoordinate.x * sizeX, 
                portalLocation.y + chunkCoordinate.y * sizeZ);

            // Store portal location
            portalFramePosition = new Vector3(
                portalX + chunkCoordinate.x * sizeX + 0.5f,
                portalY + 2.5f, // Center of the 4x5 frame
                portalZ + chunkCoordinate.y * sizeZ + 0.5f
            );
            
            gudposition = new Vector3(portalX, portalY, portalZ);
            Debug.Log($"Portal Y position: {portalY}");

            // Create 4x5 portal frame of obsidian
            for (int x = portalX - 1; x <= portalX + 2; x++)
            {
                for (int y = portalY; y < portalY + 5; y++)
                {
                    // Z-loop is fixed to only iterate over the single Z-plane
                    int z = portalZ;

                    // Check if we're within chunk bounds first
                    if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ)
                    {
                        continue; // Skip this block if it's outside the chunk
                    }

                    // Check if this is a frame block
                    if (x == portalX - 1 || x == portalX + 2 || // Left and right frame
                        y == portalY || y == portalY + 4)       // Top and bottom frame
                    {
                        blocks[x, y, z] = BlockType.Obsidian;
                        Debug.Log($"Set obsidian at {x},{y},{z}");
                    }
                    // Create portal core - only within the frame
                    else if (x > portalX - 1 && x < portalX + 2 && y > portalY && y < portalY + 4)
                    {
                        blocks[x, y, z] = BlockType.PortalCore;
                        Debug.Log($"Set portal core at {x},{y},{z}");
                    }
                }
            }
            
            return portalFramePosition;
        }
        
        return Vector3.zero;
    }

    public void GeneratePortalPlane()
    {
        // Remove any existing portal planes
        foreach (Transform child in chunkTransform)
        {
            if (child.name.StartsWith("PortalPlane_"))
            {
                Object.Destroy(child.gameObject);
            }
        }

        // First find all portal core locations
        List<Vector3Int> portalCoreLocations = new List<Vector3Int>();

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    if (blocks[x, y, z] == BlockType.PortalCore)
                    {
                        // We found a portal block
                        portalCoreLocations.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        if (portalCoreLocations.Count == 0)
            return;

        // Group portal core blocks by Z coordinate (assuming portal faces Z direction)
        var portalsGroupedByZ = portalCoreLocations.GroupBy(pos => pos.z);

        foreach (var portalGroup in portalsGroupedByZ)
        {
            int portalZ = portalGroup.Key;

            // Find min and max X, Y values
            int minX = portalGroup.Min(pos => pos.x);
            int maxX = portalGroup.Max(pos => pos.x);
            int minY = portalGroup.Min(pos => pos.y);
            int maxY = portalGroup.Max(pos => pos.y);

            // Skip portal cores that span outside the current chunk
            if (minX < 0 || maxX >= sizeX || minY < 0 || maxY >= sizeY)
            {
                Debug.LogWarning($"Skipping portal plane at Z={portalZ} because it spans outside the chunk bounds.");
                continue;
            }

            // Create a single portal plane GameObject
            GameObject portalPlane = new GameObject("PortalPlane_" + portalZ);
            portalPlane.transform.SetParent(chunkTransform);

            // Position portal plane - crucial for proper positioning
            portalPlane.transform.localPosition = portalFramePosition / 4096;

            // Add components
            MeshFilter meshFilter = portalPlane.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = portalPlane.AddComponent<MeshRenderer>();

            // Create a single plane
            Mesh planeMesh = new Mesh();

            // Create vertices for a single plane
            Vector3[] vertices = new Vector3[4];
            float zOffset = 0.5f; // Small offset for portal position

            // Use absolute positions that include the chunk offset
            vertices[0] = new Vector3(minX, minY, portalZ + zOffset); // Bottom Left
            vertices[1] = new Vector3(maxX + 1, minY, portalZ + zOffset); // Bottom Right
            vertices[2] = new Vector3(maxX + 1, maxY + 1, portalZ + zOffset); // Top Right
            vertices[3] = new Vector3(minX, maxY + 1, portalZ + zOffset); // Top Left

            // Create triangles for front and back faces
            int[] triangles = new int[12];
            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
            triangles[6] = 1; triangles[7] = 2; triangles[8] = 0;
            triangles[9] = 2; triangles[10] = 3; triangles[11] = 0;

            // Create UVs
            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(1, 1);
            uvs[3] = new Vector2(0, 1);

            // Assign to mesh
            planeMesh.vertices = vertices;
            planeMesh.triangles = triangles;
            planeMesh.uv = uvs;
            planeMesh.RecalculateNormals();

            // Assign mesh and material
            meshFilter.mesh = planeMesh;
            meshRenderer.material = portalCoreMaterial;

            // Set up video player and audio
            ConfigureVideoPlayer(portalPlane);

            // Calculate the portal dimensions based on min/max values
            Vector3 portalDimensions = new Vector3(
                maxX - minX + 1,  // Width
                maxY - minY + 1,  // Height
                0.1f              // Depth
            );

            // Add portal particles
            AddPortalParticles(portalPlane, portalDimensions);
        }
    }

    private void ConfigureVideoPlayer(GameObject portalPlane)
    {
        MeshRenderer meshRenderer = portalPlane.GetComponent<MeshRenderer>();
        
        // Configure VideoPlayer
        VideoPlayer videoPlayer = portalPlane.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = true;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
        videoPlayer.targetMaterialRenderer = meshRenderer;
        videoPlayer.targetMaterialProperty = "_MainTex";
        videoPlayer.waitForFirstFrame = true;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        AudioSource audioSource = portalPlane.AddComponent<AudioSource>();
        videoPlayer.SetTargetAudioSource(0, audioSource);

        audioSource.playOnAwake = true;
        audioSource.spatialBlend = 0.0f;
        audioSource.loop = true;
        audioSource.volume = 0.6f;

        // Dynamic audio adjustment
        DynamicAudioAdjuster audioAdjuster = portalPlane.AddComponent<DynamicAudioAdjuster>();
        audioAdjuster.Initialize(audioSource, portalPlane.transform.position);

        videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, "Videos", "finalskel.ogv");
        videoPlayer.Prepare();
        
        AudioSource capturedAudioSource = audioSource;
        videoPlayer.prepareCompleted += (vp) =>
        {
            vp.playbackSpeed = 1.2f;
            vp.Play();
        };
    }

    private void AddPortalParticles(GameObject portalPlane, Vector3 portalSize)
    {
        // Front side particles
        GameObject frontParticlesObj = new GameObject("PortalParticles_Front");
        frontParticlesObj.transform.SetParent(portalPlane.transform);
        frontParticlesObj.transform.localPosition = new Vector3(gudposition.x + 1, gudposition.y + 3, gudposition.z);

        // Add and initialize the front particle system
        PortalParticleSystem frontParticleSystem = frontParticlesObj.AddComponent<PortalParticleSystem>();
        frontParticleSystem.Initialize(portalPlane.transform, portalSize);

        // Back side particles
        GameObject backParticlesObj = new GameObject("PortalParticles_Back");
        backParticlesObj.transform.SetParent(portalPlane.transform);
        backParticlesObj.transform.localPosition = new Vector3(gudposition.x + 1, gudposition.y + 3, gudposition.z);

        // Rotate the back particle system 180 degrees around Y-axis
        backParticlesObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        // Add and initialize the back particle system
        PortalParticleSystem backParticleSystem = backParticlesObj.AddComponent<PortalParticleSystem>();
        backParticleSystem.Initialize(portalPlane.transform, portalSize);
    }

    private Vector2 GetPortalLocationInChunk(int2 chunkCoordinate)
    {
        // Get water level
        int waterLevel = 62; // Default water level
        ChunkManager chunkManager = Object.FindFirstObjectByType<ChunkManager>();
        if (chunkManager != null)
        {
            waterLevel = chunkManager.waterLevel;
        }

        // Start from 10 blocks below the top to avoid checking empty air space
        int startY = Mathf.Min(sizeY - 10, sizeY - 1);

        // Add debugging to track progress
        int candidatesChecked = 0;
        int flatAreasFound = 0;

        for (int y = startY; y >= 1; y--) // Start at 1 to avoid checking y-1 < 0
        {
            if (y + 5 <= waterLevel)
            {
                Debug.Log($"Skipping Y level {y} because it's below water level {waterLevel}");
                continue;
            }

            for (int x = 6; x < sizeX - 7; x++)
            {
                for (int z = 6; z < sizeZ - 7; z++)
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
                                        if (y + h < sizeY && IsBlockSolid(x + dx, y + h, z + dz))
                                        {
                                            surroundingsValid = false;
                                        }
                                    }
                                }
                            }
                        }

                        // If surroundings are valid, we've found our spot!
                        if (surroundingsValid)
                        {
                            Debug.Log($"Found portal location at ({x},{y},{z}) after checking {candidatesChecked} candidates and {flatAreasFound} flat areas");
                            gudposition = new Vector3(x, y, z);
                            return new Vector2(x, z);
                        }
                    }
                }
            }
        }

        Debug.LogWarning($"No suitable portal location found in chunk {chunkCoordinate.x},{chunkCoordinate.y} after checking {candidatesChecked} candidates and {flatAreasFound} flat areas");

        // As a fallback, place portal at center of chunk if possible
        int centerX = sizeX / 2;
        int centerZ = sizeZ / 2;

        // Try to find suitable Y for center placement
        for (int y = startY; y >= 1; y--)
        {
            if (blocks[centerX, y, centerZ] == BlockType.Grass && y > waterLevel)
            {
                Debug.Log($"Using fallback portal location at center: ({centerX},{y},{centerZ})");
                return new Vector2(centerX, centerZ);
            }
        }

        // If all else fails, return center anyway
        return new Vector2(sizeX / 2, sizeZ / 2);
    }

    private bool IsBlockSolid(int x, int y, int z)
    {
        return BlockUtility.IsBlockSolid(blocks, x, y, z, sizeX, sizeY, sizeZ);
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

        // Set particle properties if they exist in the shader
        if (portalCoreShader.FindPropertyIndex("_ParticleColor") != -1)
        {
            portalCoreMaterial.SetColor("_ParticleColor", new Color(0.9f, 0.6f, 1.0f, 1.0f));
            portalCoreMaterial.SetFloat("_ParticleCount", 15f);
            portalCoreMaterial.SetFloat("_ParticleSpeed", 1.5f);
            portalCoreMaterial.SetFloat("_ParticleSize", 0.01f);
        }

        return portalCoreMaterial;
    }
}