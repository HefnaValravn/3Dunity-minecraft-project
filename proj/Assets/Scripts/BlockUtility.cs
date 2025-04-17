using UnityEngine;

public static class BlockUtility
{
    public static bool IsBlockSolid(BlockType[,,] blocks, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
    {
        // Check if coordinates are within bounds
        if (x >= 0 && x < sizeX && y >= 0 && y < sizeY && z >= 0 && z < sizeZ)
        {
            // Check if this block position contains a solid block
            return blocks[x, y, z] == BlockType.Bedrock ||
                   blocks[x, y, z] == BlockType.Stone ||
                   blocks[x, y, z] == BlockType.Dirt ||
                   blocks[x, y, z] == BlockType.Grass ||
                   blocks[x, y, z] == BlockType.Obsidian;
        }

        // For coordinates outside, consider edge as having no neighbors
        return false;
    }
}