using UnityEngine;

public class bomb_effect : MonoBehaviour
{
    public tank_walk tank;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tank = FindAnyObjectByType<tank_walk>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (gameObject.CompareTag("Circle") && collision.CompareTag("Square") || gameObject.CompareTag("Square") && collision.CompareTag("Circle"))
        {
            if (!tank.Isfinish1)
            {
                tank.cureent += 1;
                tank.Isfinish1 = true;
            }
        }
        if (gameObject.CompareTag("Triangle") && collision.CompareTag("Square") || gameObject.CompareTag("Square") && collision.CompareTag("Triangle"))
        {

            if (!tank.Isfinish2)
            {
                tank.cureent += 1;
                tank.Isfinish2 = true;
            }
        }
        if (gameObject.CompareTag("Triangle") && collision.CompareTag("Circle") || gameObject.CompareTag("Circle") && collision.CompareTag("Triangle"))
        {
            if (!tank.Isfinish3)
            {
                tank.cureent += 1;
                tank.Isfinish3 = true;
            }
        }
        if (collision.gameObject.CompareTag("object"))
        {
            object_Walk target = collision.GetComponent<object_Walk>();
            GameObject effect = Instantiate(target.boom, collision.transform.position, Quaternion.identity);
            Destroy(effect, 0.4f);
            Destroy(collision.gameObject.gameObject);
        }
    }
   
}
