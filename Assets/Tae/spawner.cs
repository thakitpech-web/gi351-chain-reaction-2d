using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SpawnEntry
{
    public GameObject prefab;   // ประเภทมอน
    public int count = 1;       // จำนวน
}
public class spawner : MonoBehaviour
{
    public List<SpawnEntry> entries = new List<SpawnEntry>();
    public float spawnRadius = 5f;

    private List<GameObject> alive = new List<GameObject>();
    private bool reloading = false;

    void Start()
    {
        foreach (var e in entries)
        {
            if (e.prefab == null) continue;
            //อ่านค่าจากใน prefab และเสกตาม
            for (int i = 0; i < e.count; i++)
            {
                Vector3 pos = transform.position + (Vector3)(Random.insideUnitCircle * spawnRadius);
                alive.Add(Instantiate(e.prefab, pos, Quaternion.identity));
            }

        }
    }
   // ตอนสปาว:  alive = [Slime1, Slime2, Slime3]             ? ทุกช่องมีตัวจริง
   //Slime2 ตาย(Destroy) :  alive = [Slime1, null, Slime3]   ? ช่องของ Slime2 กลายเป็น null
   //Update ทำงาน RemoveAll: alive = [Slime1, Slime3]        ? ลบช่อง null ออก
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
    void Update()
    {
        if (reloading || alive.Count == 0) return; // ไม่ได้สปาวอะไรเลย ก็ไม่รีโหลด

        alive.RemoveAll(m => m == null); // มอนที่ถูก Destroy จะเป็น null

        if (alive.Count == 0)
        {
            reloading = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
