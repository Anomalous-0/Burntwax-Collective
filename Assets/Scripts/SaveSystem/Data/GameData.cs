using UnityEngine;

[System.Serializable]
public class GameData {
    public Vector3 playerPos;

    public GameData() {
        this.playerPos = Vector3.zero;
    }
}
