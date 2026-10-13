using UnityEngine;
using System.Collections;
using TMPro;

public class object_Walk : MonoBehaviour
{
    int direction;
    Vector2 lastPosition;
    Vector2 newPosition;
    Rigidbody2D rb;
    public float maxDistance;
    public int speed;
    public float minDistance;
    public bool Isfinish = false;
    public GameObject boom;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        randomdistance();
    }
    void randomdistance()
    {
        int random = Random.Range(1,3);
        if (random == 2)
        {

            direction = 1;
        }
        if (random == 1)
        {

            direction = -1;
        }

            
        
        newPosition = new Vector2(Random.Range(minDistance, maxDistance + 1f), Random.Range(minDistance, maxDistance + 1f));
        lastPosition = rb.position + direction * newPosition;
        Isfinish = false;
    }
        // Update is called once per frame
        void FixedUpdate()
        {

            if (!Isfinish)
            {
                rb.MovePosition(Vector2.MoveTowards(rb.position, lastPosition, speed * Time.deltaTime));
            }
            if (Vector3.Distance(rb.position, lastPosition) < 0.01f)
            {
          
                Isfinish = true;
                randomdistance();
            }
        }
    void OnCollisionEnter2D(Collision2D col) { HandleHit(col); }
    void OnCollisionStay2D(Collision2D col) { HandleHit(col); }

    void HandleHit(Collision2D col)
    {
        //ถ้าไม่ได้ชน2อย่างนี้ รีเทิร์น
        if (!(col.gameObject.CompareTag("wall") || col.gameObject.CompareTag("object")))
            return;
        float nextTime = 1;
        // กันเปลี่ยนทิศรัวทุกเฟรม
        if (Time.time < nextTime) return;
        nextTime = Time.time + 1f ;

        // normal ชี้ออกจากสิ่งที่ชน
        Vector2 normal = col.GetContact(0).normal;

        // หมุนเล็กน้อยรอบทิศที่ชี้ออก ไม่ให้เดินตรงเป๊ะซ้ำๆ
        Vector2 dir = Quaternion.Euler(0, 0, Random.Range(-60f, 60f)) * normal;

        lastPosition = rb.position + dir.normalized * Random.Range(minDistance, maxDistance);
        Isfinish = false;
    }
} 
