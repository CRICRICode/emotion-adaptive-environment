using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class EmotionResponse {
    public string emotion;
}

public class EmotionReceiver : MonoBehaviour
{
    // URL dell'endpoint Flask
    string url = "http://127.0.0.1:5001/emotion";

    void Start() 
    {
        StartCoroutine(GetEmotion());
    }

    IEnumerator GetEmotion() 
    {
        while (true) 
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success) 
            {
                Debug.LogError("Errore nella richiesta: " + www.error);
            } 
            else 
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Risposta dal server: " + jsonResult);
                EmotionResponse response = JsonUtility.FromJson<EmotionResponse>(jsonResult);
                Debug.Log("Emozione estratta: " + response.emotion);
                if (response.emotion == "fear"){
                    // Esegui azioni per l'emozione "fear"
                    Debug.Log("L'utente è spaventato!");
                }
                else if (response.emotion == "disgust"){
                    // Esegui azioni per l'emozione "disgust"
                    Debug.Log("L'utente è disgustato.");
                }
                if(response.emotion == "happy")
                {
                    // Esegui azioni per l'emozione "happy"
                    Debug.Log("L'utente è felice!");
                }
                else if(response.emotion == "sad")
                {
                    // Esegui azioni per l'emozione "sad"
                    Debug.Log("L'utente è triste.");
                }
                else if(response.emotion == "angry")
                {
                    // Esegui azioni per l'emozione "angry"
                    Debug.Log("L'utente è arrabbiato.");
                }
                else if(response.emotion == "surprised")
                {
                    // Esegui azioni per l'emozione "surprised"
                    Debug.Log("L'utente è sorpreso.");
                }
                else if(response.emotion == "neutral")
                {
                    // Esegui azioni per l'emozione "neutral"
                    Debug.Log("L'utente è neutro.");
                }
                else
                {
                    Debug.Log("Emozione non riconosciuta: " + response.emotion);
                }
                // Puoi aggiungere ulteriori azioni basate sul valore di response.emotion
            }
            yield return new WaitForSeconds(0.1f); // Polling ogni 0.1 secondi
        }
    }
}