using System.Collections.Generic;

enum GloveStates {
    rest,
    search,
    attract,
    release,
    locked,
    hold,
    shoot,
    drop,
    jolt
}

public class GloveStateFactory{

    Glove context;
    Dictionary<GloveStates, GloveBaseState> states = new Dictionary<GloveStates, GloveBaseState>();

    public GloveStateFactory(Glove currentContext) {
        context = currentContext;
        states[GloveStates.rest] = new GloveRestState(context, this);
        states[GloveStates.search] = new GloveSearchState(context, this);
        states[GloveStates.attract] = new GloveAttractState(context, this);
        states[GloveStates.release] = new GloveReleaseState(context, this);
        states[GloveStates.locked] = new GloveLockState(context, this);
        states[GloveStates.hold] = new GloveHoldState(context, this);
        states[GloveStates.shoot] = new GloveShootState(context, this);
        states[GloveStates.drop] = new GloveDropState(context, this);
        states[GloveStates.jolt] = new GloveJoltState(context, this);

    }

    public GloveBaseState Rest() {
        return states[GloveStates.rest];
    }

    public GloveBaseState Search() {
        return states[GloveStates.search];
    }

    public GloveBaseState Attract() {
        return states[GloveStates.attract];
    }

    public GloveBaseState Release () {
        return states[GloveStates.release];
    }

    public GloveBaseState Lock() {
        return states[GloveStates.locked];
    }

    public GloveBaseState Hold () {
        return states[GloveStates.hold];
    }

    public GloveBaseState Shoot() {
        return states[GloveStates.shoot];
    }   

    public GloveBaseState Drop() {
        return states[GloveStates.drop];
    }

    public GloveBaseState Jolt() {
        return states[GloveStates.jolt];
    }
 
}
