using UnityEngine;
using UnityEngine.SceneManagement;


namespace Burntwax
{
    public class PauseMenu : MonoBehaviour
    {

        public static PauseMenu Instance;
        [Header("Camera stuff")]

#if CINEMACHINE
        public CamStateMachine camStateMachine;
#endif

        public GameObject pauseMenuUI;
        public bool Paused;

        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Found more than one Pause Menu in the scene");
                Destroy(gameObject);
            }
            Instance = this;
        }
        public void Update()
        {


            if (InputManager.Instance.pauseIsPressed)
            {

                InputManager.Instance.pauseIsPressed = false;
                if (Paused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }

            }

        }
        public void Resume()
        {
            pauseMenuUI.SetActive(false);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Paused = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

#if CINEMACHINE
            camStateMachine.fpsCam.gameObject.SetActive(true);
            camStateMachine.aimCam.gameObject.SetActive(true);
            camStateMachine.chargeCam.gameObject.SetActive(true);
#endif
        }

        private void Pause()
        {
            pauseMenuUI.SetActive(true);

            Time.timeScale = 0f;
            AudioListener.pause = true;

#if CINEMACHINE
            camStateMachine.fpsCam.gameObject.SetActive(false);
            camStateMachine.aimCam.gameObject.SetActive(false);
            camStateMachine.chargeCam.gameObject.SetActive(false);
#endif
            Paused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        public void Save()
        {
            SaveSystem.Save();
        }

        //Quit Game to Main Menu
        public void MainMenu()
        {
            SceneManager.LoadScene(0);

        }

        public void Options()
        {

        }


    }
}