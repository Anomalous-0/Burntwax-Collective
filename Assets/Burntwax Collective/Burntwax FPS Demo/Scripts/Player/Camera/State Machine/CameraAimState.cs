using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAimState : CameraBaseState
{

    public CameraAimState(CamStateMachine currentContext, CameraStateFactory cameraStateFactory)
    : base(currentContext, cameraStateFactory)
    {
        IsRootState = true;

    }

    public override void EnterState()
    {
        Ctx.crosshair.SetAimCrosshair();
        Ctx.Aim();
        InitializeSubState();
    }

    public override void UpdateState()
    {

        CheckSwitchStates();

    }

    public override void ExitState()
    {
        Ctx.crosshair.ResetAimCrosshair();
    }

    public override void CheckSwitchStates()
    {
        if (Ctx.player.prioritySorter.SprintIsPriority() || !Ctx.input.aimIsPressed || Ctx.gun.CurrentState == Ctx.gun.states.Reload())
        {
            SwitchState(Factory.Fps());
        }
        else if ( Ctx.player.currentState == Ctx.player.states.Wallcharge())
        {
            SwitchState(Factory.Wallcharge());
        }



    }


    public override void InitializeSubState() { }

}
