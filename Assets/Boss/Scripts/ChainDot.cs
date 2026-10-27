using UnityEngine;

// วางบน prefab จุดที่ลอยไปมา (Sprite + Collider2D ที่ติ๊ก Is Trigger + Rigidbody2D)
// ตอนเริ่มเกม sprite จะถูกย้ายไปไว้ใน child "Visual/Sprite" เอง เพื่อให้บีบ/ยืดตามแกนจอได้ทั้งที่ตัว dot หมุนอยู่
[RequireComponent(typeof(Rigidbody2D))]
public class ChainDot : MonoBehaviour
{
    public ShapeType shape;
    public Explosion explosionPrefab;   // ระเบิดที่จะเกิดเมื่อจุดนี้แตก (ใส่รูปทรงเดียวกัน)
    public float minSpeed = 1f;
    public float maxSpeed = 2f;
    public float maxSpin = 90f;         // ความเร็วหมุนสุ่ม -maxSpin..maxSpin (องศา/วินาที)
    public bool randomColor = false;    // เปิด = สุ่มเฉดเทา (โน้ตเป็นขาวดำอยู่แล้ว ปิดไว้จะได้สีเดิมของรูป)

    [Header("Spawn")]
    public float popTime = 0.35f;       // เวลาเด้งโผล่ตอนเริ่มด่าน

    [Header("Squash (ชนขอบจอ)")]
    [Range(0f, 0.9f)] public float squashAmount = 0.35f;
    public float squashStiffness = 300f;
    public float squashDamping = 10f;

    [Header("Bounce Sound (ชนขอบจอ)")]
    [Range(0f, 1f)] public float bounceVolume = 0.35f;
    public float bounceSoundGap = 0.06f;    // dot เยอะชนพร้อมกัน -> เล่นห่างกันอย่างน้อยเท่านี้ (วินาที) จะได้ไม่ดังรัว

    static float nextBounceSoundTime;       // ใช้ร่วมกันทุก dot

    [Header("Pulse (ระหว่างลอย)")]
    [Range(0f, 0.5f)] public float pulseAmount = 0.06f;
    public float pulseSpeed = 4f;

    [Header("Trail")]
    public float trailTime = 0.25f;
    [Range(0f, 1f)] public float trailAlpha = 0.35f;
    public float trailWidth = 0.7f;     // เทียบกับขนาด dot

    [HideInInspector] public float spawnDelay;  // GameManager ตั้งให้ เพื่อทยอยโผล่ทีละตัว

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform visual;       // ไม่หมุน: ใช้บีบ/ยืด/pulse ตามแกนจอ
    Transform spriteRoot;   // หมุนตามตัว dot
    TrailRenderer trail;
    Vector3 baseScale;
    float age;
    float pulsePhase;
    bool popped;
    Vector2 squash = Vector2.one;
    Vector2 squashVel;
    bool exploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        baseScale = transform.localScale;
        transform.localScale = baseScale * 0.01f;   // ซ่อนไว้ก่อน รอเด้งโผล่
        pulsePhase = Random.Range(0f, Mathf.PI * 2f);

        BuildVisual();
        BuildTrail();
    }

    void BuildVisual()
    {
        SpriteRenderer original = GetComponent<SpriteRenderer>();

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        spriteRoot = new GameObject("Sprite").transform;
        spriteRoot.SetParent(visual, false);

        sr = spriteRoot.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = original.sprite;
        sr.color = original.color;
        sr.sharedMaterial = original.sharedMaterial;
        sr.sortingLayerID = original.sortingLayerID;
        sr.sortingOrder = original.sortingOrder;
        sr.drawMode = original.drawMode;
        sr.size = original.size;
        sr.flipX = original.flipX;
        sr.flipY = original.flipY;
        original.enabled = false;
    }

    void BuildTrail()
    {
        // อยู่ใน child แยก จะได้ปล่อยให้ค่อยๆ จางเองตอน dot แตก
        trail = new GameObject("Trail").AddComponent<TrailRenderer>();
        trail.transform.SetParent(transform, false);
        trail.sharedMaterial = FxMaterial.Sprite;
        trail.time = trailTime;
        trail.minVertexDistance = 0.05f;
        trail.sortingLayerID = sr.sortingLayerID;
        trail.sortingOrder = sr.sortingOrder - 1;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        float size = sr.sprite != null ? sr.sprite.bounds.size.x : 1f;
        trail.widthMultiplier = size * baseScale.x * trailWidth;
        trail.emitting = false;
    }

    void Start()
    {
        Vector2 dir = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) * Vector2.right;
        rb.linearVelocity = dir * Random.Range(minSpeed, maxSpeed);
        rb.angularVelocity = Random.Range(-maxSpin, maxSpin);

        if (randomColor)
            sr.color = Random.ColorHSV(0f, 1f, 0f, 0f, 0.6f, 1f);   // saturation 0 = เทา

        Color c = sr.color;
        c.a = trailAlpha;
        trail.startColor = c;
        c.a = 0f;
        trail.endColor = c;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        // เด้งโผล่ (ease-out-back = ขยายเลยไปนิดแล้วหดกลับ)
        if (!popped)
        {
            float t = (age - spawnDelay) / popTime;
            if (t >= 1f)
            {
                popped = true;
                transform.localScale = baseScale;
                trail.emitting = true;
            }
            else
                transform.localScale = baseScale * Mathf.Max(0.01f, t <= 0f ? 0f : EaseOutBack(t));
        }

        // สปริงดึง squash กลับเป็นปกติ (เด้งดึ๋งๆ นิดหน่อย)
        squashVel += (Vector2.one - squash) * squashStiffness * dt;
        squashVel *= Mathf.Exp(-squashDamping * dt);
        squash += squashVel * dt;
    }

    void LateUpdate()
    {
        float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed + pulsePhase);

        visual.rotation = Quaternion.identity;              // แกนบีบ = แกนจอ
        visual.localScale = new Vector3(squash.x * pulse, squash.y * pulse, 1f);
        spriteRoot.rotation = transform.rotation;           // รูปทรงยังหมุนตามตัว dot
    }

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    void FixedUpdate()
    {
        // เด้งกลับเมื่อถึงขอบจอ
        Rect area = GameManager.Instance.PlayArea;
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;

        if ((pos.x < area.xMin && vel.x < 0f) || (pos.x > area.xMax && vel.x > 0f))
        {
            vel.x = -vel.x;
            Squash(new Vector2(1f - squashAmount, 1f + squashAmount));   // ชนซ้าย/ขวา -> แบนแนวนอน
        }
        if ((pos.y < area.yMin && vel.y < 0f) || (pos.y > area.yMax && vel.y > 0f))
        {
            vel.y = -vel.y;
            Squash(new Vector2(1f + squashAmount, 1f - squashAmount));   // ชนบน/ล่าง -> แบนแนวตั้ง
        }

        rb.linearVelocity = vel;
    }

    void Squash(Vector2 amount)
    {
        squash = amount;
        squashVel = Vector2.zero;
        PlayBounceSound();
    }

    void PlayBounceSound()
    {
        if (!popped || Time.unscaledTime < nextBounceSoundTime) return;   // ยังเด้งโผล่ไม่เสร็จ / เพิ่งมีเสียงไป
        nextBounceSoundTime = Time.unscaledTime + bounceSoundGap;
        SoundManager.Play("Bounce", bounceVolume);
    }

    // คืน true ถ้าแตกจริง (false = แตกไปแล้ว)
    public bool Explode()
    {
        if (exploded) return false;
        exploded = true;

        Explosion explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosion.SetColor(sr.color);

        // ปล่อย trail ไว้ให้จางเอง
        trail.transform.SetParent(null, true);
        trail.emitting = false;
        trail.autodestruct = true;

        GameManager.Instance.OnDotExploded(this);
        Destroy(gameObject);
        return true;
    }
}
