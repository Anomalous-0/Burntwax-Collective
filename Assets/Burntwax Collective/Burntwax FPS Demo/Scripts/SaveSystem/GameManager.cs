using UnityEngine;
using System.Threading.Tasks;

namespace Burntwax
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public bool loadScene;

        public SceneData SceneData { get; set; }

        private bool isSaving;
        private bool isLoading;
        

        private void Awake()
        {

            DontDestroyOnLoad(gameObject);

            if (Instance != null && Instance != this)
            {

                Destroy(gameObject);
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
                    LoadAsync();
                }
            }

            if (loadScene)
            {
                Debug.Log("Loading Scene...");
                LoadAsync();
                loadScene = false;
            }
        }

        private async void SaveAsync()
        {
            isSaving = true;
            await SaveSystem.SaveAsynchronously();
            isSaving = false;
        }

        private async void LoadAsync()
        {
            isLoading = true;
            await SaveSystem.LoadAsync();
            isLoading = false;
        }
    }

}