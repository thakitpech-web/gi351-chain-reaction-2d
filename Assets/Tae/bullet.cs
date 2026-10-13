using UnityEngine;

public class bullet : MonoBehaviour
{
    public shoot Shoot;
    public object_Walk Object;
    public GameObject bomb;
    public void Start()
    {
      Shoot = FindAnyObjectByType<shoot>();
      Object = FindAnyObjectByType<object_Walk>();
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("wall"))
        {
            Shoot.hasShoot = true;
            Destroy(gameObject);
        }
        if (collision.gameObject.CompareTag("object"))
        {
            GameObject effect = Instantiate(bomb,transform.position,Quaternion.identity);
            Destroy(effect,0.4f);
            Destroy(collision.gameObject);
            Shoot.hasShoot = true;
            Destroy(gameObject);
        }
    }
}
