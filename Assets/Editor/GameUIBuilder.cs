using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Tools → Build Game UI: ochiq sahnadagi Canvas ichida HUD, MainMenu, GameOver va Pause panellarini
// Assets/UI spritelaridan quradi (ASSETS_README.md dagi border va ranglar bo'yicha), UIManager va GameManager'ni ulaydi.
// Eski ScoreText/RestartButton/GameOverText/LevelText/CoinHUD va avval qurilgan panellar o'chiriladi —
// qayta ishga tushirish xavfsiz. Ctrl+Z bilan bir qadamda qaytariladi.
public static class GameUIBuilder
{
    const string UIFolder = "Assets/UI";
    const string OutlineMaterialPath = UIFolder + "/Fonts/TMP_Outline.mat";

    // ASSETS_README.md: 9-slice border'lar (L, B, R, T bir xil)
    static readonly (string file, float border)[] SlicedSprites =
    {
        ("btn_primary", 60), ("btn_primary_pressed", 60),
        ("btn_secondary", 60), ("btn_secondary_pressed", 60),
        ("btn_danger", 60), ("btn_danger_pressed", 60),
        ("btn_icon", 50), ("btn_icon_pressed", 50),
        ("panel", 80),
        ("ribbon_gold", 45),
    };

    // Palitra (ASSETS_README.md): panel matnlari to'q ko'k, level yozuvi oltin, tugma/HUD matni oq + kontur
    static readonly Color DeepBlue = Hex("#0F3D63");
    static readonly Color Gold = Hex("#E0B04A");
    static readonly Color OutlineBlue = Hex("#09263F");
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.55f);

    static readonly Vector2 ButtonSize = new Vector2(640, 170);
    const float ButtonFontSize = 60;
    const float RowFontSize = 56;

    // Eski yoki avval qurilgan Canvas bolalari — o'chiriladi
    static readonly string[] ReplacedNames =
    {
        "ScoreText", "RestartButton", "GameOverText", "LevelText", "CoinHUD",
        "HUD", "Flash", "MainMenuPanel", "GameOverPanel", "PausePanel",
    };

    static Material outlineMaterial;

    [MenuItem("Tools/Build Game UI")]
    static void Build()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            EditorUtility.DisplayDialog("Build Game UI", "Ochiq sahnada GameManager topilmadi (SampleScene'ni oching).", "OK");
            return;
        }

        ConfigureSpriteImports();
        outlineMaterial = GetOrCreateOutlineMaterial();
        if (outlineMaterial == null)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Game UI");
        int undoGroup = Undo.GetCurrentGroup();

        Canvas canvas = GetOrCreateCanvas();
        EnsureEventSystem();
        RemoveOldUI(canvas.transform);

        UIManager ui = canvas.GetComponent<UIManager>();
        if (ui == null)
        {
            ui = Undo.AddComponent<UIManager>(canvas.gameObject);
        }
        SerializedObject uiSo = new SerializedObject(ui);

        BuildHud(canvas.transform, uiSo);
        BuildFlash(canvas.transform, uiSo); // HUD ustida, panellar ostida
        BuildMainMenu(canvas.transform, uiSo);
        BuildGameOver(canvas.transform, uiSo);
        BuildPause(canvas.transform, uiSo);
        uiSo.ApplyModifiedProperties();

        SerializedObject gmSo = new SerializedObject(gameManager);
        SetRef(gmSo, "ui", ui);
        SetRef(gmSo, "bird", Object.FindAnyObjectByType<BirdScript>());
        SetRef(gmSo, "spawner", Object.FindAnyObjectByType<SpawnerScript>());
        gmSo.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeGameObject = canvas.gameObject;
        Debug.Log("Build Game UI: tayyor. Sahnani saqlang (Ctrl+S).");
    }

    // ---------------- Ierarxiya ----------------

    static void BuildHud(Transform canvas, SerializedObject uiSo)
    {
        RectTransform hud = NewRect("HUD", canvas);
        Stretch(hud);
        hud.gameObject.AddComponent<SafeArea>(); // HUD burchaklari notch/kamera teshigi ostida qolmasin
        Undo.RegisterCreatedObjectUndo(hud.gameObject, "Build Game UI");

        // Ochko — markaz tepa
        RectTransform score = NewRect("ScoreText", hud);
        Place(score, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(600, 140));
        TextMeshProUGUI scoreText = AddText(score, "0", 72, Color.white, true);

        // Level badge — chap tepa: star_gold + raqam
        RectTransform badge = NewRect("LevelBadge", hud);
        Place(badge, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -40), new Vector2(150, 150));
        AddImage(badge, LoadFirstSprite("star_gold"), false).preserveAspect = true;
        RectTransform levelRect = NewRect("LevelText", badge);
        Stretch(levelRect);
        TextMeshProUGUI levelText = AddText(levelRect, "1", 56, Color.white, true);

        // Tangalar — o'ng tepa: coin + son
        RectTransform coins = NewRect("CoinCounter", hud);
        Place(coins, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -50), new Vector2(320, 110));
        RectTransform coinIcon = NewRect("CoinIcon", coins);
        Place(coinIcon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(100, 100));
        AddImage(coinIcon, LoadFirstSprite("coin"), false).preserveAspect = true;
        RectTransform coinTextRect = NewRect("CoinText", coins);
        Stretch(coinTextRect);
        coinTextRect.offsetMin = new Vector2(115, 0);
        TextMeshProUGUI coinText = AddText(coinTextRect, "0", 64, Color.white, true, TextAlignmentOptions.Left);

        // Pauza — tangalar ostida
        Button pause = AddButton(hud, "PauseButton", "btn_icon", "II",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40, -180), new Vector2(130, 130));

        // "Level N" — o'rtada, oltin rangda; UIManager ko'rsatib so'ndiradi
        RectTransform levelUp = NewRect("LevelUpText", hud);
        Place(levelUp, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 400), new Vector2(1000, 180));
        TextMeshProUGUI levelUpText = AddText(levelUp, "Level 2", 110, Gold, true);
        levelUp.gameObject.SetActive(false);

        SetRef(uiSo, "hud", hud.gameObject);
        SetRef(uiSo, "scoreText", scoreText);
        SetRef(uiSo, "levelText", levelText);
        SetRef(uiSo, "coinText", coinText);
        SetRef(uiSo, "levelUpText", levelUpText);
        SetRef(uiSo, "pauseButton", pause);

        // "Ekranga teging" — pastroqda, qush ustini to'smasin; UIManager pulsatsiya qiladi
        RectTransform tapHint = NewRect("TapHint", hud);
        Place(tapHint, Center, Center, new Vector2(0, -350), new Vector2(900, 140));
        TextMeshProUGUI tapHintText = AddText(tapHint, "Ekranga teging", 72, Color.white, true);
        tapHint.gameObject.SetActive(false);
        SetRef(uiSo, "tapHintText", tapHintText);

        // "+1" shabloni: anchor/pivot markazda — UIManager nusxasini tanga joyiga qo'yadi
        RectTransform popup = NewRect("CoinPopupTemplate", hud);
        Place(popup, Center, Center, Vector2.zero, new Vector2(200, 100));
        TextMeshProUGUI popupText = AddText(popup, "+1", 64, Gold, true);
        popup.gameObject.SetActive(false);
        SetRef(uiSo, "coinPopupTemplate", popupText);

        hud.gameObject.SetActive(false); // o'yin menyudan boshlanadi
    }

    // Urilgandagi oq chaqnash: butun ekran (notch ham), bosishlarni to'smaydi
    static void BuildFlash(Transform canvas, SerializedObject uiSo)
    {
        RectTransform flash = NewRect("Flash", canvas);
        Stretch(flash);
        Image image = AddImage(flash, null, false);
        image.color = new Color(1f, 1f, 1f, 0f);
        flash.gameObject.SetActive(false);
        Undo.RegisterCreatedObjectUndo(flash.gameObject, "Build Game UI");
        SetRef(uiSo, "flashImage", image);
    }

    static void BuildMainMenu(Transform canvas, SerializedObject uiSo)
    {
        RectTransform panel = BuildPanel(canvas, "MainMenuPanel", "O'ZBEKONA FLAPPY", new Vector2(860, 1050), out GameObject root);

        Button start = AddButton(panel, "StartButton", "btn_primary", "BOSHLASH", Center, Center, new Vector2(0, 270), ButtonSize);
        TextMeshProUGUI highScore = AddRow(panel, "HighScoreText", "REKORD: 0", new Vector2(0, 100));
        Button vibration = AddButton(panel, "VibrationButton", "btn_secondary", "TEBRANISH: YONIQ", Center, Center, new Vector2(0, -70), ButtonSize);
        Button exit = AddButton(panel, "ExitButton", "btn_danger", "CHIQISH", Center, Center, new Vector2(0, -270), ButtonSize);
        SetRef(uiSo, "menuVibrationButton", vibration);

        SetRef(uiSo, "mainMenuPanel", root);
        SetRef(uiSo, "menuHighScoreText", highScore);
        SetRef(uiSo, "startButton", start);
        SetRef(uiSo, "menuExitButton", exit);
        // Sahnada menyu ko'rinib tursin (Play'da UIManager baribir holatga qarab yoqadi/o'chiradi)
    }

    static void BuildGameOver(Transform canvas, SerializedObject uiSo)
    {
        RectTransform panel = BuildPanel(canvas, "GameOverPanel", "O'YIN TUGADI", new Vector2(860, 1250), out GameObject root);

        TextMeshProUGUI score = AddRow(panel, "ScoreRow", "Ochko: 0", new Vector2(0, 470));
        TextMeshProUGUI best = AddRow(panel, "HighScoreRow", "Rekord: 0", new Vector2(0, 385));
        TextMeshProUGUI coins = AddRow(panel, "CoinsRow", "Tangalar: +0", new Vector2(0, 300));

        // "YANGI REKORD!" — faqat rekord yangilanganda ko'rinadi, UIManager pulsatsiya qiladi
        RectTransform record = NewRect("NewRecordText", panel);
        Place(record, Center, Center, new Vector2(0, 190), new Vector2(720, 110));
        TextMeshProUGUI newRecord = AddText(record, "YANGI REKORD!", 68, Gold, true);
        record.gameObject.SetActive(false);
        SetRef(uiSo, "newRecordText", newRecord);

        Button retry = AddButton(panel, "RetryButton", "btn_secondary", "QAYTA O'YNASH", Center, Center, new Vector2(0, 20), ButtonSize);
        Button menu = AddButton(panel, "MenuButton", "btn_secondary", "MENYU", Center, Center, new Vector2(0, -180), ButtonSize);
        Button exit = AddButton(panel, "ExitButton", "btn_danger", "CHIQISH", Center, Center, new Vector2(0, -380), ButtonSize);

        SetRef(uiSo, "gameOverPanel", root);
        SetRef(uiSo, "goScoreText", score);
        SetRef(uiSo, "goHighScoreText", best);
        SetRef(uiSo, "goCoinsText", coins);
        SetRef(uiSo, "retryButton", retry);
        SetRef(uiSo, "goMenuButton", menu);
        SetRef(uiSo, "goExitButton", exit);

        root.SetActive(false);
    }

    static void BuildPause(Transform canvas, SerializedObject uiSo)
    {
        RectTransform panel = BuildPanel(canvas, "PausePanel", "PAUZA", new Vector2(860, 1100), out GameObject root);

        Button resume = AddButton(panel, "ResumeButton", "btn_primary", "DAVOM ETISH", Center, Center, new Vector2(0, 300), ButtonSize);
        Button vibration = AddButton(panel, "VibrationButton", "btn_secondary", "TEBRANISH: YONIQ", Center, Center, new Vector2(0, 100), ButtonSize);
        Button menu = AddButton(panel, "MenuButton", "btn_secondary", "MENYU", Center, Center, new Vector2(0, -100), ButtonSize);
        Button exit = AddButton(panel, "ExitButton", "btn_danger", "CHIQISH", Center, Center, new Vector2(0, -300), ButtonSize);
        SetRef(uiSo, "pauseVibrationButton", vibration);

        SetRef(uiSo, "pausePanel", root);
        SetRef(uiSo, "resumeButton", resume);
        SetRef(uiSo, "pauseMenuButton", menu);
        SetRef(uiSo, "pauseExitButton", exit);

        root.SetActive(false);
    }

    // To'liq ekranli qoraytirilgan fon (o'yinga bosishni to'sadi) + markazda panel + tepasida ribbon sarlavha
    static RectTransform BuildPanel(Transform canvas, string name, string title, Vector2 size, out GameObject root)
    {
        RectTransform rootRect = NewRect(name, canvas);
        Stretch(rootRect);
        Image dim = rootRect.gameObject.AddComponent<Image>();
        dim.color = Dim;
        dim.raycastTarget = true;
        Undo.RegisterCreatedObjectUndo(rootRect.gameObject, "Build Game UI");
        root = rootRect.gameObject;

        // Qoraytirish butun ekranni (notch ham) yopadi, panel esa safe area ichida markazlanadi
        RectTransform safeArea = NewRect("SafeArea", rootRect);
        Stretch(safeArea);
        safeArea.gameObject.AddComponent<SafeArea>();

        RectTransform panel = NewRect("Panel", safeArea);
        Place(panel, Center, Center, Vector2.zero, size);
        AddImage(panel, LoadSprite("panel"), true);

        RectTransform ribbon = NewRect("Ribbon", panel);
        Place(ribbon, new Vector2(0.5f, 1f), Center, Vector2.zero, new Vector2(size.x + 60, 170));
        AddImage(ribbon, LoadSprite("ribbon_gold"), true);
        RectTransform titleRect = NewRect("Title", ribbon);
        Stretch(titleRect);
        titleRect.offsetMin = new Vector2(40, 10); // ribbon uchlari va soyasiga tegmasin
        titleRect.offsetMax = new Vector2(-40, 0);
        TextMeshProUGUI titleText = AddText(titleRect, title, 64, Color.white, true);
        titleText.enableAutoSizing = true; // uzun sarlavha ribbon'dan chiqib ketmasin
        titleText.fontSizeMin = 36;
        titleText.fontSizeMax = 64;

        return panel;
    }

    // ---------------- Yordamchi qurilmalar ----------------

    static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    static Button AddButton(Transform parent, string name, string spriteName, string label,
                            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        RectTransform rect = NewRect(name, parent);
        Place(rect, anchor, pivot, pos, size);

        Image image = AddImage(rect, LoadSprite(spriteName), true);
        image.raycastTarget = true;

        // README: Image Sliced + Sprite Swap (Pressed = *_pressed)
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState { pressedSprite = LoadSprite(spriteName + "_pressed") };
        button.navigation = new Navigation { mode = Navigation.Mode.None }; // bosilgandan keyin "selected" holatda qotib qolmasin

        RectTransform labelRect = NewRect("Label", rect);
        Stretch(labelRect);
        labelRect.offsetMin = new Vector2(20, 8); // tugma pastidagi soya ustida emas, markazda ko'rinsin
        labelRect.offsetMax = new Vector2(-20, 0);
        TextMeshProUGUI text = AddText(labelRect, label, ButtonFontSize, Color.white, true);
        text.enableAutoSizing = true;
        text.fontSizeMin = 32;
        text.fontSizeMax = ButtonFontSize;

        return button;
    }

    static TextMeshProUGUI AddRow(Transform parent, string name, string text, Vector2 pos)
    {
        RectTransform rect = NewRect(name, parent);
        Place(rect, Center, Center, pos, new Vector2(720, 90));
        return AddText(rect, text, RowFontSize, DeepBlue, false);
    }

    static TextMeshProUGUI AddText(RectTransform rect, string text, float size, Color color, bool outline,
                                   TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false; // matn ustiga bosish qush sakrashini to'smasin
        if (outline)
        {
            // README: oq matn, to'q ko'k (#09263F) kontur — umumiy material, har matnga alohida nusxa emas
            tmp.fontSharedMaterial = outlineMaterial;
        }
        return tmp;
    }

    static Image AddImage(RectTransform rect, Sprite sprite, bool sliced)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = Center;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError($"Build Game UI: {so.targetObject.GetType().Name}.{field} maydoni topilmadi.");
            return;
        }
        prop.objectReferenceValue = value;
    }

    // ---------------- Sahna ----------------

    static Canvas GetOrCreateCanvas()
    {
        Canvas canvas = Object.FindObjectsByType<Canvas>().FirstOrDefault(c => c.isRootCanvas);
        if (canvas == null)
        {
            GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = LayerMask.NameToLayer("UI");
            Undo.RegisterCreatedObjectUndo(go, "Build Game UI");
            canvas = go.GetComponent<Canvas>();
        }

        Undo.RecordObject(canvas, "Build Game UI");
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
        }
        Undo.RecordObject(scaler, "Build Game UI");
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
        }
        return canvas;
    }

    // Loyiha faqat yangi Input System'da: eski StandaloneInputModule runtime'da exception beradi —
    // uni InputSystemUIInputModule bilan almashtiramiz (standart UI action'lari Reset'da o'zi beriladi)
    public static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject go = new GameObject("EventSystem", typeof(EventSystem));
            Undo.RegisterCreatedObjectUndo(go, "Build Game UI");
            eventSystem = go.GetComponent<EventSystem>();
        }

        foreach (StandaloneInputModule legacy in eventSystem.GetComponents<StandaloneInputModule>())
        {
            Undo.DestroyObjectImmediate(legacy);
            Debug.Log("Build Game UI: StandaloneInputModule → InputSystemUIInputModule almashtirildi.");
        }
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            Undo.AddComponent<InputSystemUIInputModule>(eventSystem.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(eventSystem.gameObject.scene);
    }

    static void RemoveOldUI(Transform canvas)
    {
        for (int i = canvas.childCount - 1; i >= 0; i--)
        {
            Transform child = canvas.GetChild(i);
            if (ReplacedNames.Contains(child.name))
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
            else
            {
                Debug.LogWarning($"Build Game UI: Canvas ichidagi \"{child.name}\" tanilmadi — tegilmadi.");
            }
        }
    }

    // ---------------- Assetlar ----------------

    // README: Sprite (2D and UI), Single, 9-slice border. Avtomatik "Multiple" kesish shaffof chetlarni qirqadi va
    // border'lar noto'g'ri tushadi — shuning uchun Single'ga o'tkaziladi. coin/star/coin_spin_sheet'ga tegilmaydi.
    static void ConfigureSpriteImports()
    {
        foreach ((string file, float border) in SlicedSprites)
        {
            string path = $"{UIFolder}/{file}.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"Build Game UI: {path} topilmadi.");
                continue;
            }

            Vector4 borders = new Vector4(border, border, border, border);
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                importer.spriteBorder == borders &&
                !importer.mipmapEnabled)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = borders;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; // UI uchun kerak emas, xira ko'rinishga olib keladi
            importer.SaveAndReimport();
        }
    }

    static Material GetOrCreateOutlineMaterial()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            EditorUtility.DisplayDialog("Build Game UI",
                "TMP default font topilmadi. Window → TextMeshPro → Import TMP Essential Resources.", "OK");
            return null;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (mat == null)
        {
            if (!AssetDatabase.IsValidFolder(UIFolder + "/Fonts"))
            {
                AssetDatabase.CreateFolder(UIFolder, "Fonts");
            }
            // Shrift materialidan nusxa — atlas teksturasi saqlanadi
            mat = new Material(font.material) { name = "TMP_Outline" };
            AssetDatabase.CreateAsset(mat, OutlineMaterialPath);
        }

        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.25f);
        mat.SetColor(ShaderUtilities.ID_OutlineColor, OutlineBlue);
        mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.15f); // kontur harfni ingichkalashtirmasin
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssetIfDirty(mat);
        return mat;
    }

    static Sprite LoadSprite(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIFolder}/{name}.png");
        if (sprite == null)
        {
            Debug.LogError($"Build Game UI: {UIFolder}/{name}.png sprite sifatida yuklanmadi.");
        }
        return sprite;
    }

    // coin/star — Multiple rejimda bo'lishi mumkin (Coin prefab coin_0 ga bog'langan), birinchi sprite olinadi
    static Sprite LoadFirstSprite(string name)
    {
        return AssetDatabase.LoadAllAssetsAtPath($"{UIFolder}/{name}.png").OfType<Sprite>().FirstOrDefault();
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}
