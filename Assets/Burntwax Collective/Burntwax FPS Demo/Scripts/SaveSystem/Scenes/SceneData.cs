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

        public async Task LoadAsync(SceneSaveData saveData)
        {
            var tcs = new TaskCompletionSource<bool>();

            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> handler = null;
            handler = (scene, mode) =>
            {
                tcs.SetResult(true);
                SceneManager.sceneLoaded -= handler;
            };

            SceneManager.sceneLoaded += handler;

            await GameManager.Instance.GetComponent<SceneLoader>()
                .LoadSceneByIndexAsync(saveData.ID);

            await tcs.Task; 
        }

    



        #endregion
    }

    [System.Serializable]
    public struct SceneSaveData
    {
        public string ID;
    }
}