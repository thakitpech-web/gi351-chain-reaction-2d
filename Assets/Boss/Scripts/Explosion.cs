using UnityEngine;

// วางบน prefab ระเบิด (Sprite + Collider2D ที่ติ๊ก Is Trigger + Rigidbody2D)
// รูปทรงของระเบิดมาจาก sprite/collider ใน prefab ส่วนสคริปต์นี้แค่ขยาย-ค้าง-หด
[RequireComponent(typeof(Rigidbody2D))]
public class Explosion : MonoBehaviour
{
    public float maxSize = 2.5f;     // ขนาดใหญ่สุด (หน่วย world)
    public float growTime = 0.4f;
    public float holdTime = 1f;
    public float shrinkTime = 0.4f;
    public float rotateSpeed = 0f;   // องศา/วินาที (ใช้กับสามเหลี่ยม/สี่เหลี่ยมให้หมุนกวาด)
    [Range(0f, 1f)] public float alpha = 0.5f;
    [Range(0f, 1f)] public float shake = 0.4f;  // จอสั่นตอนเกิดระเบิด (สะสมเมื่อระเบิดต่อกันหลายลูก)

    SpriteRenderer sr;
    float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        transform.localScale = Vector3.one * 0.01f;
        GameManager.Instance.RegisterExplosion(this);
        ScreenShake.Add(shake);
    }

    // Start มาหลัง SetColor แล้ว จะได้สีจริงของระเบิด
    void Start()
    {
        BackgroundFx.OnExplosion(transform.position, sr.color);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterExplosion(this);
    }

    public void SetColor(Color color)
    {
        color.a = alpha;
        sr.color = color;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float size;

        if (timer < growTime)
            size = Mathf.SmoothStep(0f, maxSize, timer / growTime);
        else if (timer < growTime + holdTime)
            size = maxSize;
        else if (timer < growTime + holdTime + shrinkTime)
            size = Mathf.SmoothStep(maxSize, 0f, (timer - growTime - holdTime) / shrinkTime);
        else
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = Vector3.one * Mathf.Max(size, 0.01f);
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    // อะไรที่เป็น ChainDot แตะระเบิด -> ระเบิดต่อ (chain reaction)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out ChainDot dot))
            dot.Explode();
    }
}
