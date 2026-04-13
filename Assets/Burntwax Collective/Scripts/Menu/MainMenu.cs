using UnityEngine;
using UnityEngine.SceneManagement;

namespace Burntwax
{
    public class MainMenu : MonoBehaviour
    {

        public static MainMenu Instance;
        public bool loadScene;


        //Load Scene

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Found more than one Main Menu in the scene");
                Destroy(gameObject);
            }
            Instance = this;
        }
        public void NewGame()
        {
            SceneManager.LoadSceneAsync(2);
            Time.timeScale = 1f;
            AudioListener.pause = false;

        }

        // public void Resume()
        // {
        //     GameManager.Instance.loadScene = true;
        //     Time.timeScale = 1f;
        //     AudioListener.pause = false;

        // }

        public async void Resume()
        {
            if (!SaveSystem.SaveExists())
            {
                Debug.LogWarning("No save file found.");
                return;
            }

            Time.timeScale = 1f;
            AudioListener.pause = false;
            GameManager.Instance.LoadAsync();
        }

        public void Quit()
        {
            Application.Quit();
        }

        public void Options()
        {

        }
    }
}