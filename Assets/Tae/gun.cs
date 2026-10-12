using UnityEngine;

public class gun : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //แปรงต่ำแหน่งเมาส์>ต่ำแหน่งในเกม
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //ทิศทางจากปืนไปเมาส์
        Vector2 direction = (Vector2)(mousePosition - transform.position);
        // คำนวณมุม
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // หมุนกระบอกปืนตามมุม
        transform.rotation = Quaternion.Euler(0f, 0f, angle + -90f);
    }
}
