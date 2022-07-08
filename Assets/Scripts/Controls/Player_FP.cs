using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_FP : MonoBehaviour
{
    [SerializeField] private Transform groundCollider;
    public float playerHeight { get; set; } = 1.8f;

    [Header("Speed")]
    [SerializeField] private float maxSpeed = 260.0f;
    [SerializeField] private float maxSprintSpeed = 0.0f;
    [Tooltip("Momentum")][SerializeField] private float walkAcceleration = 130.0f;
    [SerializeField] private float sprintAcceleration = 0.0f;
    private float envSpeedVal = 1.0f;
    [SerializeField] private float speed = 0;

    [Space][Header("Jumping")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float lowJumpGrav = 4.0f;
    [SerializeField] private float bigJumpGrav = 2.5f;

    [Space][Header("Step Cycle")]
    [SerializeField] private float runstepInterval = 0.6f;
    [SerializeField] private float stepInterval = 12.0f;
    private float stepCycle = 0.0f;
    private float nextStep = 0.0f;
    
    [SerializeField] private float death_Depht = -50.0f;

    private Rigidbody rb;    
    private PlayerController controls;
    private FirstPersonCamera cameraSc;
    private Transform cam;
    private Vector2 moveInput = Vector2.zero;
    private Vector3 origin = Vector3.zero;

    //* States
    public int playerState = 0;
    private bool isGrounded = true;
    private bool isJumping = false;
    private bool isSprinting = false;
    private bool isWalking = false;
    private bool isMoving = false;    
    private bool playerIsStopped = false;
    public Vector3 lookDirection { get; set; } = Vector3.zero;


    void Awake() 
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main.transform;
        cameraSc = Camera.main.GetComponent<FirstPersonCamera>();

        origin = transform.position;
        playerHeight = transform.localScale.y * 0.75f;

        maxSprintSpeed = maxSpeed * 1.5f;
        sprintAcceleration = walkAcceleration / 2;

        // Inputs
        controls = new PlayerController();
        controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.Player.Move.canceled += _ => moveInput = new Vector2(0,0);

        controls.Player.Sprint.performed += _ => Sprint();
        controls.Player.Sprint.canceled += _ => Sprint();

        controls.Player.Look.started += ctx => cameraSc.Look(ctx.ReadValue<Vector2>());

        controls.Player.Move.performed += _ => isMoving = true; 
        controls.Player.Move.canceled += _ => isMoving = false;

        controls.Player.Jump.performed += _ => Jump();
        controls.Player.Jump.canceled += _ => isJumping = false;

    }

    void FixedUpdate()
    {
        Move();

        // if fall of map -> spawn at origin
        if (transform.position.y <= death_Depht) { transform.position = origin; }
    }

    // Move and Rotate Player
    void Move()
    {

        Vector3 direction = new Vector3(moveInput.x, 0.0f, moveInput.y);
        //Debug.Log("Input Direction: " + direction);

        Vector3 moveDir = new Vector3(0.0f, 0.0f, 0.0f);
        
        // calc moveInput speed and sprintSpeed
        float maxMovingSpeed = isSprinting ? maxSprintSpeed : maxSpeed;
        float currentAcceleration = isSprinting ? sprintAcceleration : walkAcceleration;
        if (speed < maxMovingSpeed) speed = speed + currentAcceleration * Time.fixedDeltaTime;
        speed = Mathf.Clamp(speed, 0, maxMovingSpeed);

        isWalking = moveInput.magnitude > 0 ? true : false;
        isGrounded = GroundCheck();

        if (direction == Vector3.zero) {stepCycle = nextStep = 0;}

        float targetAngle = Mathf.Atan2(direction.x,direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, cam.eulerAngles.y, 0f);

        // Walk Direction & Orientation
        if (direction.magnitude == 0) // -> stand still
        { 
            moveDir = Quaternion.Euler(0f,targetAngle, 0f) * Vector3.zero;
        } 
        else if (direction.magnitude > 0) // -> move
        {
            moveDir = Quaternion.Euler(0f,targetAngle, 0f) * Vector3.forward;
        }  


        // Jumping & Falling
        if (rb.velocity.y < 0) // -> big Jump
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (bigJumpGrav - 1) * Time.deltaTime;
        } 
        else if (rb.velocity.y > 0 && !isJumping) // -> Fall
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpGrav - 1) * Time.deltaTime;
        }


        // add values to player 
        if(!playerIsStopped) 
        {
            float movingSpeed = speed * envSpeedVal * Time.deltaTime;
            rb.velocity = new Vector3(moveDir.x * movingSpeed, rb.velocity.y, moveDir.z * movingSpeed);
        }


        // calculate head bobbing
        if (isGrounded && !playerIsStopped && isWalking) StepCycle(speed);


        // Player Movement States
        if (!isMoving && isGrounded && !isJumping) playerState = 0; // idle
        else if (isMoving && isGrounded && !isJumping && isSprinting) playerState = 3; // sprinting
        else if (isMoving && isGrounded && !isJumping) playerState = 1; // walking
        else if (!isGrounded) playerState = 2; // inAir

    }

    
    void Jump()
    {
        if (GroundCheck())
        {
            rb.velocity = Vector3.up * jumpForce;
            isJumping = true;
        } 
        else 
        {
            isJumping = false;
        }
    }


    bool GroundCheck()
    {
        RaycastHit hit;
        Physics.Raycast(groundCollider.position, Vector3.down, out hit, 0.5f);
        return hit.collider != null;
    }


    void Sprint() 
    {
        if (isSprinting) 
        {
            speed/=2;
            isSprinting = false;
        } 
        else 
        {
            speed*=2; 
            isSprinting = true;
        }
    }


    void StepCycle(float speed)
    {
        if (moveInput.magnitude > 0) 
        {
            stepCycle += (moveInput.magnitude + (speed*(isSprinting ? runstepInterval : 1f))/10)*
                             Time.fixedDeltaTime;
        }
        else if (stepCycle > 1)
        {
            stepCycle = 0;
            nextStep = 0;
        }

        float stepProgressAmount = (nextStep - stepCycle) / stepInterval;
        cameraSc.HeadMove(stepProgressAmount, isSprinting);
        
        if (!(stepCycle > nextStep))
        {
            return;
        }

        nextStep = stepCycle + stepInterval;
        
        
        PlayFootStepSound();
    }


    void PlayFootStepSound()
    {
        // Play Footstep Audio
    }


    private void OnEnable() => controls.Enable();
    private void OnDisable() => controls.Disable();
}
