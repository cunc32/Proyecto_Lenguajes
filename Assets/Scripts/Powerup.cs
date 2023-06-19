using UnityEngine;

public enum PowerupType
{
    None = 0,
    Teleport = 1,
    Invincible = 2,
    OtherMove = 3,
    Shambles = 4,
    DoubleAction = 5,
    TeleportOponent = 6,
    OneShot = 7,
    Freeze = 8,
    Minimize = 9,
    Promotion = 10
}

public class Powerup : MonoBehaviour
{
    public int currentX;
    public int currentY;
    public PowerupType type;
    
    void Update()
    {
        
    }
}
