using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Burntwax
{
    public class GameManager : MonoBehaviour
    {

        public bool IsLoadingFromSave { get; set; }
        public static GameManager Instance;

        public bool loadScene;

        public SceneData SceneData { get; set; }

        public string currentSceneID;

        public bool isSaving;
        public bool isLoading;


        private void Awake()
        {

            DontDestroyOnLoad(gameObject);

            if (Instance != null && Instance != this)
            {

                Destroy(gameObject);
                return;
            }
            Instance = this;

        }

        private void Start()
        {
            var sceneData = SaveSystem.GetSavedSceneData();

            if (sceneData.HasValue)
            {
                currentSceneID = sceneData.Value.ID;
                // Debug.Log($"Loaded last saved scene ID: {currentSceneID}");
            }
        }

        private void Update()
        {
            if (InputManager.Instance)
            {
                if (InputManager.Instance.saveIsPressed)
                {
                    SaveAsync();
                }
                if (InputManager.Instance.loadIsPressed)
                {
                    if (currentSceneID != SceneManager.GetActiveScene().name)
                    {
                        LoadAsync();
                    }
                    else
                    {
                        SaveSystem.Load();
                    }
                }
            }
        }

        public async void SaveAsync()
        {
            isSaving = true;
            await SaveSystem.SaveAsynchronously();
            isSaving = false;
        }

        public async void LoadAsync()
        {
            await SaveSystem.LoadAsync();
            
        }
    }

}