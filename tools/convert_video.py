"""
Convierte el video de pantalla verde de Bad Apple en los archivos que usa el mod:

  badapple.bam  -> máscara por fotograma (0 = transparente/verde, 1..255 = gris del fondo)
  badapple.wav  -> audio del video

Uso:
  python3 convert_video.py "ruta/al/video.mp4" carpeta_salida
"""
import struct
import subprocess
import sys
from pathlib import Path

import numpy as np

W, H = 480, 360          # resolución de la máscara (el MV es 4:3)
GRAY_STEP = 8            # cuantiza el gris para comprimir mejor
GREEN_THRESHOLD = 60     # cuánto más verde que rojo/azul debe ser un píxel


def encode_frame(frame: np.ndarray) -> bytes:
    """RLE: [valor][largo varint] repetido, filas de abajo hacia arriba (orden de Unity)."""
    r = frame[:, :, 0].astype(np.int16)
    g = frame[:, :, 1].astype(np.int16)
    b = frame[:, :, 2].astype(np.int16)
    green = (g - np.maximum(r, b)) > GREEN_THRESHOLD

    gray = ((r + g + b) // 3).astype(np.int16)
    gray = np.clip((gray + GRAY_STEP // 2) // GRAY_STEP * GRAY_STEP, 1, 255)
    values = np.where(green, 0, gray).astype(np.uint8)
    flat = np.flipud(values).ravel()

    change = np.flatnonzero(np.diff(flat)) + 1
    starts = np.concatenate(([0], change))
    lengths = np.diff(np.concatenate((starts, [flat.size])))

    out = bytearray()
    for start, length in zip(starts.tolist(), lengths.tolist()):
        out.append(int(flat[start]))
        while True:
            byte = length & 0x7F
            length >>= 7
            if length:
                out.append(byte | 0x80)
            else:
                out.append(byte)
                break
    return bytes(out)


def main() -> None:
    video = Path(sys.argv[1])
    out_dir = Path(sys.argv[2])
    out_dir.mkdir(parents=True, exist_ok=True)

    probe = subprocess.run(
        ["ffprobe", "-v", "error", "-select_streams", "v:0",
         "-show_entries", "stream=width,height,r_frame_rate", "-of", "csv=p=0", str(video)],
        capture_output=True, text=True, check=True).stdout.strip().split(",")
    vw, vh = int(probe[0]), int(probe[1])
    num, den = probe[2].split("/")
    fps = float(num) / float(den)

    # Recorta el área 4:3 del centro (quita las barras negras) y escala.
    crop_w = vh * 4 // 3
    vf = f"crop={crop_w}:{vh}:{(vw - crop_w) // 2}:0,scale={W}:{H}:flags=area"
    proc = subprocess.Popen(
        ["ffmpeg", "-v", "error", "-i", str(video), "-vf", vf,
         "-f", "rawvideo", "-pix_fmt", "rgb24", "-"],
        stdout=subprocess.PIPE)

    frames = []
    frame_size = W * H * 3
    while True:
        raw = proc.stdout.read(frame_size)
        if len(raw) < frame_size:
            break
        frames.append(encode_frame(np.frombuffer(raw, np.uint8).reshape(H, W, 3)))
        if len(frames) % 500 == 0:
            print(f"  {len(frames)} fotogramas...")
    proc.wait()

    with open(out_dir / "badapple.bam", "wb") as f:
        f.write(b"BAM1")
        f.write(struct.pack("<iiif", W, H, len(frames), fps))
        for data in frames:
            f.write(struct.pack("<i", len(data)))
            f.write(data)
    print(f"Máscara: {len(frames)} fotogramas a {fps} fps")

    subprocess.run(
        ["ffmpeg", "-v", "error", "-y", "-i", str(video), "-vn",
         "-c:a", "pcm_s16le", "-ar", "44100", "-ac", "2", "-bitexact",
         str(out_dir / "badapple.wav")],
        check=True)
    print("Audio listo")


if __name__ == "__main__":
    main()
