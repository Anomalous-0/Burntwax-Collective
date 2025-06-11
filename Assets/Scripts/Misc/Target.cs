using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{

    [Header("Settings")]
    public float teleportDistance = 10f;
    public float rotationVelocity = 3f;
    public float minHeightOffset = 0f;  // Minimum extra height above the player
    public float maxHeightOffset = 5f;

    public PlayerStateMachine player;

    void Start() {

    }


    void Update() {
        RotateToPlayer();

    }


    private void RotateToPlayer() {
        Vector3 direction = (player.transform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, direction.y, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationVelocity);
    }

    public void Teleport() {

        Vector2 randomDirection2D = Random.insideUnitCircle.normalized;
        Vector3 randomDirection = new Vector3(randomDirection2D.x, 0f, randomDirection2D.y);


        Vector3 teleportPosition = player.transform.position + randomDirection * teleportDistance;


        float randomHeightOffset = Random.Range(minHeightOffset, maxHeightOffset);
        teleportPosition.y = player.transform.position.y + randomHeightOffset;
        // Optional: raycast down to adjust for terrain height
        // if (Physics.Raycast(teleportPosition + Vector3.up * 50f, Vector3.down, out RaycastHit hitInfo, 100f)) {
        //     teleportPosition.y = hitInfo.point.y;
        // }


        transform.position = teleportPosition;
    }
    

}
