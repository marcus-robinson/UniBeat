using UnityEngine;
using System;

namespace UniBeat.RhythmEngine
{
    public class BeatCalibration
    {
        public TimeSpan AudioLatency; // How long it takes for audio to be heard after we call Sound.play()
        public TimeSpan VisualLatency; // How long it takes for the screen to display visuals after a GPU render call.
        public TimeSpan InputLatency; // How long it takes for Unity to register input after the player performs the input.
        private TimeSpan InputLatencyPlayer2;

        private readonly TimeSpan _increment = TimeSpan.FromMilliseconds(10);

        public BeatCalibration()
        {
            VisualLatency = TimeSpan.FromMilliseconds(7);
            InputLatency = TimeSpan.FromMilliseconds(4);
            AudioLatency = GetFmodLatency();
            LoadCalibration();
        }

        protected TimeSpan GetFmodLatency()
        {
            uint bufferlength;
            int numbuffers;
            int frequency;

            FMODUnity.RuntimeManager.CoreSystem.getDSPBufferSize(out bufferlength, out numbuffers);
            FMODUnity.RuntimeManager.CoreSystem.getSoftwareFormat(out frequency, out _, out _);

            var blocksize = TimeSpan.FromMilliseconds((float)bufferlength * 1000.0f / (float)frequency);
            var ms = blocksize.TotalMilliseconds;

            Debug.Log(@$"
            FMOD LATENCY SETTINGS:
            Mixer blocksize        = {ms.ToString("F2")} ms
            Mixer Total buffersize = {(ms * numbuffers).ToString("F2")}
            Mixer Average Latency  = {(ms * ((float)numbuffers - 1.5f)).ToString("F2")} ms");

            return blocksize;
        }

        public void LoadCalibration()
        {
            // Persistence.GetCalibrationValues(out calibration_v, out calibration_i, out calibration_i_P2_internal, out latency);

            // if ((double)VisualLatency == (double)NotSet)
            SetToPresets();

            //Persistence.SetLatencyValue(latency);
        }

        private void SetToPresets()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    VisualLatency = TimeSpan.FromMilliseconds(20);
                    break;
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    VisualLatency = TimeSpan.FromMilliseconds(7);
                    break;
                case RuntimePlatform.IPhonePlayer:
                    VisualLatency = TimeSpan.FromMilliseconds(50);
                    InputLatency = TimeSpan.FromMilliseconds(70);
                    break;
                case RuntimePlatform.Android:
                    VisualLatency = TimeSpan.FromMilliseconds(230);
                    InputLatency = TimeSpan.FromMilliseconds(230);
                    break;
                case RuntimePlatform.WebGLPlayer:
                    if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
                    {
                        VisualLatency = TimeSpan.FromMilliseconds(20);
                        break;
                    }
                    VisualLatency = TimeSpan.FromMilliseconds(90);
                    break;
                case RuntimePlatform.Switch:
                    Debug.Log((object)"calibration set to Nintendo Switch (non-docked) defaults.");
                    VisualLatency = TimeSpan.FromMilliseconds(40);
                    InputLatency = TimeSpan.FromMilliseconds(60);
                    break;
            }
            // if (!AppUtil.isMobile && Application.platform != RuntimePlatform.Switch)
            //     InputLatency = VisualLatency + (style == InputStyle.TriggerHitOnButtonUp ? 0.03f : 0.0f);

            // if ((double)InputLatency == (double)NotSet)
            // {
            //     Debug.LogError($"Platform not detected!");
            //     InputLatency = VisualLatency + (style == InputStyle.TriggerHitOnButtonUp ? 0.03f : 0.0f);
            // }

            InputLatencyPlayer2 = InputLatency;

            Save();
        }

        public void IncreaseCalibration()
        {
            InputLatency += _increment;
            Save();
        }

        public void DecreaseCalibration()
        {
            InputLatency -= _increment;
            Save();
        }

        public void IncreaseCalibrationP2()
        {
            InputLatencyPlayer2 += _increment;
            Save();
        }

        public void DecreaseCalibrationP2()
        {
            InputLatencyPlayer2 -= _increment;
            Save();
        }

        public void IncreaseLatency()
        {
            AudioLatency += _increment;
            Save();
        }

        public void DecreaseLatency()
        {
            AudioLatency -= _increment;
            Save();
        }

        public void IncreaseCalibrationV()
        {
            VisualLatency += _increment;
            Save();
        }

        public void DecreaseCalibrationV()
        {
            VisualLatency -= _increment;
            Save();
        }

        public void Save()
        {
            Debug.Log($"CALIBRATION SAVED: InputLatency {InputLatency} | VisualLatency {VisualLatency} | AudioLatency {AudioLatency}");
            // Persistence.SetCalibrationValues(calibration_v, calibration_i, calibration_i_P2_internal, latency);
        }
    }
}

