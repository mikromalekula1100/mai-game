using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    private bool isDead = false;
    private float deathTimer = 0f;
    private int finalWave = 0;
    private int finalKills = 0;
    private GUIStyle gameOverStyle;
    private GUIStyle subTextStyle;
    private GUIStyle restartStyle;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (!isDead) return;

        deathTimer += Time.unscaledDeltaTime;
        if (deathTimer >= 4f)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        deathTimer = 0f;
        finalWave = WaveManager.Instance != null ? WaveManager.Instance.currentWave : 0;
        finalKills = WaveManager.Instance != null ? WaveManager.Instance.totalKills : 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0.1f;
    }

    void OnGUI()
    {
        if (!isDead) return;

        if (gameOverStyle == null)
        {
            gameOverStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 64,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            gameOverStyle.normal.textColor = new Color(0.9f, 0.15f, 0.15f);

            subTextStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            subTextStyle.normal.textColor = Color.white;

            restartStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
            };
            restartStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        }

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Label(new Rect(cx - 300, cy - 80, 600, 80), "GAME OVER", gameOverStyle);
        GUI.Label(new Rect(cx - 300, cy, 600, 50),
            $"Wave {finalWave}  |  Kills: {finalKills}", subTextStyle);
        GUI.Label(new Rect(cx - 300, cy + 60, 600, 30),
            "Restarting...", restartStyle);
    }
}
