using UnityEngine;
using UnityEngine.UI;

// ขอบจอดำแว้บตอน player โดนชน
// ไม่ต้องวางในฉาก สร้าง canvas + รูปขอบจอเองตอนถูกเรียกครั้งแรก
public class DamageVignette : MonoBehaviour
{
    const float FadeTime = 0.5f;        // จางหายในกี่วินาที
    const int SortingOrder = 500;       // เหนือ UI เกม แต่ใต้ SceneFader (999)
    static readonly Color Tint = Color.black;

    static DamageVignette instance;

    RawImage image;
    float alpha;

    public static void Flash(float strength = 1f)
    {
        DamageVignette v = Get();
        v.alpha = Mathf.Max(v.alpha, Mathf.Clamp01(strength));
        v.Refresh();
    }

    static DamageVignette Get()
    {
        if (instance != null) return instance;

        var go = new GameObject("DamageVignette");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        var image = new GameObject("Edge").AddComponent<RawImage>();
        image.transform.SetParent(go.transform, false);
        image.texture = BuildTexture();
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;

        instance = go.AddComponent<DamageVignette>();
        instance.image = image;
        instance.Refresh();
        return instance;
    }

    // กลางใส ขอบทึบ
    static Texture2D BuildTexture()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 uv = new Vector2(x, y) / (size - 1) * 2f - Vector2.one;   // -1..1
            float a = Mathf.SmoothStep(0f, 1f, (uv.magnitude - 0.55f) / 0.7f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (alpha <= 0f) return;
        alpha = Mathf.Max(0f, alpha - Time.unscaledDeltaTime / FadeTime);  // ไม่ค้างตอน hit stop
        Refresh();
    }

    void Refresh()
    {
        Color c = Tint;
        c.a = alpha;
        image.color = c;
        image.enabled = alpha > 0f;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
