using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    [Range(0.01f, 0.1f)] public float minTimeScale = 0.05f;
    [Range(0.5f, 1f)] public float maxTimeScale = 1f;
    public float smoothSpeed = 10f;

    private float targetTimeScale;
    private float defaultFixedDeltaTime;

    public float CurrentTimeScale => Time.timeScale;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        targetTimeScale = minTimeScale;
    }

    void Update()
    {
        Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, Time.unscaledDeltaTime * smoothSpeed);
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    /// <summary>
    /// normalizedSpeed: 0 = standing still, 1 = full movement
    /// </summary>
    public void SetTargetFromPlayerSpeed(float normalizedSpeed)
    {
        float clamped = Mathf.Clamp01(normalizedSpeed);
        targetTimeScale = Mathf.Lerp(minTimeScale, maxTimeScale, clamped);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}
