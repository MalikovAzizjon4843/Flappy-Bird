# O'zbekona Flappy

[O'zbekcha](#ozbekcha) · [English](#english)

Unity 6 da yozilgan 2D Flappy Bird uslubidagi mobil o'yin: level tizimi, tangalar, rekord va Android uchun tayyor build.
A 2D Flappy Bird–style mobile game built with Unity 6: levels, coins, high score and a ready-to-ship Android build.

**[⬇ APK yuklab olish / Download APK](https://github.com/MalikovAzizjon4843/Flappy-Bird/releases/latest)** — Android 8.0+ (API 26), ARM64

<!--
Skrinshotlarni docs/screenshots/ ga qo'shgach, shu izohni olib tashlang (qarang: docs/screenshots/README.md).
After adding screenshots to docs/screenshots/, remove this comment (see docs/screenshots/README.md).

<p align="center">
  <img src="docs/screenshots/menu.png" width="240" alt="Bosh menyu / Main menu">
  <img src="docs/screenshots/gameplay.png" width="240" alt="O'yin / Gameplay">
  <img src="docs/screenshots/gameover.png" width="240" alt="O'yin tugadi / Game over">
</p>
-->

---

## O'zbekcha

### O'yin haqida
Qushni quvurlar orasidan uchirib o'tkazing: har bir juft quvur — 1 ochko. Quvurlar orasida tangalar uchraydi, har 5 quvurdan keyin level oshadi va o'yin tezlashadi. Rekord va yig'ilgan tangalar qurilmada saqlanadi.

### Boshqaruv
| Amal | Telefon | Kompyuter (Editor / Windows) |
|---|---|---|
| Sakrash | Ekranga teging | Sichqoncha chap tugmasi yoki `Space` |
| O'yinni boshlash | BOSHLASH, keyin ekranga birinchi teginish | xuddi shunday |
| Pauza / davom etish | Pauza tugmasi (o'ng tepada) yoki **Orqaga** tugmasi | `Esc` |
| Tebranishni yoqish/o'chirish | Menyu yoki pauzadagi **TEBRANISH** tugmasi | — |

Ilova fonga o'tsa, o'yin avtomatik pauzaga tushadi.

### Level va tangalar
- **Level:** har **5** ta quvurdan o'tganda level +1. Quvurlar tezligi har levelda **+12%** (ko'pi bilan 2 barobar), yangi quvurlar orasidagi vaqt **÷(1 + 0.08·(level−1))**, lekin 1.1 soniyadan kam emas. Level oshganda "Level N" yozuvi va ovoz chiqadi.
- **Tangalar:** har bir juft quvur bilan birga bo'shliq ichida **0…N** ta tanga paydo bo'ladi, N = 1 + level/2 (ko'pi bilan 4). Tanga olinganda "+1" yozuvi, ovoz va tebranish.
- **Saqlanadi:** jami tangalar (HUD'da o'ng tepada) va rekord. O'yin tugaganda: ochko, rekord va shu o'yinda yig'ilgan tangalar; yangi rekord bo'lsa — "YANGI REKORD!".

### Build qilish
**Talablar:** [Unity Hub](https://unity.com/download) va **Unity 6000.6.3f1** quyidagi modullar bilan:
- *Android Build Support* (+ *OpenJDK* va *Android SDK & NDK Tools*)
- *Windows Build Support* (ixtiyoriy)

**Qadamlar:**
1. Repozitoriyni klon qiling va Unity Hub'da **Add → Add project from disk** orqali oching.
2. `Assets/Scenes/SampleScene.unity` ni oching.
3. **Tools → Prepare Android Release** — Player Settings (package, versiya, Portrait, IL2CPP + ARM64, ikonka, splash) va Input System'ni sozlaydi, build'ga tayyorlikni tekshiradi. Unity qayta ishga tushirishni so'rasa, rozi bo'ling.
4. **Tools → Build Android APK** → `Build/Android/Flappy Bird.apk`.
   Windows uchun: **Tools → Build Windows** → `Build/Windows/Flappy Bird.exe`.

Xato bo'lsa, sababi `Logs/BuildReport-Android.txt` va `Logs/AndroidReleaseCheck.txt` ga yoziladi.

Buyruq qatoridan (CI):
```bash
Unity.exe -batchmode -quit -projectPath "<loyiha papkasi>" -executeMethod BuildScript.BuildAndroid -logFile Logs/build-android.log
```

> APK debug kalit bilan imzolanadi — telefonda o'rnatish uchun yetarli. Google Play uchun **Player Settings → Publishing Settings** da o'z keystore'ingizni yarating.

### Texnologiyalar
Unity 6 (6000.6.3f1) · URP 2D · Input System · uGUI + TextMesh Pro · C#

### Litsenziya
[MIT](LICENSE) © 2026 Azizjon Malikov

Istisno: TextMesh Pro bilan kelgan LiberationSans shrifti — SIL Open Font License (`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`).

---

## English

### About
Guide the bird through the gaps between pipes — every pipe pair is 1 point. Coins appear between the pipes, and every 5 pipes the level goes up and the game speeds up. Your high score and collected coins are saved on the device.

### Controls
| Action | Phone | Computer (Editor / Windows) |
|---|---|---|
| Flap | Tap the screen | Left mouse button or `Space` |
| Start | BOSHLASH (Start), then the first tap | same |
| Pause / resume | Pause button (top right) or the **Back** button | `Esc` |
| Toggle vibration | **TEBRANISH** button in the menu or pause panel | — |

The game pauses automatically when the app goes to the background.

### Levels and coins
- **Levels:** every **5** pipes passed → level +1. Pipe speed grows **+12%** per level (capped at 2×), the spawn interval is **÷(1 + 0.08·(level−1))** with a 1.1 s minimum. A "Level N" banner and sound play on level up.
- **Coins:** **0…N** coins spawn in the gap of each pipe pair, N = 1 + level/2 (max 4). Picking one up shows a "+1" popup, plays a sound and vibrates.
- **Saved:** total coins (top-right HUD) and the high score. The game-over panel shows the score, the high score and coins collected this run; a new record shows "YANGI REKORD!" (New record!).

### Building
**Requirements:** [Unity Hub](https://unity.com/download) and **Unity 6000.6.3f1** with:
- *Android Build Support* (+ *OpenJDK* and *Android SDK & NDK Tools*)
- *Windows Build Support* (optional)

**Steps:**
1. Clone the repository and open it in Unity Hub via **Add → Add project from disk**.
2. Open `Assets/Scenes/SampleScene.unity`.
3. **Tools → Prepare Android Release** — applies Player Settings (package, version, Portrait, IL2CPP + ARM64, icons, splash) and the Input System setting, then checks build readiness. Accept if Unity asks to restart.
4. **Tools → Build Android APK** → `Build/Android/Flappy Bird.apk`.
   For Windows: **Tools → Build Windows** → `Build/Windows/Flappy Bird.exe`.

On failure the reason is written to `Logs/BuildReport-Android.txt` and `Logs/AndroidReleaseCheck.txt`.

From the command line (CI):
```bash
Unity.exe -batchmode -quit -projectPath "<project folder>" -executeMethod BuildScript.BuildAndroid -logFile Logs/build-android.log
```

> The APK is signed with a debug key — fine for installing on a phone. For Google Play, create your own keystore in **Player Settings → Publishing Settings**.

### Tech stack
Unity 6 (6000.6.3f1) · URP 2D · Input System · uGUI + TextMesh Pro · C#

### License
[MIT](LICENSE) © 2026 Azizjon Malikov

Exception: the LiberationSans font bundled with TextMesh Pro is under the SIL Open Font License (`Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`).
