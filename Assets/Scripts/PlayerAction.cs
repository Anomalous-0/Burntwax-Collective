using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerAction : MonoBehaviour
{
    [SerializeField]
    private PlayerGunSelector GunSelector;
    public InputManager input;

    private void Update()
    {
        if (input.shootIsPressed
            && GunSelector.ActiveGun != null)
        {
            GunSelector.ActiveGun.Shoot();
        }
    }
}
