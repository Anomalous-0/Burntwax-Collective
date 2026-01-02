using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Camera stuff")]
    public CamStateMachine camStateMachine;

    public GameObject pauseMenuUI;
    public bool Paused;

    void Awake()
    {
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
        camStateMachine.fpsCam.gameObject.SetActive(true);
        camStateMachine.aimCam.gameObject.SetActive(true);
        camStateMachine.chargeCam.gameObject.SetActive(true);
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        AudioListener.pause = true;
        camStateMachine.fpsCam.gameObject.SetActive(false);
        camStateMachine.aimCam.gameObject.SetActive(false);
        camStateMachine.chargeCam.gameObject.SetActive(false);
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