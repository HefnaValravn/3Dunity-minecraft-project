using UnityEngine;
using Unity.Mathematics;

public class ChunkTerrain
{
    private BlockType[,,] blocks;
    private TerrainGenerator terrainGenerator;
    private int2 chunkCoordinate;
    private int sizeX, sizeY, sizeZ;

    public ChunkTerrain(BlockType[,,] blocks, TerrainGenerator terrainGen, int2 chunkCoord, int sizeX, int sizeY, int sizeZ)
    {
        this.blocks = blocks;
        this.terrainGenerator = terrainGen;
        this.chunkCoordinate = chunkCoord;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
    }

    public void GenerateTerrainBlocks()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                float worldX = x + chunkCoordinate.x * sizeX;
                float worldZ = z + chunkCoordinate.y * sizeZ;

                int terrainHeight = terrainGenerator.GetTerrainHeight(worldX, worldZ);
                int bedrockHeight = terrainGenerator.GetBedrockHeight(worldX, worldZ);

                // These track grass layer for smoothing the terrain surface
                bool grassLayerPlaced = false;
                bool hasGrass = false;

                for (int y = 0; y <= terrainHeight; y++)
                {
                    if (y < bedrockHeight)
                    {
                        blocks[x, y, z] = BlockType.Bedrock;
                    }
                    else if (y < terrainHeight - 4)
                    {
                        blocks[x, y, z] = BlockType.Stone;
                    }
                    else if (y < terrainHeight)
                    {
                        blocks[x, y, z] = BlockType.Dirt;
                    }
                    else if (y == terrainHeight)
                    {
                        blocks[x, y, z] = BlockType.Grass;
                        hasGrass = true;
                    }
                }

                // Remove excess grass from the top
                if (hasGrass)
                {
                    for (int y = terrainHeight; y >= 0; y--)
                    {
                        if (blocks[x, y, z] == BlockType.Grass)
                        {
                            if (!grassLayerPlaced)
                            {
                                // Keep the first (lowest) grass block
                                grassLayerPlaced = true;
                            }
                            else
                            {
                                // Remove any grass blocks above the first one
                                blocks[x, y, z] = BlockType.Air;
                            }
                        }
                    }
                }
            }
        }

        // Second pass: Remove isolated grass blocks
        RemoveIsolatedGrass();
    }

    private void RemoveIsolatedGrass()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = 1; y < sizeY - 1; y++)
                {
                    if (blocks[x, y, z] == BlockType.Grass)
                    {
                        // Check if all surrounding blocks are air
                        bool isIsolated = true;
                        int[] dx = { -1, 1, 0, 0, 0, 0 };
                        int[] dy = { 0, 0, -1, 1, 0, 0 };
                        int[] dz = { 0, 0, 0, 0, -1, 1 };

                        for (int i = 0; i < 6; i++)
                        {
                            int nx = x + dx[i];
                            int ny = y + dy[i];
                            int nz = z + dz[i];

                            if (nx >= 0 && nx < sizeX &&
                                ny >= 0 && ny < sizeY &&
                                nz >= 0 && nz < sizeZ)
                            {
                                if (blocks[nx, ny, nz] != BlockType.Air)
                                {
                                    isIsolated = false;
                                    break;
                                }
                            }
                        }

                        if (isIsolated)
                        {
                            blocks[x, y, z] = BlockType.Air;
                        }
                    }
                }
            }
        }
    }

    public void GenerateCaves()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float worldX = x + chunkCoordinate.x * sizeX;
                    float worldY = y;
                    float worldZ = z + chunkCoordinate.y * sizeZ;

                    // Check if this block should be part of a cave
                    if (blocks[x, y, z] != BlockType.Air && terrainGenerator.IsCaveBlock(worldX, worldY, worldZ))
                    {
                        blocks[x, y, z] = BlockType.Air;
                    }
                }
            }
        }
    }

    public void ConvertGrassToRiverbed(int waterLevel)
    {
        // Identify blocks that are grass and below or at water level
        for (int x = 0; x < sizeX; x++)
        {
            for (int z = 0; z < sizeZ; z++)
            {
                for (int y = waterLevel; y >= 0; y--)
                {
                    // If we find a grass block below water level, convert it to dirt
                    if (blocks[x, y, z] == BlockType.Grass)
                    {
                        if (y + 1 <= waterLevel)
                        {
                            blocks[x, y, z] = BlockType.Dirt;
                        }
                    }
                }
            }
        }
    }
}