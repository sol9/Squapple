using UnityEngine;

namespace Squapple.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class PlayerFeedback : MonoBehaviour
    {
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;

        private const string SoundKey = "Squapple.Settings.Sound";
        private const string VibrationKey = "Squapple.Settings.Vibration";
        private AudioSource _source;

        public bool SoundEnabled { get; private set; }
        public bool VibrationEnabled { get; private set; }

        public void Initialize()
        {
            _source = GetComponent<AudioSource>();
            SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) != 0;
            VibrationEnabled = PlayerPrefs.GetInt(VibrationKey, 0) != 0;
        }

        public void SetSound(bool enabled)
        {
            SoundEnabled = enabled;
            if (!enabled)
                Stop();
            PlayerPrefs.SetInt(SoundKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetVibration(bool enabled)
        {
            VibrationEnabled = enabled;
            PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Play(bool success)
        {
            Stop();
            if (SoundEnabled)
                _source.PlayOneShot(success ? successSound : errorSound);
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (success && VibrationEnabled)
                Handheld.Vibrate();
#endif
        }

        public void Stop()
        {
            if (_source != null)
                _source.Stop();
        }

        private void OnDisable() => Stop();
    }
}
