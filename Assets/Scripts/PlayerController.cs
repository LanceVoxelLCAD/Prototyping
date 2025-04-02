using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public CharacterController controller;

    [Header("Stats")]
    public float health = 100f;
    public float maxHealth = 100f;
    public float food = 80f;
    public float maxFood = 100f;
    public float hungerRate = 1f;
    public float regenRate = 1f;
    public bool passiveHeal = true;
    public float attackDamage = 10f;
    public bool canAttack = true;
    public float attackCooldown = 1f;

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
    public float playerReach = 5f;
    public Animator weaponAnimator;

    [Header("Other")]
    public GameObject overheadLight;
    public TMP_Text healthTextUI;
    public TMP_Text foodTextUI;
    public LayerMask clickableRayMask;
    public Image damagedUIEffect;
    public Image healedUIEffect;

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
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        //float pitchYaw = Input.GetAxis("PitchYaw");

        ApplyGravity(jump);
        AimingRay();
        ManageHealth();
        ManageFood();
        Hotbar(scrollInput);

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

        if (Input.GetKeyDown(KeyCode.E))
        {
            torchLightCone.SetActive(!torchLightCone.gameObject.activeSelf);
        }

        //Prototype 1 nonsense:
        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    overheadLight.SetActive(!overheadLight.gameObject.activeSelf);
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
        //torch.transform.LookAt(hitPoint);
        //playerEyesCam.transform.LookAt(hitPoint);

        //clickable stuff
        //using different ray due to twitchy torch..?
        Ray clickableRay;
        clickableRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit clickableHit;

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(clickableRay, out clickableHit, playerReach, clickableRayMask))
            {
                //if enemy..
                //if item..
                //if bed/clickable...

                Debug.Log("Player clicked: " + clickableHit.collider.gameObject.name);

                //this is checking for the name, which feels bad
                if (clickableHit.collider.transform.parent != null)
                {
                    if (clickableHit.collider.transform.parent.gameObject.name == "Bed")
                    {
                        health = maxHealth;
                        food -= hungerRate * 5;
                    }
                    return;
                    //don't needlessly attack the bed or other usable items
                    //maybe an else if (item) and then else if (enemy) would be better
                }

                if (canAttack)
                {
                    //play an animation here
                    weaponAnimator.SetTrigger("PerformAttack");

                    canAttack = false;
                    Invoke(nameof(ResetAttack), attackCooldown);

                    if (hit.transform.TryGetComponent<EnemyController>(out EnemyController T))
                    {
                        T.TakeDamage(attackDamage);
                    }
                }

            }
            //if we hit nothing and CAN attack... swing at the sky
            else if (canAttack)
            {
                weaponAnimator.SetTrigger("PerformAttack");
                canAttack = false;
                Invoke(nameof(ResetAttack), attackCooldown);
            }
        }

    }
    public void ResetAttack()
    {
        canAttack = true;
    }

    public void ManageHealth()
    {
        float damageDebug = 5f;
        float healDebug = 15f;

        //debug damage
        if (Input.GetKeyDown(KeyCode.B))
        {
            if(health <= damageDebug)
            {
                health = 0;
                Destroy(gameObject);
            } 
            else
            {
                health -= damageDebug;
                StartCoroutine(DamageUIEffect());
            }
        }

        //debug heal
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (health >= (maxHealth - healDebug))
            {
                health = maxHealth;
            }
            else
            {
                health += healDebug;
            }
        }

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

        if (healthTextUI != null)
        {
            string displayedHealth = health.ToString("F0");
            healthTextUI.text = "Health: " + displayedHealth;
        }
    }

    public void TakeDamage(float damageAmtReceived)
    {
        health -= damageAmtReceived;

        if (health <= 0)
        {
            Destroy(gameObject);
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

    public void Eat(float satiation)
    {
        food += satiation;

        if (food >= maxFood)
        {
            food = maxFood;
        }

        //play eating sound, in theory
    }

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

    public void ManageFood()
    {
        float eatDebug = 15f;
        float hungerDebug = 10f;

        if (food > 80)
        {
            passiveHeal = true;
        }
        else
        {
            passiveHeal = false;
        }

        food -= (hungerRate * Time.deltaTime);

        if (food < 0)
        {
            food = 0;
        }

        if (food == 0)
        {
            health -= (regenRate * 2 * Time.deltaTime);
        }

        //debug hunger
        if (Input.GetKeyDown(KeyCode.N))
        {
            if (food <= hungerDebug)
            {
                food = 0;
            }
            else
            {
                food -= hungerDebug;
            }
        }

        //debug eating
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (food >= (maxFood - eatDebug))
            {
                food = maxFood;
            }
            else
            {
                food += eatDebug;
            }
        }

        if (foodTextUI != null)
        {
            string displayedHunger = food.ToString("F0");
            foodTextUI.text = "Hunger: " + displayedHunger;
        }
    }

    public void Hotbar(float scrollInput)
    {
        //when weapon is active, change damage
        int currentHotbar = 1;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            currentHotbar = 1;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            currentHotbar = 2;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
        {
            currentHotbar = 3;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
        {
            currentHotbar = 4;
        }

        if(scrollInput > 0)
        {
            if(currentHotbar >= 3)
            {
                //currentHotbar...
            }
        }
        //RESUME HERE: this wont work well if we're constantly setting this active

        if (currentHotbar == 1)
        {
            torch.SetActive(true);
            weapon.SetActive(false);
        }
        else if (currentHotbar == 2)
        {
            torch.SetActive(false);
            weapon.SetActive(true);
        }
        else if (currentHotbar == 3)
        {
            torch.SetActive(false);
            weapon.SetActive(false);
            //building items?
        }
        else if (currentHotbar == 4)
        {
            torch.SetActive(false);
            weapon.SetActive(false);
            //food items?
        }

    }
}
