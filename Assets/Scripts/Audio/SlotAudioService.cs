using UnityEngine;
using SlotGame.Core;

namespace SlotGame.Audio
{
    /// <summary>
    /// Manages slot machine audio effects, volume controls, and sound events.
    /// Uses dedicated audio sources for oneshots and looping reel spin sound.
    /// </summary>
    public class SlotAudioService : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource loopSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip leverPullClip;
        [SerializeField] private AudioClip spinLoopClip;
        [SerializeField] private AudioClip reelStopClip;
        [SerializeField] private AudioClip winBellClip;
        [SerializeField] private AudioClip jackpotFanfareClip;

        public bool IsMuted { get; private set; }

        private void Awake()
        {
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            if (loopSource == null)
            {
                loopSource = gameObject.AddComponent<AudioSource>();
                loopSource.playOnAwake = false;
                loopSource.loop = true;
            }
        }

        public void PlayButtonClick()
        {
            PlayOneShot(buttonClickClip, 0.8f);
        }

        public void PlayLeverPull()
        {
            PlayOneShot(leverPullClip, 1.0f);
        }

        public void PlaySpinLoop()
        {
            if (IsMuted || spinLoopClip == null) return;

            loopSource.clip = spinLoopClip;
            loopSource.volume = 0.65f;
            loopSource.Play();
        }

        public void StopSpinLoop()
        {
            if (loopSource != null && loopSource.isPlaying)
            {
                loopSource.Stop();
            }
        }

        public void PlayReelStop(int reelIndex)
        {
            if (IsMuted || reelStopClip == null) return;

            // Pitch climbs slightly with each subsequent reel stop: 1.0 -> 1.06 -> 1.12
            float pitch = 1.0f + (reelIndex * 0.06f);
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(reelStopClip, 0.9f);
            sfxSource.pitch = 1.0f;
        }

        public void PlayWin(WinTier tier)
        {
            if (IsMuted) return;

            switch (tier)
            {
                case WinTier.Jackpot:
                case WinTier.BigWin:
                    PlayOneShot(jackpotFanfareClip, 1.0f);
                    break;

                case WinTier.SmallWin:
                case WinTier.MediumWin:
                    PlayOneShot(winBellClip, 0.9f);
                    break;
            }
        }

        public void SetMute(bool muted)
        {
            IsMuted = muted;
            if (sfxSource != null) sfxSource.mute = muted;
            if (loopSource != null) loopSource.mute = muted;
        }

        public void ToggleMute()
        {
            SetMute(!IsMuted);
        }

        private void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (IsMuted || clip == null || sfxSource == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
