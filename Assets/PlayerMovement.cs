using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Movement Settings")]
    public float moveSpeed;

    public float groundDrag; // Quando il giocatore è a terra, la velocità di movimento sarà limitata da questo valore

    [Header("Gorund Check")]
    public float playerHeight; 
    public LayerMask ground; 
    bool grounded;

    public Transform orientation; // Orientamento del personaggio, usato per calcolare la direzione del movimento

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // Previene la rotazione del Rigidbody per evitare comportamenti indesiderati durante il movimento
    }

    // Update is called once per frame
    public void Update()
    {

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, ground); // Controlla se il giocatore è a terra usando un raycast verso il basso
        
        MyInput(); 
        SpeedControl();

        if (grounded) // Se è a terra
        {
            rb.linearDamping = groundDrag; // Appica la resistenza al movimento quando il giocatore è a terra
        }
        else
        {
            rb.linearDamping = 0f; // La rimuove quando il giocatore non è a terra
        }
        
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal"); 
        verticalInput = Input.GetAxisRaw("Vertical"); 
    }

    private void MovePlayer(){

        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput; // Calcola la direzione del movimento in base all'input

        rb.AddForce(moveDirection.normalized * 10f * moveSpeed, ForceMode.Force); // Applico la forza al Rigidbody
    
    }

    private void SpeedControl(){
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed) // Controlla se la velocità orizzontale supera la velocità di movimento
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed; // Limito la velocità orizzontale
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z); // Applico la velocità limitata al Rigidbody
        }
    }
}

