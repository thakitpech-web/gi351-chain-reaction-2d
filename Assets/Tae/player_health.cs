using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class player_health : MonoBehaviour
{
    public int maxHealth;
    public float invincibleTime = 2f;
    public float blinkInterval = 0.1f;  // ความเร็วกะพริบ

    int currentHealth;
    bool isInvincible;
    SpriteRenderer[] sr;

    private void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponentsInChildren<SpriteRenderer>();
    }
   public void takeDamage(int damage)
    {
        if (isInvincible) return;   // อยู่ในไอเฟรม ไม่โดนดาเมจ
        currentHealth -= damage;
        Debug.Log("HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }
        StartCoroutine(InvincibleRoutine());
    }
    IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        float timer = 0f;
        bool visible = true;

        while (timer < invincibleTime)
        {
            visible = !visible;
            SetVisible(visible);
            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        SetVisible(true);
        isInvincible = false;
    }

    void SetVisible(bool value)
    {
        foreach (var r in sr)
            r.enabled = value;
    }

    void Die()
    {
        Destroy(gameObject);   // เปลี่ยนเป็น game over ตามที่ต้องการ
    }
    private void Update()
    {
        if (currentHealth == 0)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
  
