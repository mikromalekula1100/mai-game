using UnityEngine;

public class BulletImpact : MonoBehaviour
{
    public static BulletImpact Instance { get; private set; }

    private GameObject[] particlePools;
    private int poolIndex = 0;
    private const int POOL_SIZE = 20;

    private Material decalMatYellow;
    private Material decalMatRed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        InitPool();
        InitDecalMaterials();
    }

    void InitPool()
    {
        particlePools = new GameObject[POOL_SIZE];
        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject go = new GameObject("ImpactFX_" + i);
            go.transform.SetParent(transform);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.3f;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.maxParticles = 15;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 8, 15)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.05f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Sprites/Default"));

            go.SetActive(false);
            particlePools[i] = go;
        }
    }

    void InitDecalMaterials()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");

        decalMatYellow = new Material(shader);
        decalMatYellow.color = new Color(1f, 0.85f, 0.15f, 0.7f);

        decalMatRed = new Material(shader);
        decalMatRed.color = new Color(0.9f, 0.15f, 0.1f, 0.7f);
    }

    public void SpawnImpact(Vector3 position, Vector3 normal, bool isPlayerBullet)
    {
        // Particle burst
        GameObject fx = particlePools[poolIndex];
        poolIndex = (poolIndex + 1) % POOL_SIZE;

        fx.transform.position = position;
        fx.transform.rotation = Quaternion.LookRotation(normal);
        fx.SetActive(true);

        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = isPlayerBullet
            ? new Color(1f, 0.9f, 0.3f)
            : new Color(1f, 0.3f, 0.15f);
        ps.Play();

        // Decal
        SpawnDecal(position, normal, isPlayerBullet);
    }

    void SpawnDecal(Vector3 position, Vector3 normal, bool isPlayerBullet)
    {
        GameObject decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
        decal.name = "BulletDecal";
        Object.Destroy(decal.GetComponent<MeshCollider>());

        float size = Random.Range(0.1f, 0.2f);
        decal.transform.localScale = new Vector3(size, size, size);
        decal.transform.position = position + normal * 0.001f; // Slight offset to avoid z-fighting
        decal.transform.rotation = Quaternion.LookRotation(-normal);
        decal.transform.Rotate(0, 0, Random.Range(0f, 360f)); // Random rotation for variety

        Renderer r = decal.GetComponent<Renderer>();
        r.material = isPlayerBullet ? decalMatYellow : decalMatRed;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;

        // Fade and destroy after 15 seconds
        DecalFade fade = decal.AddComponent<DecalFade>();
        fade.lifetime = 15f;
    }
}

public class DecalFade : MonoBehaviour
{
    public float lifetime = 15f;
    private float timer;
    private Renderer rend;
    private Color originalColor;

    void Start()
    {
        rend = GetComponent<Renderer>();
        originalColor = rend.material.color;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Start fading in last 3 seconds
        float fadeStart = lifetime - 3f;
        if (timer > fadeStart)
        {
            float alpha = 1f - (timer - fadeStart) / 3f;
            Color c = originalColor;
            c.a = originalColor.a * alpha;
            rend.material.color = c;
        }
    }
}
