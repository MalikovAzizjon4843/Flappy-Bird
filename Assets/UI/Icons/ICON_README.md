# Icons & splash — put into Assets/UI/Icons/

| file | where in Unity |
|---|---|
| icon_512.png | Player Settings → Icon → Default Icon |
| adaptive_foreground_432.png + adaptive_background_432.png | Player Settings → Android → Icon → Adaptive (all sizes) |
| icon_192.png | Android → Legacy icons (optional) |
| splash_background_1080x1920.png | Splash Image → Background (Portrait) |
| splash_logo_1024.png | Splash Image → Logos list (Sprite) |

Import all as Texture Type = Sprite (2D and UI), Single, Max Size 2048, no compression for icons (Compression = None).
Splash: Show Splash Screen on, Draw Mode = All Sequential, Background Color #0F3D63, Overlay Opacity 0.
