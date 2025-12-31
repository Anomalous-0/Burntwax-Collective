using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFpsState : CameraBaseState
{

    public CameraFpsState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
    : base(currentContext, cameraStateFactory)
    {
        IsRootState = true;

    }

    public override void EnterState()
    {
        Ctx.FPS();
        InitializeSubState();
    }

    public override void UpdateState()
    {

        CheckSwitchStates();

    }

    public override void ExitState()
    {

    }

    public override void CheckSwitchStates()
    {
        if (Ctx.player.prioritySorter.AimIsPriority() && Ctx.gun.CurrentState != Ctx.gun.states.Reload() && Ctx.gun.CurrentState.CurrentSubstate() != Ctx.gun.states.Shoot())
        {
            SwitchState(Factory.Aim());
        }

        else if (Ctx.player.currentState == Ctx.player.states.Wallcharge())
        {
            SwitchState(Factory.Wallcharge());
        }
    }

    public override void InitializeSubState() { }

}
