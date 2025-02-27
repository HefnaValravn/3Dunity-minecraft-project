using UnityEngine;
using UnityEngine.InputSystem;

public class scriptComponent : MonoBehaviour
{

    [SerializeField] public float forwardSpeed = 5f;
    [SerializeField] public float rightSpeed = 5f;

    [SerializeField] public float upSpeed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.transform.Translate(new Vector3(0, 0, forwardSpeed * Time.deltaTime));
        }

        if (Input.GetKey(KeyCode.S))
        {
            transform.transform.Translate(new Vector3(0, 0, -forwardSpeed * Time.deltaTime));
        }

        if (Input.GetKey(KeyCode.A))
        {
            transform.transform.Translate(new Vector3(-rightSpeed * Time.deltaTime, 0, 0));
        }

        if (Input.GetKey(KeyCode.D))
        {
            transform.transform.Translate(new Vector3(rightSpeed * Time.deltaTime, 0, 0));
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            transform.transform.Translate(new Vector3(0, -upSpeed * Time.deltaTime, 0));
        }

        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.transform.Translate(new Vector3(0, upSpeed * Time.deltaTime, 0));
        }
    }
}
