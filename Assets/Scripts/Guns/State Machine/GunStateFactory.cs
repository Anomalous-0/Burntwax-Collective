// using System.Collections.Generic;

// enum GunStates {
//     idle,
//     walk,
//     sprint,
//     crouch,
//     grounded,
//     jump,
//     fall,
//     wallrun,
//     walljump
// }

// public class GunStateFactory{

//     PlayerStateMachine context;
//     Dictionary<GunStates, GunBaseState> states = new Dictionary<GunStates, GunBaseState>();

//     public GunStateFactory(PlayerStateMachine currentContext) {
//         context = currentContext;
//         // states[GunStates.idle] = new GunIdleState(context, this);
//         // states[GunStates.walk] = new GunWalkState(context, this);
//         // states[GunStates.sprint] = new GunSprintState(context, this);
//         // states[GunStates.crouch] = new PlayerCrouchState(context, this);
//         // states[GunStates.jump] = new PlayerJumpState(context, this);
//         // states[GunStates.grounded] = new PlayerGroundedState(context, this);
//         // states[GunStates.fall] = new PlayerFallState(context, this);
//         // states[GunStates.wallrun] = new PlayerWallRunState(context, this);
//         // states[GunStates.walljump] = new PlayerWallJumpState(context, this);
//     }

//     public PlayerBaseState Idle() {
//         return states[GunStates.idle];
//     }

//     public PlayerBaseState Walk() {
//         return states[GunStates.walk];
//     }

//     public PlayerBaseState Sprint () {
//         return states[GunStates.sprint];
//     }

//     public PlayerBaseState Crouch () {
//         return states[GunStates.crouch];
//     }

//     public PlayerBaseState Jump() {
//         return states[GunStates.jump];
//     }   

//     public PlayerBaseState Grounded() {
//         return states[GunStates.grounded];
//     }

//     public PlayerBaseState Fall() {
//         return states[GunStates.fall];
//     }

//     public PlayerBaseState Wallrun() {
//         return states[GunStates.wallrun];
//     }
    
//     public PlayerBaseState Walljump() {
//         return states[GunStates.walljump];
//     }
 
// }
