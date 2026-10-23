# HollowKnight.BadApple

Mod which plays the "Bad Apple!!" shadow-art music video on a loop over the entire game. Everything outside the silhouettes of the video is covered by the video's background; the game can only be seen through the inside of the silhouettes. 

The overlay is purely visual and does not change any gameplay. It is hidden on the main menu and while the game is paused, so menus remain usable. The song keeps playing while paused, and when music is enabled the video stays synced to it.

Options (Options -> Mods -> Bad Apple)
- Bad Apple: enables or disables the mod
- Music: plays the "Bad Apple!!" song together with the video
- Music Volume: 0% to 100%. This is multiplied by the game's master volume
- Background: what is shown outside the silhouettes
	- Original: the original black, white and gradient background of the video
	- Black: solid black
	- White: solid white
- Size: how the video is fitted to the screen
	- 4:3 (bars): keeps the original 4:3 aspect ratio, with black bars on the sides
	- Stretched: stretches the video to fill the whole screen

Notes
- The game's own music still plays underneath the song. To hear only Bad Apple, lower the music volume in the game's audio settings.
- Gradients in the "Original" background are slightly banded. This keeps the mod's download size down.
- Settings are saved in `BadAppleMod.GlobalSettings.json`, in your saves folder.

Files
- `BadApple.dll`: the mod
- `badapple.bam`: the video's silhouette mask (6572 frames, 480x360, 30 fps)
- `badapple.wav`: the song. If this file is missing, the mod still works, without music

Credits
- Dreamy-Catty - developer
- "Bad Apple!!" - original composition by ZUN (Touhou Project), arrangement by Alstroemeria Records feat. nomico
- Shadow art music video by Anira

## Building

The `tools/convert_video.py` script converts a green screen version of the video into `badapple.bam` and `badapple.wav` (requires Python 3, numpy and ffmpeg):

```bash
python3 tools/convert_video.py "path/to/video.mp4" data
```

Then build the mod (requires the .NET SDK). By default the project looks for the game in the macOS Steam location; on other systems pass the path to the game's `Managed` folder:

```bash
cd BadApple
dotnet build -c Release -p:HKManaged="path/to/Hollow Knight/hollow_knight_Data/Managed"
```

The build copies the mod and its data files into `Managed/Mods/BadApple`.

## License

BadApple's source code is licensed under MIT. The song and the video belong to their respective creators (see Credits).

## Installation

BadApple can be installed by any of the various modding installers (Scarab, Lumafly, etc) made by the Hollow Knight modding community.
