#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS

using System;
using UnityEngine;

namespace uMicrophoneWebGL
{
    public class EditorMicrophoneDataRetriever : MonoBehaviour
    {
        public DataEvent dataEvent { get; } = new DataEvent();
        public bool playMicrophoneSound { get; set; } = false;

        private AudioClip _clip;
        private bool _isRecording = false;
        private string _deviceName = "";
        private int lastSamplePosition = 0;
        private int currentPosition = 0;
        private float[] _data = null;
        private int recordLengthSec = 3;
        private float[] _buffer = new float[320];

        public void Begin(string deviceName, int freq)
        {
            if (_isRecording) return;
            _data = new float[recordLengthSec * freq];
            _deviceName = deviceName;
            _clip = Microphone.Start(_deviceName, true, recordLengthSec, freq);

            int retryCount = 0;
            while (Microphone.GetPosition(_deviceName) <= 0)
            {
                if (++retryCount >= 1000)
                {
                    Debug.LogError("Failed to get microphone.");
                    return;
                }
                System.Threading.Thread.Sleep(1);
            }
            lastSamplePosition = Microphone.GetPosition(_deviceName);
            _isRecording = true;
        }

        private float timer = 0f;
        private float interval = 0.02f;

        void Update()
        {
            if (!_isRecording)
            {
                return;
            }
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                GetData();
                timer = 0;
            }
        }

        void GetData()
        {
            currentPosition = Microphone.GetPosition(_deviceName);
            if (currentPosition < 0 || lastSamplePosition == currentPosition)
            {
                return;
            }

            _clip.GetData(_data, 0);

            int GetDataLength(int bufferLength, int head, int tail) =>
                head < tail ? tail - head : bufferLength - head + tail;

            while (GetDataLength(_data.Length, lastSamplePosition, currentPosition) > _buffer.Length)
            {
                var remain = _data.Length - lastSamplePosition;
                if (remain < _buffer.Length)
                {
                    Array.Copy(_data, lastSamplePosition, _buffer, 0, remain);
                    Array.Copy(_data, 0, _buffer, 0, _buffer.Length - remain);
                }
                else
                {
                    Array.Copy(_data, lastSamplePosition, _buffer, 0, _buffer.Length);
                }
                dataEvent.Invoke(_buffer);
                lastSamplePosition += _buffer.Length;
                if (lastSamplePosition > _data.Length)
                {
                    lastSamplePosition -= _data.Length;
                }
            }
        }


        public void End()
        {
            Microphone.End(_deviceName);
            _isRecording = false;
        }
    }
}

#endif