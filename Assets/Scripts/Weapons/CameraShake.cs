using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private float shakeDuration;
    private float shakeIntensity;
    private Vector3 originalLocalPos;

    void Start()
    {
        originalLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (shakeDuration > 0f)
        {
            transform.localPosition = originalLocalPos + Random.insideUnitSphere * shakeIntensity;
            shakeDuration -= Time.unscaledDeltaTime;
        }
        else
        {
            transform.localPosition = originalLocalPos;
        }
    }

    public void Shake(float duration, float intensity)
    {
        shakeDuration = duration;
        shakeIntensity = intensity;
    }
}
