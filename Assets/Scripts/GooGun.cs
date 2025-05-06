using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using FMOD.Studio;
using System.Collections;


public class GooGun : MonoBehaviour
{
    [Header("FMOD Events")]
    public EventReference gooFireSound;
    public EventReference beamLoopSound;
    public EventReference modeSwitchSound;


    [Header("FMOD Impact Loop")]
    public EventReference beamImpactLoopSound;

    [Header("Impact Effects")]
    public GameObject burnMarkPrefab;
    public float burnDuration = 5f;


    [Tooltip("Controls the scale of burn mark decals")]
    public Vector3 decalScale = new Vector3(1f, 1f, 1f);

    [Range(0f, 1f)]
    [Tooltip("Opacity of the burn mark (0 = transparent, 1 = fully visible)")]
    public float decalOpacity = 1f;

    private EventInstance beamImpactInstance;
    private bool beamImpactSoundPlaying = false;

    private EventInstance beamInstance;
    private bool beamSoundPlaying = false;

    public GameObject gooProjectilePrefab;
    public Transform firePoint;
    public float launchForce = 20f;
    public float gooFireCooldown = 0.3f;
    //public float beamFireCooldown = 0.4f;

    public float arcStrength = .1f;

    public float beamRange = 8f;

    private float lastGooFireTime;
    private float lastBeamFireTime;
    Vector3 targetingPt;

    public bool firingModeBlue = true;
    //could be a bool maybe

    public float attackDamage;
    public LayerMask beamMask;

    [Header("Gun StaMana/Resin-due")]
    //public float maxStaMana = 80f; //move to player controller
    //public float staManaRegenRate = 1f;
    public float staManaBeamDrain = 2f;
    public float staManaGooCost = 6f;
    //public float staManaRegenDelay = 1f;
    //public float currStaMana;
    private PlayerController playCont;

    [Header("Hookups")]
    public GameObject player;
    public Slider staManaSlider;
    public LineRenderer beamLine;

    public ParticleSystem gunModeParticles;
    public Gradient greenParticles;
    public Gradient blueParticles;

    public Renderer blueIndicator;
    public Renderer greenIndicator;
    public Material blueUnlitMaterial;
    public Material blueLitMaterial;
    public Material greenUnlitMaterial;
    public Material greenlitMaterial;

    public Image hudBars;
    public Sprite blueHudBars;
    public Sprite greenHudBars;

    public Image centerReticle;
    public Color blueReticleCircle;
    public Color GreenReticleCircle;

    public GameObject wholeReticle;
    public GameObject tinyReticle;

    public ParticleSystem beamParticle;

    private void Start()
    {

        player = GameObject.Find("Player");
        playCont = player.GetComponent<PlayerController>();
        playCont.currStaMana = playCont.maxStaMana;
        staManaSlider.maxValue = playCont.maxStaMana;
    }

    private void Update()
    {
        
    }

    private void Awake()
    {
        wholeReticle.SetActive(true);

        if (tinyReticle.activeSelf) { Destroy(tinyReticle); }
    }

    private IEnumerator FadeAndDestroy(GameObject decal, float duration)
    {
        if (decal == null) yield break;

        Renderer renderer = decal.GetComponent<Renderer>();
        if (renderer == null || !renderer.material.HasProperty("_Color"))
        {
            Destroy(decal);
            yield break;
        }

        Material mat = renderer.material;
        Color startColor = mat.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(decal);
    }


    // Update is called once per frame
    void LateUpdate()
    {
        if (MenuPause.IsPaused)
            return; // No gun on pause

        if (Input.GetKeyDown(KeyCode.C) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonUp(1))
        {
            firingModeBlue = !firingModeBlue;

            RuntimeManager.PlayOneShot(modeSwitchSound, firePoint.position);

            var colorOverLifetime = gunModeParticles.colorOverLifetime;

            if (firingModeBlue)
            {
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(blueParticles);
                blueIndicator.material = blueLitMaterial;
                greenIndicator.material = greenUnlitMaterial;

                hudBars.sprite = blueHudBars;
                centerReticle.color = blueReticleCircle;
            }
            else
            {
                DisableBeam();

                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(greenParticles);
                blueIndicator.material = blueUnlitMaterial;
                greenIndicator.material = greenlitMaterial;

                hudBars.sprite = greenHudBars;
                centerReticle.color = GreenReticleCircle;
            }
        }

        if (Input.GetMouseButton(0))
        {
            if (!firingModeBlue)
            {
                if (Time.time > lastGooFireTime + gooFireCooldown && playCont.currStaMana >= staManaGooCost)
                {
                    FireGoo();
                    lastGooFireTime = Time.time;
                    playCont.currStaMana -= staManaGooCost; //should only cost when it fires
                }
            }
            else
            {
                if(playCont.currStaMana >= staManaBeamDrain)
                {
                    FireBeam();
                   
                    lastBeamFireTime = Time.time;
                     
                    //should continously cost
                    playCont.currStaMana -= staManaBeamDrain * Time.deltaTime;

                    if (playCont.currStaMana < staManaBeamDrain)
                    {
                        //cool dying beam logic would be cool
                        DisableBeam();
                    }
                }
            }
        }
        else
        {
            DisableBeam();

            float mostRecentFireTime = Mathf.Max(lastBeamFireTime, lastGooFireTime);

            if(Time.time - mostRecentFireTime > playCont.staManaRegenDelay)
            playCont.currStaMana += playCont.staManaRegenRate * Time.deltaTime; //dont go above max, fix this buddy
            playCont.currStaMana = Mathf.Min(playCont.currStaMana, playCont.maxStaMana); //well this is cooler than what i usually do
        }

        staManaSlider.value = playCont.currStaMana;
    }

    void DisableBeam()
    {
        beamLine.enabled = false;
        if (beamParticle.isPlaying) { beamParticle.Stop(); }
        StopBeamSound(); //please stop the musik
        StopBeamSound();
        StopBeamImpactSound();
    }

    void StartBeamSound()
    {
        if (!beamLoopSound.IsNull && !beamSoundPlaying)
        {
            beamInstance = RuntimeManager.CreateInstance(beamLoopSound);
            beamInstance.set3DAttributes(RuntimeUtils.To3DAttributes(firePoint));
            beamInstance.start();
            beamSoundPlaying = true;
        }
    }

    void StopBeamSound()
    {
        if (beamSoundPlaying)
        {
            beamInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            beamInstance.release();
            beamSoundPlaying = false;
        }
    }
    void StartBeamImpactSound(Vector3 hitPoint)
    {
        if (beamImpactLoopSound.IsNull) return;

        // Clamp the hitPoint to avoid spatial audio artifacts
        Vector3 listenerPos = Camera.main.transform.position;
        Vector3 clampedPos = Vector3.Lerp(firePoint.position, hitPoint, 0.5f); // Midpoint between gun and target
        float maxRange = 100f;

        if (Vector3.Distance(listenerPos, clampedPos) > maxRange)
        {
            clampedPos = listenerPos + (clampedPos - listenerPos).normalized * maxRange;
        }

        if (!beamImpactSoundPlaying)
        {
            beamImpactInstance = RuntimeManager.CreateInstance(beamImpactLoopSound);
            beamImpactInstance.set3DAttributes(RuntimeUtils.To3DAttributes(clampedPos));
            beamImpactInstance.start();
            beamImpactSoundPlaying = true;
        }
        else
        {
            // Update location while beam is moving
            beamImpactInstance.set3DAttributes(RuntimeUtils.To3DAttributes(clampedPos));
        }
    }

    void StopBeamImpactSound()
    {
        if (beamImpactSoundPlaying)
        {
            beamImpactInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            beamImpactInstance.release();
            beamImpactSoundPlaying = false;
        }
    }

    void FireGoo()
    {
        Ray gooRay;
        gooRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit gooHit;

        Debug.DrawRay(gooRay.origin, gooRay.direction * 100f, Color.red, 12f);

        if (Physics.Raycast(gooRay, out gooHit, 100f))
        {
            targetingPt = gooHit.point;
        }
        else
        { //if there's nothing there
            targetingPt = gooRay.origin + gooRay.direction * 100f;
        }

        Vector3 directionFromGunToReticle = (targetingPt - firePoint.position).normalized;

        if (!gooFireSound.IsNull)
        {
            RuntimeManager.PlayOneShot(gooFireSound, firePoint.position);
        }

        GameObject projectile = Instantiate(gooProjectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            projectile.transform.forward = directionFromGunToReticle;
            //Vector3 liftArc = directionFromGunToReticle + Vector3.up * 0.15f;
            //Vector3 liftArc = Vector3.Slerp(directionFromGunToReticle, Vector3.up, 0.15f);
            //rb.AddForce(liftArc.normalized * launchForce, ForceMode.Impulse);

            ////alright different approach //nvm thats a hard no
            //Vector3 direction = directionFromGunToReticle.normalized;
            //Vector3 arcLift = Vector3.up * 0.3f * Mathf.Clamp01(1f - Vector3.Dot(direction, Vector3.up)); // less lift when shooting upward
            //Vector3 finalDirection = (direction + arcLift).normalized;
            //rb.AddForce(finalDirection.normalized * launchForce, ForceMode.Impulse);

            Vector3 direction = directionFromGunToReticle.normalized;
            Vector3 arc = Vector3.Cross(direction, Vector3.Cross(Vector3.up, direction)) * arcStrength;
            Vector3 finalDirection = (direction + arc).normalized;

            rb.AddForce(finalDirection * launchForce, ForceMode.Impulse);

        }
    }

    void CreateBurnMark(RaycastHit hit)
    {
        if (burnMarkPrefab == null) return;

        Quaternion decalRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
        GameObject burnMark = Instantiate(
            burnMarkPrefab,
            hit.point + hit.normal * 0.01f,
            decalRotation
        );

        burnMark.transform.SetParent(hit.collider.transform); // be sticky
        burnMark.transform.localScale = decalScale;           // apply scale here

        // Set decal opacity if a renderer + material exists
        Renderer decalRenderer = burnMark.GetComponent<Renderer>();
        if (decalRenderer != null && decalRenderer.material.HasProperty("_Color"))
        {
            Color color = decalRenderer.material.color;
            color.a = decalOpacity;
            decalRenderer.material.color = color;
        }


        StartCoroutine(FadeAndDestroy(burnMark, burnDuration)); // begone thot
    }


    void FireBeam()
    {
      

        beamLine.enabled = true;

        Ray ray;
        Vector3 hitPoint;

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //beam sound
        if (!beamSoundPlaying)
        {
            StartBeamSound();
        }


        if (Physics.Raycast(ray, out hit, beamRange, beamMask))
        {
            hitPoint = ray.GetPoint(hit.distance);

            //move and play the fizzle particle
            if (!beamParticle.isPlaying) { beamParticle.Play(); }
            beamParticle.transform.position = hitPoint;

            StartBeamImpactSound(hitPoint); // Only play if hit is valid

            if (!hit.collider.CompareTag("Enemy"))
            {
                CreateBurnMark(hit);
            }

            //if it should do damage again
            //if (Time.time > lastBeamFireTime + beamFireCooldown)
            {
                //lastBeamFireTime = Time.time;

                if (hit.transform.TryGetComponent<EnemyController>(out EnemyController T))
                {
                    //T.TakeDamage(attackDamage);
                    T.ApplyBeam(Time.deltaTime);
                }

                if (hit.transform.TryGetComponent<Health>(out Health H))
                {
                    H.TakeDamage(attackDamage); //this is better and works for the foam, remember for one day fixing enemies
                }

                Debug.DrawLine(firePoint.position, hitPoint, Color.blue, 1f);


            }
        }
        else
        {
            //i want this to speed up after sustained fire on one enemy
            hitPoint = ray.GetPoint(beamRange);

            if (beamParticle.isPlaying) { beamParticle.Stop(); }

            // Don't call StartBeamImpactSound()
            hitPoint = ray.GetPoint(beamRange);

            if (beamParticle.isPlaying) beamParticle.Stop();

            // Never update 3D attributes on a miss
            StopBeamImpactSound(); // must be silent

            Debug.DrawLine(firePoint.position, hitPoint, Color.blue, 1f);
        }

        beamLine.SetPosition(0, firePoint.position);
        beamLine.SetPosition(1, hitPoint);
    }
}
