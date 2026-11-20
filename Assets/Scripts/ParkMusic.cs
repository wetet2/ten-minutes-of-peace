using System.Collections.Generic;
using UnityEngine;

namespace PeacePark
{
    internal sealed class ParkMusic
    {
        const string VolumePreference = "PeacePark.MusicVolume";
        const string LegacyEnabledPreference = "PeacePark.MusicEnabled";
        const float MaxSourceVolume = .42f;
        const float FadeSeconds = 1.4f;
        static readonly Dictionary<string, string[]> Playlists = new Dictionary<string, string[]>
        {
            { "공원", new[] { "Garden", "DaisyPath" } },
            { "숲", new[] { "Forest", "FernWhistle" } },
            { "놀이터", new[] { "Hopscotch", "PocketKites", "Playground" } },
            { "사막", new[] { "Desert", "SandstoneSteps" } },
            { "시계탑 광장", new[] { "Christmas", "SnowglobeBells" } },
            { "놀이공원", new[] { "CarouselWaltz", "RibbonParade", "LanternWheel" } }
        };
        readonly Dictionary<string, int> nextTrack = new Dictionary<string, int>();
        readonly AudioSource[] sources = new AudioSource[2];
        int current;
        string activeVenue;
        double expectedEnd;
        internal float Volume { get; private set; } = Mathf.Clamp01(
            PlayerPrefs.GetFloat(VolumePreference, PlayerPrefs.GetInt(LegacyEnabledPreference, 1) == 0 ? 0f : 1f));

        internal ParkMusic(GameObject owner)
        {
            for (int i = 0; i < sources.Length; i++)
            {
                var source = owner.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = false;
                source.volume = 0f;
                sources[i] = source;
            }
        }

        internal void PlayVenue(string venue, bool fadeIn = true)
        {
            if (venue == activeVenue) return;
            if (venue == null || !Playlists.ContainsKey(venue) || Volume == 0f)
            {
                FadeOut();
                return;
            }
            if (!sources[current].isActiveAndEnabled) return;
            activeVenue = venue;
            PlayNext(fadeIn);
        }

        void PlayNext(bool fadeIn)
        {
            var playlist = Playlists[activeVenue];
            nextTrack.TryGetValue(activeVenue, out int index);
            for (int attempt = 0; attempt < playlist.Length; attempt++)
            {
                var clip = Resources.Load<AudioClip>("Music/" + playlist[index]);
                index = (index + 1) % playlist.Length;
                if (!clip) continue;
                nextTrack[activeVenue] = index;
                current = 1 - current;
                var source = sources[current];
                source.Stop();
                source.clip = clip;
                source.volume = fadeIn ? 0f : MaxSourceVolume * Volume;
                source.Play();
                expectedEnd = AudioSettings.dspTime + clip.length;
                return;
            }
            FadeOut();
        }

        internal void SetVolume(float volume)
        {
            float previous = Volume;
            Volume = Mathf.Clamp01(volume);
            if (Volume == previous) return;

            PlayerPrefs.SetFloat(VolumePreference, Volume);
            if (Volume == 0f)
            {
                if (activeVenue == null) { Stop(); return; }
                sources[current].Pause();
                sources[current].volume = 0f;
                Clear(sources[1 - current]);
            }
            else if (previous == 0f && activeVenue != null)
            {
                var source = sources[current];
                expectedEnd = AudioSettings.dspTime + source.clip.length - (double)source.timeSamples / source.clip.frequency;
                source.volume = MaxSourceVolume * Volume;
                source.UnPause();
            }
            else if (previous > 0f)
                foreach (var source in sources)
                    source.volume *= Volume / previous;
        }

        internal void SaveVolume() => PlayerPrefs.Save();

        internal void FadeOut()
        {
            activeVenue = null;
            if (Volume == 0f) Stop();
        }

        internal void Advance(float seconds)
        {
            if (Volume == 0f || AudioListener.pause) return;
            if (activeVenue != null)
            {
                var playing = sources[current];
                var clip = playing.clip;
                // Sample position avoids drift on a long visit. The DSP clock also handles
                // a short frame stall that carries us past the clip's final sample.
                if (playing.isPlaying
                    ? clip.samples - playing.timeSamples <= clip.frequency * FadeSeconds
                    : AudioSettings.dspTime >= expectedEnd)
                    PlayNext(true);
            }
            float gain = MaxSourceVolume * Volume;
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (!source.clip) continue;
                float target = activeVenue != null && i == current ? gain : 0f;
                source.volume = Mathf.MoveTowards(source.volume, target, gain * seconds / FadeSeconds);
                if (target == 0f && source.volume == 0f) Clear(source);
            }
        }

        internal void Stop()
        {
            activeVenue = null;
            foreach (var source in sources) Clear(source);
        }

        static void Clear(AudioSource source)
        {
            source.Stop();
            source.volume = 0f;
            source.clip = null;
        }
    }
}
