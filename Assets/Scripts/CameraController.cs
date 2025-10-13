using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hookups")]
    public Camera worldCamera;       // Main camera
    public Camera weaponCamera;      // Overlay camera for gun
    public PlayerController controller;
    public Transform cameraRig;

    [Header("FOV")]
    public float baseFOV = 60f;
    public float sprintFOVOffset = +8f;
    public float crouchFOVOffset = -3f;
    public float fovSmooth = 8f;
    public float weaponCameraOffset = 8f;
    public bool isRunning = false;

    [Header("Tilt")]
    public float maxTilt = 5f;
    public float tiltSmooth = 6f;

    [Header("Crouch")]
    public bool isCrouching;
    public float standCameraHeight = 0f;
    public float crouchCameraHeight = -.2f;
    public float smoothSpeed = 8f;
    private float targetHeight;

    [Header("Crouch Dip Animation")]
    public float crouchDipAnimAmount = 0.05f;
    public float crouchDipAnimSpeed = 8f;
    public float crouchPitchAnimAmount = 2f;
    public float crouchDipAnimOffset = 0.5f;
    public float crouchPitchAnimOffset = 5f;

    [Header("Headbob")]
    public float bobFrequency = 6f;
    public float bobAmplitude = 0.05f;
    public float bobSmoothing = 6f;
    //public float bobRunMultiplier = 2f;
    public float runBobAmplitudeMultiplier = 1.5f; //1.5-2 ish
    public float crouchBobAmplitudeMultiplier = .5f;
    public float currAmplitude;
    private float bobTimer;
    private float currBobOffset;
    private bool isBobbing;

    [Header("Landing Dip")]
    public float dipAmount = 0.2f;
    public float dipSpeed = 6f;

    private Vector3 originalLocalPos;
    private float targetFOV;
    private float currentTilt;
    private float landDipOffset;
    private bool wasGroundedLastFrame; //i replaced this var in the player controller...

    void Start()
    {
        if (!worldCamera) worldCamera = GetComponent<Camera>();
        if (!weaponCamera) weaponCamera = GetComponentInChildren<Camera>();
        if (!controller) controller = GetComponentInParent<PlayerController>();
        if (cameraRig == null) cameraRig = transform.parent;

        if (weaponCamera)
        {
            //"Sync FOV at start"
            weaponCamera.fieldOfView = worldCamera.fieldOfView + weaponCameraOffset;
        }

        originalLocalPos = cameraRig.localPosition;
        targetFOV = baseFOV;
        targetHeight = standCameraHeight;
    }

    void LateUpdate()
    {
        //no longer internet code as it has been rewritten, again

        HandleFOV();
        HandleTilt();
        HandleCrouch();
        HandleCrouchDip();
        HandleLandingDip();

        Vector3 targetPos = originalLocalPos;

        //crouch
        targetPos.y += targetHeight;

        //crouchDip
        targetPos.y += crouchDipAnimOffset;

        //landDip
        targetPos.y += landDipOffset;

        //headbob
        targetPos += GetHeadbobOffset();

        //apply everything to cam transform
        cameraRig.localPosition = targetPos;
        cameraRig.localRotation = Quaternion.Euler(0, 0, currentTilt);

    }

    void HandleFOV()
    {
        //I dont think I need to check move velo here due to the CanRun method but im leaving it for now
        isRunning = controller.isRunning && controller.currMoveVelocity.magnitude > 0.1f;
        targetFOV = isRunning ? (baseFOV + sprintFOVOffset) : baseFOV;
        targetFOV = isCrouching ? (baseFOV + crouchFOVOffset) : baseFOV;

        worldCamera.fieldOfView = Mathf.Lerp(worldCamera.fieldOfView, targetFOV, Time.deltaTime * fovSmooth);
    }

    void HandleTilt()
    {
        float strafe = Input.GetAxis("Horizontal");
        float targetTilt = -strafe * maxTilt;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmooth);
    }

    void HandleCrouch()
    {
        isCrouching = controller.isCrouching;

        //crouch
        float crouchTarget = isCrouching ? crouchCameraHeight : standCameraHeight;
        targetHeight = Mathf.Lerp(targetHeight, crouchTarget, Time.deltaTime * smoothSpeed);
    }

    void HandleCrouchDip()
    {
        float targetDip = controller.isCrouching ? -crouchDipAnimAmount : 0f;
        float targetPitch = controller.isCrouching ? crouchPitchAnimAmount : 0f;

        crouchDipAnimOffset = Mathf.Lerp(crouchDipAnimOffset, targetDip, Time.deltaTime * crouchDipAnimSpeed);
        crouchPitchAnimOffset = Mathf.Lerp(crouchPitchAnimOffset, targetPitch, Time.deltaTime * crouchDipAnimSpeed);
    }

    Vector3 GetHeadbobOffset()
    {
        if (isBobbing)
        {
            //internet code interjection
            float horizontalSpeed = new Vector3(controller.currMoveVelocity.x, 0, controller.currMoveVelocity.z).magnitude;
            float speedMultiplier = horizontalSpeed / controller.walkSpeed; //ratio of current speed to walk speed

            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
            isBobbing = false; //only advance on trigger
        }
        else if (controller.currMoveVelocity.magnitude > 0.1f && controller.isGrounded)
        {
            //"subtle sway between steps" in theory an idle bob
            //I thought i knew what I was doing, but removing this breaks the shit,
            //so i guess it is the only functional line here
            bobTimer += Time.deltaTime * bobFrequency * 0.2f;
        }

        currAmplitude = bobAmplitude;
        if (isRunning) currAmplitude *= runBobAmplitudeMultiplier;
        if (isCrouching) currAmplitude *= crouchBobAmplitudeMultiplier;

        float yOffset = Mathf.Sin(bobTimer * Mathf.PI * 2f) * currAmplitude;
        currBobOffset = Mathf.Lerp(currBobOffset, yOffset, Time.deltaTime * 10f);
        return new Vector3(0, currBobOffset, 0);
    }

    public void TriggerFootstep()
    {
        //Debug.Log("Triggered Footsteps from Cam Controller!"); //this is finally getting called
        isBobbing = true;
        bobTimer = 0f;
    }

    void HandleLandingDip()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && !wasGroundedLastFrame)
        {
            //just landed
            StopAllCoroutines();
            StartCoroutine(DoLandingDip());
            Debug.Log("Landing Dip played... was it supposed to? If you were crouching, probably not!!!!");
        }

        wasGroundedLastFrame = isGrounded;
    }

    System.Collections.IEnumerator DoLandingDip()
    {
        float t = 0f;
        while (t < 1f)
        {
            landDipOffset = -Mathf.Sin(t * Mathf.PI) * dipAmount;
            t += Time.deltaTime * dipSpeed;
            yield return null;
        }
        landDipOffset = 0f;
    }
}
