using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Modding;
using UnityEngine;

namespace BadApple
{
    public class GlobalSettings
    {
        public bool Music = true;
        public int Volume = 7;          // 0..10
        public int Background = 0;      // 0 = original, 1 = black, 2 = white
        public bool Stretch = false;    // true = stretch to fill the screen (no bars)
    }

    public class BadAppleMod : Mod, IMenuMod, ITogglableMod, IGlobalSettings<GlobalSettings>
    {
        internal static BadAppleMod Instance;
        public static GlobalSettings Settings = new GlobalSettings();

        private static MaskVideo video;
        private static AudioClip music;
        private Overlay overlay;

        public BadAppleMod() : base("Bad Apple") { }

        public override string GetVersion() => "1.0.0.0";

        public override void Initialize()
        {
            Instance = this;
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (video == null)
            {
                string maskPath = Path.Combine(dir, "badapple.bam");
                if (!File.Exists(maskPath))
                {
                    LogError("badapple.bam not found in " + dir);
                    return;
                }
                video = MaskVideo.Load(maskPath);
                Log($"Mask loaded: {video.FrameCount} frames {video.Width}x{video.Height}");
            }

            if (music == null)
            {
                string wavPath = Path.Combine(dir, "badapple.wav");
                if (File.Exists(wavPath))
                    music = WavLoader.Load(wavPath);
                else
                    LogWarn("badapple.wav not found, the mod will run without music");
            }

            var go = new GameObject("BadAppleOverlay");
            Object.DontDestroyOnLoad(go);
            overlay = go.AddComponent<Overlay>();
            overlay.Setup(video, music);
        }

        public void Unload()
        {
            if (overlay != null)
                Object.Destroy(overlay.gameObject);
            overlay = null;
        }

        public void OnLoadGlobal(GlobalSettings s) => Settings = s;
        public GlobalSettings OnSaveGlobal() => Settings;

        public bool ToggleButtonInsideMenu => true;

        public List<IMenuMod.MenuEntry> GetMenuData(IMenuMod.MenuEntry? toggleButtonEntry)
        {
            var entries = new List<IMenuMod.MenuEntry>();
            if (toggleButtonEntry.HasValue)
                entries.Add(toggleButtonEntry.Value);

            entries.Add(new IMenuMod.MenuEntry
            {
                Name = "Music",
                Description = "Plays the Bad Apple!! song",
                Values = new[] { "Off", "On" },
                Saver = i => { Settings.Music = i == 1; overlay?.ApplySettings(); },
                Loader = () => Settings.Music ? 1 : 0
            });

            var volumes = new string[11];
            for (int i = 0; i <= 10; i++)
                volumes[i] = (i * 10) + "%";
            entries.Add(new IMenuMod.MenuEntry
            {
                Name = "Music Volume",
                Description = "Multiplied by the game's master volume",
                Values = volumes,
                Saver = i => { Settings.Volume = i; overlay?.ApplySettings(); },
                Loader = () => Settings.Volume
            });

            entries.Add(new IMenuMod.MenuEntry
            {
                Name = "Background",
                Description = "What is shown outside the silhouettes",
                Values = new[] { "Original", "Black", "White" },
                Saver = i => { Settings.Background = i; overlay?.ApplySettings(); },
                Loader = () => Settings.Background
            });

            entries.Add(new IMenuMod.MenuEntry
            {
                Name = "Size",
                Description = "4:3 with black bars, or stretched to fill the screen",
                Values = new[] { "4:3 (bars)", "Stretched" },
                Saver = i => { Settings.Stretch = i == 1; overlay?.ApplySettings(); },
                Loader = () => Settings.Stretch ? 1 : 0
            });

            return entries;
        }
    }
}
