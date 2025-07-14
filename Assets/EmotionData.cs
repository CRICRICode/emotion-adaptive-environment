using System;
using UnityEngine;

[Serializable]
public enum EmotionType
{
    Neutral = 0,
    Happiness = 1,
    Surprise = 2,
    Fear = 3,
    Anger = 4,
    Sadness = 5
}

[Serializable]
public class EmotionData :MonoBehaviour
{
    /// <summary>
    /// Tipo di emozione rilevata (Neutral, Happiness, Surprise, Fear, Anger, Sadness).
    /// </summary>
    public EmotionType Emotion { get; private set; }

    /// <summary>
    /// Intensità dell’emozione, scala 0.0–1.0. 
    /// Puoi usarlo per modulare, ad esempio, la forza del feedback visivo o sonoro.
    /// </summary>
    public float Intensity { get; private set; }

    /// <summary>
    /// Timestamp (UTC) del momento in cui l’emozione è stata rilevata.
    /// </summary>
    public DateTime DetectedAt { get; private set; }

    /// <summary>
    /// Costruttore completo.
    /// </summary>
    /// <param name="type">Tipo di emozione.</param>
    /// <param name="intensity">Intensità (0.0–1.0).</param>
    /// <param name="detectedAt">Ora esatta del rilevamento (DateTime.UtcNow di default).</param>
    public EmotionData(EmotionType type, float intensity = 1.0f, DateTime? detectedAt = null)
    {
        Emotion = type;
        Intensity = Mathf.Clamp01(intensity);
        DetectedAt = detectedAt ?? DateTime.UtcNow;
    }

    public override string ToString()
    {
        return $"[EmotionData] Emotion={Emotion}, Intensity={Intensity:F2}, Time={DetectedAt:O}";
    }
}
