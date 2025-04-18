using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {

    public InputManager input;

    [Header("Camera stuff")]
    public CamStateMachine cam;
    /*[HideInInspector] */public int pauseCounterCamThing = 0;

    Vector3 targetRot;

    public GameObject pauseMenuUI;
    public bool Paused;

    public void Update() {
        if(input.pauseIsPressed)  {
            Debug.Log("Paused");
            input.pauseIsPressed = false;
            if(Paused) {
                Resume();
                pauseCounterCamThing = 0;
            }
            else{
              
                Pause();
            }

        }

        
    }
    //Load Scene
    public void Resume() {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        Paused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
    }

    private void Pause() {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
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