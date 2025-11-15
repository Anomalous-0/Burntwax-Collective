using UnityEngine;

[System.Serializable]
public class GameData {

    [Header("Player Data")]
    public Vector3 playerPos;
    public Vector3 playerOrientation;

    [Header("Camera Data")]
    public float yaw;
    public float pitch;

    public GameData() {
        playerPos = Vector3.zero;
        playerOrientation = Vector3.zero;
        yaw = 0;
        pitch = 0;
    }
}
