using UnityEngine;

// วางบน prefab จุดที่ลอยไปมา (Sprite + Collider2D ที่ติ๊ก Is Trigger + Rigidbody2D)
[RequireComponent(typeof(Rigidbody2D))]
public class ChainDot : MonoBehaviour
{
    public ShapeType shape;
    public Explosion explosionPrefab;   // ระเบิดที่จะเกิดเมื่อจุดนี้แตก (ใส่รูปทรงเดียวกัน)
    public float minSpeed = 1f;
    public float maxSpeed = 2f;
    public float maxSpin = 90f;         // ความเร็วหมุนสุ่ม -maxSpin..maxSpin (องศา/วินาที)
    public bool randomColor = true;

    Rigidbody2D rb;
    SpriteRenderer sr;
    bool exploded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    void Start()
    {
        Vector2 dir = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)) * Vector2.right;
        rb.linearVelocity = dir * Random.Range(minSpeed, maxSpeed);
        rb.angularVelocity = Random.Range(-maxSpin, maxSpin);

        if (randomColor)
            sr.color = Random.ColorHSV(0f, 1f, 0.5f, 0.8f, 0.9f, 1f);
    }

    void FixedUpdate()
    {
        // เด้งกลับเมื่อถึงขอบจอ
        Rect area = GameManager.Instance.PlayArea;
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;

        if ((pos.x < area.xMin && vel.x < 0f) || (pos.x > area.xMax && vel.x > 0f)) vel.x = -vel.x;
        if ((pos.y < area.yMin && vel.y < 0f) || (pos.y > area.yMax && vel.y > 0f)) vel.y = -vel.y;

        rb.linearVelocity = vel;
    }

    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        Explosion explosion = Instantiate(explosionPrefab, transform.position, transform.rotation);
        explosion.SetColor(sr.color);

        GameManager.Instance.OnDotExploded(this);
        Destroy(gameObject);
    }
}
