using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float gravity = -20f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 0.15f;

    private CharacterController controller;
    private Transform cam;
    private float verticalVelocity;
    private float cameraPitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>().transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private bool isPaused = false;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (isPaused) return;

        HandleMouseLook();
        HandleMovement();
    }

    void PauseGame()
    {
        isPaused = true;
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetEnemiesActive(false);
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = savedTimeScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SetEnemiesActive(true);
    }

    private float savedTimeScale = 1f;

    void SetEnemiesActive(bool active)
    {
        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude))
            enemy.enabled = active;
        foreach (Bullet bullet in FindObjectsByType<Bullet>(FindObjectsInactive.Exclude))
        {
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = !active;
        }
    }

    void OnGUI()
    {
        if (!isPaused) return;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        // Dim background
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, 0.5f), 0, 0);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(cx - 200, cy - 100, 400, 60), "PAUSED", titleStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22 };

        if (GUI.Button(new Rect(cx - 100, cy - 10, 200, 45), "Resume", btnStyle))
            ResumeGame();

        if (GUI.Button(new Rect(cx - 100, cy + 50, 200, 45), "Quit", btnStyle))
            Application.Quit();
    }

    void HandleMouseLook()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float dx = mouse.delta.x.ReadValue() * mouseSensitivity;
        float dy = mouse.delta.y.ReadValue() * mouseSensitivity;

        transform.Rotate(Vector3.up, dx);
        cameraPitch = Mathf.Clamp(cameraPitch - dy, -89f, 89f);
        cam.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    void HandleMovement()
    {
        var kb = Keyboard.current;
        Vector3 input = Vector3.zero;

        if (kb != null)
        {
            if (kb.wKey.isPressed) input.z += 1f;
            if (kb.sKey.isPressed) input.z -= 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
        }

        input = Vector3.ClampMagnitude(input, 1f);
        Vector3 worldMove = transform.TransformDirection(input) * moveSpeed;

        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        verticalVelocity += gravity * Time.unscaledDeltaTime;
        worldMove.y = verticalVelocity;

        // Use unscaledDeltaTime so player moves at full speed even when time is slowed
        controller.Move(worldMove * Time.unscaledDeltaTime);

        // Report movement to TimeManager
        float speed = input.magnitude;

        // Mouse movement also slightly speeds up time (looking around)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float mouseDelta = new Vector2(mouse.delta.x.ReadValue(), mouse.delta.y.ReadValue()).magnitude;
            if (mouseDelta > 1f)
                speed = Mathf.Max(speed, 0.25f);
        }

        // Shooting also speeds up time
        if (mouse != null && mouse.leftButton.isPressed)
            speed = Mathf.Max(speed, 0.5f);

        if (TimeManager.Instance != null)
            TimeManager.Instance.SetTargetFromPlayerSpeed(speed);
    }
}
