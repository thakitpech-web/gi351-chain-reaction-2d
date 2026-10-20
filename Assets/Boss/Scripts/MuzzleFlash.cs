using UnityEngine;

// แสงวาบที่ปากกระบอกตอนยิง (ยืดไปตามทิศยิง ขยายนิดแล้วจางหาย)
public class MuzzleFlash : MonoBehaviour
{
    const float Life = 0.08f;
    const float Stretch = 1.8f;         // ยาวตามทิศยิงกี่เท่าของความกว้าง
    const float Grow = 0.6f;            // ขยายเพิ่มอีกเท่าไหร่ก่อนหาย

    SpriteRenderer sr;
    Color color;
    Vector3 startScale;
    float age;

    public static void Spawn(Vector3 position, Quaternion rotation, Sprite sprite, Color color, float size, int sortingOrder)
    {
        if (sprite == null) return;

        MuzzleFlash flash = new GameObject("MuzzleFlash").AddComponent<MuzzleFlash>();
        flash.transform.SetPositionAndRotation(position, rotation);

        // ปรับให้ขนาดเป็นหน่วย world ไม่ว่า sprite จะใหญ่แค่ไหน
        float spriteSize = Mathf.Max(sprite.bounds.size.x, 0.001f);
        flash.startScale = new Vector3(size * Stretch, size, 1f) / spriteSize;
        flash.transform.localScale = flash.startScale;

        flash.sr = flash.gameObject.AddComponent<SpriteRenderer>();
        flash.sr.sprite = sprite;
        flash.sr.sharedMaterial = FxMaterial.Sprite;
        flash.sr.sortingOrder = sortingOrder;
        flash.color = color;
        flash.sr.color = color;
    }

    void Update()
    {
        age += Time.deltaTime;
        float t = age / Life;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = startScale * (1f + Grow * t);
        Color c = color;
        c.a = color.a * (1f - t);
        sr.color = c;
    }
}
