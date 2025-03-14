using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;

    [Header("Move")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float turnSpeedSensitivity = 2500f;
    public float health = 100f;

    private float playerScale = 1.1f;

    [Header("Jump")]
    public bool grounded;
    public LayerMask groundMask;
    public Vector3 fallVelocity;
    public float gravity = -9.81f;
    public float jumpHeight = 1f;

    [Header("Flashlight")]
    public GameObject torch;
    public GameObject torchLightCone;
    public GameObject playerEyesCam;
    public float verticalLookSpeed = 180f;
    public Vector3 torchRot;
    public Vector3 eyesRot;
    public LayerMask torchRayMask;

    [Header("Other")]
    public GameObject overheadLight;

    private void Start()
    {
        torchRot = torch.transform.localEulerAngles;
        eyesRot = playerEyesCam.transform.localEulerAngles;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float forward = Input.GetAxis("Vertical");
        float right = Input.GetAxis("Horizontal");
        float mouseXInput = Input.GetAxis("Mouse X");
        float mouseYInput = Input.GetAxis("Mouse Y");
        bool run = Input.GetKey(KeyCode.LeftShift);
        bool jump = Input.GetButtonDown("Jump");
        //float pitchYaw = Input.GetAxis("PitchYaw");

        ApplyGravity(jump);
        AimingRay();

        if (forward != 0 || right != 0) //only update movement if pressing something
        {
            //DetermineForward(forward, right);
            HandleMovement(forward, right, run);
        }

        transform.Rotate(Vector3.up, mouseXInput * turnSpeedSensitivity * Time.deltaTime);

        torchRot.x += verticalLookSpeed * mouseYInput * -1 * Time.deltaTime;
        torchRot.x = Mathf.Clamp(torchRot.x, -80, 80);
        torch.transform.localEulerAngles = torchRot;

        eyesRot.x += verticalLookSpeed * mouseYInput * -1 * Time.deltaTime;
        eyesRot.x = Mathf.Clamp(eyesRot.x, -80, 80);
        playerEyesCam.transform.localEulerAngles = eyesRot;

        if (Input.GetMouseButtonDown(0))
        {
            torchLightCone.SetActive(!torchLightCone.gameObject.activeSelf);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            overheadLight.SetActive(!overheadLight.gameObject.activeSelf);
        }
    }

    public void HandleMovement(float forward, float right, bool run)
    {
        //only moving forward.. so this is ONLY speed for the mathf stuff

        float moveSpeed = walkSpeed;
        if (run)
        {
            moveSpeed = runSpeed;
        }

        //float input = Mathf.Max(Mathf.Abs(forward), Mathf.Abs(right));
        //controller.Move(input * transform.forward * moveSpeed * Time.deltaTime);
        Vector3 moving = new Vector3(right, 0, forward);

        moving = transform.TransformDirection(moving);

        controller.Move(moving * Time.deltaTime * moveSpeed);

        //transform.Rotate(0, mouseXInput, 0);
    }

    public void ApplyGravity(bool jump)
    {
        grounded = CheckGrounded();

        //if grounded and falliing
        if (grounded && fallVelocity.y < 0)
        {
            fallVelocity.y = 0;
        }

        if (jump && grounded)
        {
            //apply upwards gravity, neg * neg number
            fallVelocity.y += Mathf.Sqrt(jumpHeight * -3f * gravity);
        }

        //applying gravity
        fallVelocity.y += gravity * Time.deltaTime;
        controller.Move(fallVelocity * Time.deltaTime);
    }

    public bool CheckGrounded()
    {
        return Physics.CheckSphere(transform.position + Vector3.down * 0.51f * playerScale, 0.5f * playerScale, groundMask);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.51f * playerScale, 0.5f * playerScale);
    }

    public void AimingRay()
    {
        Ray ray;
        Vector3 hitPoint;

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, torchRayMask))
        {
            hitPoint = ray.GetPoint(hit.distance);
        }
        else
        {
            hitPoint = ray.GetPoint(100);
        }

        //torch looks at mouse, aka center of screen
        torch.transform.LookAt(hitPoint);
        //playerEyesCam.transform.LookAt(hitPoint);
       
    }
}
