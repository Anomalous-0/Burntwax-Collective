// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.AI;

// public class AILocomotion : MonoBehaviour {

//     NavMeshAgent agent;
//     [SerializeField] Animator animator;
//     [SerializeField] Transform player;
//     [SerializeField] float maxTime = 0.1f;
//     [SerializeField] float maxDistance = 0.2f;
//     float timer = 0.0f;
//     // Start is called before the first frame update
//     void Start() {
//         agent = GetComponent<NavMeshAgent>();
//     }

//     // Update is called once per frame
//     void Update() {
//         timer -= Time.deltaTime;
//         if(timer < 0.0f) {
//             float sqDistance = (player.position - agent.destination).sqrMagnitude;
//             if(sqDistance > maxDistance*maxDistance) {
//                 agent.destination = player.position;
//             }
//             timer = maxTime;
//         }
        
//         animator.SetFloat("Velocity", agent.velocity.magnitude);
//     }
// }
