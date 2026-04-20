using UnityEngine;

namespace Burntwax
{
    public class KatanaDriver : MonoBehaviour
    {
        [Header("References")]
        public Transform weaponPivot;

        [Header("Input Smoothing")]
        [Range(0.01f, 0.5f)]
        public float smoothing = 0.15f;

        [HideInInspector] public bool isSwinging;

        private Vector2 smoothedInput;
        private string currentAction; // "Slash" or "Parry"

        public void BeginSwing(Vector2 startDirection, string action)
        {
            isSwinging = true;
            smoothedInput = startDirection;
            currentAction = action;

            Debug.Log($"[KatanaDriver] {currentAction} started — initial direction: {GetDirectionName(startDirection)}");
        }

        public void FeedInput(Vector2 mouseDelta)
        {
            if (!isSwinging) return;

            smoothedInput = Vector2.Lerp(smoothedInput, mouseDelta, 1f - smoothing);

            Vector2 dir = smoothedInput.normalized;
            // Debug.Log($"[KatanaDriver] {currentAction} direction: {GetDirectionName(dir)} | raw: ({dir.x:F2}, {dir.y:F2})");
        }

        public void EndSwing()
        {
            // Debug.Log($"[KatanaDriver] {currentAction} ended");
            isSwinging = false;
        }

        private string GetDirectionName(Vector2 dir)
        {
            if (dir.magnitude < 0.1f) return "neutral";

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            // 8 directions, 45 degree slices, offset by 22.5 so "right" is 0±22.5
            if (angle < 22.5f || angle >= 337.5f) return "RIGHT";
            if (angle < 67.5f) return "UP-RIGHT";
            if (angle < 112.5f) return "UP";
            if (angle < 157.5f) return "UP-LEFT";
            if (angle < 202.5f) return "LEFT";
            if (angle < 247.5f) return "DOWN-LEFT";
            if (angle < 292.5f) return "DOWN";
            return "DOWN-RIGHT";
        }
    }

}