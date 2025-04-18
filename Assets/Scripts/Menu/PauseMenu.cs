using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {

    public InputManager input;

    public GameObject pauseMenuUI;
    public static bool Paused;

    public void Update() {
        if(input.pauseIsPressed)  {
            Debug.Log("Paused");
            input.pauseIsPressed = false;
            if(Paused) {
                Resume();
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
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    private void Pause() {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        Paused = true;
        // Cursor.lockState = CursorLockMode.Confined;
        // Cursor.visible = false;
        
        
    }

    //Quit Game
    public void MainMenu() {
        SceneManager.LoadScene(0);
    }

    public void Options() {

    }

    
}