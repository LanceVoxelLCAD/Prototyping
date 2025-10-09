﻿using System.Collections;
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
    public CameraController camController;

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
    //public EventReference footstepSound;
    private GooGun gooGun;

    [Header("Canister Sounds")]
    public EventReference redCanisterSound;
    public EventReference yellowCanisterSound;

    [Header("Move")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float backpedalSpeedMultiplier = 0.7f;
    public float turnSpeedSensitivity = 2500f;
    public Vector3 currMoveVelocity;
    public bool wantsToRun = false;
    public bool canRun = true;
    public bool isRunning = false;

    private Animator anim;

    [Header("Crouch")]
    public bool isCrouching = false;
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchSpeedMultiplier = 0.8f;
    public float crouchTransitionSpeed = 0.6f;

    [Header("Jump")]
    public bool isGrounded;
    public LayerMask groundMask;
    public Vector3 fallVelocity;
    //public float gravity = -9.81f;
    public float jumpHeight = 1f;
    private float gravityUp = -20f;
    private float gravityDown = -40f;
    private float coyoteTime = 0.1f;
    private float jumpBufferTime = 0.1f;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;

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

    [Header("Pickups")]
    public int healthRefillCount;
    public int ammoRefillHeldCount;
    public int explorationRewardCount;
    public int combatRewardCount;
    public float healthDropValue;
    public float ammoDropValue;
    public float explorationRewardValue;
    public float combatRewardValue;
    public TMP_Text ammoDropUICount;
    public TMP_Text healthDropUICount;
    public GameObject pickupPopupContainer;
    public TMP_Text pickupPopupTxt;

    [Header("Other")]
    public GameObject overheadLight;
    //public TMP_Text healthTextUI;
    //public TMP_Text foodTextUI;
    public LayerMask clickableRayMask;
    public Image damagedUIEffect;
    public Image healedUIEffect;
    //public TMP_Text killcountTextUI;
    public Slider healthSlider;

    private PlayerUI playerUI;




    private void Start()
    {
        torchRot = torch.transform.localEulerAngles;
        eyesRot = playerEyesCam.transform.localEulerAngles;
        Cursor.lockState = CursorLockMode.Locked;

        passiveHeal = true; //no more food system
        healthSlider.maxValue = maxHealth;

        anim = GetComponentInChildren<Animator>();
        playerUI = FindAnyObjectByType<PlayerUI>();

        if (weapon != null)
        {
            // if GooGun is on the weapon object:
            gooGun = weapon.GetComponent<GooGun>();
        }
    }

    void Update()
    {
        float forward = Input.GetAxis("Vertical");
        float right = Input.GetAxis("Horizontal");
        float mouseXInput = Input.GetAxis("Mouse X");
        float mouseYInput = Input.GetAxis("Mouse Y");
        wantsToRun = Input.GetKey(KeyCode.LeftShift);
        bool jump = Input.GetButtonDown("Jump");
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        bool wantsToCrouchTransition = Input.GetKeyDown(KeyCode.LeftControl);
        //float pitchYaw = Input.GetAxis("PitchYaw");

        ApplyGravity(jump);
        AimingRay();
        ManageHealth();

        if (wantsToCrouchTransition)
        {
            ToggleCrouch();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (healthRefillCount > 0 && health < maxHealth)
            {
                UseHeldHealthDrop();
            }
        }


        if (Input.GetKeyDown(KeyCode.R)) 
        {
            if (ammoRefillHeldCount > 0 && currStaMana < maxStaMana)
            {
                UseHeldAmmoDrop();
            }
        }

        HandleMovement(forward, right, wantsToRun);


        //left to right visuals
        transform.Rotate(Vector3.up, mouseXInput * turnSpeedSensitivity * Time.deltaTime);

        //up and down visuals
        eyesRot.x += verticalLookSpeed * mouseYInput * -1 * Time.deltaTime;
        eyesRot.x = Mathf.Clamp(eyesRot.x, -80, 80);
        playerEyesCam.transform.localEulerAngles = eyesRot;

        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    torchLightCone.SetActive(!torchLightCone.gameObject.activeSelf);
        //}

    }

    //LEO WHY DID YOU ADD THIS? IT IS NEVER CALLED
    //FIND THE REAL SCRIPT ON THE PLAYER BODY AHHH
    //public void PlayFootstep()
    //{
    //    Debug.Log("Playing Footsteps from Player Controller!");

    //    if (!footstepSound.IsNull)
    //    {
    //        RuntimeManager.PlayOneShot(footstepSound, transform.position);
    //    }
    //}

    public void HandleMovement(float forward, float right, bool wantsToRun)
    {

        //cleaner internet code
        //canRun = wantsToRun && isGrounded;

        float targetMoveSpeed;

        Vector3 inputDir = new Vector3(right, 0, forward).normalized;

        //no more speed up when running diagonally "Clamp diagonal movement to not exceed magnitude 1"
        if (inputDir.magnitude > 1f)
        {
            inputDir.Normalize();
        }

        bool backtracking = (forward < 0f && Mathf.Abs(right) < 0.1f);
        isRunning = CanRun(forward, right, backtracking);

        //no running backwards, but allows strafing, just no straight back
        if (backtracking)
        {
            //canRun = false;
            targetMoveSpeed = walkSpeed * backpedalSpeedMultiplier;
        }
        else if (isCrouching)
        {
            targetMoveSpeed = walkSpeed * crouchSpeedMultiplier;
        }
        else
        {
            //if (canRun)
            //{
            //    canRun = false;
            //}

            targetMoveSpeed = isRunning ? runSpeed : walkSpeed;
        }

        //isRunning = canRun;

        //if (!isGrounded )

        Vector3 targetVelocity = transform.TransformDirection(inputDir) * targetMoveSpeed;

        currMoveVelocity = Vector3.Lerp(currMoveVelocity, targetVelocity, 0.4f); //.2-.3 is "FPS standard" but it feels too slidy
        controller.Move(currMoveVelocity * Time.deltaTime);
        anim.SetFloat("Speed", targetVelocity.magnitude > 0 ? (isRunning ? 3 : 1) : 0);


    }

    public bool CanRun(float forward, float right, bool backtracking)
    {
        bool isMoving = Mathf.Abs(forward) > 0.1f || Mathf.Abs(right) > 0.1f;

        if (wantsToRun && isMoving && !backtracking)
        {
            if (isCrouching)
            {
                ToggleCrouch();
            }

            if (isGrounded)
            {
                return true;
            }
            else
            {
                //if not grounded but already running....
                return isRunning;
            }
        }
        
        return false;

    }

    public void ApplyGravity(bool jump)
    {
        isGrounded = CheckGrounded();
        wasGroundedLastFrame = isGrounded;

        //STOP: coyote time ("counts down after leaving ground")
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        //jump buffer ("stores jump input for a short time")
        if (jump)
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        //jump jump jump jump (w/ spicy new internet code)
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            //calculate initial velocity needed to reach jumpHeight
            fallVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityUp);
            jumpBufferCounter = 0; //"consume buffer"

            if (!jumpEvent.IsNull)
            {
                EventInstance jumpInstance = RuntimeManager.CreateInstance(jumpEvent);
                jumpInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
                jumpInstance.setParameterByName("VoiceGender", (int)VoiceSelectionMenu.SelectedVoiceGender);
                jumpInstance.setParameterByName("VoicePitch", VoiceSelectionMenu.SelectedVoicePitch);
                jumpInstance.start();
                jumpInstance.release();
            }
        }

        //gravity (different for jumping vs falling)
        if (fallVelocity.y > 0) //rising
            fallVelocity.y += gravityUp * Time.deltaTime;
        else //falling
            fallVelocity.y += gravityDown * Time.deltaTime;

        //set fall velocity on touchdown
        if (isGrounded && fallVelocity.y < 0)
        {
            fallVelocity.y = -2f; //small stick-to-ground force
        }

        controller.Move(fallVelocity * Time.deltaTime);
    }

    public bool CheckGrounded()
    {
        float radius = controller.radius * 0.9f;
        float groundedOffset = 0.1f;

        Vector3 point1 = transform.position + Vector3.up * 0.1f;
        Vector3 point2 = transform.position + Vector3.down * (controller.height / 2f - radius + groundedOffset);

        return Physics.CheckCapsule(point1, point2, radius, groundMask, QueryTriggerInteraction.Ignore);
    }

    void ToggleCrouch()
    {
        Debug.Log("Can stand: " + CanStand());

        if (!isCrouching)
        {

            float centerOffset = (standHeight - crouchHeight) / 2f;
            controller.center -= new Vector3(0, centerOffset, 0);
            controller.height = crouchHeight;
            //controller.height = Mathf.Lerp(, Time.deltaTime * crouchTransitionSpeed);
            //controller.center -= new Vector3(0, crouchHeight, 0);

            playerEyesCam.transform.position -= new Vector3(0, .3f, 0);

            isCrouching = true;
            //Debug.Log("Squat!");

            return;
        }
        
        if (CanStand())
        {
            float centerOffset = (standHeight - crouchHeight) / 2f;
            controller.center += new Vector3(0, centerOffset, 0);
            controller.height = standHeight;
            //controller.center += new Vector3(0, crouchHeight, 0);

            playerEyesCam.transform.position += new Vector3(0, .3f, 0);

            isCrouching = false;
            //Debug.Log("Stood up!");
        }

    }

    bool CanStand()
    {
        //dont touch our own collider
        //float radius = Mathf.Max(0.0f, controller.radius - 0.02f);
        //float standDiff = standHeight - crouchHeight;

        //when controller.center.y = height/2, - oh no this is NOT true, the center is at 0!!!!
        //transform.position is at bottom of capsule (FEET) (this might be a lie!!!)
        //Vector3 bottom = transform.position + Vector3.up * radius;
        //Vector3 top = transform.position + Vector3.up * (standHeight - radius);

        Vector3 bottom = transform.position + controller.center - Vector3.up * (controller.height / 2f) + Vector3.up * controller.radius;
        Vector3 top = bottom + Vector3.up * (standHeight - controller.radius * 2f);

        //true if head space clear
        bool blocked = Physics.CheckCapsule(bottom, top, controller.radius, groundMask, QueryTriggerInteraction.Ignore);
        return !blocked;
    }
    
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        //grounded check gizmos
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, controller.radius * 0.9f);
        Gizmos.DrawWireSphere(transform.position + Vector3.down * (controller.height / 2f - (controller.radius * 0.9f) + .01f), controller.radius * 0.9f);
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.down * (controller.height / 2f - (controller.radius * 0.9f) + .01f));
        
        
        
        //Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.51f * playerScale, 0.5f * playerScale);
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

        //moved E check inside....
        Ray clickableRay;
        clickableRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit clickableHit;

        if (Physics.Raycast(clickableRay, out clickableHit, playerReach, clickableRayMask, QueryTriggerInteraction.Ignore))
        {
            Pickup pickup = clickableHit.collider.GetComponent<Pickup>();

            if (pickup)
            {
                playerUI.ShowButtonPrompt(true);

                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log("Player hit E on: " + clickableHit.collider.gameObject.name);

                    if (pickup != null)
                    {
                        CollectPickup(pickup);
                    }
                }
            }
            else
            {
                playerUI.ShowButtonPrompt(false);
            }
        }
        else
        {
            playerUI.ShowButtonPrompt(false);
        }
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    if (Physics.Raycast(clickableRay, out clickableHit, playerReach, clickableRayMask, QueryTriggerInteraction.Ignore))
        //    {
        //        Debug.Log("Player hit E on: " + clickableHit.collider.gameObject.name);

        //        Pickup pickup = clickableHit.collider.GetComponent<Pickup>();

        //        if(pickup != null)
        //        {
        //            CollectPickup(pickup);
        //        }
        //    }

        //}

    }

    public void CollectPickup(Pickup pickup)
    {
        switch (pickup.pickupType)
        {
            case Pickup.PickupType.HealthRefill:
                healthRefillCount++;
                break;

            case Pickup.PickupType.AmmoRefill:
                ammoRefillHeldCount++;
                break;

            case Pickup.PickupType.ExplorationReward:
                explorationRewardCount++;
                ApplyExplorationReward();
                break;

            case Pickup.PickupType.CombatReward:
                combatRewardCount++;
                ApplyCombatReward();
                break;

            case Pickup.PickupType.FakeGun:
                weapon.SetActive(true);
                break;

            default:
                Debug.Log("Messed up your switch statement for the pickups probably");
                break;
        }

        UpdatePickupUI();
        Destroy(pickup.gameObject);

    }

    public void UpdatePickupUI()
    {
        healthDropUICount.text = healthRefillCount.ToString();
        ammoDropUICount.text = ammoRefillHeldCount.ToString();
    }

    public void UseHeldHealthDrop()
    {
        if (!redCanisterSound.IsNull)
        {
            RuntimeManager.PlayOneShot(redCanisterSound, transform.position);
        }

        health = Mathf.Min(maxHealth, health + healthDropValue);
        healthRefillCount--;
        UpdatePickupUI();
        StartCoroutine(HealUIEffect());

    }

    public void UseHeldAmmoDrop()
    {
        if (!yellowCanisterSound.IsNull)
        {
            RuntimeManager.PlayOneShot(yellowCanisterSound, transform.position);
        }

        currStaMana = Mathf.Min(maxStaMana, currStaMana + ammoDropValue);
        ammoRefillHeldCount--;
        UpdatePickupUI();
    }

    // (A green canister will make your TORCH’s resin-due refill faster,
    //while blue ones will increase your TORCH’s maximum resin-due reservoir.)

    public void ApplyExplorationReward()
    {
        staManaRegenRate += explorationRewardValue;
        pickupPopupContainer.SetActive(true);
        pickupPopupTxt.text = "Resin Regen Increased";
    }

    public void ApplyCombatReward()
    {
        maxStaMana += combatRewardValue;
        pickupPopupContainer.SetActive(true);
        pickupPopupTxt.text = "Max Regen Increased";
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
            instance.setParameterByName("VoiceGender", (int)VoiceSelectionMenu.SelectedVoiceGender);
            instance.setParameterByName("VoicePitch", VoiceSelectionMenu.SelectedVoicePitch);
            instance.start();
            instance.release();
        }

        if (health <= 0)
        {
            if (gooGun != null) gooGun.HandlePlayerDeath();
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
