using UnityEngine;

public class bomb_effect : MonoBehaviour
{
   public GameObject bomb;
    public object_Walk Object;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Object = FindAnyObjectByType<object_Walk>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("object"))
        {
            GameObject effect = Instantiate(bomb,collision.transform.position, Quaternion.identity);
            Destroy(effect, 0.4f);
            Destroy(collision.gameObject.gameObject);
        }
    }
}
