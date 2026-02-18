using UnityEngine;


namespace Burntwax
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public bool loadScene;

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
                    SaveSystem.Save();
                }
                if (InputManager.Instance.loadIsPressed)
                {
                    SaveSystem.Load();
                }
            }

            if (loadScene)
            {
                Debug.Log("Loading Scene...");
                SaveSystem.Load();
                loadScene = false;
            }
        }
    }

}