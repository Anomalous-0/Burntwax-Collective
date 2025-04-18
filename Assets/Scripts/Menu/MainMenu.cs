using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {
    //Load Scene
    public void Play() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1f;
    }

    //Quit Game
    public void Quit() {
        Application.Quit();
        Debug.Log("The Player has Quit the game");
    }

    public void Options(){
        
    }
}