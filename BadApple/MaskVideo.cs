using System;
using System.IO;
using UnityEngine;

namespace BadApple
{
    /// <summary>
    /// Lee badapple.bam (generado por tools/convert_video.py).
    /// Cada fotograma es RLE de pares [valor][largo varint]; valor 0 = silueta (transparente),
    /// 1..255 = gris del fondo del MV.
    /// </summary>
    public class MaskVideo
    {
        public int Width, Height, FrameCount;
        public float Fps;

        private byte[] data;
        private int[] offsets;

        public static MaskVideo Load(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (bytes[0] != 'B' || bytes[1] != 'A' || bytes[2] != 'M' || bytes[3] != '1')
                throw new InvalidDataException("badapple.bam is not a valid mask file");

            var v = new MaskVideo
            {
                data = bytes,
                Width = BitConverter.ToInt32(bytes, 4),
                Height = BitConverter.ToInt32(bytes, 8),
                FrameCount = BitConverter.ToInt32(bytes, 12),
                Fps = BitConverter.ToSingle(bytes, 16)
            };

            v.offsets = new int[v.FrameCount];
            int pos = 20;
            for (int i = 0; i < v.FrameCount; i++)
            {
                int len = BitConverter.ToInt32(bytes, pos);
                v.offsets[i] = pos + 4;
                pos += 4 + len;
            }
            return v;
        }

        public float Duration => FrameCount / Fps;

        /// <summary>Escribe el fotograma en el buffer de píxeles según el modo de fondo.</summary>
        public void Decode(int frame, Color32[] pixels, int background)
        {
            int pos = offsets[frame];
            int end = frame + 1 < FrameCount ? offsets[frame + 1] - 4 : data.Length;
            int p = 0;
            var clear = new Color32(0, 0, 0, 0);

            while (pos < end && p < pixels.Length)
            {
                byte value = data[pos++];
                int run = 0, shift = 0;
                byte b;
                do
                {
                    b = data[pos++];
                    run |= (b & 0x7F) << shift;
                    shift += 7;
                } while ((b & 0x80) != 0);

                Color32 c;
                if (value == 0)
                    c = clear;
                else if (background == 1)
                    c = new Color32(0, 0, 0, 255);
                else if (background == 2)
                    c = new Color32(255, 255, 255, 255);
                else
                    c = new Color32(value, value, value, 255);

                int stop = Math.Min(p + run, pixels.Length);
                for (; p < stop; p++)
                    pixels[p] = c;
            }
        }
    }
}
