// using UnityEngine;

// public class MainMenuSwitch : MonoBehaviour {
//     public GameObject introMenu;
//     public GameObject mainMenu;

//     public GameManager game;

//     private void Awake() {
//         game = GameManager.Instance;
//         game.gameData = game.dataHandler.Load();
//         if(game.gameData != null) {
//             mainMenu.SetActive(true);
//             introMenu.SetActive(false);

//         }
//         else {
//             mainMenu.SetActive(false);
//             introMenu.SetActive(true);
//         }
//     }
// }
