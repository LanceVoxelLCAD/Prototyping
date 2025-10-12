using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hookups")]
    public Camera worldCamera;       // Main camera
    public Camera weaponCamera;      // Overlay camera for gun
    public PlayerController controller;

    [Header("FOV")]
    public float baseFOV = 60f;
    public float sprintFOV = 75f;
    public float fovSmooth = 8f;
    public float weaponCameraOffset = 8f;
    public bool isRunning = false;

    [Header("Tilt")]
    public float maxTilt = 5f;
    public float tiltSmooth = 6f;

    [Header("Crouch")]
    public float standCameraHeight = 0f;
    public float crouchCameraHeight = -.2f;
    public float smoothSpeed = 8f;
    private float targetHeight;

    [Header("Headbob")]
    public float bobFrequency = 6f;
    public float bobAmplitude = 0.05f;
    public float bobSmoothing = 6f;
    //public float bobRunMultiplier = 2f;
    public float runBobAmplitudeMultiplier = 1.5f; //1.5-2 ish
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
    private float dipOffset;
    private bool wasGroundedLastFrame; //i replaced this var in the player controller...

    void Start()
    {
        if (!worldCamera) worldCamera = GetComponent<Camera>();
        if (!weaponCamera) weaponCamera = GetComponentInChildren<Camera>();
        if (!controller) controller = GetComponentInParent<PlayerController>();

        if (weaponCamera)
        {
            //"Sync FOV at start"
            weaponCamera.fieldOfView = worldCamera.fieldOfView + weaponCameraOffset;
        }

        originalLocalPos = transform.localPosition;
        targetFOV = baseFOV;
        targetHeight = standCameraHeight;
    }

    void Update()
    {
        //no longer internet code as it has been rewritten, again

        HandleFOV();
        HandleTilt();
        HandleLandingDip();

        Vector3 targetPos = originalLocalPos;

        //crouch
        float crouchTarget = controller.isCrouching ? crouchCameraHeight : standCameraHeight;
        targetHeight = Mathf.Lerp(targetHeight, crouchTarget, Time.deltaTime * smoothSpeed);
        targetPos.y += targetHeight;

        //landdip
        targetPos.y += dipOffset;

        //headbob
        targetPos += GetHeadbobOffset();

        //apply everything to cam transform
        //transform.localPosition = originalLocalPos + new Vector3(0, dipOffset, 0) + GetHeadbobOffset();
        transform.localPosition = targetPos;
        transform.localRotation = Quaternion.Euler(0, 0, currentTilt);



        //nah
        ////sync cameras' FOV
        //if (weaponCamera)
        //    weaponCamera.fieldOfView = worldCamera.fieldOfView;
    }

    void HandleFOV()
    {
        //I dont think I need to check move velo here due to the CanRun method but im leaving it for now
        isRunning = controller.isRunning && controller.currMoveVelocity.magnitude > 0.1f;
        targetFOV = isRunning ? sprintFOV : baseFOV;

        worldCamera.fieldOfView = Mathf.Lerp(worldCamera.fieldOfView, targetFOV, Time.deltaTime * fovSmooth);
    }

    void HandleTilt()
    {
        float strafe = Input.GetAxis("Horizontal");
        float targetTilt = -strafe * maxTilt;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmooth);
    }

    //void HandleCrouch(bool isCrouching)
    //{
    //    targetHeight = isCrouching ? crouchCameraHeight : standCameraHeight;

    //    Vector3 localPos = transform.localPosition;
    //    localPos.y = Mathf.Lerp(localPos.y, targetHeight, Time.deltaTime * smoothSpeed);
    //    transform.localPosition = localPos;
    //}
    //void HandleHeadbob()
    //{

    //}
    //Vector3 GetCrouchOffset(bool isCrouching)
    //{
    //    targetHeight = isCrouching ? crouchCameraHeight : standCameraHeight;

    //    Vector3 localPos = transform.localPosition;
    //    localPos.y = Mathf.Lerp(localPos.y, targetHeight, Time.deltaTime * smoothSpeed);
    //    //transform.localPosition = localPos;

    //    //this is not functional, just a test without the right code...
    //    return new Vector3(0, localPos.y, 0);
    //}

    Vector3 GetHeadbobOffset()
    {
        //if (bobTimer > 0)
        //{
        //    float bobOffsetY = Mathf.Sin(bobTimer) * bobAmplitude;
        //    float bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * bobAmplitude * 0.5f;
        //    return new Vector3(bobOffsetX, bobOffsetY, 0);
        //}
        //return Vector3.zero;

        //float speedMultiplier = isRunning ? bobRunMultiplier : 1f; //now triggered when the foot hits the ground

        if (isBobbing)
        {
            //internet code interjection
            float horizontalSpeed = new Vector3(controller.currMoveVelocity.x, 0, controller.currMoveVelocity.z).magnitude;
            float speedMultiplier = horizontalSpeed / controller.walkSpeed; // ratio of current speed to walk speed

            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
            isBobbing = false; //only advance on trigger
        }
        else if (controller.currMoveVelocity.magnitude > 0.1f && controller.isGrounded)
        {
            //"subtle sway between steps" in theory an idle bob
            bobTimer += Time.deltaTime * bobFrequency * 0.2f;
        }

        currAmplitude = bobAmplitude;
        if (isRunning) currAmplitude *= runBobAmplitudeMultiplier;

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
            // Just landed
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
            dipOffset = -Mathf.Sin(t * Mathf.PI) * dipAmount;
            t += Time.deltaTime * dipSpeed;
            yield return null;
        }
        dipOffset = 0f;
    }
}
