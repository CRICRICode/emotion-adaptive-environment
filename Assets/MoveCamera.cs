using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPosition; // Reference to the camera position object
    
    // Update is called once per frame
    void Update()
    {
        transform.position = cameraPosition.position; // Set the camera position to the position of the cameraPosition object
    }
}
