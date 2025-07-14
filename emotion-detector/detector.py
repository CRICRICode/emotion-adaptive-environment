import threading
import cv2
from flask import Flask, jsonify
from fer import FER

app = Flask(__name__)
emotion_detector = FER()
current_emotion = "no_face"


def video_loop():
    global current_emotion
    cap = cv2.VideoCapture(0)  # Prova 1, 2 se non funziona il dispositivo 0
    if not cap.isOpened():
        print("Impossibile aprire la webcam")
        return

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        # --------------------------------------------------
        # 1) Ridimensionamento per aumentare velocità/affidabilità
        #    Manteniamo larghezza = 480 pixel (altezza proporzionata)
        height, width = frame.shape[:2]
        new_width = 480
        new_height = int((new_width / width) * height)
        small_frame = cv2.resize(frame, (new_width, new_height))

        # 2) Conversione in RGB (FER si aspetta immagini in RGB)
        rgb_frame = cv2.cvtColor(small_frame, cv2.COLOR_BGR2RGB)

        # 3) (Opzionale) Equalizzazione della luminosità/contrasto
        gray = cv2.cvtColor(small_frame, cv2.COLOR_BGR2GRAY)
        equalized = cv2.equalizeHist(gray)
        # Ricreiamo un’immagine a 3 canali per passare a FER
        colored_eq = cv2.cvtColor(equalized, cv2.COLOR_GRAY2BGR)
        rgb_eq_frame = cv2.cvtColor(colored_eq, cv2.COLOR_BGR2RGB)

        # 4) Rilevamento dell’emozione sul frame equalizzato+ridotto
        analysis = emotion_detector.detect_emotions(rgb_eq_frame)
        if analysis:
            detection = analysis[0]
            if "box" in detection and len(detection["box"]) == 4:
                x, y, w, h = detection["box"]
                # Riporto le coordinate al frame originale (per disegnare)
                scale_x = width / new_width
                scale_y = height / new_height
                x0 = int(x * scale_x)
                y0 = int(y * scale_y)
                w0 = int(w * scale_x)
                h0 = int(h * scale_y)
                cv2.rectangle(frame, (x0, y0), (x0 + w0, y0 + h0), (238, 130, 238), 2)

            # Scegli l’emozione con probabilità massima
            current_emotion = max(detection["emotions"], key=detection["emotions"].get)
        else:
            current_emotion = "no_face"

        # Mostra il frame originale (con rettangolo) per debug
        cv2.imshow("Webcam Debug", frame)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()


@app.route("/emotion")
def emotion():
    # Restituisce JSON con la stringa corrente di emozione
    return jsonify({"emotion": current_emotion})


def run_flask():
    # Avvia Flask in background (porta 5001, host locale)
    app.run(host="127.0.0.1", port=5001)


if __name__ == "__main__":
    # Avvio del server Flask in un thread separato
    flask_thread = threading.Thread(target=run_flask, daemon=True)
    flask_thread.start()

    # Avvio del loop video nel thread principale
    video_loop()
