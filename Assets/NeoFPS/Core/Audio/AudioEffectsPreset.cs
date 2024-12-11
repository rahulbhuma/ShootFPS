using System;
using UnityEngine;
using NeoFPS.Constants;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/audioref-so-audioeffectspresets.html")]
    [CreateAssetMenu(fileName = "AudioEffectsPreset", menuName = "NeoFPS/Audio/Audio Effects Preset", order = NeoFpsMenuPriorities.audio_effectspreset)]
    public class AudioEffectsPreset : ScriptableObject
    {
        [SerializeField, Tooltip("The name of this effects preset.")]
        private string m_EffectName;

        public string effectName { get => m_EffectName; }

        [Header("High-pass")]

        [SerializeField, Range(10f, 22000f), Tooltip("The cut-off frequency for the high pass effect. Only frequencies above this will pass through to the final mix.")]
        private float m_HighpassCutOff = 10f;
        [SerializeField, Range(1f, 10f), Tooltip("The resonance determines how much the filter’s self-resonance is dampened. A higher value means more resonance.")]
        private float m_HighpassResonance = 1f;

        public float highpassCutOff { get => m_HighpassCutOff; }
        public float highpassResonance { get => m_HighpassResonance; }

        [Header("Low-pass")]

        [SerializeField, Range(10f, 22000f), Tooltip("The cut-off frequency for the low pass effect. Only frequencies below this will pass through to the final mix.")]
        private float m_LowpassCutOff = 22000f;
        [SerializeField, Range(1f, 10f), Tooltip("The resonance determines how much the filter’s self-resonance is dampened. A higher value means more resonance.")]
        private float m_LowpassResonance = 1f;

        public float lowpassCutOff { get => m_LowpassCutOff; }
        public float lowpassResonance { get => m_LowpassResonance; }

        [Header("Distortion")]

        [SerializeField, Range(0f, 1f), Tooltip("The amount of distortion to apply to the sound.")]
        private float m_Distortion = 0f;

        public float distortion { get => m_Distortion; }

        [Header("Reverb")]

        [SerializeField, Tooltip("Is reverb enabled with this preset.")]
        private bool m_ReverbEnabled = false;
        [SerializeField, Range(-10000f, 0f), Tooltip("Mix level of dry signal in output in mB. Ranges from –10000.0 to 0.0.")]
        private float m_ReverbDryLevel = ReverbDefaults.dryLevel;
        [SerializeField, Range(-10000f, 0f), Tooltip("Room effect level at low frequencies in mB. Ranges from –10000.0 to 0.0.")]
        private float m_ReverbRoom = ReverbDefaults.room;
        [SerializeField, Range(-10000f, 0f), Tooltip("Room effect high-frequency level in mB. Ranges from –10000.0 to 0.0.")]
        private float m_ReverbRoomHF = ReverbDefaults.roomHF;
        [SerializeField, Range(-10000f, 0f), Tooltip("Room effect low-frequency level in mB. Ranges from –10000.0 to 0.0.")]
        private float m_ReverbRoomLF = ReverbDefaults.roomLF;
        [SerializeField, Range(0.1f, 20f), Tooltip("Reverberation decay time at low-frequencies in seconds. Ranges from 0.1 to 20.0.")]
        private float m_ReverbDecayTime = ReverbDefaults.decayTime;
        [SerializeField, Range(0.1f, 2f), Tooltip("High-frequency to low-frequency decay time ratio. Ranges from 0.1 to 2.0.")]
        private float m_ReverbDecayHFRatio = ReverbDefaults.decayHFRatio;
        [SerializeField, Range(-10000f, 1000f), Tooltip("Early reflections level relative to room effect in mB. Ranges from –10000.0 to 1000.0.")]
        private float m_ReverbReflectionsLevel = ReverbDefaults.reflectionsLevel;
        [SerializeField, Range(-10000f, 2000f), Tooltip("Early reflections delay time relative to room effect in mB. Ranges from –10000.0 to 2000.0.")]
        private float m_ReverbReflectionsDelay = ReverbDefaults.reflectionsDelay;
        [SerializeField, Range(-10000f, 2000f), Tooltip("Late reverberation level relative to room effect in mB. Ranges from –10000.0 to 2000.0.")]
        private float m_ReverbLevel = ReverbDefaults.level;
        [SerializeField, Range(0f, 0.1f), Tooltip("Late reverberation delay time relative to first reflection in seconds. Ranges from 0.0 to 0.1.")]
        private float m_ReverbDelay = ReverbDefaults.delay;
        [SerializeField, Range(1000f, 20000f), Tooltip("Reference high frequency in Hz. Ranges from 20.0 to 20000.0. Default is 5000.0 Hz.")]
        private float m_ReverbHFReference = ReverbDefaults.hfReference;
        [SerializeField, Range(20f, 1000f), Tooltip("Reference low-frequency in Hz. Ranges from 20.0 to 1000.0.")]
        private float m_ReverbLFReference = ReverbDefaults.lfReference;
        [SerializeField, Range(0f, 100f), Tooltip("Reverberation diffusion (echo density) in percent. Ranges from 0.0 to 100.0.")]
        private float m_ReverbDiffusion = ReverbDefaults.diffusion;
        [SerializeField, Range(0f, 100f), Tooltip("Reverberation density (modal density) in percent. Ranges from 0.0 to 100.0.")]
        private float m_ReverbDensity = ReverbDefaults.density;

        public bool reverbEnabled { get => m_ReverbEnabled; }
        public float reverbDryLevel { get => m_ReverbDryLevel; }
        public float reverbRoom { get => m_ReverbRoom; }
        public float reverbRoomHF { get => m_ReverbRoomHF; }
        public float reverbRoomLF { get => m_ReverbRoomLF; }
        public float reverbDecayTime { get => m_ReverbDecayTime; }
        public float reverbDecayHFRatio { get => m_ReverbDecayHFRatio; }
        public float reverbReflectionsLevel { get => m_ReverbReflectionsLevel; }
        public float reverbReflectionsDelay { get => m_ReverbReflectionsDelay; }
        public float reverbLevel { get => m_ReverbLevel; }
        public float reverbDelay { get => m_ReverbDelay; }
        public float reverbHFReference { get => m_ReverbHFReference; }
        public float reverbLFReference { get => m_ReverbLFReference; }
        public float reverbDiffusion { get => m_ReverbDiffusion; }
        public float reverbDensity { get => m_ReverbDensity; }
    }
}