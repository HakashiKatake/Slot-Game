using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Audio
{
    public class SlotAudioService : MonoBehaviour
    {
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource loopSource;

        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip leverPullClip;
        [SerializeField] private AudioClip spinLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip winBellClip;
        [SerializeField] private AudioClip jackpotFanfareClip;

        public bool IsMuted { get; private set; }

        private void Awake()
        {
            if (sfxSource == null) { sfxSource = gameObject.AddComponent<AudioSource>(); sfxSource.playOnAwake = false; }
            if (loopSource == null) { loopSource = gameObject.AddComponent<AudioSource>(); loopSource.playOnAwake = false; loopSource.loop = true; }
        }

        public void PlayButtonClick() => Sfx(buttonClickClip, 0.8f);
        public void PlayLeverPull()   => Sfx(leverPullClip, 1.0f);
        public void StopSpinLoop()    => loopSource?.Stop();
        public void ToggleMute()      => SetMute(!IsMuted);

        public void PlaySpinLoop()
        {
            if (IsMuted || spinLoopClip == null) return;
            loopSource.clip = spinLoopClip;
            loopSource.volume = 0.65f;
            loopSource.Play();
        }

        public void PlayReelStop(int reelIndex)
        {
            if (IsMuted || reelStopClip == null) return;
            sfxSource.pitch = 1.0f + reelIndex * 0.06f;   // pitch rises: 1.0 → 1.06 → 1.12
            sfxSource.PlayOneShot(reelStopClip, 0.9f);
            sfxSource.pitch = 1.0f;
        }

        public void PlayWin(WinTier tier)
        {
            var clip = (tier == WinTier.Jackpot || tier == WinTier.BigWin) ? jackpotFanfareClip : winBellClip;
            Sfx(clip, tier == WinTier.Jackpot ? 1.0f : 0.9f);
        }

        public void SetMute(bool muted)
        {
            IsMuted = muted;
            if (sfxSource != null) sfxSource.mute = muted;
            if (loopSource != null) loopSource.mute = muted;
        }

        private void Sfx(AudioClip clip, float volume)
        {
            if (IsMuted || clip == null || sfxSource == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
