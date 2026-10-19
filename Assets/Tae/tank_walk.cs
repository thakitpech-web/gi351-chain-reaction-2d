using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

public class tank_walk : MonoBehaviour
{

    public int speed;

    public Rigidbody2D rb;
    public int cureent = 0;
    public bool Isfinish1 = false;
    public bool Isfinish2 = false;
    public bool Isfinish3 = false;
    public bool Isfinish4 = false;
    public bool Isfinish5 = false;
    public bool Isfinish6 = false;

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
        if(cureent >= 5)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);

        }
    }
    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveinput * speed * Time.deltaTime );
    }
}
