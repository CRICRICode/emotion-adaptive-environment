using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;


public class EmotionController : MonoBehaviour
{

    [Serializable]
    public class EmotionResponse
    {
        public string emotion;
    }


    // URL dell’endpoint Flask che restituisce {"emotion":"neutral"}, {"emotion":"fear"}, ecc.
    private const string EmotionUrl = "http://127.0.0.1:5001/emotion";

    // Riferimenti ai GameObject da attivare/disattivare in base all’emozione.
    // Nell’Inspector, trascinare qui i prefab/oggetti presenti in scena.
    [Header("GameObjects associati alle emozioni")]
    [Tooltip("GameObject da attivare quando l’emozione è \"neutral\"")]
    public GameObject neutralObject;

    [Tooltip("GameObject da attivare quando l’emozione è \"happiness\"")]
    public GameObject happinessObject;

    [Tooltip("GameObject da attivare quando l’emozione è \"surprise\"")]
    public GameObject surpriseObject;

    [Tooltip("GameObject da attivare quando l’emozione è \"fear\"")]
    public GameObject fearObject;

    [Tooltip("GameObject da attivare quando l’emozione è \"anger\"")]
    public GameObject angerObject;

    [Tooltip("GameObject da attivare quando l’emozione è \"sadness\"")]
    public GameObject sadnessObject;

    // Mappa interna per velocizzare l’attivazione: associa una stringa-emozione al relativo GameObject.
    private readonly System.Collections.Generic.Dictionary<string, GameObject> _emotionToObject =
        new System.Collections.Generic.Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

    // GameObject attualmente attivo (per evitare di riattivare ripetutamente lo stesso oggetto).
    private GameObject _currentlyActiveObject;


    // --------- NUOVA SEZIONE PER IL CONTROLLO LUCE ---------
    [Header("Light Settings")]
    [Tooltip("La luce principale della scena (Directional Light)")]
    public Light sceneLight;

    [Serializable]
    public class LightSettings
    {
        public Color color = Color.white;
        public float intensity = 1f;
        public Vector3 eulerAngles = Vector3.zero;
    }

    public LightSettings neutralLight;
    public LightSettings happinessLight;
    public LightSettings surpriseLight;
    public LightSettings fearLight;
    public LightSettings angerLight;
    public LightSettings sadnessLight;

    private readonly System.Collections.Generic.Dictionary<string, LightSettings> _emotionToLight =
        new System.Collections.Generic.Dictionary<string, LightSettings>(StringComparer.OrdinalIgnoreCase);

    private Coroutine _lightCoroutine;


    void Awake()
    {
        // Inizializza la mappa stringa → GameObject basata sui riferimenti pubblici
        if (neutralObject != null)   _emotionToObject["neutral"]   = neutralObject;
        if (happinessObject != null) _emotionToObject["happiness"] = happinessObject;
        if (surpriseObject != null)  _emotionToObject["surprise"]  = surpriseObject;
        if (fearObject != null)      _emotionToObject["fear"]      = fearObject;
        if (angerObject != null)     _emotionToObject["anger"]     = angerObject;
        if (sadnessObject != null)   _emotionToObject["sadness"]   = sadnessObject;

        // Disattiva tutti i GameObject all’avvio
        foreach (var pair in _emotionToObject)
            if (pair.Value != null)
                pair.Value.SetActive(false);

        _currentlyActiveObject = null;


        // Inizializza la mappa stringa → LightSettings
        _emotionToLight["neutral"]   = neutralLight;
        _emotionToLight["happiness"] = happinessLight;
        _emotionToLight["surprise"]  = surpriseLight;
        _emotionToLight["fear"]      = fearLight;
        _emotionToLight["anger"]     = angerLight;
        _emotionToLight["sadness"]   = sadnessLight;
    }

    void Start()
    {
        // Avvia la coroutine di polling in background
        StartCoroutine(PollEmotionCoroutine());
    }

    /// <summary>
    /// Coroutine che invia una richiesta GET ogni 0.1 secondi all’endpoint Flask.
    /// Quando riceve una risposta valida, deserializza l’emozione e la passa ad ActivateForEmotion().
    /// </summary>
    private IEnumerator PollEmotionCoroutine()
    {
        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(EmotionUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    // Deserializzo la stringa JSON in un oggetto EmotionResponse
                    EmotionResponse resp = JsonUtility.FromJson<EmotionResponse>(www.downloadHandler.text);
                    string corrente = resp.emotion?.ToLower().Trim();

                    if (!string.IsNullOrEmpty(corrente))
                    {
                        ActivateForEmotion(corrente);
                    }
                    else
                    {
                        Debug.LogWarning($"[EmotionActivator] Risposta JSON non valida: {www.downloadHandler.text}");
                    }
                }
                else
                {
                    Debug.LogError($"[EmotionActivator] Errore nella richiesta: {www.error}");
                }
            }

            // Attende 4 secondi prima del prossimo polling
            yield return new WaitForSeconds(4f);
        }
    }

    /// <summary>
    /// Attiva il GameObject associato all’emozione fornita e disattiva quello precedentemente attivo.
    /// Se l’emozione non ha un GameObject corrispondente, disattiva solo quello attualmente attivo.
    /// </summary>
    /// <param name="emotion">Stringa che rappresenta l’emozione ricevuta (es. “fear”, “happiness”)</param>
    private void ActivateForEmotion(string emotion)
    {
        // 1) GESTIONE PREFAB
        if (_emotionToObject.TryGetValue(emotion, out GameObject target))
        {
            if (target != _currentlyActiveObject)
            {
                if (_currentlyActiveObject != null)
                    _currentlyActiveObject.SetActive(false);

                target.SetActive(true);
                _currentlyActiveObject = target;
            }
        }
        else
        {
            if (_currentlyActiveObject != null)
            {
                _currentlyActiveObject.SetActive(false);
                _currentlyActiveObject = null;
            }
            Debug.LogWarning($"[EmotionActivator] Nessun GameObject associato all’emozione “{emotion}”.");
        }

        // 2) GESTIONE LUCE (TRANSIZIONE GRADUALE)
        if (sceneLight != null && _emotionToLight.TryGetValue(emotion, out LightSettings ls))
        {
            TransitionLight(ls, duration: 2f);
        }
    }

    //COROUTINE PER IL CHANGE GRADUALE DELLA LUCE
    private void TransitionLight(LightSettings target, float duration)
    {
        if (_lightCoroutine != null)
            StopCoroutine(_lightCoroutine);

        _lightCoroutine = StartCoroutine(LightLerpCoroutine(target, duration));
    }

    private IEnumerator LightLerpCoroutine(LightSettings target, float duration)
    {
        Color   startColor     = sceneLight.color;
        float   startIntensity = sceneLight.intensity;
        Vector3 startEuler     = sceneLight.transform.rotation.eulerAngles;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, t / duration);

            sceneLight.color     = Color.Lerp(startColor,     target.color,     blend);
            sceneLight.intensity = Mathf.Lerp(startIntensity, target.intensity, blend);

            Vector3 ea = Vector3.Lerp(startEuler, target.eulerAngles, blend);
            sceneLight.transform.rotation = Quaternion.Euler(ea);

            yield return null;
        }

        // Forza valori finali
        sceneLight.color     = target.color;
        sceneLight.intensity = target.intensity;
        sceneLight.transform.rotation = Quaternion.Euler(target.eulerAngles);

        _lightCoroutine = null;
    }
}
