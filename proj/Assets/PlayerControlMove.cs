using UnityEngine;

public class scriptComponent : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.transform.Translate(new Vector3(0, 0, 0.05f));
    }
}
