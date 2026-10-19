using System.Collections;
using UnityEngine;

// Player เดินด้วย WASD ยิงกระสุนไปทางเมาส์ และเสียเลือดเมื่อชน dot
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    public float speed = 5f;

    [Header("Shoot")]
    public Bullet bulletPrefab;
    public Transform gun;               // (ไม่ใส่ก็ได้) ส่วนที่หมุนตามเมาส์ ให้ sprite ชี้ไปทางขวา (+X)
    public Transform firePoint;         // (ไม่ใส่ก็ได้) จุดที่กระสุนออก ถ้าไม่ใส่จะออกจากกลางตัว
    public float bulletSpeed = 12f;
    public float fireCooldown = 0.2f;

    [Header("Health")]
    public int maxHealth = 3;
    public float invincibleTime = 1.5f;
    public float blinkInterval = 0.1f;

    Rigidbody2D rb;
    SpriteRenderer[] renderers;
    Vector2 moveInput;
    float nextFireTime;
    int health;
    bool invincible;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    public void ResetForLevel(Vector2 position)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        SetVisible(true);
        invincible = false;

        transform.position = position;
        rb.position = position;

        health = maxHealth;
        GameManager.Instance.OnPlayerHealthChanged(health, maxHealth);
    }

    void Update()
    {
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        Vector2 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aim = mouse - (Vector2)transform.position;
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;

        if (gun != null)
            gun.rotation = Quaternion.Euler(0f, 0f, angle);

        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
            Shoot(aim.normalized, angle);
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position + moveInput * speed * Time.fixedDeltaTime;

        Rect area = GameManager.Instance.PlayArea;
        pos.x = Mathf.Clamp(pos.x, area.xMin, area.xMax);
        pos.y = Mathf.Clamp(pos.y, area.yMin, area.yMax);
        rb.MovePosition(pos);
    }

    void Shoot(Vector2 direction, float angle)
    {
        if (!GameManager.Instance.TryUseAmmo()) return;
        nextFireTime = Time.time + fireCooldown;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Bullet bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.Euler(0f, 0f, angle));
        bullet.Launch(direction * bulletSpeed);
    }

    // ---------- เลือด ----------

    void OnTriggerEnter2D(Collider2D other) { CheckHit(other); }
    void OnTriggerStay2D(Collider2D other) { CheckHit(other); }

    void CheckHit(Collider2D other)
    {
        if (other.GetComponent<ChainDot>() != null)
            TakeDamage(1);
    }

    public void TakeDamage(int damage)
    {
        if (invincible || health <= 0) return;

        health -= damage;
        GameManager.Instance.OnPlayerHealthChanged(health, maxHealth);

        if (health <= 0)
        {
            gameObject.SetActive(false);
            GameManager.Instance.OnPlayerDied();
            return;
        }
        StartCoroutine(InvincibleRoutine());
    }

    // อมตะชั่วคราว + กะพริบ หลังโดนชน
    IEnumerator InvincibleRoutine()
    {
        invincible = true;
        bool visible = true;

        for (float t = 0f; t < invincibleTime; t += blinkInterval)
        {
            visible = !visible;
            SetVisible(visible);
            yield return new WaitForSeconds(blinkInterval);
        }

        SetVisible(true);
        invincible = false;
    }

    void SetVisible(bool value)
    {
        foreach (SpriteRenderer r in renderers)
            r.enabled = value;
    }
}
