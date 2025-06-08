using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {

    public InputManager input;

    [Header("Camera stuff")]
    public CamStateMachine camStateMachine;

    public GameObject pauseMenuUI;
    public bool Paused;

    public void Update() {
        

        if (input.pauseIsPressed) {
           
            input.pauseIsPressed = false;
            if (Paused)
            {
                Resume();
                
            }
            else
            {

                Pause();
            }

        }

        // if(Paused) {
        //     input.LookInput = Vector3.zero;
        // }

        
    }
    //Load Scene
    public void Resume() {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        Paused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        camStateMachine.primaryCam.SetActive(true);
        camStateMachine.secondaryCam.SetActive(true);
        
        
    }

    private void Pause() {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        camStateMachine.secondaryCam.SetActive(false);
        camStateMachine.primaryCam.SetActive(false);
        Paused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        
        
    }


    //Quit Game
    public void MainMenu() {
        SceneManager.LoadScene(0);
    }

    public void Options() {

    }

    
}