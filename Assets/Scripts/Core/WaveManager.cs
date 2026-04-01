using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Settings")]
    public int baseEnemyCount = 3;
    public int enemiesPerWave = 2;
    public float timeBetweenWaves = 3f;
    public float minSpawnDistFromPlayer = 10f;

    [Header("Enemy Prefab Data")]
    public GameObject bulletPrefab;
    public Material enemyBodyMat;
    public Material enemyHeadMat;

    [Header("Map Generation")]
    public Transform coverParent;
    public Material coverMat;
    public Material accentMat;

    [Header("State")]
    public int currentWave = 0;
    public int enemiesAlive = 0;
    public bool waveInProgress = false;
    public int totalKills = 0;

    public string announcement = "";
    public float announcementTimer = 0f;

    private Transform player;
    private float waveCountdown;
    private List<Bounds> placedCovers = new List<Bounds>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        waveCountdown = 2f;
    }

    void Update()
    {
        if (announcementTimer > 0f)
            announcementTimer -= Time.unscaledDeltaTime;

        if (waveInProgress)
        {
            if (enemiesAlive <= 0)
            {
                waveInProgress = false;
                waveCountdown = timeBetweenWaves;
                ShowAnnouncement("WAVE CLEARED!");
                HealPlayer();
            }
            return;
        }

        waveCountdown -= Time.unscaledDeltaTime;
        if (waveCountdown <= 0f)
        {
            StartNextWave();
        }
    }

    void StartNextWave()
    {
        currentWave++;

        // Regenerate covers
        RegenerateCovers();


        int enemyCount = baseEnemyCount + (currentWave - 1) * enemiesPerWave;
        enemyCount = Mathf.Min(enemyCount, 20);

        ShowAnnouncement($"WAVE {currentWave}");

        int health = 50 + (currentWave - 1) * 10;
        float fireRate = 1f / (1f + (currentWave - 1) * 0.15f);
        float speed = 3.5f + (currentWave - 1) * 0.3f;
        float inaccuracy = Mathf.Max(0.5f, 2f - (currentWave - 1) * 0.2f);

        int actualSpawned = 0;
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = GetValidNavMeshPosition();
            if (spawnPos != Vector3.zero)
            {
                SpawnEnemy(spawnPos, health, fireRate, speed, inaccuracy);
                actualSpawned++;
            }
        }

        enemiesAlive = actualSpawned;
        waveInProgress = true;
    }

    // ===== MAP GENERATION =====

    void RegenerateCovers()
    {
        if (coverParent == null) return;

        // Remove old covers instantly
        for (int i = coverParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(coverParent.GetChild(i).gameObject);

        // Remove lingering bullet decals
        foreach (GameObject decal in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude))
        {
            if (decal.name == "BulletDecal")
                DestroyImmediate(decal);
        }

        placedCovers.Clear();
        Vector3 playerPos = player != null ? player.position : new Vector3(0, 0, -20);

        // Pillars: 4-6
        int pillars = Random.Range(4, 7);
        for (int i = 0; i < pillars; i++)
        {
            float w = Random.Range(1.2f, 2f);
            Vector3 size = new Vector3(w, 5f, w);
            TryPlaceCover($"Pillar_{i}", size, coverMat, playerPos);
        }

        // Crates: 3-5
        int crates = Random.Range(3, 6);
        for (int i = 0; i < crates; i++)
        {
            float s = Random.Range(1.2f, 1.8f);
            Vector3 size = new Vector3(s, s, s);
            TryPlaceCover($"Crate_{i}", size, accentMat, playerPos);
        }

        // Half-walls: 3-4
        int walls = Random.Range(3, 5);
        for (int i = 0; i < walls; i++)
        {
            float len = Random.Range(4f, 7f);
            Vector3 size = Random.value > 0.5f
                ? new Vector3(len, 1.5f, 0.4f)
                : new Vector3(0.4f, 1.5f, len);
            TryPlaceCover($"HalfWall_{i}", size, coverMat, playerPos);
        }

        // Blocks: 1-2
        int blocks = Random.Range(1, 3);
        for (int i = 0; i < blocks; i++)
        {
            float bx = Random.Range(3f, 5f);
            float bz = Random.Range(3f, 5f);
            Vector3 size = new Vector3(bx, 0.8f, bz);
            TryPlaceCover($"Block_{i}", size, accentMat, playerPos);
        }
    }

    void TryPlaceCover(string name, Vector3 size, Material mat, Vector3 playerPos)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float x = Random.Range(-22f, 22f);
            float z = Random.Range(-22f, 22f);
            float y = size.y / 2f;
            Vector3 pos = new Vector3(x, y, z);

            // Not too close to player or spawn point
            Vector3 spawnPoint = new Vector3(0, 0, -20);
            if (Vector3.Distance(new Vector3(x, 0, z), new Vector3(playerPos.x, 0, playerPos.z)) < 8f)
                continue;
            if (Vector3.Distance(new Vector3(x, 0, z), spawnPoint) < 6f)
                continue;

            // Not overlapping other covers
            Bounds newBounds = new Bounds(pos, size + Vector3.one * 2f);
            bool overlaps = false;
            foreach (Bounds b in placedCovers)
            {
                if (b.Intersects(newBounds)) { overlaps = true; break; }
            }
            if (overlaps) continue;

            // Place it
            GameObject cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name = name;
            cover.transform.SetParent(coverParent);
            cover.transform.position = pos;
            cover.transform.localScale = size;
            if (mat != null) cover.GetComponent<Renderer>().sharedMaterial = mat;

            // NavMeshObstacle carves itself out of NavMesh instantly — no rebuild needed
            NavMeshObstacle obstacle = cover.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            obstacle.center = Vector3.zero;

            placedCovers.Add(new Bounds(pos, size));
            return;
        }
    }

    // ===== ENEMY SPAWNING =====

    void SpawnEnemy(Vector3 position, int health, float fireRate, float speed, float inaccuracy)
    {
        GameObject enemy = new GameObject("Enemy");
        enemy.transform.position = position;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(enemy.transform);
        body.transform.localPosition = new Vector3(0, 1f, 0);
        body.transform.localScale = new Vector3(0.6f, 0.9f, 0.4f);
        if (enemyBodyMat != null) body.GetComponent<Renderer>().sharedMaterial = enemyBodyMat;
        Object.Destroy(body.GetComponent<CapsuleCollider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(enemy.transform);
        head.transform.localPosition = new Vector3(0, 2.0f, 0);
        head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        if (enemyHeadMat != null) head.GetComponent<Renderer>().sharedMaterial = enemyHeadMat;
        Object.Destroy(head.GetComponent<SphereCollider>());

        CreateLimb(enemy.transform, "LeftArm", new Vector3(-0.45f, 1.2f, 0), new Vector3(0.15f, 0.45f, 0.15f));
        CreateLimb(enemy.transform, "RightArm", new Vector3(0.45f, 1.2f, 0), new Vector3(0.15f, 0.45f, 0.15f));
        CreateLimb(enemy.transform, "LeftLeg", new Vector3(-0.15f, 0.3f, 0), new Vector3(0.18f, 0.4f, 0.18f));
        CreateLimb(enemy.transform, "RightLeg", new Vector3(0.15f, 0.3f, 0), new Vector3(0.18f, 0.4f, 0.18f));

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(enemy.transform);
        firePoint.transform.localPosition = new Vector3(0.5f, 1.4f, 0.4f);

        CapsuleCollider col = enemy.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 1f, 0);
        col.height = 2f;
        col.radius = 0.4f;

        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = speed;
        agent.angularSpeed = 120f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1f;

        EnemyAI ai = enemy.AddComponent<EnemyAI>();
        ai.bulletPrefab = bulletPrefab;
        ai.firePoint = firePoint.transform;
        ai.fireRate = fireRate;
        ai.aimInaccuracy = inaccuracy;

        EnemyHealth eh = enemy.AddComponent<EnemyHealth>();
        eh.maxHealth = health;
        eh.currentHealth = health;
    }

    void CreateLimb(Transform parent, string name, Vector3 localPos, Vector3 localScale)
    {
        GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        limb.name = name;
        limb.transform.SetParent(parent);
        limb.transform.localPosition = localPos;
        limb.transform.localScale = localScale;
        if (enemyBodyMat != null) limb.GetComponent<Renderer>().sharedMaterial = enemyBodyMat;
        Object.Destroy(limb.GetComponent<CapsuleCollider>());
    }

    Vector3 GetValidNavMeshPosition()
    {
        for (int i = 0; i < 30; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * 22f;
            randomDir.y = 0;

            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                if (player == null || Vector3.Distance(hit.position, player.position) >= minSpawnDistFromPlayer)
                    return hit.position;
            }
        }
        return Vector3.zero;
    }

    // ===== UTILITIES =====

    public void OnEnemyKilled()
    {
        enemiesAlive--;
        totalKills++;
    }

    void HealPlayer()
    {
        if (player == null) return;
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
            ph.currentHealth = ph.maxHealth;
    }

    void ShowAnnouncement(string text)
    {
        announcement = text;
        announcementTimer = 2.5f;
    }
}
