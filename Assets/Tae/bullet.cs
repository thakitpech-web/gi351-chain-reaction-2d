using UnityEngine;

public class bullet : MonoBehaviour
{
    public shoot Shoot;
    public void Start()
    {
      Shoot = FindAnyObjectByType<shoot>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            Shoot.hasShoot = true;
            Destroy(gameObject);
        }
    }
}
