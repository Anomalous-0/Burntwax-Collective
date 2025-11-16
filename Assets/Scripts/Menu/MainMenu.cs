using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

    //Load Scene
    public void NewGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1f;
        
    }

    public void Resume() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        SaveSystem.Load();
        Time.timeScale = 1f;
        
    }

    //Quit Game
    public void Quit() {
        Application.Quit();
    }

    public void Options(){
        
    }
}