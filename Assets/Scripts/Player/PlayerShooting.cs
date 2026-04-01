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

    private float nextFireTime;
    private CameraShake cameraShake;

    void Start()
    {
        cameraShake = GetComponentInChildren<CameraShake>();
        if (firePoint == null)
            firePoint = GetComponentInChildren<Camera>().transform;
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
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint.position + firePoint.forward * 0.8f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, firePoint.rotation);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = firePoint.forward * bulletSpeed;

        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
            b.isPlayerBullet = true;

        if (cameraShake != null)
            cameraShake.Shake(0.05f, 0.03f);
    }
}
