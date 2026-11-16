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
        readonly GameObject owner;
        readonly Dictionary<string, AudioSource> tracks = new Dictionary<string, AudioSource>();
        AudioSource current;
        internal float Volume { get; private set; } = Mathf.Clamp01(
            PlayerPrefs.GetFloat(VolumePreference, PlayerPrefs.GetInt(LegacyEnabledPreference, 1) == 0 ? 0f : 1f));

        internal ParkMusic(GameObject owner)
        {
            this.owner = owner;
        }

        internal void PlayVenue(string venue, bool fadeIn = true)
        {
            if (Volume == 0f) return;

            string track = venue switch
            {
                "공원" => "Garden",
                "숲" => "Forest",
                "놀이터" => "Playground",
                "사막" => "Desert",
                "시계탑 광장" => "Christmas",
                "해변가" => "Beach",
                _ => null
            };
            current = null;
            if (track == null) return;

            if (!tracks.TryGetValue(track, out current))
            {
                var clip = Resources.Load<AudioClip>($"Music/{track}");
                if (!clip) return;

                current = owner.AddComponent<AudioSource>();
                current.playOnAwake = false;
                current.spatialBlend = 0f;
                current.loop = true;
                current.volume = 0f;
                current.clip = clip;
                tracks.Add(track, current);
            }

            if (!current.isPlaying)
            {
                if (!fadeIn) current.volume = MaxSourceVolume * Volume;
                current.Play();
            }
        }

        internal void SetVolume(float volume)
        {
            float previous = Volume;
            Volume = Mathf.Clamp01(volume);
            if (Volume == previous) return;

            PlayerPrefs.SetFloat(VolumePreference, Volume);
            if (Volume == 0f) Stop();
            else if (previous > 0f)
                foreach (var source in tracks.Values)
                    source.volume *= Volume / previous;
        }

        internal void SaveVolume() => PlayerPrefs.Save();

        internal void FadeOut()
        {
            current = null;
        }

        internal void Advance(float seconds)
        {
            float gain = MaxSourceVolume * Volume;
            foreach (var source in tracks.Values)
            {
                float target = source == current ? gain : 0f;
                source.volume = Mathf.MoveTowards(source.volume, target, gain * seconds / FadeSeconds);
                if (target == 0f && source.volume == 0f && source.isPlaying) source.Stop();
            }
        }

        internal void Stop()
        {
            current = null;
            foreach (var source in tracks.Values)
            {
                source.Stop();
                source.volume = 0f;
            }
        }
    }
}
