# O'zbekona Flappy Bird — generated assets

Copy `Audio/` and `UI/` into `Assets/`.

## Audio/ (wav, 44.1kHz mono, 8-bit style)
| file | use |
|---|---|
| jump.wav | bird flap |
| score.wav | passed a pipe pair |
| coin.wav | coin pickup |
| death.wav | hit pipe / out of bounds |
| levelup.wav | level increased |
| click.wav | any UI button press |
Import: Force To Mono = on, Load Type = Decompress On Load, Preload Audio Data = on.

## UI/ (png, transparent). All have transparent padding around the shape.
| file | size | Texture Type | Sprite Mode | Border (L,B,R,T) for 9-slice |
|---|---|---|---|---|
| btn_primary(.png / _pressed) | 420x140 | Sprite (2D and UI) | Single | 60,60,60,60 |
| btn_secondary(_pressed) | 420x140 | same | Single | 60,60,60,60 |
| btn_danger(_pressed) | 420x140 | same | Single | 60,60,60,60 |
| btn_icon(_pressed) | 140x140 | same | Single | 50,50,50,50 |
| panel | 628x528 | same | Single | 80,80,80,80 |
| ribbon_gold | 520x110 | same | Single | 45,45,45,45 |
| coin | 128x128 | same | Single | — (PPU 100 → 1.28 units; scale in scene ~0.5) |
| coin_spin_sheet | 768x128 | same | Multiple, slice grid 128x128 → 6 frames | — |
| star_gold / star_grey | 128x128 | same | Single | — |

Palette: turquoise #1FA3A8, deep blue #0F3D63, gold #E0B04A, cream #F7E9C8.
Buttons: Image Type = Sliced, Button Transition = Sprite Swap (Pressed Sprite = *_pressed).
Text on buttons: TMP, white with dark-blue outline (#09263F), bold.
Colors of buttons: primary = Start/Play/Continue, secondary = Menu/Restart, danger = Exit/Quit.
Coins: use coin_spin_sheet as a 6-frame Animator clip (12 fps, loop) on the Coin prefab.
