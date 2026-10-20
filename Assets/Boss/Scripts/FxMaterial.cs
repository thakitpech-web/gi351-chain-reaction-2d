using UnityEngine;

// material กลางสำหรับเส้น / trail / sprite ที่สร้างจากโค้ด (ใช้ร่วมกันทุกที่ จะได้ไม่สร้างซ้ำ)
public static class FxMaterial
{
    static Material sprite;

    public static Material Sprite
    {
        get
        {
            if (sprite == null) sprite = new Material(Shader.Find("Sprites/Default"));
            return sprite;
        }
    }
}
