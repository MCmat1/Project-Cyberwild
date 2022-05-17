using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    CharacterController controller;
    State state;

    [Header("References")]
    public Transform cameraHolder;
    public Transform grapplingHook;

    [Header("Camera")]
    public float mouseSensitivity = 500f;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float crouchSpeed = 2f;
    [Space(10)]
    public float jumpHeight = 6f;
    public float standHeight = 2.0f;
    public float crouchHeight = 1.0f;
    [Space(10)]
    public float moveSmoothing = 0.15f;
    public float crouchSmoothing = 10f;

    [Header("Grappling Hook")]
    public float grapplingHookSpeed = 20f;
    public float grapplingHookThrownSpeed = 40f;
    public float grapplingHookDistance = 30f;
    
    enum State 
    {
        Normal,
        GrapplingHookThrown,
        GrapplingHookFlyingPlayer  
    }

    //Look
    float cameraPitch;

    //Gravity
    float velocityY;

    bool isSprinting = false;
    bool isJumping = false;
    bool isCrouching = false;

    //Move
    float defaultSpeed = 4f;
    Vector2 currentDir;
    Vector2 currentDirVelocity;
    Vector3 direction;

    //grapplingHook
    float grapplingHookSize;
    Vector3 grapplingHookPosition;
    Vector3 characterVelocityMomentum;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        state = State.Normal;
        grapplingHook.gameObject.SetActive(false);
    }

    void Update()
    {   
        switch(state)
        {   
            default:
            case State.Normal:
                Look();
                Run();
                Crouch();
                Move();
                Gravity();
                Jump();
                GrapplingHookStart();
            break;
            case State.GrapplingHookThrown:
                Look();
                Run();
                Crouch();
                Move();
                Gravity();
                Jump();
                GrapplingHookThrown();
            break;
            case State.GrapplingHookFlyingPlayer:
                Look();
                GrapplingHookMovement();
            break;
        }
    }

    void Gravity()
    {
        float gravity = -19.82f;

        if(controller.isGrounded)
        {
            velocityY = -4.5f;
        }

        velocityY += gravity * Time.deltaTime;
    }

    void ResetGravity()
    {
        velocityY = 0f;
    }

    void Look()
    {
        Vector2 mouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        cameraPitch -= mouseInput.y * mouseSensitivity * Time.deltaTime;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        cameraHolder.localEulerAngles = Vector3.right * cameraPitch;
        transform.Rotate(Vector3.up * mouseInput.x * mouseSensitivity * Time.deltaTime);
    }

    void Move()
    {
        Vector2 targetDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        targetDir.Normalize();

        currentDir = Vector2.SmoothDamp(currentDir, targetDir, ref currentDirVelocity, moveSmoothing);

        direction = (transform.forward * currentDir.y + transform.right * currentDir.x) * defaultSpeed + Vector3.up * velocityY;
        if(!controller.isGrounded)
        {
            direction += characterVelocityMomentum;
        }

        controller.Move(direction * Time.deltaTime);

        if(characterVelocityMomentum.magnitude >= 0f)
        {
            float mometumDrag = 3f;
            characterVelocityMomentum -= characterVelocityMomentum * mometumDrag * Time.deltaTime;
            if(characterVelocityMomentum.magnitude < .0f)
            {
                characterVelocityMomentum = Vector3.zero;
            }
        }
    }

    void Run()
    {
        if (Input.GetKey(KeyCode.LeftShift) && isCrouching == false)
        {
            defaultSpeed = runSpeed;
            isSprinting = true; 
        }   
        else
        {
            defaultSpeed = walkSpeed;
            isSprinting = false;
            
        }
    }

    void Jump()
    {
        if(Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            isJumping = true;
            velocityY = jumpHeight;
        }  
        else
        {
            isJumping = false;
        }    
    }

    void AdjustHeight(float height)
    {
        float center = height / 2;

        controller.height = Mathf.Lerp(controller.height, height, crouchSmoothing * Time.deltaTime);
        controller.center = Vector3.Lerp(controller.center, new Vector3(0, center, 0), crouchSmoothing * Time.deltaTime);
    }

    void Crouch()
    {
        isCrouching = Input.GetKey(KeyCode.C);

        var desiredHeight = isCrouching ? crouchHeight : standHeight;

        if(Physics.Raycast(cameraHolder.transform.position, Vector3.up, 1f))
        {
            isCrouching = true;
        }

        if(controller.height != desiredHeight && !Physics.Raycast(cameraHolder.transform.position, Vector3.up, 1f))
        {
            AdjustHeight(desiredHeight);
            cameraHolder.transform.localPosition = new Vector3(0, controller.height - 0.25f, 0);
        }

        if(controller.isGrounded && isCrouching == true)
        {
            defaultSpeed = crouchSpeed;
        }
        else if(isSprinting == true)
        {
            defaultSpeed = runSpeed;
        }
    }

    void GrapplingHookStart()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            LayerMask hookable = LayerMask.GetMask("Hookable");
            if(Physics.Raycast(cameraHolder.transform.position, cameraHolder.transform.forward, out RaycastHit raycastHit, grapplingHookDistance, hookable))
            {      
                grapplingHookPosition = raycastHit.point;
                grapplingHookSize = 0f;
                grapplingHook.gameObject.SetActive(true);
                grapplingHook.localScale = Vector3.zero;
                state = State.GrapplingHookThrown;
            }
            else
            {
                state = State.Normal;
            }
        }

    }

    void GrapplingHookThrown()
    {
        grapplingHook.LookAt(grapplingHookPosition);
        grapplingHookSize += grapplingHookThrownSpeed * Time.deltaTime;
        grapplingHook.localScale = new Vector3(1, 1, grapplingHookSize);

        if(grapplingHookSize >= Vector3.Distance(transform.position, grapplingHookPosition))
        {
            state = State.GrapplingHookFlyingPlayer;
        }
    }

    void GrapplingHookMovement()
    {
        grapplingHook.LookAt(grapplingHookPosition);
        Vector3 grapplingHookDir = (grapplingHookPosition - transform.position).normalized;

        float grapplingHookReleaseDistance = 1f;

        controller.Move(grapplingHookDir * grapplingHookSpeed * Time.deltaTime);
        grapplingHookSize -= grapplingHookSpeed * Time.deltaTime;
        grapplingHook.localScale = new Vector3(1, 1, grapplingHookSize);
        
        if(Vector3.Distance(transform.position, grapplingHookPosition) < grapplingHookReleaseDistance || Input.GetKeyDown(KeyCode.E))
        {
            characterVelocityMomentum = grapplingHookDir * grapplingHookSpeed;
            StopGrapplingHook();
        }
        else if(Input.GetKeyDown(KeyCode.Space))
        {
            StopGrapplingHook();
            float jumpSpeed = 10f;
            characterVelocityMomentum = grapplingHookDir * grapplingHookSpeed;
            characterVelocityMomentum += Vector3.up * jumpSpeed;

        }
    }

    void StopGrapplingHook()
    {
        state = State.Normal;
        grapplingHook.gameObject.SetActive(false);
        ResetGravity();
    }
}
