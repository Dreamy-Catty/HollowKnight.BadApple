using UnityEngine;
using UnityEngine.UI;

namespace BadApple
{
    /// <summary>
    /// Capa encima de todo el juego: el fondo del MV tapa la pantalla y
    /// las siluetas quedan transparentes, así que solo ahí se ve el juego.
    /// </summary>
    public class Overlay : MonoBehaviour
    {
        private MaskVideo video;
        private AudioSource audioSource;
        private Texture2D texture;
        private Color32[] pixels;
        private GameObject canvasObject;
        private RawImage image;
        private AspectRatioFitter fitter;
        private RectTransform leftBar, rightBar;

        private float clock;
        private int lastFrame = -1;

        public void Setup(MaskVideo video, AudioClip music)
        {
            this.video = video;

            texture = new Texture2D(video.Width, video.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            pixels = new Color32[video.Width * video.Height];

            canvasObject = new GameObject("BadAppleCanvas");
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            var imageObject = new GameObject("Mask");
            imageObject.transform.SetParent(canvasObject.transform, false);
            image = imageObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            Stretch(imageObject.GetComponent<RectTransform>());
            fitter = imageObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 4f / 3f;

            // Barras negras a los lados (modo 4:3)
            leftBar = MakeBar("LeftBar");
            rightBar = MakeBar("RightBar");

            if (music != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = music;
                audioSource.loop = true;
                audioSource.playOnAwake = false;
                audioSource.ignoreListenerPause = true;
                audioSource.spatialBlend = 0f;
            }

            ApplySettings();
        }

        private RectTransform MakeBar(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(canvasObject.transform, false);
            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public void ApplySettings()
        {
            var s = BadAppleMod.Settings;
            fitter.enabled = !s.Stretch;
            if (s.Stretch)
                Stretch(image.rectTransform);
            leftBar.gameObject.SetActive(!s.Stretch);
            rightBar.gameObject.SetActive(!s.Stretch);
            lastFrame = -1; // fuerza redibujar con el nuevo fondo

            if (audioSource != null)
            {
                if (s.Music && !audioSource.isPlaying)
                {
                    audioSource.time = Mathf.Clamp(clock, 0f, audioSource.clip.length - 0.01f);
                    audioSource.Play();
                }
                else if (!s.Music && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
            }
        }

        private void Update()
        {
            // Reloj: si suena la música, el video la sigue para ir sincronizado.
            if (audioSource != null && audioSource.isPlaying)
                clock = audioSource.time;
            else
                clock = (clock + Time.unscaledDeltaTime) % video.Duration;

            if (audioSource != null)
                audioSource.volume = BadAppleMod.Settings.Volume / 10f * GameVolume();

            // En el menú principal o en pausa se oculta para poder usar los menús.
            canvasObject.SetActive(!InMenu());

            int frame = Mathf.Clamp((int)(clock * video.Fps), 0, video.FrameCount - 1);
            if (frame != lastFrame)
            {
                video.Decode(frame, pixels, BadAppleMod.Settings.Background);
                texture.SetPixels32(pixels);
                texture.Apply(false);
                lastFrame = frame;
            }

            if (!BadAppleMod.Settings.Stretch)
                LayoutBars();
        }

        private void LayoutBars()
        {
            // Ancho de cada barra como fracción de la pantalla
            float screenAspect = (float)Screen.width / Screen.height;
            float side = Mathf.Max(0f, (1f - (4f / 3f) / screenAspect) / 2f);

            leftBar.anchorMin = new Vector2(0f, 0f);
            leftBar.anchorMax = new Vector2(side, 1f);
            leftBar.offsetMin = leftBar.offsetMax = Vector2.zero;

            rightBar.anchorMin = new Vector2(1f - side, 0f);
            rightBar.anchorMax = new Vector2(1f, 1f);
            rightBar.offsetMin = rightBar.offsetMax = Vector2.zero;
        }

        private static bool InMenu()
        {
            var gm = GameManager.instance;
            if (gm == null)
                return true;
            return gm.gameState == GlobalEnums.GameState.MAIN_MENU
                || gm.gameState == GlobalEnums.GameState.PAUSED
                || gm.IsMenuScene();
        }

        private static float GameVolume()
        {
            var gm = GameManager.instance;
            if (gm == null || gm.gameSettings == null)
                return 1f;
            return gm.gameSettings.masterVolume / 10f;
        }

        private void OnDestroy()
        {
            if (texture != null)
                Destroy(texture);
        }
    }
}
