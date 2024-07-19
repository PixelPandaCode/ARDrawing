using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPanel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate the object to face the camera
        transform.LookAt(Camera.main.transform);
        // Optional: Invert the rotation if needed to make the front face the camera
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        transform.Rotate(-90, 0, 0);
    }
}
