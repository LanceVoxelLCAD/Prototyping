using UnityEngine;

public class GooGun : MonoBehaviour
{
    public GameObject gooProjectilePrefab;
    public Transform firePoint;
    public float launchForce = 20f;
    public float fireCooldown = 0.5f;

    private float lastFireTime;
    Vector3 targetingPt;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time > lastFireTime + fireCooldown)
        {
            FireGoo();
            lastFireTime = Time.time;
        }
    }

    void FireGoo()
    {
        Ray gooRay;
        gooRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit gooHit;

        if(Physics.Raycast(gooRay, out gooHit, 100f))
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
            rb.AddForce(firePoint.forward * launchForce, ForceMode.VelocityChange);
        }
    }
}
