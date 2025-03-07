using UnityEngine;

public class HeadTexturing : MonoBehaviour
{
    public Texture headTexture;
    public Transform headTransform;
    public float brightnessMultiplier = 1.5f; // Adjust this value to increase or decrease brightness

    void Start()
    {
        if (headTransform != null)
        {
            // Get the MeshRenderer component of the head
            //this also means this script uses whatever material is assigned to the MeshRenderer component of the head child within Unity!!!!!
            MeshRenderer headRenderer = headTransform.GetComponent<MeshRenderer>();

            if (headRenderer != null)
            {
                // Assign the chosen texture to the head
                headRenderer.material.mainTexture = headTexture;

                // Adjust the material color to make the texture brighter
                headRenderer.material.color *= brightnessMultiplier;

                // Get the MeshFilter component to access the mesh
                MeshFilter meshFilter = headTransform.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    Mesh mesh = meshFilter.mesh;
                    Vector2[] uv = new Vector2[mesh.uv.Length];

                    // Define UV mapping for each face of the cube
                    // Assuming the texture is divided into 6 parts (3x2 grid)
                    // Each face of the cube will get a 1/3 x 1/2 portion of the texture

                    // Front face
                    uv[0] = new Vector2(1f / 3f, 0f);
                    uv[1] = new Vector2(2f / 3f, 0f);
                    uv[2] = new Vector2(1f / 3f, 0.5f);
                    uv[3] = new Vector2(2f / 3f, 0.5f);

                    // Back face
                    uv[4] = new Vector2(0f, 0f);
                    uv[5] = new Vector2(1f / 3f, 0f);
                    uv[6] = new Vector2(0f, 0.5f);
                    uv[7] = new Vector2(1f / 3f, 0.5f);

                    // Top face
                    uv[8] = new Vector2(1f / 3f, 0.5f);
                    uv[9] = new Vector2(2f / 3f, 0.5f);
                    uv[10] = new Vector2(1f / 3f, 1f);
                    uv[11] = new Vector2(2f / 3f, 1f);

                    // Bottom face
                    uv[12] = new Vector2(2f / 3f, 0.5f);
                    uv[13] = new Vector2(1f, 0.5f);
                    uv[14] = new Vector2(2f / 3f, 1f);
                    uv[15] = new Vector2(1f, 1f);

                    // Left face
                    uv[16] = new Vector2(2f / 3f, 0f);
                    uv[17] = new Vector2(1f, 0f);
                    uv[18] = new Vector2(2f / 3f, 0.5f);
                    uv[19] = new Vector2(1f, 0.5f);

                    // Right face
                    uv[20] = new Vector2(0f, 0.5f);
                    uv[21] = new Vector2(1f / 3f, 0.5f);
                    uv[22] = new Vector2(0f, 1f);
                    uv[23] = new Vector2(1f / 3f, 1f);

                    // Assign the new UVs to the mesh
                    mesh.uv = uv;
                }
                else
                {
                    Debug.LogError("MeshFilter component not found on the head object.");
                }
            }
            else
            {
                Debug.LogError("MeshRenderer component not found on the head object.");
            }
        }
        else
        {
            Debug.LogError("Head Transform not assigned in the inspector.");
        }
    }
}
