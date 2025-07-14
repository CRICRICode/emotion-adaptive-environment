"""Run the original detector with emotion labels compatible with the Unity scene."""
import threading
import detector

UNITY_LABELS = {
    'neutral': 'neutral',
    'happy': 'happiness',
    'happiness': 'happiness',
    'surprise': 'surprise',
    'fear': 'fear',
    'angry': 'anger',
    'anger': 'anger',
    'sad': 'sadness',
    'sadness': 'sadness',
    'disgust': 'neutral',
    'no_face': 'neutral',
}


def unity_emotion():
    label = UNITY_LABELS.get(detector.current_emotion, 'neutral')
    return detector.jsonify({'emotion': label})


def main():
    detector.app.view_functions['emotion'] = unity_emotion
    flask_thread = threading.Thread(target=detector.run_flask, daemon=True)
    flask_thread.start()
    detector.video_loop()


if __name__ == '__main__':
    main()
