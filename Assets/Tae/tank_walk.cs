using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class tank_walk : MonoBehaviour
{
    public int speed;
    public Rigidbody2D rb;

    Vector2 moveinput;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        moveinput = new Vector2(x, y).normalized;
    }
    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveinput * speed * Time.deltaTime );
    }
}
