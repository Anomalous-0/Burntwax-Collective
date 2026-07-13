using System.Collections.Generic;
using UnityEngine;

namespace Burntwax
{

    public class InputPrioritySorter : MonoBehaviour
    {
        string sprint = "sprint";
        string aim = "aim";
        string attack = "attack";

        public static InputPrioritySorter Instance;


        void Awake()
        {

            Instance = this;
        }
        List<string> inputPriority = new List<string>();
        void Update()
        {
            if (InputManager.Instance.aimIsPressed && !inputPriority.Contains(aim))
            {
                // Debug.Log("Should add aim to list");
                inputPriority.Add(aim);
            }
            else if (!InputManager.Instance.aimIsPressed && inputPriority.Contains(aim))
            {
                // Debug.Log("Deleted aim from list");
                inputPriority.Remove(aim);
            }

            if (InputManager.Instance.sprintIsPressed && !inputPriority.Contains(sprint))
            {
                // Debug.Log("Should add sprint to list");
                inputPriority.Add(sprint);
            }
            else if (!InputManager.Instance.sprintIsPressed && inputPriority.Contains(sprint))
            {
                // Debug.Log("Deleted sprint from list");
                inputPriority.Remove(sprint);
            }
            if (InputManager.Instance.attackIsPressed && !inputPriority.Contains(attack))
            {
                // Debug.Log("Should add attack to list");
                inputPriority.Add(attack);
            }
            else if (!InputManager.Instance.attackIsPressed && inputPriority.Contains(attack))
            {
                // Debug.Log("Deleted attack from list");
                inputPriority.Remove(attack);
            }



        }

        public bool SprintIsPriority()
        {
            return inputPriority.IndexOf(sprint) == inputPriority.Count - 1 && inputPriority.Count >= 1;
        }

        public bool AimIsPriority()
        {
            // Glove raised (tap F) blocks ADS for both the gun and the camera, which both gate
            // their aim transitions on this method. Weapon use/switch/reload are unaffected.
            return inputPriority.IndexOf(aim) == inputPriority.Count - 1 && inputPriority.Count >= 1
                && !InputManager.Instance.useGloves;
        }

        public bool AttackIsPriority()
        {
            return inputPriority.IndexOf(attack) == inputPriority.Count - 1 && inputPriority.Count >= 1;
        }

    }

}