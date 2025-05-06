using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;

    [Header("Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    //public float food = 80f;
    //public float maxFood = 100f;
    //public float hungerRate = 1f;
    public float regenRate = 1f;
    public bool passiveHeal = true;
    public float attackDamage = 10f;
    public bool canAttack = true;
    public float attackCooldown = 1f;
    public float killcount = 0f;

    [Header("Audio")]
    public EventReference hurtEvent;
    public EventReference jumpEvent;
    public EventReference landEvent;
    private bool wasGroundedLastFrame = false;


    [Header("Move")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float turnSpeedSensitivity = 2500f;

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

    [Header("Weapon")]
    public GameObject weapon;
    public float maxStaMana = 80f;
    public float currStaMana;
    public float playerReach = 3f;
    public float staManaRegenRate = 1f;
    public float staManaRegenDelay = 1f;
    //public Animator weaponAnimator;

    [Header("Canisters")]
    public int redCanisterCount;
    public int yellowCanisterCount;
    public int greenCanisterCount;
    public int blueCanisterCount;
    public float redCanisterValue;
    public float yellowCanisterValue;
    public float greenCanisterValue;
    public float blueCanisterValue;
    public TMP_Text yellowCanisterUICount;
    public TMP_Text redCanisterUICount;
    public GameObject canPopupContainer;
    public TMP_Text canPopupTxt;

    [Header("Other")]
    public GameObject overheadLight;
    //public TMP_Text healthTextUI;
    //public TMP_Text foodTextUI;
    public LayerMask clickableRayMask;
    public Image damagedUIEffect;
    public Image healedUIEffect;
    //public TMP_Text killcountTextUI;
    public Slider healthSlider;

    private void Start()
    {
        torchRot = torch.transform.localEulerAngles;
        eyesRot = playerEyesCam.transform.localEulerAngles;
        Cursor.lockState = CursorLockMode.Locked;

        passiveHeal = true; //no more food system
        healthSlider.maxValue = maxHealth;
    }

    void Update()
    {
        float forward = Input.GetAxis("Vertical");
        float right = Input.GetAxis("Horizontal");
        float mouseXInput = Input.GetAxis("Mouse X");
        float mouseYInput = Input.GetAxis("Mouse Y");
        bool run = Input.GetKey(KeyCode.LeftShift);
        bool jump = Input.GetButtonDown("Jump");
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        //float pitchYaw = Input.GetAxis("PitchYaw");

        ApplyGravity(jump);
        AimingRay();
        ManageHealth();

        if (Input.GetKeyDown(KeyCode.H)) { UseRedCanister(); }
        if (Input.GetKeyDown(KeyCode.R)) { UseYellowCanister(); }

        //ManageFood();
        //Hotbar(scrollInput);

        //if (killcountTextUI != null)
        //{
        //    string displayedKillcount = killcount.ToString("F0");
        //    killcountTextUI.text = "Killcount: " + killcount;
        //}

        if (forward != 0 || right != 0) //only update movement if pressing something
        {
            //DetermineForward(forward, right);
            HandleMovement(forward, right, run);
        }

        //left to right visuals
        transform.Rotate(Vector3.up, mouseXInput * turnSpeedSensitivity * Time.deltaTime);

        //torchRot.x += verticalLookSpeed * mouseYInput * -1 * Time.deltaTime;
        //torchRot.x = Mathf.Clamp(torchRot.x, -80, 80);
        //torch.transform.localEulerAngles = torchRot;

        //up and down visuals
        eyesRot.x += verticalLookSpeed * mouseYInput * -1 * Time.deltaTime;
        eyesRot.x = Mathf.Clamp(eyesRot.x, -80, 80);
        playerEyesCam.transform.localEulerAngles = eyesRot;

        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    torchLightCone.SetActive(!torchLightCone.gameObject.activeSelf);
        //}

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

        // Landing logic — just hit the ground this frame
        if (grounded && !wasGroundedLastFrame)
        {
            if (!landEvent.IsNull)
            {
                EventInstance landInstance = RuntimeManager.CreateInstance(landEvent);
                landInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                landInstance.setParameterByName("VoiceGender", (int)VoiceSelectionMenu.SelectedVoiceGender); // Optional
                landInstance.start();
                landInstance.release();
            }
        }

        // Jump logic
        if (jump && grounded)
        {
            if (!jumpEvent.IsNull)
            {
                EventInstance jumpInstance = RuntimeManager.CreateInstance(jumpEvent);
                jumpInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                jumpInstance.setParameterByName("VoiceGender", (int)VoiceSelectionMenu.SelectedVoiceGender); // Optional
                jumpInstance.start();
                jumpInstance.release();
            }

            fallVelocity.y += Mathf.Sqrt(jumpHeight * -3f * gravity);
        }

        if (grounded && fallVelocity.y < 0)
        {
            fallVelocity.y = 0;
        }

        fallVelocity.y += gravity * Time.deltaTime;
        controller.Move(fallVelocity * Time.deltaTime);

        wasGroundedLastFrame = grounded;

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

        if (Physics.Raycast(ray, out hit, 100f, torchRayMask, QueryTriggerInteraction.Ignore))
        {
            hitPoint = ray.GetPoint(hit.distance);
        }
        else
        {
            hitPoint = ray.GetPoint(100);
        }

        //torch looks at mouse, aka center of screen
        //torch.transform.LookAt(hitPoint);
        //playerEyesCam.transform.LookAt(hitPoint);

        //clickable stuff
        //using different ray due to twitchy torch..?
        Ray clickableRay;
        clickableRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit clickableHit;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (Physics.Raycast(clickableRay, out clickableHit, playerReach, clickableRayMask, QueryTriggerInteraction.Ignore))
            {
                Debug.Log("Player hit E on: " + clickableHit.collider.gameObject.name);

                Pickup pickup = clickableHit.collider.GetComponent<Pickup>();

                if(pickup != null)
                {
                    CollectPickup(pickup);
                }
            }

        }

    }

    public void CollectPickup(Pickup pickup)
    {
        switch (pickup.pickupType)
        {
            case Pickup.PickupType.RedCanister:
                redCanisterCount++;
                break;

            case Pickup.PickupType.YellowCanister:
                yellowCanisterCount++;
                break;

            case Pickup.PickupType.GreenCanister:
                greenCanisterCount++;
                UseGreenCanister();
                break;

            case Pickup.PickupType.BlueCanister:
                blueCanisterCount++;
                UseBlueCanister();
                break;

            case Pickup.PickupType.FakeGun:
                weapon.SetActive(true);
                break;

            default:
                Debug.Log("Messed up your switch statement for the canisters probably");
                break;
        }

        UpdatePickupUI();
        Destroy(pickup.gameObject);

    }

    public void UpdatePickupUI()
    {
        redCanisterUICount.text = redCanisterCount.ToString();
        yellowCanisterUICount.text = yellowCanisterCount.ToString();
    }

    public void UseRedCanister()
    {
        if (redCanisterCount > 0 && health < maxHealth)
        {
            health = Mathf.Min(maxHealth, health + redCanisterValue);
            redCanisterCount--;
            UpdatePickupUI();
            StartCoroutine(HealUIEffect());
        }
    }

    public void UseYellowCanister()
    {
        if (yellowCanisterCount > 0 && currStaMana < maxStaMana )
        {
            currStaMana = Mathf.Min(maxStaMana, currStaMana + yellowCanisterValue);
            yellowCanisterCount--;
            UpdatePickupUI();
        }
    }

    // (A green canister will make your TORCH’s resin-due refill faster,
    //while blue ones will increase your TORCH’s maximum resin-due reservoir.)

    public void UseGreenCanister()
    {
        staManaRegenRate += greenCanisterValue;
        canPopupContainer.SetActive(true);
        canPopupTxt.text = "Resin Regen Increased";
    }

    public void UseBlueCanister()
    {
        maxStaMana += blueCanisterValue;
        canPopupContainer.SetActive(true);
        canPopupTxt.text = "Max Regen Increased";
        //currStaMana += Mathf.Min(maxStaMana, currStaMana + blueCanisterValue); //give them that little boost?
    }

    //public void ResetAttack()
    //{
    //    canAttack = true;
    //}

    public void ManageHealth()
    {
        //float damageDebug = 5f;
        //float healDebug = 15f;

        //debug damage
        //if (Input.GetKeyDown(KeyCode.B))
        //{
        //    if(health <= damageDebug)
        //    {
        //        health = 0;
        //        Destroy(gameObject);
        //    } 
        //    else
        //    {
        //        health -= damageDebug;
        //        StartCoroutine(DamageUIEffect());
        //    }
        //}

        ////debug heal
        //if (Input.GetKeyDown(KeyCode.H))
        //{
        //    if (health >= (maxHealth - healDebug))
        //    {
        //        health = maxHealth;
        //    }
        //    else
        //    {
        //        health += healDebug;
        //    }
        //}

        if (passiveHeal)
        {
            if (health < maxHealth)
            {
                health += (regenRate * Time.deltaTime);
            }
            else
            {
                health = maxHealth;
            }
        }

        //if (healthTextUI != null)
        //{
        //    string displayedHealth = health.ToString("F0");
        //    healthTextUI.text = "Health: " + displayedHealth;
        //}

        healthSlider.value = health;
    }

    public void TakeDamage(float damageAmtReceived)
    {
        health -= damageAmtReceived;

        // Play hurt sound
        if (!hurtEvent.IsNull)
        {
            EventInstance instance = RuntimeManager.CreateInstance(hurtEvent);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            instance.setParameterByName("VoiceGender", (int)VoiceSelectionMenu.SelectedVoiceGender); // optional
            instance.start();
            instance.release();
        }

        if (health <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            //Destroy(gameObject);
        }

        //show player was hurt?
        //id like the screen to darken, then turn red and fade back
        //for now itll just flash red.

        StartCoroutine(DamageUIEffect());
    }

    public void HealFromDamage(float damageAmtHealed)
    {
        health += damageAmtHealed;

        if (health >= maxHealth)
        {
            health = maxHealth;
        }

        //show player was healed?

        StartCoroutine(HealUIEffect());
    }

    //replace this with the stamana thing maybe
    //public void Eat(float satiation)
    //{
    //    food += satiation;

    //    if (food >= maxFood)
    //    {
    //        food = maxFood;
    //    }

    //    //play eating sound, in theory
    //}

    private IEnumerator DamageUIEffect()
    {
        damagedUIEffect.gameObject.SetActive(true);

        yield return new WaitForSeconds(.2f);

        damagedUIEffect.gameObject.SetActive(false);
    }

    private IEnumerator HealUIEffect()
    {
        healedUIEffect.gameObject.SetActive(true);

        yield return new WaitForSeconds(.2f);

        healedUIEffect.gameObject.SetActive(false);
    }

}
