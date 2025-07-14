using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using HoudiniEngineUnity;   // Namespace del plugin Houdini Engine per Unity

[RequireComponent(typeof(HEU_HoudiniAssetRoot))]
public class HdaScript : MonoBehaviour
{
    // Servirà per deserializzare la stringa JSON {"emotion":"fear"}, {"emotion":"happiness"}, ecc.
    [Serializable]
    private class EmotionResponse
    {
        public string emotion;
    }

    // URL dell’endpoint Flask che restituisce {"emotion":"..."}
    private const string EmotionUrl = "http://127.0.0.1:5001/emotion";

    // -------------------------------------------------------------------------
    // 1) COPIA QUI SOTTO, DALL’INSPECTOR DI UNITY,
    //    l’ultima parte del “Copy Parameter Path” per lo slider “Emotion Selector”:
    //    → Seleziona “Emotion Selector” nell’Inspector → clic ingranaggio → Copy Parameter Path
    //    → Incolla qui il nome esatto (case‐sensitive). Esempio: "PARAM_EmotionSelector"
    // -------------------------------------------------------------------------
    private const string ParamNameEmotion = "PARAM_EmotionSelector";
    // -------------------------------------------------------------------------

    // Riferimento al componente HEU_HoudiniAsset (il vero asset Houdini figlio di HDA_Data)
    private HEU_HoudiniAsset asset;

    void Awake()
    {
        // La gerarchia interna in Unity:
        //   GameObject HDA_Root (HEU_HoudiniAssetRoot)
        //     └─ HDA_Data (HEU_HoudiniAsset)
        //
        // Con GetComponentInChildren troviamo il HEU_HoudiniAsset che espone i parametri.
        asset = GetComponentInChildren<HEU_HoudiniAsset>();
        if (asset == null)
        {
            Debug.LogError("[HdaScript] HEU_HoudiniAsset non trovato sotto HDA_Data.");
            enabled = false;
            return;
        }

        // (Facoltativo) Controllo rapido dei nomi interni:
        // LogAllParameterNames();
    }

    void Start()
    {
        // Avvia la coroutine che interroga Flask ogni 0.1 secondi
        StartCoroutine(PollEmotionCoroutine());
    }

    /// <summary>
    /// (Facoltativo) Stampa in console tutti i nomi interni dei parametri 
    /// leggendo il campo privato "_parameterName" di HEU_ParameterData via riflessione.
    /// Utile solo per debug iniziale, poi puoi commentarlo.
    /// </summary>
    private void LogAllParameterNames()
    {
        Debug.Log("---- PARAMETRI INTERNI HDA ----");
        var allParms = asset.Parameters.GetParameters();
        FieldInfo fi = typeof(HEU_ParameterData).GetField("_parameterName",
                         BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var p in allParms)
        {
            if (fi != null)
            {
                string internalName = fi.GetValue(p) as string;
                Debug.Log($"Parametro interno: '{internalName}'");
            }
        }
        Debug.Log("------------------------------");
    }

    /// <summary>
    /// Coroutine che invia una GET a EmotionUrl ogni 0.1 secondi,
    /// deserializza {"emotion":"..."} e chiama ApplyEmotionToHda.
    /// </summary>
    private IEnumerator PollEmotionCoroutine()
    {
        while (true)
        {
            using (var www = UnityWebRequest.Get(EmotionUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    EmotionResponse resp = JsonUtility.FromJson<EmotionResponse>(www.downloadHandler.text);
                    string currentEmotion = resp.emotion?.ToLower().Trim();

                    if (!string.IsNullOrEmpty(currentEmotion))
                    {
                        Debug.Log($"[HdaScript] Emozione ricevuta: {currentEmotion}");
                        ApplyEmotionToHda(currentEmotion);
                    }
                    else
                    {
                        Debug.LogWarning($"[HdaScript] JSON vuoto o non valido: {www.downloadHandler.text}");
                    }
                }
                else
                {
                    Debug.LogError($"[HdaScript] Errore richiesta emotion: {www.error}");
                }
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 1) Imposta lo slider "Emotion Selector" (intero 0–5);
    /// 2) Individua il parametro colore per quell’emozione e gli assegna un valore random 0–5;
    /// 3) Esegue un unico RequestCook() asincrono (bAsync=true) per aggiornare in memoria
    ///    senza alcuna operazione di baking.
    /// </summary>
    private void ApplyEmotionToHda(string emotion)
    {
        // --- 1) Mappatura emotion → intero 0–5 ----------------------------
        //     0=neutral, 1=happiness, 2=surprise, 3=fear, 4=anger, 5=sadness
        int intVal = emotion switch
        {
            "neutral"   => 0,
            "happiness" => 1,
            "surprise"  => 2,
            "fear"      => 3,
            "anger"     => 4,
            "sadness"   => 5,
            _           => 0    // fallback su “neutral” se stringa inaspettata
        };

        // Imposta il parametro Emotion Selector su intVal (senza cook immediato)
        asset.Parameters.SetIntParameterValue(
            ParamNameEmotion,
            intVal,
            0,      // indice di tuple (0 se non è un array)
            false   // false = non cucinare subito, aspettiamo di settare anche il colore
        );

        // --- 2) Individuazione dinamica del parametro colore per l’emozione -----
        // Ogni emozione ha un proprio slider colore: ad esempio
        //   "Neutral_Color_Selection", "Happiness_Color_Selection", "Fear_Color", ecc.
        // Questi cambiano nome interno in base all’emozione: perciò li cerchiamo via riflessione.

        // 2.1) Prendo la lista di tutti i parametri interni
        var allParms = asset.Parameters.GetParameters();

        // 2.2) Costruisco le keyword per la ricerca:
        string keywordEmotion = emotion.ToLower();  // es. “fear”
        string keywordColor   = "color";            // cerco “color” nel nome interno

        FieldInfo fiNameField = typeof(HEU_ParameterData).GetField(
            "_parameterName",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        string foundParamColor = null;

        // Scorro tutti i parameter data e scelgo il primo che contiene "<emotion>color"
        foreach (var p in allParms)
        {
            if (fiNameField != null)
            {
                string internalName = fiNameField.GetValue(p) as string;
                if (!string.IsNullOrEmpty(internalName))
                {
                    string lowerName = internalName.ToLower();
                    if (lowerName.Contains(keywordEmotion) && lowerName.Contains(keywordColor))
                    {
                        foundParamColor = internalName;
                        break;
                    }
                }
            }
        }

        if (foundParamColor == null)
        {
            Debug.LogWarning($"[HdaScript] Parametro colore non trovato per l’emozione “{emotion}”.");
        }
        else
        {
            // --- 2.3) Genero un valore random intero 0–5 per il colore -------------
            int randomColorIndex = UnityEngine.Random.Range(0, 6);  // 0,1,2,3,4,5

            // Imposto il parametro colore (senza cook immediato)
            asset.Parameters.SetIntParameterValue(
                foundParamColor,
                randomColorIndex,
                0,
                false  // false = aspettiamo di cucinare tutto insieme alla fine
            );
        }

        // --- 3) Unico RequestCook() asincrono per applicare i parametri
        // (QUESTO NON FA NESSUN BAKING, ma solo “cucina” l’asset Houdini in memoria):
        asset.RequestCook(
            bCheckParametersChanged: true,
            bAsync:                true,   // ← cook in background, non blocca Unity
            bSkipCookCheck:        true,
            bUploadParameters:     true
        );
    }
}
