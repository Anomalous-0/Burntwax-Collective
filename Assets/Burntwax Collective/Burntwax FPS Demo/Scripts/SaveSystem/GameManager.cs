using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private void Awake()
    {
       
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
        if(MainMenu.Instance.loadScene)
        {
            MainMenu.Instance.loadScene = false;
            SaveSystem.Load();
        }


    }
}
