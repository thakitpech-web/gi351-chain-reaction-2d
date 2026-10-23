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
    [Range(0f, 1f)] public float shootShake = 0.1f;

    [Header("Shoot Feel")]
    public float recoilForce = 4f;      // ตัว player ถูกถีบถอยหลัง (ความเร็วตอนเริ่ม หน่วย/วินาที)
    public float recoilDecay = 10f;     // แรงถีบหายเร็วแค่ไหน
    public float gunKick = 0.3f;        // ปืนกระตุกถอยหลังเท่าไหร่ (local)
    public float gunKickRecover = 12f;
    public float muzzleFlashSize = 0.5f;
    public Color muzzleFlashColor = new Color(1f, 0.95f, 0.6f);

    [Header("Health")]
    public int maxHealth = 3;
    public float invincibleTime = 1.5f;
    public float blinkInterval = 0.1f;
    public Color hitColor = new Color(1f, 0.25f, 0.25f);   // สีตอนกะพริบหลังโดนชน
    [Range(0f, 1f)] public float hitShake = 0.6f;
    public float hitStop = 0.08f;       // หยุดเวลาแว้บตอนโดนชน (วินาที)

    Rigidbody2D rb;
    SpriteRenderer[] renderers;
    Color[] baseColors;
    Vector2 moveInput;
    float nextFireTime;
    int health;
    bool invincible;
    Vector2 recoil;
    float kick;
    Vector3 gunBasePos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
        if (gun != null) gunBasePos = gun.localPosition;
    }

    public void ResetForLevel(Vector2 position)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        SetHurt(false);
        invincible = false;
        recoil = Vector2.zero;
        kick = 0f;

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
        {
            gun.rotation = Quaternion.Euler(0f, 0f, angle);
            kick = Mathf.Lerp(kick, 0f, 1f - Mathf.Exp(-gunKickRecover * Time.deltaTime));
            gun.localPosition = gunBasePos - gun.localRotation * Vector3.right * kick;
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
            Shoot(aim.normalized, angle);
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position + (moveInput * speed + recoil) * Time.fixedDeltaTime;
        recoil *= Mathf.Exp(-recoilDecay * Time.fixedDeltaTime);

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
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Bullet bullet = Instantiate(bulletPrefab, spawnPos, rotation);
        bullet.Launch(direction * bulletSpeed);

        ScreenShake.Add(shootShake);
        recoil = -direction * recoilForce;
        kick = gunKick;

        SpriteRenderer bulletSr = bulletPrefab.GetComponent<SpriteRenderer>();
        if (bulletSr != null)
            MuzzleFlash.Spawn(spawnPos, rotation, bulletSr.sprite, muzzleFlashColor, muzzleFlashSize,
                              bulletSr.sortingOrder + 10);
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
        if (invincible || health <= 0 || !GameManager.Instance.IsPlaying) return;

        health -= damage;
        GameManager.Instance.OnPlayerHealthChanged(health, maxHealth);
        ScreenShake.Add(hitShake);
        TimeFx.HitStop(hitStop);
        DamageVignette.Flash();

        if (health <= 0)
        {
            gameObject.SetActive(false);
            GameManager.Instance.OnPlayerDied();
            return;
        }
        StartCoroutine(InvincibleRoutine());
    }

    // อมตะชั่วคราว + กะพริบแดง หลังโดนชน
    IEnumerator InvincibleRoutine()
    {
        invincible = true;
        bool hurt = false;

        for (float t = 0f; t < invincibleTime; t += blinkInterval)
        {
            hurt = !hurt;
            SetHurt(hurt);
            yield return new WaitForSeconds(blinkInterval);
        }

        SetHurt(false);
        invincible = false;
    }

    void SetHurt(bool value)
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = value ? hitColor : baseColors[i];
    }
}
