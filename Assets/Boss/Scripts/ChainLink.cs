using UnityEngine;

// เส้นแว้บสั้นๆ จากระเบิดไปหา dot ที่มันจุดต่อ ให้เห็นว่า chain ไหลไปทางไหน
// ไม่ต้องทำ prefab: เรียก ChainLink.Spawn(...) แล้วมันสร้างและลบตัวเอง
public class ChainLink : MonoBehaviour
{
    const float Life = 0.25f;           // อยู่นานกี่วินาที
    const float Width = 0.08f;
    const float InkMix = 0.85f;         // ผสมดำเข้าไป ให้เป็นเส้นหมึกเข้มบนพื้นกระดาษ
    const int SortingOrder = 3;         // เหนือ dot/กระสุน ใต้ player

    LineRenderer line;
    Color color;
    float age;

    public static void Spawn(Vector2 from, Vector2 to, Color color)
    {
        ChainLink link = new GameObject("ChainLink").AddComponent<ChainLink>();
        link.color = Color.Lerp(color, Color.black, InkMix);
        link.color.a = 1f;

        LineRenderer line = link.gameObject.AddComponent<LineRenderer>();
        line.sharedMaterial = FxMaterial.Sprite;
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.sortingOrder = SortingOrder;
        line.widthCurve = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);   // ปลายฝั่ง dot หนากว่า
        link.line = line;
        link.Refresh(0f);
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
        Refresh(t);
    }

    void Refresh(float t)
    {
        Color c = color;
        c.a = 1f - t;
        line.startColor = line.endColor = c;
        line.widthMultiplier = Width * (1f - t * t);
    }
}
