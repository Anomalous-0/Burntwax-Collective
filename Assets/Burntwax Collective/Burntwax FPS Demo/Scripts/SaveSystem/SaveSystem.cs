using UnityEngine;
using System.IO;
using System.Threading.Tasks;

//If you need to find the save file --> C:\Users\*INSERT USER NAME HERE*\AppData\LocalLow\Burntwax Collective\Burntwax Demo
namespace Burntwax
{
    public class SaveSystem
    {

        private static SaveData _saveData = new SaveData();
        public static bool loadScene;

        [System.Serializable]
        public struct SaveData
        {
            public SceneSaveData SceneData;
            public PlayerSaveData PlayerData;
            public CamSaveData CamData;
            public GunSaveData GunData;
            public PlayerHealthSaveData PlayerHealthData;
            public ObjectConsumableData ObjectConsumableData;

        }

        public static bool SaveExists()
        {
            return File.Exists(SaveFileName());
        }

        public static string SaveFileName()
        {
            string saveFile = Application.persistentDataPath + "/save" + ".GAME";
            return saveFile;
        }

        public static void Save()
        {
            HandleSaveData();
            string json = JsonUtility.ToJson(_saveData);
            string encryptedData = EncryptionUtility.EncryptString(json);

            File.WriteAllText(SaveFileName(), encryptedData);
        }


        public static async Task SaveAsynchronously()
        {
            GameManager.Instance.isSaving = true;
            await SaveAsync();
            GameManager.Instance.isSaving = false;
        }

        private static async Task SaveAsync()
        {
            HandleSaveData();
            string json = JsonUtility.ToJson(_saveData);
            string encryptedData = EncryptionUtility.EncryptString(json);

            await File.WriteAllTextAsync(SaveFileName(), encryptedData);
        }

        private static void HandleSaveData()
        {
            GameManager.Instance.SceneData.Save(ref _saveData.SceneData);

            PlayerStateMachine player = GameManager.FindFirstObjectByType<PlayerStateMachine>();
            if (player != null)
                player.SaveData(ref _saveData.PlayerData);

            player.GetComponent<CamStateMachine>().SaveData(ref _saveData.CamData);
            player.GetComponent<PlayerHealth>().SaveData(ref _saveData.PlayerHealthData);

            GunStateMachine gun = GameManager.FindFirstObjectByType<GunStateMachine>();
            if (gun)
                gun.SaveData(ref _saveData.GunData);


            ObjectManager.Instance.SaveData(ref _saveData.ObjectConsumableData);
        }



        public static async Task LoadAsync()
        {
            GameManager.Instance.isLoading = true;
            string saveData = File.ReadAllText(SaveFileName());
            string decryptedData = EncryptionUtility.DecryptString(saveData);
            _saveData = JsonUtility.FromJson<SaveData>(decryptedData);

            await HandleLoadDataAsync();
            GameManager.Instance.isLoading = false;

        }

        private static async Task HandleLoadDataAsync()
        {
            GameManager.Instance.IsLoadingFromSave = true;
            await GameManager.Instance.SceneData.LoadAsync(_saveData.SceneData);

            // GameManager.Instance.SceneData.Load(_saveData.SceneData);

            PlayerStateMachine player = GameManager.FindFirstObjectByType<PlayerStateMachine>();
            if (player)
            {
                player.LoadData(_saveData.PlayerData);
                player.GetComponent<CamStateMachine>().LoadData(_saveData.CamData);
                player.GetComponent<PlayerHealth>().LoadData(_saveData.PlayerHealthData);

            }

            GunStateMachine gun = GameManager.FindFirstObjectByType<GunStateMachine>();
            if (gun)
            {
                gun.LoadData(_saveData.GunData);
            }

            ObjectManager.Instance.LoadData(_saveData.ObjectConsumableData);
            GameManager.Instance.IsLoadingFromSave = false;
        }
        public static void Load()
        {
            string saveData = File.ReadAllText(SaveFileName());
            string decryptedData = EncryptionUtility.DecryptString(saveData);
            _saveData = JsonUtility.FromJson<SaveData>(decryptedData);
            HandleLoadData();
        }
        private static void HandleLoadData()
        {

            // GameManager.Instance.SceneData.Load(_saveData.SceneData);

            PlayerStateMachine player = GameManager.FindFirstObjectByType<PlayerStateMachine>();
            if (player)
            {
                player.LoadData(_saveData.PlayerData);
                player.GetComponent<CamStateMachine>().LoadData(_saveData.CamData);
                player.GetComponent<PlayerHealth>().LoadData(_saveData.PlayerHealthData);

            }

            GunStateMachine gun = GameManager.FindFirstObjectByType<GunStateMachine>();
            if (gun)
            {
                gun.LoadData(_saveData.GunData);
            }
            ObjectManager.Instance.LoadData(_saveData.ObjectConsumableData);
        }


        public static SceneSaveData? GetSavedSceneData()
        {
            string path = SaveFileName();

            if (!File.Exists(path))
                return null;

            string encrypted = File.ReadAllText(path);
            string decrypted = EncryptionUtility.DecryptString(encrypted);

            SaveData data = JsonUtility.FromJson<SaveData>(decrypted);

            return data.SceneData;
        }


    }

}