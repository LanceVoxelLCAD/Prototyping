using UnityEngine;
using UnityEngine.UI;

public class GooGun : MonoBehaviour
{
    public GameObject gooProjectilePrefab;
    public Transform firePoint;
    public float launchForce = 20f;
    public float gooFireCooldown = 0.3f;
    public float beamFireCooldown = 0.4f;

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

    // Update is called once per frame
    void LateUpdate()
    {

        if (Input.GetKeyDown(KeyCode.C) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonUp(1))
        {
            firingModeBlue = !firingModeBlue;

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

    void FireBeam()
    {
        beamLine.enabled = true;

        Ray ray;
        Vector3 hitPoint;

        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, beamRange, beamMask))
        {
            hitPoint = ray.GetPoint(hit.distance);

            //move and play the fizzle particle
            if (!beamParticle.isPlaying) { beamParticle.Play(); }
            beamParticle.transform.position = hitPoint;

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

            Debug.DrawLine(firePoint.position, hitPoint, Color.blue, 1f);
        }

        beamLine.SetPosition(0, firePoint.position);
        beamLine.SetPosition(1, hitPoint);
    }
}
