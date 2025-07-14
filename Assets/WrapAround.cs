using UnityEngine;

public class WrapAround : MonoBehaviour
{

    // Limiti impostabili dal Inspector
    public float minX = -35f, maxX = 35f; // Limiti orizzontali
    public float minZ = -35f, maxZ = 35f;   // Limiti verticali

    private void LateUpdate()               
    {
        Vector3 p = transform.position; // ottiene la posizione corrente dell'oggetto

        if (p.x < minX)      p.x = maxX; //controlla se la posizione x è al di fuori dei limiti
        else if (p.x > maxX) p.x = minX; 

        if (p.z < minZ)      p.z = maxZ; //controlla se la posizione z è al di fuori dei limiti
        else if (p.z > maxZ) p.z = minZ;

        transform.position = p; // aggiorna la posizione dell'oggetto
    }
}



