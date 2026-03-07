using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace Burntwax
{
    public class SceneData : MonoBehaviour
    {
        public SceneDataSO data;

        void Awake()
        {
            GameManager.Instance.SceneData = this;
        }


        #region Save and Load

        public void Save(ref SceneSaveData saveData)
        {
            saveData.ID = data.Name;
            GameManager.Instance.currentSceneID = data.Name;
            Debug.Log($"Saving Scene with ID: {saveData.ID}");
        }

        public void Load(SceneSaveData saveData)
        {
            GameManager.Instance.GetComponent<SceneLoader>().LoadSceneByIndex(saveData.ID);
        }

        public async Task LoadAsync(SceneSaveData saveData)
        {
            await GameManager.Instance.GetComponent<SceneLoader>().LoadSceneByIndexAsync(saveData.ID);
        }

        public Task WaitForSceneFullLoad()
        {
            TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoaderHandler = null;
            sceneLoaderHandler = (scene, mode) =>
            {
                taskCompletion.SetResult(true);
                SceneManager.sceneLoaded -= sceneLoaderHandler;
            };

            SceneManager.sceneLoaded += sceneLoaderHandler;

            return taskCompletion.Task;
        }



        #endregion
    }

    [System.Serializable]
    public struct SceneSaveData
    {
        public string ID;
    }
}