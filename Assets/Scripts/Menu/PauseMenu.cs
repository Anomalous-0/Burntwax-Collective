using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {

    public InputManager input;


    public void Update() {
        if(input.) 
    }
    //Load Scene
    public void Resume() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    //Quit Game
    public void MainMenu() {
        SceneManager.LoadScene(0);
    }

    public void Options() {

    }

    
}