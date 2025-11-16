using UnityEngine;
using System.IO;
public class SaveSystem {
    
    private static SaveData _saveData = new SaveData();
    
    [System.Serializable]
    public struct SaveData {
        public PlayerSaveData PlayerData;
        public CamSaveData CamData;

        public GunSaveData GunData;
        public PlayerHealthSaveData PlayerHealthData;


    }

    public static string SaveFileName() {
        string saveFile = Application.persistentDataPath + "/savefil" + ".save";
        return saveFile;
    }

    public static void Save() {
        HandleSaveData();
        File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));
    }

    private static void HandleSaveData() {
        GameManager.Instance.player.SaveData(ref _saveData.PlayerData);
        GameManager.Instance.player.GetComponent<CamStateMachine>().SaveData(ref _saveData.CamData);
        GameManager.Instance.player.GetComponent<PlayerHealth>().SaveData(ref _saveData.PlayerHealthData);
        GameManager.Instance.gun.SaveData(ref _saveData.GunData);

    }

    public static void Load() {
        string saveContent = File.ReadAllText(SaveFileName());
        _saveData = JsonUtility.FromJson<SaveData>(saveContent);
        HandleLoadData();
    }

    private static void HandleLoadData() {
        GameManager.Instance.player.LoadData(_saveData.PlayerData);
        GameManager.Instance.player.GetComponent<CamStateMachine>().LoadData(_saveData.CamData);
        GameManager.Instance.player.GetComponent<PlayerHealth>().LoadData(_saveData.PlayerHealthData);
        GameManager.Instance.gun.LoadData(_saveData.GunData);
    }
}
