using System.Collections.Generic;

enum GunStates {
    rest,
    shoot,
    reload,
    aim,

    stow


}

public class GunStateFactory{

    GunStateMachine context;
    Dictionary<GunStates, GunBaseState> states = new Dictionary<GunStates, GunBaseState>();

    public GunStateFactory(GunStateMachine currentContext) {
        context = currentContext;
        states[GunStates.rest] = new GunRestState(context, this);
        states[GunStates.shoot] = new GunShootState(context, this);
        states[GunStates.reload] = new GunReloadState(context, this);
        states[GunStates.aim] = new GunAimState(context, this);
        states[GunStates.stow] = new GunStowState(context, this);
       
    }

    public GunBaseState Rest() {
        return states[GunStates.rest];
    }

    public GunBaseState Shoot() {
        return states[GunStates.shoot];
    }

    public GunBaseState Reload () {
        return states[GunStates.reload];
    }

    public GunBaseState Aim () {
        return states[GunStates.aim];
    }

    public GunBaseState Stow() {
        return states[GunStates.stow];
    }   
}
