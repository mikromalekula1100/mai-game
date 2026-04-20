using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting")]
    public GameObject bulletPrefab;
    public float bulletSpeed = 40f;
    public float fireRate = 0.15f;

    [Header("Bullet Spawn")]
    public Transform firePoint;

    [Header("Aiming")]
    public Camera aimCamera;
    public float maxAimDistance = 100f;
    public float muzzleForwardOffset = 0.05f;
    public float wallShotPadding = 0.05f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootClip;
    [Range(0f, 1f)] public float shootVolume = 0.8f;

    private float nextFireTime;
    private CameraShake cameraShake;

    void Start()
    {
        cameraShake = GetComponentInChildren<CameraShake>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (aimCamera == null)
            aimCamera = GetComponentInChildren<Camera>();

        if (firePoint == null)
            firePoint = aimCamera != null ? aimCamera.transform : transform;
    }

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.isPressed && Time.unscaledTime >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.unscaledTime + fireRate;
        }
    }

    void Fire()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 shootDirection = GetShootDirection();
        Vector3 spawnPos = firePoint.position + shootDirection * muzzleForwardOffset;

        if (TryBlockShotThroughWall(spawnPos, out Vector3 blockedSpawnPos, out Vector3 blockedDirection))
        {
            spawnPos = blockedSpawnPos;
            shootDirection = blockedDirection;
        }

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(shootDirection));

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = shootDirection * bulletSpeed;

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
            b.isPlayerBullet = true;

        if (cameraShake != null)
            cameraShake.Shake(0.05f, 0.03f);

        PlayShootSound();
    }

    void PlayShootSound()
    {
        if (shootClip == null) return;

        if (audioSource != null)
            audioSource.PlayOneShot(shootClip, shootVolume);
        else
            AudioSource.PlayClipAtPoint(shootClip, firePoint.position, shootVolume);
    }

    Vector3 GetShootDirection()
    {
        if (aimCamera == null)
            return firePoint.forward;

        Ray aimRay = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
        Vector3 targetPoint = aimRay.origin + aimRay.direction * maxAimDistance;
        RaycastHit[] hits = Physics.RaycastAll(aimRay, maxAimDistance, ~0, QueryTriggerInteraction.Ignore);

        float closestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                targetPoint = hit.point;
            }
        }

        Vector3 direction = targetPoint - firePoint.position;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : aimCamera.transform.forward;
    }

    bool TryBlockShotThroughWall(Vector3 intendedSpawnPos, out Vector3 safeSpawnPos, out Vector3 safeDirection)
    {
        safeSpawnPos = intendedSpawnPos;
        safeDirection = (intendedSpawnPos - firePoint.position).normalized;

        if (aimCamera == null)
            return false;

        Vector3 origin = aimCamera.transform.position;
        Vector3 cameraToMuzzle = intendedSpawnPos - origin;
        float distanceToMuzzle = cameraToMuzzle.magnitude;

        if (distanceToMuzzle <= 0.001f)
            return false;

        Vector3 checkDirection = cameraToMuzzle / distanceToMuzzle;
        RaycastHit[] hits = Physics.RaycastAll(origin, checkDirection, distanceToMuzzle, ~0, QueryTriggerInteraction.Ignore);

        float closestDistance = float.PositiveInfinity;
        RaycastHit closestHit = default;
        bool hasHit = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestHit = hit;
                hasHit = true;
            }
        }

        if (!hasHit)
            return false;

        safeSpawnPos = closestHit.point - checkDirection * wallShotPadding;
        Vector3 directionToWall = closestHit.point - safeSpawnPos;
        safeDirection = directionToWall.sqrMagnitude > 0.001f
            ? directionToWall.normalized
            : aimCamera.transform.forward;

        return true;
    }
}
