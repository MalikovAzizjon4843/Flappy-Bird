using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Collider'larni SpriteRenderer sprite'ining ko'rinadigan chegarasiga moslash asboblari.
public static class FitCollider
{
    // 1 = aniq sprite chegarasi; 0.9 = 10% kichikroq (o'yinchiga biroz "kechirimli" hitbox)
    const float Scale = 1f;

    // Circle radiusi sprite'ning kichik yarim o'lchamidan shuncha kichik bo'ladi
    const float CircleScale = 0.9f;

    // Tanlangan obyekt(lar)dagi BoxCollider2D ni moslaydi.
    // Bolalar obyektlari ham hisobga olinadi (masalan, PipePair tanlansa ikkala quvur ham moslanadi).
    [MenuItem("Tools/Fit Collider To Sprite")]
    static void FitSelected()
    {
        int count = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            foreach (BoxCollider2D box in go.GetComponentsInChildren<BoxCollider2D>(true))
            {
                if (Fit(box)) count++;
            }

            // Project oynasidagi prefab asset tanlangan bo'lsa, o'zgarishni diskka yozish
            if (PrefabUtility.IsPartOfPrefabAsset(go))
            {
                PrefabUtility.SavePrefabAsset(go.transform.root.gameObject);
            }
        }
        Debug.Log($"Fit Collider To Sprite: {count} ta BoxCollider2D moslandi.");
    }

    [MenuItem("Tools/Fit Collider To Sprite", true)]
    static bool FitSelectedValidate()
    {
        return Selection.gameObjects.Length > 0;
    }

    // Tanlangan obyektdagi BoxCollider2D ni o'chirib, sprite'dan ~10% kichik CircleCollider2D qo'yadi.
    // Faqat sahnadagi obyekt uchun (masalan "bird"); Ctrl+Z bilan qaytarish mumkin.
    [MenuItem("Tools/Replace Box With Circle Collider")]
    static void ReplaceWithCircle()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning($"Replace With Circle: {go.name} da sprite yo'q — o'tkazib yuborildi.");
                continue;
            }

            GetSpriteLocalBounds(sr, out Vector2 min, out Vector2 max);
            Vector2 center = (min + max) * 0.5f;
            Vector2 size = max - min;

            Undo.SetCurrentGroupName("Replace Box With Circle Collider");
            foreach (BoxCollider2D box in go.GetComponents<BoxCollider2D>())
            {
                Undo.DestroyObjectImmediate(box);
            }

            CircleCollider2D circle = go.GetComponent<CircleCollider2D>();
            if (circle == null)
            {
                circle = Undo.AddComponent<CircleCollider2D>(go);
            }
            Undo.RecordObject(circle, "Replace Box With Circle Collider");
            // Kichik tomon bo'yicha: aylana sprite ichida qoladi va qanot uchlari quvurga ilinmaydi
            circle.radius = Mathf.Min(size.x, size.y) * 0.5f * CircleScale;
            circle.offset = center;

            EditorSceneManager.MarkSceneDirty(go.scene);
            Debug.Log($"Replace With Circle: {go.name} — radius {circle.radius:F3} (local), offset {center}. Sahnani saqlang (Ctrl+S).");
        }
    }

    [MenuItem("Tools/Replace Box With Circle Collider", true)]
    static bool ReplaceWithCircleValidate()
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.scene.IsValid();
    }

    static bool Fit(BoxCollider2D box)
    {
        SpriteRenderer sr = box.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            return false;
        }

        // Trigger (masalan ScoreSensor) sprite'ga bog'liq emas — tegmaymiz
        if (box.isTrigger)
        {
            return false;
        }

        GetSpriteLocalBounds(sr, out Vector2 min, out Vector2 max);
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = (max - min) * Scale;

        Undo.RecordObject(box, "Fit Collider To Sprite");
        box.offset = center;
        box.size = size;
        EditorUtility.SetDirty(box);
        return true;
    }

    // Sprite'ning obyekt local koordinatalaridagi ko'rinadigan chegarasi (flip hisobga olingan)
    static void GetSpriteLocalBounds(SpriteRenderer sr, out Vector2 min, out Vector2 max)
    {
        if (sr.drawMode == SpriteDrawMode.Simple)
        {
            // Tight mesh vertices shaffof chetlarni hisobga olmaydi — rect'dan aniqroq
            Vector2[] verts = sr.sprite.vertices;
            min = verts[0];
            max = verts[0];
            foreach (Vector2 v in verts)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
        }
        else
        {
            // Sliced/Tiled rejimida o'lcham SpriteRenderer.size dan olinadi
            Vector2 pivotNorm = new Vector2(sr.sprite.pivot.x / sr.sprite.rect.width, sr.sprite.pivot.y / sr.sprite.rect.height);
            min = -Vector2.Scale(pivotNorm, sr.size);
            max = min + sr.size;
        }

        // FlipX/FlipY sprite'ni pivot atrofida aks ettiradi
        if (sr.flipX)
        {
            float x = min.x;
            min.x = -max.x;
            max.x = -x;
        }
        if (sr.flipY)
        {
            float y = min.y;
            min.y = -max.y;
            max.y = -y;
        }
    }
}
