using UnityEngine;

public class TestZoom : MonoBehaviour
{
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            cam.orthographicSize += 1f;

            Debug.Log(cam.orthographicSize);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            cam.orthographicSize -= 1f;

            Debug.Log(cam.orthographicSize);
        }
    }
}