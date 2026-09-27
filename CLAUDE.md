# CLAUDE.md

Unity 2D (URP) da yozilgan oddiy Flappy Bird klon. Kod izohlari o'zbek tilida — yangi kodda ham shu uslubni saqlang.

## Unity versiyasi va muhit

- **Unity Editor:** `6000.6.3f1` (Unity 6) — `ProjectSettings/ProjectVersion.txt`
- **Render pipeline:** URP 2D (`com.unity.render-pipelines.universal` 17.6.0, `Assets/Settings/Renderer2D.asset`)
- **UI:** uGUI + TextMesh Pro
- **Input:** faqat yangi Input System (`com.unity.inputsystem`), Active Input Handling = **Input System Package (New)** (`activeInputHandler: 1`; Tools → Prepare Android Release o'rnatadi, keyin Unity'ni qayta ishga tushirish kerak). Eski `UnityEngine.Input` API'ni **ishlatmang** — New rejimda exception beradi. Tap: `Touchscreen.touches[*].press` + `Mouse.leftButton` + `Keyboard.spaceKey`; Back/Esc: `Keyboard.escapeKey`. EventSystem'da `InputSystemUIInputModule` (StandaloneInputModule emas). `TextMesh Pro/Examples & Extras` skriptlari eski API'da, lekin sahnada ishlatilmaydi.
- **Fizika:** Rigidbody2D, `linearVelocity` (Unity 6 API; eski `velocity` emas). Fixed Timestep 0.02; qush Rigidbody2D Interpolate.

## Loyiha tuzilishi

Asosiy skriptlar va PipePair prefab `Assets/` ildizida turadi; yangi skriptlar `Assets/Scripts/` ga, Editor asboblari `Assets/Editor/` ga qo'shiladi.

```
Assets/
  Scripts/ParallaxBackground.cs  # fonni cheksiz chapga aylantiradi (o'z nusxasini yaratadi), tezligi × SpeedMultiplier
  Scripts/AudioManager.cs        # singleton + DontDestroyOnLoad; PlayJump/Death/Score/Coin/LevelUp/Click
  Scripts/Coin.cs                # tanga: Collect() → AddCoin + ovoz + Destroy
  Scripts/GameState.cs           # enum: Menu, Ready, Playing, Paused, Dying, GameOver
  Scripts/UIManager.cs           # HUD va panellar, tugmalar → GameManager
  Scripts/SafeArea.cs            # RectTransform → Screen.safeArea (notch)
  Scripts/CameraFitter.cs        # Main Camera: orthographicSize aspect bo'yicha
  Scripts/Haptics.cs             # tebranish + PlayerPrefs["Vibration"]
  Scripts/CameraShake.cs         # urilganda kamera silkinishi (Main Camera'ga o'zi qo'shiladi)
  Audio/                         # jump, score, coin, death, levelup, click (.wav)
  UI/                            # tugmalar, panel, ribbon, coin.png, coin_spin_sheet.png (6 kadr), yulduzlar; Fonts/TMP_Outline.mat
  ASSETS_README.md               # UI/Audio import sozlamalari (9-slice border'lar, palitra)
  Prefabs/Coin.prefab            # Tools/Create Coin Prefab yaratadi (qo'lda tahrirlamang)
  Animations/Coin/               # coin_spin.anim (12 fps loop), Coin.controller — ham o'sha menyu yaratadi
  Editor/FitCollider.cs          # Tools/Fit Collider To Sprite, Tools/Replace Box With Circle Collider
  Editor/BuildScript.cs          # Tools/Build Android APK, Tools/Build Windows; xato → Logs/BuildReport-*.txt
  Editor/CoinPrefabCreator.cs    # Tools/Create Coin Prefab; "Coin" tagini yaratadi, Spawner'ga ulaydi
  Editor/GameUIBuilder.cs        # Tools/Build Game UI — Canvas ierarxiyasi, sprite import, UIManager/GameManager ulash
  Editor/AndroidRelease.cs       # Tools/Prepare Android Release — reliz Player Settings + tayyorlik tekshiruvi
  BirdScript.cs        # qush: sakrash, collision/trigger → GameManager
  SpawnerScript.cs     # PipePair + bo'shliq ichida 0..N tanga yaratadi
  PipeScript.cs        # quvurni GameManager.CurrentSpeed bilan chapga suradi, x < -15 da o'chiradi
  GameManager.cs       # GameState, ochko, level, tangalar, rekord (PlayerPrefs), reset (sahna qayta yuklanmaydi)
  PipePair.prefab      # ikki quvur + ScoreSensor trigger
  Scenes/SampleScene.unity   # yagona o'yin sahnasi (Build Settings'da ham yagona)
  bird.png, birdold.png, pipe.png, background.jpg, Дизайн_без_названия-removebg-preview.png
  Settings/            # URP, Renderer2D, Input actions, Build Profiles
  TextMesh Pro/, Welcome/    # Unity shablonidan kelgan, o'yin mantiqiga aloqasi yo'q
```

Build natijalari (manba emas, tahrirlamang): `Build/` (Windows `.exe`), `Flappy Bird.apk`, `Build.zip`, `*_BackUpThisFolder_ButDontShipItWithYourGame/`, `GeneratedAssets/`. Unity avtomatik yaratadigan papkalar: `Library/`, `Temp/`, `Logs/`, `UserSettings/`.

## O'yin mantiqi

**SampleScene** obyektlari: `bird`, `Spawner`, `GameManager`, `AudioManager`, `Main Camera` (orthographic size 5), `Canvas` (+ `UIManager`; bolalari `HUD`, `MainMenuPanel`, `GameOverPanel`, `PausePanel` — Tools/Build Game UI yaratadi), `EventSystem`, `Global Light 2D`.

**Taglar** (katta-kichik harf muhim): `pipe` — `PipePair` ichidagi `pipe` va `pipe (1)` (ildizda emas); `Coin` — Coin prefab.

**State mashinasi** (`GameState`: Menu → Ready → Playing ⇄ Paused → Dying → GameOver). `GameManager.SetState()` → `Time.timeScale` (Ready/Playing/Dying'da 1, qolganida 0) + `UIManager.OnStateChanged()`. O'yin Menu'dan boshlanadi. BOSHLASH/QAYTA O'YNASH → **Ready** ("Ekranga teging", qush fizikasiz tebranadi, quvur yo'q) → birinchi tap → **Playing**. Urilish → **Dying** (vaqt yuradi, quvur/fon/spawner to'xtaydi, qush aylanib tushadi) → `gameOverPanelDelay` (0.5 s, realtime) → **GameOver**. Pauza Ready yoki Playing'dan, Resume o'sha holatga qaytaradi. **Sahna hech qachon qayta yuklanmaydi** — restart/menyu `ResetRun()` orqali: `SpawnerScript.ResetSpawner()` (quvur+tangalarni o'chiradi), `BirdScript.ResetBird()` (joy, tezlik, aylanish, scale, collider, `rb.simulated = false`), ochko/level/runCoins = 0, kechikkan GameOver korutini to'xtatiladi. Shuning uchun `AudioManager` (DontDestroyOnLoad) ko'paymaydi. `StateChangedFrame` — tugma bosilgan kadrdagi bosish qushga tap bo'lib o'tmasligi uchun.

- **GameManager** (singleton `Instance`) — holat va qoidalar; UI'ni o'zi chizmaydi, `UIManager` ga xabar beradi.
  - Inspector: `ui`, `bird`, `spawner` (bo'sh bo'lsa Awake'da sahnadan topiladi); `pipesPerLevel = 5`, `baseSpeed = 3`, `baseSpawnRate = 2`; game feel: `gameOverPanelDelay = 0.5`, `shakeDuration = 0.2`, `shakeMagnitude = 0.15`.
  - UI chaqiradi: `StartGame()` (reset + Ready), `Restart()`, `Pause()`, `Resume()`, `GoToMenu()`, `Quit()` (Editor'da Play Mode'dan chiqadi, build'da `Application.Quit()`). `BeginPlaying()` — BirdScript, Ready'dagi birinchi tapda.
  - `AddScore()` → har `pipesPerLevel` da Level++ → `ui.ShowLevelUp()` + `PlayLevelUp()`.
  - `CurrentSpeed = baseSpeed × min(1 + 0.12·(Level−1), 2)`; `CurrentSpawnRate = max(baseSpawnRate / (1 + 0.08·(Level−1)), 1.1)`; `SpeedMultiplier = CurrentSpeed / baseSpeed`.
  - `AddCoin(worldPos)` → `PlayerPrefs["Coins"]` (jami; HUD shuni ko'rsatadi) + `runCoins` (game over paneli) + "+1" popup. `PlayerPrefs.Save()` — GameOver, Quit va `OnApplicationPause` da.
  - `GameOver(hit)` → `PlayerPrefs["HighScore"]`, Dying; `hit` bo'lsa `ui.Flash()` + `CameraShake.Shake()`; kechikib `ui.ShowGameOver(score, high, runCoins, newRecord)` + GameOver.
  - Esc / Android "Orqaga" (`Keyboard.escapeKey`): Ready/Playing ↔ Paused. Ilova fonga o'tsa — avtomatik pauza.
- **UIManager** (`Canvas` ustida) — panellarni state bo'yicha yoqadi (HUD: Ready/Playing/Paused/Dying; TapHint: Ready), tugmalarni `GameManager.Instance` metodlariga Awake'da (kod orqali, persistent listener emas) ulaydi, har tugmaga click.wav qo'shadi. Effektlar: `Flash()` (oq, 0.15 s), `ShowCoinPopup()` ("+1" `CoinPopupTemplate` nusxasi, dunyo → ekran koordinatasi), TapHint alfa-pulsatsiyasi va "YANGI REKORD!" scale-pulsatsiyasi — hammasi unscaled vaqtda. Maydon nomlari `Editor/GameUIBuilder.cs` dagi `SetRef(...)` nomlari bilan bir xil bo'lishi shart.
- **BirdScript** (`bird` ustida) — Ready'da tebranadi va birinchi tapni kutadi; Playing'da kiritish. UI ustidagi bosish sakratmaydi: tap nuqtasida `EventSystem.RaycastAll` (IsPointerOverGameObject emas — u UI modulining oldingi kadr holatiga qaraydi). Bir kadrda ko'pi bilan bitta `Flap()` (`lastFlapFrame`).
  - Fizika qiymatlari **GameManager** Inspector'ida: `birdGravityScale = 2.5`, `birdJumpForce = 7` (sakrash ≈ 1 birlik = v²/(2·9.81·g)), `birdMaxFallSpeed = 8`; `Start()` da `bird.ConfigurePhysics()` qo'llaydi (+ Interpolate). BirdScript'dagi `jumpForce`/`maxFallSpeed` maydonlari faqat ko'rsatish uchun — runtime'da ustidan yoziladi. Sahnaga yozish (Rigidbody2D + BirdScript): **Tools → Apply Bird Physics**.
  - `FixedUpdate`: vertikal tezlik `[-maxFallSpeed, jumpForce]` oralig'ida; egilish −60° ga tezlik `-maxFallSpeed` ga yetganda chiqadi (faqat tirik paytda — Dying'da cheklov yo'q).
  - Egilish `FixedUpdate` da `rb.rotation` orqali (`freezeRotation = true`, to'qnashuvlar aylantirmaydi): tepaga +25° tez, qulashda −60° gacha silliq.
  - Sakrash effekti: `flapFrames` (2+ sprite) bo'lsa qanot animatsiyasi, bo'lmasa 0.1 s Y-squash (faqat Y — CircleCollider2D radiusi max(scale.x, scale.y) ga bog'liq, hitbox o'zgarmaydi). Hozir qushda bitta sprite bor → squash.
  - `OnCollisionEnter2D` faqat `CompareTag("pipe")` bo'lsa → `Die(true)`: collider o'chadi, `freezeRotation = false`, sakrab aylanib tushadi, tebranish. `|y| > 10` → `Die(false)` (effektsiz).
  - `OnTriggerEnter2D`: `Coin` tag → `Coin.Collect()`; boshqa trigger (ScoreSensor) → `AddScore()`.
  - `isDead` bayrog'i: o'lim bir marta hisoblanadi; `ResetBird()` tozalaydi.
  - Ovozlar `AudioManager.Instance` orqali (null tekshiruvi bilan).
- **SpawnerScript** (`Spawner` ustida, sahnada `heightOffset = 1.5`) — faqat Playing'da ishlaydi; oraliq `GameManager.CurrentSpawnRate` dan; birinchi quvur Playing boshlangan kadrda.
  - `coinPrefab` berilgan bo'lsa: 0..N tanga (N = min(1 + Level/2, 4)), bo'shliq markazida 0.7 oraliqli qator. Bo'shliq quvur collider bounds'idan hisoblanadi. Tangalar PipePair bolasi — u bilan birga siljiydi/o'chadi.
- **PipeScript** (`PipePair` ildizida) — faqat Playing'da, `GameManager.CurrentSpeed` bilan chapga; `x < -15` bo'lsa `Destroy`.
- **CameraShake** — `CameraShake.Shake()` Main Camera'ga komponentni o'zi qo'shadi (sahnada sozlash shart emas), LateUpdate'da unscaled.

**UI** (Canvas Scaler: 1080×1920, match 0.5). Canvas bolalari tartibi: HUD, Flash, MainMenuPanel, GameOverPanel, PausePanel. HUD: ochko (tepa markaz), LevelBadge (star_gold, chap tepa), CoinCounter (o'ng tepa), PauseButton (btn_icon), LevelUpText, TapHint, CoinPopupTemplate (yashirin). GameOverPanel: 3 qator + NewRecordText + 3 tugma. Tugmalar: Image Sliced + Sprite Swap (`*_pressed`), matn oq + `Assets/UI/Fonts/TMP_Outline.mat` (kontur #09263F). 9-slice sprite'lar Single rejimda, border'lar `ASSETS_README.md` bo'yicha (builder o'zi sozlaydi). UI'ni qo'lda emas, builder orqali o'zgartiring — qayta ishga tushirilganda qo'lda qilingan o'zgarishlar yo'qoladi.

**Game state:** `GameManager.State`. Menu/Paused/GameOver'da `Time.timeScale == 0`; Dying'da vaqt yuradi, shuning uchun dunyo skriptlari (`PipeScript`, `SpawnerScript`, `ParallaxBackground`) holatni o'zi tekshiradi. `PipeScript`/`SpawnerScript`/`BirdScript` sahnada `GameManager` bo'lishini talab qiladi.

**PipePair.prefab:** `pipe` (pastki) va `pipe (1)` (yuqori) — oddiy BoxCollider2D (trigger emas); `ScoreSensor` — ular orasidagi bo'shliqdagi trigger BoxCollider2D.

Inspector'dagi qiymatlar skriptdagi default'lardan farq qiladi (masalan `jumpForce` 5 → 6, `heightOffset` 2.5 → 1.5). Balansni o'zgartirishda sahna/prefab qiymatlarini tekshiring.

## Build platformalari

Build'lar `Editor/BuildScript.cs` menyulari orqali: **Tools → Build Android APK** → `Build/Android/Flappy Bird.apk`, **Tools → Build Windows** → `Build/Windows/Flappy Bird.exe` (xato → `Logs/BuildReport-*.txt`). `Assets/Settings/Build Profiles/` dagi profillarda (Windows 64-bit, Android) PlayerSettings/sahna override'lari yo'q.

**Android reliz** — `Editor/AndroidRelease.cs` (**Tools → Prepare Android Release**; Build Android APK ham shuni chaqiradi):
- Qiymatlar kodda: package `uz.azizjoon.flappybird`, product "O'zbekona Flappy", company "Azizjon Malikov", version 1.0 (code 1), faqat Portrait (autorotate o'chiq), IL2CPP + ARM64, min SDK 26, target SDK avtomatik. Reliz qiymatini shu fayldagi konstantalarda o'zgartiring — ProjectSettings'da qo'lda o'zgartirilsa, keyingi build qaytarib yozadi.
- Tekshiruv natijasi `Logs/AndroidReleaseCheck.txt` ga yoziladi: `[X]` xato (build to'xtaydi), `[!]` ogohlantirish. Sahna tekshiruvlari faqat ochiq sahna build sahnasi bo'lsa ishlaydi.
- Keystore yo'q — APK debug imzoli (Google Play uchun emas). Android toolchain (SDK 34/36/37, build-tools 36, NDK 27.2, OpenJDK) Unity moduli ichida.
- Ikonka/splash (`Assets/UI/Icons/`, `ICON_README.md`) ham shu yerda o'rnatiladi: Default icon `icon_512`, Adaptive (fon `adaptive_background_432`, old qatlam `adaptive_foreground_432`; `#if UNITY_ANDROID` — aktiv target Android bo'lishi kerak), Splash: portrait fon, logo (2 s), rang #0F3D63, All Sequential, overlay 0. Icons papkasidagi barcha PNG: Sprite, Single, Max 2048, Compression None.
- Prepare Android Release yana: Input handling = New (restart taklifi), `EnsureSceneSetup()` (CameraFitter, qush fizikasi, InputSystemUIInputModule). Build Android APK ham `EnsureSceneSetup()` ni chaqirib, sahnani saqlashni taklif qiladi.
- companyName/productName o'zgargani uchun Editor/Windows'dagi PlayerPrefs yangi joyda (eski rekord/tangalar ko'rinmaydi); Android'da yangi package = alohida ilova.

**Ekran moslashuvi:** `CameraFitter` (Main Camera) — kenglik 5.625 birlik qat'iy (9:16 da orthographicSize 5), uzunroq ekranda orthographicSize oshadi, kengroq ekranda 5 dan kamaymaydi. `SpawnerScript.PlaceOffScreen()` quvurni kameraning o'ng chetidan 0.5 birlik tashqarida chiqaradi (Spawner X'i ahamiyatsiz). `ParallaxBackground.fitCameraHeight` fonni kamera balandligigacha kattalashtiradi. UI: `SafeArea` HUD ildizida va har panelning `SafeArea` konteynerida.

**Tebranish:** `Haptics.Vibrate()` (tanga, quvurga urilish) → Android'da `Handheld.Vibrate()`; `PlayerPrefs["Vibration"]`, MainMenu/Pause'dagi "TEBRANISH" tugmasi bilan o'chiriladi.

## Repozitoriy

- GitHub: `MalikovAzizjon4843/Flappy-Bird` (`main`). `README.md` (o'zbek + ingliz), `LICENSE` (MIT; LiberationSans — OFL), `docs/screenshots/` (README skrinshotlari: `menu.png`, `gameplay.png`, `gameover.png`; README'da izoh ichida). APK'lar GitHub Releases orqali tarqatiladi, repoga qo'shilmaydi.
- `.gitignore` — GitHub Unity shabloni + loyiha qo'shimchalari: `*_BackUpThisFolder_ButDontShipItWithYourGame/`, `*_BurstDebugInformation_DoNotShip/`, ildizdagi `/*.zip`, `/GeneratedAssets/`. `Build/`, `*.apk`, `*.aab`, `Logs/`, `Library/` shablonda bor.

## Ish qoidalari

- `.unity` / `.prefab` fayllarini qo'lda tahrirlashdan qoching — fileID/GUID havolalari buzilishi mumkin. Imkon bo'lsa, Unity Editor orqali o'zgartiring.
- Yangi asset qo'shilganda Unity `.meta` fayl yaratadi; `.meta` fayllarni o'chirmang va GUID'larni o'zgartirmang (sahna skriptlarga GUID orqali bog'langan).
- Avtomatlashtirilgan testlar yo'q; tekshirish — Unity Editor'da Play Mode orqali.
