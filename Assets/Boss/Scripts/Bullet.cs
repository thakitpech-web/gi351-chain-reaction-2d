using UnityEngine;

// วางบน prefab กระสุน (Sprite + Collider2D ที่ติ๊ก Is Trigger + Rigidbody2D)
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float offscreenMargin = 1f;  // ออกนอกจอเกินระยะนี้แล้วลบทิ้ง
    [Range(0f, 1f)] public float hitShake = 1f;   // จอสั่นตอนยิงโดน dot
    public float hitStop = 0.06f;                 // หยุดเวลาแว้บตอนยิงโดน (วินาที)

    [Header("Trail")]
    public float trailTime = 0.12f;
    [Range(0f, 1f)] public float trailAlpha = 0.8f;
    public float trailWidth = 0.8f;     // เทียบกับขนาดกระสุน

    Rigidbody2D rb;
    TrailRenderer trail;
    bool hit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        GameManager.Instance.RegisterBullet(this);
        BuildTrail();
    }

    void BuildTrail()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // อยู่ใน child แยก จะได้ปล่อยให้ค่อยๆ จางเองตอนกระสุนโดน
        trail = new GameObject("Trail").AddComponent<TrailRenderer>();
        trail.transform.SetParent(transform, false);
        trail.sharedMaterial = FxMaterial.Sprite;
        trail.time = trailTime;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        float size = sr != null && sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
        trail.widthMultiplier = size * transform.localScale.x * trailWidth;

        Color c = sr != null ? sr.color : Color.white;
        c.a = trailAlpha;
        trail.startColor = c;
        c.a = 0f;
        trail.endColor = c;
        if (sr != null)
        {
            trail.sortingLayerID = sr.sortingLayerID;
            trail.sortingOrder = sr.sortingOrder - 1;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterBullet(this);
    }

    public void Launch(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    void Update()
    {
        Rect area = GameManager.Instance.PlayArea;
        Vector2 pos = transform.position;
        if (pos.x < area.xMin - offscreenMargin || pos.x > area.xMax + offscreenMargin ||
            pos.y < area.yMin - offscreenMargin || pos.y > area.yMax + offscreenMargin)
        {
            Destroy(gameObject);
        }
    }

    // ยิงโดน dot -> dot แตก แล้วเริ่ม chain reaction
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hit) return;
        if (other.TryGetComponent(out ChainDot dot))
        {
            hit = true;
            ScreenShake.Add(hitShake);
            TimeFx.HitStop(hitStop);
            dot.Explode();

            trail.transform.SetParent(null, true);
            trail.autodestruct = true;
            trail.emitting = false;
            Destroy(gameObject);
        }
    }
}
