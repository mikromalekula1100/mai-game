using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public int damage = 25;
    public float lifetime = 5f;
    public bool isPlayerBullet = true;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        bool hitTarget = false;

        if (isPlayerBullet)
        {
            EnemyHealth enemy = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                hitTarget = true;
            }
        }
        else
        {
            PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(damage);
                hitTarget = true;
            }
        }

        // Spawn impact effect on surfaces (not on characters)
        if (!hitTarget && BulletImpact.Instance != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            BulletImpact.Instance.SpawnImpact(contact.point, contact.normal, isPlayerBullet);
        }

        Destroy(gameObject);
    }
}
