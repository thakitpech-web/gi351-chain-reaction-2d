using UnityEngine;

// วางบน prefab กระสุน (Sprite + Collider2D ที่ติ๊ก Is Trigger + Rigidbody2D)
[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float offscreenMargin = 1f;  // ออกนอกจอเกินระยะนี้แล้วลบทิ้ง

    Rigidbody2D rb;
    bool hit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        GameManager.Instance.RegisterBullet(this);
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
            dot.Explode();
            Destroy(gameObject);
        }
    }
}
