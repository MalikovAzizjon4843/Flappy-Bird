using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools → Create Coin Prefab:
//   Assets/Animations/Coin/coin_spin.anim + Coin.controller (coin_spin_sheet, 12 fps, loop)
//   Assets/Prefabs/Coin.prefab (SpriteRenderer coin.png, CircleCollider2D trigger, Animator, Coin, tag "Coin", scale 0.5)
// "Coin" tagi bo'lmasa yaratiladi; ochiq sahnadagi SpawnerScript.coinPrefab ga avtomatik ulanadi.
// Qayta ishga tushirilsa, mavjud fayllar ustidan yoziladi.
public static class CoinPrefabCreator
{
    const string CoinSpritePath = "Assets/UI/coin.png";
    const string SpinSheetPath = "Assets/UI/coin_spin_sheet.png";
    const string AnimFolder = "Assets/Animations/Coin";
    const string ClipPath = AnimFolder + "/coin_spin.anim";
    const string ControllerPath = AnimFolder + "/Coin.controller";
    const string PrefabPath = "Assets/Prefabs/Coin.prefab";
    const string CoinTag = "Coin";
    const float FrameRate = 12f;
    const float Scale = 0.5f;

    [MenuItem("Tools/Create Coin Prefab")]
    static void CreateCoinPrefab()
    {
        Sprite coinSprite = LoadSprites(CoinSpritePath).FirstOrDefault();
        // Kadrlar tartibi — varaqdagi chapdan o'ngga joylashuvi bo'yicha
        Sprite[] frames = LoadSprites(SpinSheetPath).OrderBy(s => s.rect.x).ToArray();

        if (coinSprite == null || frames.Length == 0)
        {
            EditorUtility.DisplayDialog("Create Coin Prefab",
                $"Sprite topilmadi. {CoinSpritePath} va {SpinSheetPath} Texture Type = Sprite (2D and UI) bo'lishi, " +
                "coin_spin_sheet esa Sprite Mode = Multiple qilib kesilgan bo'lishi kerak.", "OK");
            return;
        }

        EnsureTag(CoinTag);
        EnsureFolder(AnimFolder);
        EnsureFolder("Assets/Prefabs");

        AnimationClip clip = CreateSpinClip(frames);
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);
        // Controller'ning ichki qismlari (layer, state) diskka yozilsin — aks holda import worker uni
        // "not valid" deb ko'radi, prefab esa chala faylga bog'lanadi
        AssetDatabase.SaveAssets();

        // Vaqtinchalik obyekt yig'ib, prefab sifatida saqlash
        GameObject go = new GameObject("Coin");
        try
        {
            go.tag = CoinTag;
            go.transform.localScale = new Vector3(Scale, Scale, 1f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = coinSprite;
            sr.sortingOrder = 1; // quvurlar (0) ustida ko'rinsin

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = coinSprite.bounds.extents.x;

            Animator animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            go.AddComponent<Coin>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            AssignToSpawner(prefab);

            Debug.Log($"Create Coin Prefab: {PrefabPath} yaratildi ({frames.Length} kadr, {FrameRate} fps).");
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    // "Assets/A/B" kabi papkalarni AssetDatabase orqali bosqichma-bosqich yaratish
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static Sprite[] LoadSprites(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
    }

    static AnimationClip CreateSpinClip(Sprite[] frames)
    {
        AnimationClip clip = new AnimationClip { frameRate = FrameRate };

        // Har kadr uchun bitta kalit. Unity sprite klip uzunligiga oxirgi kalitdan keyin o'zi bir kadr qo'shadi
        // (6 kadr → 0.5 s), shuning uchun oxirgi kadrni takrorlash kerak emas — aks holda u ikki barobar uzoq turadi.
        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i / FrameRate, value = frames[i] };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.DeleteAsset(ClipPath);
        AssetDatabase.CreateAsset(clip, ClipPath);
        return clip;
    }

    // Tag yo'q bo'lsa TagManager'ga qo'shish (Project Settings → Tags and Layers bilan bir xil)
    static void EnsureTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag)
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"Create Coin Prefab: \"{tag}\" tagi yaratildi.");
    }

    static void AssignToSpawner(GameObject prefab)
    {
        SpawnerScript spawner = Object.FindAnyObjectByType<SpawnerScript>();
        if (spawner == null)
        {
            Debug.LogWarning("Create Coin Prefab: ochiq sahnada SpawnerScript topilmadi — Coin Prefab'ni qo'lda ulang.");
            return;
        }

        Undo.RecordObject(spawner, "Assign Coin Prefab");
        spawner.coinPrefab = prefab;
        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Debug.Log("Create Coin Prefab: Spawner.coinPrefab ulandi. Sahnani saqlang (Ctrl+S).");
    }
}
