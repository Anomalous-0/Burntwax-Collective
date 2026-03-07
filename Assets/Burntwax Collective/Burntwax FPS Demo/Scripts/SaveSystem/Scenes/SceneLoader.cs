using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Burntwax
{

    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private SceneDataSO[] sceneDataSOArray;

        private Dictionary<string, int> sceneIDToIndexMap = new Dictionary<string, int>();



        private void Awake()
        {

            PopulateSceneMapping();
        }

        private void PopulateSceneMapping()
        {
            foreach (var sceneDataSO in sceneDataSOArray)
            {
                sceneIDToIndexMap[sceneDataSO.Name] = sceneDataSO.index;
            }
        }


        public void LoadSceneByIndex(string sceneID)
        {
            if (sceneIDToIndexMap.TryGetValue(sceneID, out int sceneIndex))
            {
                SceneManager.LoadScene(sceneIndex);
            }
            else
            {
                Debug.LogError($"Scene ID {sceneID} not found in mapping.");
            }
        }

        public async Task LoadSceneByIndexAsync(string sceneID)
        {
            if (sceneIDToIndexMap.TryGetValue(sceneID, out int sceneIndex))
            {
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

                while (!asyncLoad.isDone)
                {
                    if (asyncLoad.progress >= 0.9f)
                    {
                        asyncLoad.allowSceneActivation = true;
                        break;
                    }
                    await Task.Yield();
                }
            }
            else
            {
                Debug.LogError($"Scene ID {sceneID} not found in mapping.");
            }
        }
    }
}