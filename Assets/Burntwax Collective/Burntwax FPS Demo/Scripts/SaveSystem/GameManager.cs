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

        private bool isSaving;
        private bool isLoading;


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

            if (loadScene)
            {
                Debug.Log("Loading Scene...");
                LoadAsync();
                loadScene = false;
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
            isLoading = true;
            await SaveSystem.LoadAsync();
            isLoading = false;
        }
    }

}