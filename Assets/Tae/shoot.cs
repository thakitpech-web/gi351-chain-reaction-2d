using UnityEngine;

public class shoot : MonoBehaviour
{
    public bool hasShoot = true;
    public Transform Fire;
    public GameObject shooting;
    public GameObject bullet;
    Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }



    // Update is called once per frame
    void Update()
    {

        if (hasShoot == true)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Shoot();
            }
        }
    }
    public void Shoot()
    {
        shooting = Instantiate(bullet, Fire.position, Fire.rotation);
        rb = shooting.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = (Vector2)Fire.up * 10;
        }
        hasShoot = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            Destroy(gameObject);
            hasShoot = true;
        }
    }
}

