using System;
using System.IO;
using UnityEngine;

namespace BadApple
{
    /// <summary>Carga un WAV PCM de 16 bits como AudioClip.</summary>
    public static class WavLoader
    {
        public static AudioClip Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int channels = 2, rate = 44100, bits = 16;
            int pos = 12;

            while (pos + 8 <= bytes.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(bytes, pos, 4);
                int size = BitConverter.ToInt32(bytes, pos + 4);
                int body = pos + 8;

                if (id == "fmt ")
                {
                    channels = BitConverter.ToInt16(bytes, body + 2);
                    rate = BitConverter.ToInt32(bytes, body + 4);
                    bits = BitConverter.ToInt16(bytes, body + 14);
                }
                else if (id == "data")
                {
                    if (bits != 16)
                        throw new InvalidDataException("badapple.wav must be 16-bit PCM");

                    size = Math.Min(size, bytes.Length - body);
                    int count = size / 2;
                    var samples = new float[count];
                    for (int i = 0; i < count; i++)
                        samples[i] = BitConverter.ToInt16(bytes, body + i * 2) / 32768f;

                    var clip = AudioClip.Create("BadApple", count / channels, channels, rate, false);
                    clip.SetData(samples, 0);
                    return clip;
                }
                pos = body + size + (size & 1);
            }
            throw new InvalidDataException("badapple.wav has no audio data");
        }
    }
}
