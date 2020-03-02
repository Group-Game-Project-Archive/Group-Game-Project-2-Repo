using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    //private const float NORMAL_FOV = 60f;
    //private const float HOOKSHOT_FOV = 100f;

    [SerializeField] private float mouseSensitivity = 1f;
    [SerializeField] private Transform debugHitPointTransform;
    [SerializeField] private Transform hookshotTransform;

    private CharacterController characterController;
    private float cameraVerticalAngle;
    private float characterVelocityY;
    private Vector3 characterVelocityMomentum;
    private Camera playerCamera;
    private CameraFov cameraFov;
    private State state;
    private Vector3 hookshotPosition;
    private float hookshotSize;


    public AudioSource audioSource;
    public AudioClip grappleSound;
    [Range(0.0f, 1.0f)]
    public float grappleVolume;


    private enum State
    {
        Normal,
        HookshotThrown,
        HookshotFlyingPlayer,
        
    }

    // Start is called before the first frame update
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = transform.Find("Camera").GetComponent<Camera>();
        cameraFov = playerCamera.GetComponent<CameraFov>();
        Cursor.lockState = CursorLockMode.Locked;
        state = State.Normal;
        hookshotTransform.gameObject.SetActive(false);
    }

    // Update is called once per frame
    private void Update()
    {
        {
            switch (state)
            {
                default:
                case State.Normal:
                    HandleCharacterLook();
                    HandleCharacterMovement();
                    HandleHookshotStart();
                    break;
                case State.HookshotThrown:
                    HandleHookshotThrow();
                    HandleCharacterLook();
                    HandleCharacterMovement();
                    break;
                case State.HookshotFlyingPlayer:
                    HandleCharacterLook();
                    HandleHookshotMovement();
                    break;
            }
        }
    }
    private void HandleCharacterLook()
    {
        float lookX = Input.GetAxisRaw("Mouse X");
        float lookY = Input.GetAxisRaw("Mouse Y");

        //rotate the transform with the input speed on the local Y axis
        transform.Rotate(new Vector3(0f, lookX * mouseSensitivity, 0f), Space.Self);

        cameraVerticalAngle -= lookY * mouseSensitivity;

        cameraVerticalAngle = Mathf.Clamp(cameraVerticalAngle, -89f, 89);

        playerCamera.transform.localEulerAngles = new Vector3(cameraVerticalAngle, 0, 0);
    }

    private void HandleCharacterMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        float moveSpeed = 20f;

        Vector3 characterVelocity = transform.right * moveX * moveSpeed + transform.forward * moveZ * moveSpeed;

        if (characterController.isGrounded)
        {
            characterVelocityY = 0f;
            //Jump
            if (TestInputJump())
            {
                float jumpSpeed = 30f;
                characterVelocityY = jumpSpeed;
            }
        }
        //This applies gravity to the velocity and vector
        float gravityDownForce = -60f;
        characterVelocityY += gravityDownForce * Time.deltaTime;

        characterVelocity.y = characterVelocityY;
        //Apply momentum
        characterVelocity += characterVelocityMomentum;

        characterController.Move(characterVelocity * Time.deltaTime);

        //Dampen momentum
        if(characterVelocityMomentum.magnitude >= 0f)
        {
            float momentumDrag = 3f;
            characterVelocityMomentum -= characterVelocityMomentum * momentumDrag * Time.deltaTime;
            if(characterVelocityMomentum.magnitude < .0f)
            {
                characterVelocityMomentum = Vector3.zero;
            }
        }
    }
    private void ResetGravityEffect()
    {
        characterVelocityY = 0f;
    }
    private void HandleHookshotStart()
    {
        if (TestInputDownHookshot())
        {
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit raycastHit))
            {
                debugHitPointTransform.position = raycastHit.point;
                hookshotPosition = raycastHit.point;
                hookshotSize = 0f;
                hookshotTransform.gameObject.SetActive(true);
                hookshotTransform.localScale = Vector3.zero;
                state = State.HookshotThrown;

            }
        }

    }

    private void HandleHookshotThrow()
    {
        hookshotTransform.LookAt(hookshotPosition);

        Vector3 hookshotDir = (hookshotPosition - transform.position).normalized;
        float hookshotThrowSpeed = 150f;
        hookshotSize += hookshotThrowSpeed * Time.deltaTime;
        hookshotTransform.localScale = new Vector3(1, 1, hookshotSize);

        if (hookshotSize >= Vector3.Distance(transform.position, hookshotPosition))
        {
            state = State.HookshotFlyingPlayer;
            //cameraFov.SetCameraFov(HOOKSHOT_FOV);
        }
    }
    private void HandleHookshotMovement()
    {
        Vector3 hookshotDir = (hookshotPosition - transform.position).normalized;

        float hookshotSpeed = Vector3.Distance(transform.position, hookshotPosition);
        float hookshotSpeedMultiplier = 2f;

        characterController.Move(hookshotDir * hookshotSpeed * hookshotSpeedMultiplier * Time.deltaTime);

        float reachedHookshotPositionDistance = 3f;
        if (Vector3.Distance(transform.position, hookshotPosition) < reachedHookshotPositionDistance)
        {
            state = State.Normal;
            ResetGravityEffect();
            hookshotTransform.gameObject.SetActive(false);
        }
        
        {
            if (TestInputDownHookshot())
            {
                //cancel Hookshot
                StopHookshot();
            }
            if (TestInputJump())
            {
                //Cancelled with jump
                float momentumExtraSpeed = 7f;
                characterVelocityMomentum = hookshotDir * hookshotSpeed * momentumExtraSpeed;
                float jumpSpeed = 40f;
                characterVelocityMomentum += Vector3.up * jumpSpeed;
                state = State.Normal;
                ResetGravityEffect();
                StopHookshot();
            }
        }
        
    void StopHookshot()
        {
            state = State.Normal;
            ResetGravityEffect();
            hookshotTransform.gameObject.SetActive(false);
            //cameraFov.SetCameraFov(targetFov: NORMAL_FOV);
        }
    }
    private bool TestInputDownHookshot()

    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            audioSource.PlayOneShot(grappleSound, grappleVolume);
        }
        return Input.GetKeyDown(KeyCode.Mouse0);
    }
    private bool TestInputJump()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
}
