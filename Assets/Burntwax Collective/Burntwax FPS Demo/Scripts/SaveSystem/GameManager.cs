using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;
using System.Data.Common;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    private void Awake()
    {
       
        Instance = this;
    }

    private void Start()
    {

    }

    private void FixedUpdate()
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
