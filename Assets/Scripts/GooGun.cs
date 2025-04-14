using UnityEngine;

public class GooGun : MonoBehaviour
{
    public GameObject gooProjectilePrefab;
    public Transform firePoint;
    public float launchForce = 20f;
    public float fireCooldown = 0.5f;

    public float arcStrength = .1f;

    private float lastFireTime;
    Vector3 targetingPt;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time > lastFireTime + fireCooldown)
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
}
