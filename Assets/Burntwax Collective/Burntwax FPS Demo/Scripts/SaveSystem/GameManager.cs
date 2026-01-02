using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Data.Common;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;
    public PauseMenu pauseMenu;

    public PlayerStateMachine player;

    public GunStateMachine gun;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (Instance != null)
        {
            Debug.LogError("Found more than one Game Manager in the scene");
            Destroy(gameObject);
        }
        Instance = this;
    }

    private void Start()
    {

    }

    private void Update()
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
}
