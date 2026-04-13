using UnityEngine;

public class GameHUD : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private GUIStyle labelStyle;
    private GUIStyle announcementStyle;
    private GUIStyle waveInfoStyle;
    private Texture2D healthBarBg;
    private Texture2D healthBarFill;
    private Texture2D timeFill;
    private Texture2D crosshairTex;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        healthBarBg = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f, 0.8f));
        healthBarFill = MakeTex(1, 1, new Color(0.8f, 0.15f, 0.15f, 0.9f));
        timeFill = MakeTex(1, 1, new Color(0.2f, 0.6f, 1f, 0.9f));
        crosshairTex = MakeTex(1, 1, new Color(1f, 1f, 1f, 0.9f));
    }

    void OnGUI()
    {
        if (labelStyle == null) InitStyles();

        DrawCrosshair();
        DrawHealthBar();
        DrawTimeBar();
        DrawWaveInfo();
        DrawAnnouncement();
    }

    void DrawCrosshair()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        float t = 2f;
        float len = 10f;
        float gap = 4f;

        // 4 lines with gap in center
        GUI.DrawTexture(new Rect(cx - t / 2, cy - gap - len, t, len), crosshairTex);  // top
        GUI.DrawTexture(new Rect(cx - t / 2, cy + gap, t, len), crosshairTex);         // bottom
        GUI.DrawTexture(new Rect(cx - gap - len, cy - t / 2, len, t), crosshairTex);   // left
        GUI.DrawTexture(new Rect(cx + gap, cy - t / 2, len, t), crosshairTex);         // right
    }

    void DrawHealthBar()
    {
        float barW = 200f;
        float barH = 20f;
        float barX = 20f;
        float barY = Screen.height - 50f;

        GUI.Label(new Rect(barX, barY - 22, 100, 20), "HEALTH", labelStyle);
        GUI.DrawTexture(new Rect(barX, barY, barW, barH), healthBarBg);

        if (playerHealth != null)
        {
            float healthPct = (float)playerHealth.currentHealth / playerHealth.maxHealth;
            GUI.DrawTexture(new Rect(barX, barY, barW * healthPct, barH), healthBarFill);
        }
    }

    void DrawTimeBar()
    {
        if (TimeManager.Instance == null) return;

        float barW = 200f;
        float barH = 20f;
        float barY = Screen.height - 50f;
        float tBarX = Screen.width - 220f;
        float timeScale = TimeManager.Instance.CurrentTimeScale;

        GUI.Label(new Rect(tBarX, barY - 22, 200, 20),
            $"TIME: {Mathf.RoundToInt(timeScale * 100)}%", labelStyle);
        GUI.DrawTexture(new Rect(tBarX, barY, barW, barH), healthBarBg);
        GUI.DrawTexture(new Rect(tBarX, barY, barW * timeScale, barH), timeFill);
    }

    void DrawWaveInfo()
    {
        if (WaveManager.Instance == null) return;

        string waveText = $"WAVE: {WaveManager.Instance.currentWave}";
        string enemyText = $"ENEMIES: {WaveManager.Instance.enemiesAlive}";
        string killText = $"KILLS: {WaveManager.Instance.totalKills}";

        GUI.Label(new Rect(Screen.width / 2f - 60, 15, 200, 25), waveText, waveInfoStyle);
        GUI.Label(new Rect(Screen.width / 2f - 60, 38, 200, 25), enemyText, labelStyle);
        GUI.Label(new Rect(20, 15, 200, 25), killText, waveInfoStyle);
    }

    void DrawAnnouncement()
    {
        if (WaveManager.Instance == null) return;
        if (WaveManager.Instance.announcementTimer <= 0f) return;

        string text = WaveManager.Instance.announcement;
        float alpha = Mathf.Clamp01(WaveManager.Instance.announcementTimer / 0.5f);
        Color c = announcementStyle.normal.textColor;
        c.a = alpha;
        announcementStyle.normal.textColor = c;

        float w = 400f;
        float h = 60f;
        GUI.Label(new Rect(Screen.width / 2f - w / 2, Screen.height / 2f - 100, w, h),
            text, announcementStyle);
    }

    void InitStyles()
    {
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
        };
        labelStyle.normal.textColor = Color.white;

        waveInfoStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperCenter,
        };
        waveInfoStyle.normal.textColor = new Color(1f, 0.9f, 0.3f);

        announcementStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        announcementStyle.normal.textColor = Color.white;
    }

    Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D tex = new Texture2D(w, h);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }
}
