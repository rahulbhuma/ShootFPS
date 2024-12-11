using UnityEngine;

namespace NeoFPS
{
    [RequireComponent(typeof(AudioHighPassFilter))]
    [RequireComponent(typeof(AudioLowPassFilter))]
    [RequireComponent(typeof(AudioDistortionFilter))]
    [RequireComponent(typeof(AudioReverbFilter))]
    public class FpsCharacterAudioEffects : MonoBehaviour
    {
        [SerializeField, Tooltip("The different audio effect presets available to this character.")]
        private AudioEffectsPreset[] m_Presets = { };

        struct EffectBlend
        {
            public float strength;
            public float target;
            public float inverseDuration;
        }

        private EffectBlend[] m_Blends = null;
        private AudioHighPassFilter m_HighPass = null;
        private AudioLowPassFilter m_LowPass = null;
        private AudioDistortionFilter m_Distortion = null;
        private AudioReverbFilter m_Reverb = null;
        private float m_CurrentHpCutoff = 0f;
        private float m_CurrentHpResonance = 1f;
        private float m_CurrentLpCutoff = 22000f;
        private float m_CurrentLpResonance = 1f;
        private float m_CurrentDistortion = 0f;
        private float m_CurrentReverbDryLevel = ReverbDefaults.dryLevel;
        private float m_CurrentReverbRoom = ReverbDefaults.room;
        private float m_CurrentReverbRoomHF = ReverbDefaults.roomHF;
        private float m_CurrentReverbRoomLF = ReverbDefaults.roomLF;
        private float m_CurrentReverbDecayTime = ReverbDefaults.decayTime;
        private float m_CurrentReverbDecayHFRatio = ReverbDefaults.decayHFRatio;
        private float m_CurrentReverbReflectionsLevel = ReverbDefaults.reflectionsLevel;
        private float m_CurrentReverbReflectionsDelay = ReverbDefaults.reflectionsDelay;
        private float m_CurrentReverbLevel = ReverbDefaults.level;
        private float m_CurrentReverbDelay = ReverbDefaults.delay;
        private float m_CurrentReverbHFReference = ReverbDefaults.hfReference;
        private float m_CurrentReverbLFReference = ReverbDefaults.lfReference;
        private float m_CurrentReverbDiffusion = ReverbDefaults.diffusion;
        private float m_CurrentReverbDensity = ReverbDefaults.density;
        private bool m_ReverbChanged = false;
        private bool m_ReverbEnabled = false;

        private void Awake()
        {
            m_Blends = new EffectBlend[m_Presets.Length];
            m_HighPass = GetComponent<AudioHighPassFilter>();
            m_LowPass = GetComponent<AudioLowPassFilter>();
            m_Distortion = GetComponent<AudioDistortionFilter>();
            m_Reverb = GetComponent<AudioReverbFilter>();
        }

        private void Start()
        {
            m_HighPass.enabled = false;
            m_HighPass.cutoffFrequency = 10f;
            m_HighPass.highpassResonanceQ = 1.0f;

            m_LowPass.enabled = false;
            m_LowPass.cutoffFrequency = 22000f;
            m_LowPass.lowpassResonanceQ = 1.0f;

            m_Distortion.enabled = false;
            m_Distortion.distortionLevel = 0f;

            m_Reverb.enabled = false;
            m_Reverb.reverbPreset = AudioReverbPreset.Off;
        }

        public void SetEffectStrength(string effectName, float strength, float blendDuration)
        {
            // Find the effect index
            int index = -1;
            for(int i = 0; i < m_Presets.Length; i++)
            {
                if (m_Presets[i].effectName == effectName)
                {
                    index = i;
                    break;
                }
            }

            // Set the target etc
            if (index != -1)
            {
                var blend = m_Blends[index];
                blend.target = strength;
                float diff = Mathf.Abs(blend.target - blend.strength);
                if (blendDuration > 0.001f)
                    blend.inverseDuration = Mathf.Clamp(diff / blendDuration, 0.001f, 10000f);
                else
                    blend.inverseDuration = 10000f;
                m_Blends[index] = blend;
            }
            else
                Debug.LogErrorFormat("Attempting to set audio effect strength for effect name that isn't registered: {0}", effectName);
        }

        private void Update()
        {
            float hpCutoff = 10f;
            float hpResonance = 1f;
            float lpCutoff = 22000f;
            float lpResonance = 1f;
            float distortion = 0f;
            float reverbDryLevel = ReverbDefaults.dryLevel;
            float reverbRoom = ReverbDefaults.room;
            float reverbRoomHF = ReverbDefaults.roomHF;
            float reverbRoomLF = ReverbDefaults.roomLF;
            float reverbDecayTime = ReverbDefaults.decayTime;
            float reverbDecayHFRatio = ReverbDefaults.decayHFRatio;
            float reverbReflectionsLevel = ReverbDefaults.reflectionsLevel;
            float reverbReflectionsDelay = ReverbDefaults.reflectionsDelay;
            float reverbLevel = ReverbDefaults.level;
            float reverbDelay = ReverbDefaults.delay;
            float reverbHFReference = ReverbDefaults.hfReference;
            float reverbLFReference = ReverbDefaults.lfReference;
            float reverbDiffusion = ReverbDefaults.diffusion;
            float reverbDensity = ReverbDefaults.density;
            int reverbDecayTimeCount = 0;
            int reverbDecayHFRatioCount = 0;
            int reverbReflectionsLevelCount = 0;
            int reverbReflectionsDelayCount = 0;
            int reverbLevelCount = 0;
            int reverbDelayCount = 0;
            int reverbHFReferenceCount = 0;
            int reverbLFReferenceCount = 0;
            bool wasReverbEnabled = m_ReverbEnabled;
            m_ReverbEnabled = false;
            m_ReverbChanged = false;

            // Go through each effect and interpolate, accumulate, etc
            for (int i = 0; i < m_Blends.Length; ++i)
            {
                // Update the blend
                var blend = m_Blends[i];
                if (!Mathf.Approximately(blend.strength, blend.target))
                {
                    if (blend.strength < blend.target)
                    {
                        blend.strength += Time.deltaTime * blend.inverseDuration;
                        if (blend.strength > blend.target)
                            blend.strength = blend.target;
                    }
                    else
                    {
                        blend.strength -= Time.deltaTime * blend.inverseDuration;
                        if (blend.strength < blend.target)
                            blend.strength = blend.target;
                    }
                    m_Blends[i] = blend;
                }

                // Update target values
                if (blend.strength > 0f)
                {
                    var preset = m_Presets[i];
                    var strength = blend.strength;

                    // High-pass
                    hpCutoff = Mathf.Max(hpCutoff, Mathf.Lerp(10f, preset.highpassCutOff, strength));
                    hpResonance = Mathf.Max(hpResonance, Mathf.Lerp(1f, preset.highpassResonance, strength));
                    // Low-pass
                    lpCutoff = Mathf.Min(lpCutoff, Mathf.Lerp(22000f, preset.lowpassCutOff, strength));
                    lpResonance = Mathf.Max(lpResonance, Mathf.Lerp(1f, preset.lowpassResonance, strength));
                    // Distortion
                    distortion = Mathf.Max(distortion, Mathf.Lerp(0f, preset.distortion, strength));
                    // Reverb
                    if (preset.reverbEnabled)
                    {
                        m_ReverbEnabled = true;
                        reverbDryLevel = ModifyReverbValueMin(m_CurrentReverbDryLevel, reverbDryLevel, ReverbDefaults.dryLevel, preset.reverbDryLevel, strength);
                        reverbRoom = ModifyReverbValueMax(m_CurrentReverbRoom, reverbRoom, ReverbDefaults.room, preset.reverbRoom, strength);
                        reverbRoomHF = ModifyReverbValueMax(m_CurrentReverbRoomHF, reverbRoomHF, ReverbDefaults.roomHF, preset.reverbRoomHF, strength);
                        reverbRoomLF = ModifyReverbValueMin(m_CurrentReverbRoomLF, reverbRoomLF, ReverbDefaults.roomLF, preset.reverbRoomLF, strength);
                        reverbDecayTime = ModifyReverbValueAvg(m_CurrentReverbDecayTime, reverbDecayTime, ReverbDefaults.decayTime, preset.reverbDecayTime, strength, reverbDecayTimeCount++);
                        reverbDecayHFRatio = ModifyReverbValueAvg(m_CurrentReverbDecayHFRatio, reverbDecayHFRatio, ReverbDefaults.decayHFRatio, preset.reverbDecayHFRatio, strength, reverbDecayHFRatioCount++);
                        reverbReflectionsLevel = ModifyReverbValueAvg(m_CurrentReverbReflectionsLevel, reverbReflectionsLevel, ReverbDefaults.reflectionsLevel, preset.reverbReflectionsLevel, strength, reverbReflectionsLevelCount++);
                        reverbReflectionsDelay = ModifyReverbValueAvg(m_CurrentReverbReflectionsDelay, reverbReflectionsDelay, ReverbDefaults.reflectionsDelay, preset.reverbReflectionsDelay, strength, reverbReflectionsDelayCount++);
                        reverbLevel = ModifyReverbValueAvg(m_CurrentReverbLevel, reverbLevel, ReverbDefaults.level, preset.reverbLevel, strength, reverbLevelCount++);
                        reverbDelay = ModifyReverbValueAvg(m_CurrentReverbDelay, reverbDelay, ReverbDefaults.delay, preset.reverbDelay, strength, reverbDelayCount++);
                        reverbHFReference = ModifyReverbValueAvg(m_CurrentReverbHFReference, reverbHFReference, ReverbDefaults.hfReference, preset.reverbHFReference, strength, reverbHFReferenceCount++);
                        reverbLFReference = ModifyReverbValueAvg(m_CurrentReverbLFReference, reverbLFReference, ReverbDefaults.lfReference, preset.reverbLFReference, strength, reverbLFReferenceCount++);
                        reverbDiffusion = ModifyReverbValueMax(m_CurrentReverbDiffusion, reverbDiffusion, ReverbDefaults.diffusion, preset.reverbDiffusion, strength);
                        reverbDensity = ModifyReverbValueMax(m_CurrentReverbDensity, reverbDensity, ReverbDefaults.density, preset.reverbDensity, strength);
                    }
                }
            }

            // Apply highpass
            if (!Mathf.Approximately(hpCutoff, m_CurrentHpCutoff) || !Mathf.Approximately(hpResonance, m_CurrentHpResonance))
            {
                // Enable / disable the high pass filter
                if (m_CurrentHpCutoff == 0f)
                    m_HighPass.enabled = true;
                else
                {
                    if (hpCutoff == 0f)
                        m_HighPass.enabled = false;
                }

                // Set values
                m_CurrentHpCutoff = hpCutoff;
                m_CurrentHpResonance = hpResonance;
                m_HighPass.cutoffFrequency = hpCutoff;
                m_HighPass.highpassResonanceQ = hpResonance;
            }

            // Apply lowpass
            if (!Mathf.Approximately(lpCutoff, m_CurrentLpCutoff) || !Mathf.Approximately(lpResonance, m_CurrentLpResonance))
            {
                // Enable / disable the high pass filter
                if (m_CurrentLpCutoff > 21999f)
                    m_LowPass.enabled = true;
                else
                {
                    if (lpCutoff > 21999f)
                        m_LowPass.enabled = false;
                }

                // Set values
                m_CurrentLpCutoff = lpCutoff;
                m_CurrentLpResonance = lpResonance;
                m_LowPass.cutoffFrequency = lpCutoff;
                m_LowPass.lowpassResonanceQ = lpResonance;
            }

            // Apply distortion
            if (!Mathf.Approximately(distortion, m_CurrentDistortion))
            {
                // Enable / disable the high pass filter
                if (m_CurrentDistortion == 0f)
                    m_Distortion.enabled = true;
                else
                {
                    if (distortion == 0f)
                        m_Distortion.enabled = false;
                }

                // Set values
                m_CurrentDistortion = distortion;
                m_Distortion.distortionLevel = distortion;
            }

            // Apply reverb
            if (wasReverbEnabled && !m_ReverbEnabled)
            {
                m_Reverb.enabled = false;
                m_Reverb.reverbPreset = AudioReverbPreset.Off;
            }
            else
            {
                if (!wasReverbEnabled && m_ReverbEnabled)
                {
                    m_Reverb.enabled = true;
                    m_Reverb.reverbPreset = AudioReverbPreset.User;
                }
            }

            if (m_ReverbChanged)
            {
                m_CurrentReverbDryLevel = reverbDryLevel;
                m_CurrentReverbRoom = reverbRoom;
                m_CurrentReverbRoomHF = reverbRoomHF;
                m_CurrentReverbRoomLF = reverbRoomLF;
                m_CurrentReverbDecayTime = reverbDecayTime;
                m_CurrentReverbDecayHFRatio = reverbDecayHFRatio;
                m_CurrentReverbReflectionsLevel = reverbReflectionsLevel;
                m_CurrentReverbReflectionsDelay = reverbReflectionsDelay;
                m_CurrentReverbLevel = reverbLevel;
                m_CurrentReverbDelay = reverbDelay;
                m_CurrentReverbHFReference = reverbHFReference;
                m_CurrentReverbLFReference = reverbLFReference;
                m_CurrentReverbDiffusion = reverbDiffusion;
                m_CurrentReverbDensity = reverbDensity;

                m_Reverb.dryLevel = m_CurrentReverbDryLevel;
                m_Reverb.room = m_CurrentReverbRoom;
                m_Reverb.roomHF = m_CurrentReverbRoomHF;
                m_Reverb.roomLF = m_CurrentReverbRoomLF;
                m_Reverb.decayTime = m_CurrentReverbDecayTime;
                m_Reverb.decayHFRatio = m_CurrentReverbDecayHFRatio;
                m_Reverb.reflectionsLevel = m_CurrentReverbReflectionsLevel;
                m_Reverb.reflectionsDelay = m_CurrentReverbReflectionsDelay;
                m_Reverb.reverbLevel = m_CurrentReverbLevel;
                m_Reverb.reverbDelay = m_CurrentReverbDelay;
                m_Reverb.hfReference = m_CurrentReverbHFReference;
                m_Reverb.lfReference = m_CurrentReverbLFReference;
                m_Reverb.diffusion = m_CurrentReverbDiffusion;
                m_Reverb.density = m_CurrentReverbDensity;
            }
        }

        float ModifyReverbValueMin(float lastValue, float currentValue, float defaultValue, float presetValue, float strength)
        {
            currentValue = Mathf.Min(currentValue, Mathf.Lerp(defaultValue, presetValue, strength));
            m_ReverbChanged |= !Mathf.Approximately(currentValue, lastValue);
            m_ReverbEnabled |= !Mathf.Approximately(currentValue, defaultValue);
            return currentValue;
        }

        float ModifyReverbValueMax(float lastValue, float currentValue, float defaultValue, float presetValue, float strength)
        {
            currentValue = Mathf.Max(currentValue, Mathf.Lerp(defaultValue, presetValue, strength));
            m_ReverbChanged |= !Mathf.Approximately(currentValue, lastValue);
            m_ReverbEnabled |= !Mathf.Approximately(currentValue, defaultValue);
            return currentValue;
        }

        float ModifyReverbValueAvg(float lastValue, float currentValue, float defaultValue, float presetValue, float strength, int count)
        {
            float total = count + 1;
            float left = (float)count / total;
            float right = 1f / total;

            float targetValue = Mathf.Lerp(defaultValue, presetValue, strength);
            currentValue = left * currentValue + right * targetValue;

            m_ReverbChanged |= !Mathf.Approximately(currentValue, lastValue);
            m_ReverbEnabled |= !Mathf.Approximately(currentValue, defaultValue);

            return currentValue;
        }
    }
}