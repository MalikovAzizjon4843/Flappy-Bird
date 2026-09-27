using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

// Tools → Prepare Android Release: reliz Player Settings'ni qo'llaydi, Main Camera'ga CameraFitter qo'yadi
// va build'ga tayyorlikni tekshiradi. Natija: dialog + Logs/AndroidReleaseCheck.txt.
// BuildScript.BuildAndroid() ham build'dan oldin shu sozlamalar va tekshiruvlarni ishlatadi.
public static class AndroidRelease
{
    // --- Reliz qiymatlari (Google Play'ga chiqqandan keyin PackageName o'zgartirib bo'lmaydi) ---
    public const string PackageName = "uz.azizjoon.flappybird";
    const string ProductName = "O'zbekona Flappy";
    const string CompanyName = "Azizjon Malikov";
    const string Version = "1.0";
    const int VersionCode = 1;

    public const string ReportPath = "Logs/AndroidReleaseCheck.txt";

    // Android package name qoidasi: kamida 2 bo'lak, har biri harf bilan boshlanadi, faqat harf/raqam/_
    static readonly Regex PackageNameRegex = new Regex(@"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$");

    // Ikonka va splash fayllari (Assets/UI/Icons/ICON_README.md)
    const string IconFolder = "Assets/UI/Icons";
    const string DefaultIconPath = IconFolder + "/icon_512.png";
    const string AdaptiveForegroundPath = IconFolder + "/adaptive_foreground_432.png";
    const string AdaptiveBackgroundPath = IconFolder + "/adaptive_background_432.png";
    const string SplashBackgroundPath = IconFolder + "/splash_background_1080x1920.png";
    const string SplashLogoPath = IconFolder + "/splash_logo_1024.png";
    static readonly Color SplashColor = new Color32(0x0F, 0x3D, 0x63, 0xFF); // #0F3D63
    const float SplashLogoDuration = 2f;

    // ProjectSettings.asset → activeInputHandler: 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both
    const int InputHandlerNew = 1;

    [MenuItem("Tools/Prepare Android Release")]
    static void Prepare()
    {
        bool inputChanged = SetNewInputHandling();
        ApplySettings();
        EnsureSceneSetup();
        int errors = RunChecks(out string report);
        EditorUtility.DisplayDialog("Android reliz tekshiruvi",
            (errors == 0 ? "Build'ga tayyor.\n\n" : $"{errors} ta xato — build ishlamaydi.\n\n") + Trim(report), "OK");

        // Input handling o'zgargan bo'lsa, Unity'ni qayta ishga tushirish kerak — oxirida so'raymiz
        if (inputChanged)
        {
            OfferRestart();
        }
    }

    [MenuItem("Tools/Apply Bird Physics")]
    static void ApplyBirdPhysicsMenu()
    {
        ApplyBirdPhysics();
    }

    // Sahnaga tegishli reliz sozlamalari: kamera moslashuvi, qush fizikasi, yangi Input System UI moduli
    public static void EnsureSceneSetup()
    {
        EnsureCameraFitter();
        ApplyBirdPhysics();
        GameUIBuilder.EnsureEventSystem();
    }

    public static void ApplySettings()
    {
        ConfigureIconImports();
        ApplyIconsAndSplash();

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.productName = ProductName;
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.Android.bundleVersionCode = VersionCode;

        // Faqat Portrait, avtomatik burilish yo'q
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // IL2CPP + ARM64 (Google Play 64-bit talab qiladi)
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        AssetDatabase.SaveAssets(); // ProjectSettings.asset diskka yozilsin
    }

    // --- Input System ---

    static SerializedObject PlayerSettingsAsset()
    {
        return new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
    }

    static int ActiveInputHandler()
    {
        SerializedProperty prop = PlayerSettingsAsset().FindProperty("activeInputHandler");
        return prop != null ? prop.intValue : -1;
    }

    // "Both" build'da ogohlantirish dialogini beradi; loyiha kodi faqat yangi Input System'dan foydalanadi.
    // O'zgartirilgan bo'lsa true qaytaradi (Unity'ni qayta ishga tushirish kerak).
    static bool SetNewInputHandling()
    {
        if (ActiveInputHandler() == InputHandlerNew)
        {
            return false;
        }

        SerializedObject settings = PlayerSettingsAsset();
        settings.FindProperty("activeInputHandler").intValue = InputHandlerNew;
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log("Prepare Android Release: Active Input Handling = Input System Package (New). Unity qayta ishga tushirilgach kuchga kiradi.");
        return true;
    }

    static void OfferRestart()
    {
        bool restart = EditorUtility.DisplayDialog("Input System",
            "Active Input Handling \"Input System Package (New)\" ga o'tkazildi. Kuchga kirishi uchun Unity'ni qayta ishga tushirish kerak.\n\n" +
            "Hozir qayta ishga tushiraymi? (Saqlanmagan sahna bo'lsa, avval saqlash so'raladi.)",
            "Ha, qayta ishga tushir", "Keyinroq");

        if (restart && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorApplication.OpenProject(Directory.GetCurrentDirectory());
        }
    }

    // --- Qush fizikasi ---

    // GameManager'dagi qiymatlarni sahnadagi qushga yozadi: Rigidbody2D (gravity, interpolate) va BirdScript
    // (jumpForce, maxFallSpeed — Rigidbody2D'da tushish tezligi chegarasi yo'q). Runtime'da ConfigurePhysics baribir
    // qo'llaydi; bu Inspector'da ko'rinadigan qiymat haqiqiysi bilan bir xil bo'lishi uchun.
    static void ApplyBirdPhysics()
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        BirdScript bird = Object.FindAnyObjectByType<BirdScript>();
        Rigidbody2D rb = bird != null ? bird.GetComponent<Rigidbody2D>() : null;
        if (gm == null || rb == null)
        {
            Debug.LogWarning("Apply Bird Physics: sahnada GameManager yoki qush Rigidbody2D topilmadi.");
            return;
        }

        if (BirdPhysicsMatches(gm, bird, rb))
        {
            return;
        }

        Undo.RecordObject(rb, "Apply Bird Physics");
        rb.gravityScale = gm.BirdGravityScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // BirdScript maydonlari private [SerializeField] — SerializedObject orqali (Undo bilan)
        SerializedObject birdSo = new SerializedObject(bird);
        birdSo.FindProperty("jumpForce").floatValue = gm.BirdJumpForce;
        birdSo.FindProperty("maxFallSpeed").floatValue = gm.BirdMaxFallSpeed;
        birdSo.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(rb.gameObject.scene);
        Debug.Log($"Apply Bird Physics: Gravity Scale = {gm.BirdGravityScale}, Jump Force = {gm.BirdJumpForce}, " +
                  $"Max Fall Speed = {gm.BirdMaxFallSpeed}, Interpolate. Sahnani saqlang (Ctrl+S).");
    }

    static bool BirdPhysicsMatches(GameManager gm, BirdScript bird, Rigidbody2D rb)
    {
        return Mathf.Approximately(rb.gravityScale, gm.BirdGravityScale) &&
               rb.interpolation == RigidbodyInterpolation2D.Interpolate &&
               Mathf.Approximately(bird.JumpForce, gm.BirdJumpForce) &&
               Mathf.Approximately(bird.MaxFallSpeed, gm.BirdMaxFallSpeed);
    }

    // --- Ikonka va splash ---

    // ICON_README: Sprite (2D and UI), Single, Max Size 2048, Compression = None, mipmap'siz
    static void ConfigureIconImports()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { IconFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                continue;
            }

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.maxTextureSize == 2048 &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                !importer.mipmapEnabled &&
                !android.overridden)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            if (android.overridden)
            {
                // Android override siqishni qaytarib qo'ymasin — umumiy (siqilmagan) sozlama ishlatiladi
                android.overridden = false;
                importer.SetPlatformTextureSettings(android);
            }
            importer.SaveAndReimport();
        }
    }

    static void ApplyIconsAndSplash()
    {
        Texture2D defaultIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultIconPath);
        if (defaultIcon != null)
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { defaultIcon }, IconKind.Any);
        }
        else
        {
            Debug.LogWarning($"Prepare Android Release: {DefaultIconPath} topilmadi.");
        }

#if UNITY_ANDROID
        // Adaptive ikonka (Android 8+): har o'lcham uchun bir xil 432 px qatlamlar; 0 — fon, 1 — old qatlam
        Texture2D foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveForegroundPath);
        Texture2D background = AssetDatabase.LoadAssetAtPath<Texture2D>(AdaptiveBackgroundPath);
        if (foreground != null && background != null)
        {
            PlatformIconKind adaptive = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, adaptive);
            foreach (PlatformIcon icon in icons)
            {
                icon.SetTextures(background, foreground);
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, adaptive, icons);
        }
#endif

        // Splash: fon rangi va portrait fon rasmi, logo, avval Unity logosi keyin bizniki
        Sprite splashBackground = AssetDatabase.LoadAssetAtPath<Sprite>(SplashBackgroundPath);
        Sprite splashLogo = AssetDatabase.LoadAssetAtPath<Sprite>(SplashLogoPath);
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
        PlayerSettings.SplashScreen.backgroundColor = SplashColor;
        PlayerSettings.SplashScreen.overlayOpacity = 0f;
        PlayerSettings.SplashScreen.backgroundPortrait = splashBackground;
        if (splashLogo != null)
        {
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(SplashLogoDuration, splashLogo) };
        }
    }

    // --- Sahna ---

    // Main Camera'da CameraFitter bo'lmasa qo'shish (ochiq sahnada, Undo bilan)
    static void EnsureCameraFitter()
    {
        Camera cam = Camera.main;
        if (cam == null || cam.GetComponent<CameraFitter>() != null)
        {
            return;
        }
        Undo.AddComponent<CameraFitter>(cam.gameObject);
        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        Debug.Log("Prepare Android Release: Main Camera'ga CameraFitter qo'shildi. Sahnani saqlang (Ctrl+S).");
    }

    // Xatolar soni qaytadi (0 = build qilish mumkin). Ogohlantirishlar build'ni to'xtatmaydi.
    public static int RunChecks(out string report)
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        List<string> ok = new List<string>();

        // --- Muhit ---
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            errors.Add("Android Build Support moduli o'rnatilmagan (Unity Hub → Installs → Add modules).");
        else
            ok.Add("Android Build Support moduli o'rnatilgan.");

        if (EditorUtility.scriptCompilationFailed)
            errors.Add("Skriptlarda kompilyatsiya xatosi bor — Console'ni tekshiring.");

        // --- Player Settings ---
        string package = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (!PackageNameRegex.IsMatch(package))
            errors.Add($"Package name noto'g'ri: \"{package}\" (faqat harf, raqam, _ va nuqta).");
        else if (package != PackageName)
            warnings.Add($"Package name \"{package}\", kutilgan \"{PackageName}\" — Tools → Prepare Android Release'ni bosing.");
        else
            ok.Add($"Package name: {package}");

        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
            errors.Add($"Orientation {PlayerSettings.defaultInterfaceOrientation}, Portrait bo'lishi kerak.");
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            errors.Add("Scripting backend IL2CPP emas.");
        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            errors.Add("ARM64 arxitekturasi tanlanmagan.");
        ok.Add($"{PlayerSettings.productName} {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}), " +
               $"min SDK {(int)PlayerSettings.Android.minSdkVersion}, target SDK {PlayerSettings.Android.targetSdkVersion}");

        if (!PlayerSettings.Android.useCustomKeystore)
            warnings.Add("Keystore yo'q — APK debug kalit bilan imzolanadi. Telefonda sinash uchun yetarli, Google Play uchun " +
                         "Publishing Settings'da o'z keystore'ingizni yarating (parolni o'zingiz kiriting va saqlang).");

        // Kod faqat yangi Input System API'sini ishlatadi; "Both" build'da dialog beradi, "Old" da o'yin ishlamaydi
        int inputHandler = ActiveInputHandler();
        if (inputHandler != InputHandlerNew)
            errors.Add($"Active Input Handling = {(inputHandler == 0 ? "Input Manager (Old)" : "Both")}, " +
                       "\"Input System Package (New)\" bo'lishi kerak — Tools → Prepare Android Release, keyin Unity'ni qayta ishga tushiring.");
        else
            ok.Add("Input handling = New (Input System Package).");

        // 50 Hz fizika + Rigidbody2D interpolatsiyasi — 60/120 Hz ekranlarda harakat bir xil tekis
        if (!Mathf.Approximately(Time.fixedDeltaTime, 0.02f))
            warnings.Add($"Fixed Timestep = {Time.fixedDeltaTime}, 0.02 kutilgan (Project Settings → Time).");
        else
            ok.Add("Fixed Timestep = 0.02.");

        // Default Icon (barcha platformalar) yoki Android override — birortasi bo'lsa yetarli
        bool hasIcon = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any).Any(i => i != null) ||
                       PlayerSettings.GetIcons(NamedBuildTarget.Android, IconKind.Any).Any(i => i != null);
        if (!hasIcon)
            warnings.Add("Ilova ikonkasi berilmagan — telefonda Unity'ning standart ikonkasi ko'rinadi (Player → Icon).");
        else
            ok.Add("Default ikonka berilgan.");
#if UNITY_ANDROID
        bool hasAdaptive = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive)
            .Any(icon => icon.GetTextures().Length >= 2 && icon.GetTextures().All(t => t != null));
        if (!hasAdaptive)
            warnings.Add("Adaptive ikonka (fon + old qatlam) berilmagan — Android 8+ da ikonka kvadrat ichida kichik ko'rinadi.");
        else
            ok.Add("Adaptive ikonka berilgan.");
#endif
        if (PlayerSettings.SplashScreen.backgroundPortrait == null || PlayerSettings.SplashScreen.logos.Length == 0)
            warnings.Add("Splash fon rasmi yoki logo berilmagan.");
        else
            ok.Add("Splash: fon + logo, All Sequential.");

        // --- Taglar ---
        foreach (string tag in new[] { "pipe", "Coin" })
        {
            if (!InternalEditorUtility.tags.Contains(tag))
                errors.Add($"\"{tag}\" tagi yo'q (BirdScript CompareTag bilan ishlatadi).");
        }

        // --- Sahna ---
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
        if (scenes.Length == 0)
        {
            errors.Add("Build Settings'da yoqilgan sahna yo'q.");
        }
        else
        {
            foreach (EditorBuildSettingsScene missing in scenes.Where(s => !File.Exists(s.path)))
                errors.Add($"Build sahnasi topilmadi: {missing.path}");

            Scene active = SceneManager.GetActiveScene();
            if (active.path != scenes[0].path)
            {
                warnings.Add($"Ochiq sahna ({active.path}) build sahnasi emas ({scenes[0].path}) — sahna ichidagi tekshiruvlar o'tkazib yuborildi.");
            }
            else
            {
                if (active.isDirty)
                    warnings.Add("Sahnada saqlanmagan o'zgarishlar bor — build diskdagi versiyani oladi (Ctrl+S).");
                CheckSceneObjects(errors, warnings, ok);
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}  xatolar: {errors.Count}, ogohlantirishlar: {warnings.Count}");
        foreach (string e in errors) sb.AppendLine("[X] " + e);
        foreach (string w in warnings) sb.AppendLine("[!] " + w);
        foreach (string o in ok) sb.AppendLine("[OK] " + o);
        report = sb.ToString();

        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, report);
        if (errors.Count > 0) Debug.LogError("Android reliz tekshiruvi:\n" + report);
        else Debug.Log("Android reliz tekshiruvi:\n" + report);

        return errors.Count;
    }

    static void CheckSceneObjects(List<string> errors, List<string> warnings, List<string> ok)
    {
        GameManager gm = Object.FindAnyObjectByType<GameManager>();
        UIManager ui = Object.FindAnyObjectByType<UIManager>();
        if (gm == null) errors.Add("Sahnada GameManager yo'q.");
        if (ui == null) errors.Add("Sahnada UIManager yo'q — Tools → Build Game UI.");
        else ReportNullRefs(ui, errors, "UIManager (Tools → Build Game UI'ni qayta bosing)");

        AudioManager audio = Object.FindAnyObjectByType<AudioManager>();
        if (audio == null) warnings.Add("Sahnada AudioManager yo'q — ovozsiz ishlaydi.");
        else ReportNullRefs(audio, warnings, "AudioManager");

        SpawnerScript spawner = Object.FindAnyObjectByType<SpawnerScript>();
        if (spawner == null) errors.Add("Sahnada SpawnerScript yo'q.");
        else if (spawner.pipePrefab == null) errors.Add("Spawner.pipePrefab bo'sh.");
        else if (spawner.coinPrefab == null) warnings.Add("Spawner.coinPrefab bo'sh — tangalar chiqmaydi (Tools → Create Coin Prefab).");

        Camera cam = Camera.main;
        if (cam == null) errors.Add("Sahnada MainCamera tagli kamera yo'q.");
        else if (cam.GetComponent<CameraFitter>() == null) warnings.Add("Main Camera'da CameraFitter yo'q — uzun ekranlarda ko'rinish farq qiladi.");

        // Eski StandaloneInputModule yangi Input System'da har kadrda exception beradi
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null) errors.Add("Sahnada EventSystem yo'q — UI tugmalari bosilmaydi (Tools → Build Game UI).");
        else if (eventSystem.GetComponent<StandaloneInputModule>() != null) errors.Add("EventSystem'da StandaloneInputModule turibdi — Tools → Build Game UI uni almashtiradi.");
        else if (eventSystem.GetComponent<InputSystemUIInputModule>() == null) errors.Add("EventSystem'da InputSystemUIInputModule yo'q.");

        // Runtime'da GameManager baribir qo'llaydi; bu yerda Inspector'dagi qiymat mos kelishi tekshiriladi
        BirdScript bird = Object.FindAnyObjectByType<BirdScript>();
        Rigidbody2D birdBody = bird != null ? bird.GetComponent<Rigidbody2D>() : null;
        if (gm != null && birdBody != null && !BirdPhysicsMatches(gm, bird, birdBody))
            warnings.Add($"Qush: Gravity {birdBody.gravityScale}, Jump {bird.JumpForce}, Max Fall {bird.MaxFallSpeed}, {birdBody.interpolation} — " +
                         $"GameManager'dagi qiymatlar ({gm.BirdGravityScale}, {gm.BirdJumpForce}, {gm.BirdMaxFallSpeed}, Interpolate) " +
                         "runtime'da qo'llanadi. Sahnaga yozish: Tools → Apply Bird Physics.");

        if (errors.Count == 0) ok.Add("Sahna obyektlari va havolalar joyida.");
    }

    // Komponentning bo'sh qolgan [SerializeField] havolalarini ro'yxatga qo'shish
    static void ReportNullRefs(Object target, List<string> list, string label)
    {
        SerializedProperty prop = new SerializedObject(target).GetIterator();
        List<string> missing = new List<string>();
        while (prop.NextVisible(true))
        {
            if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue == null && prop.name != "m_Script")
                missing.Add(prop.name);
        }
        if (missing.Count > 0)
            list.Add($"{label}: bo'sh maydonlar — {string.Join(", ", missing)}");
    }

    static string Trim(string text) => text.Length > 1500 ? text.Substring(0, 1500) + "…" : text;
}
