using UnityEngine;

public class PlayerCam : MonoBehaviour
{

    public float sesX;
    public float sesY;

    public Transform orientation;

    float xRotation;
    float yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Blocco il cursore al centro dello schermo
        Cursor.visible = false; // Nascondo il cursore
    }

    // Update is called once per frame
    void Update()
    {
        float mousX = Input.GetAxis("Mouse X") * sesX * Time.deltaTime;
        float mousY = Input.GetAxis("Mouse Y") * sesY * Time.deltaTime;
        
        yRotation += mousX;
        xRotation -= mousY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Faccio il clamp della rotazione verticale per evitare rotazioni eccessive
        
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f); // Rotazione verticale della telecamera
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f); //Rotazione orizzontale della telecamera

    }
}


