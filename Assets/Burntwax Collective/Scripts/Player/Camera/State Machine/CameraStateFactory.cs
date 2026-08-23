using System.Collections.Generic;
using Burntwax;

namespace Burntwax
{
    enum CameraStates
    {
        fps,
        aim,
        wallcharge,
        glovecharge
    }

    public class CameraStateFactory
    {

        CamStateMachine context;
        Dictionary<CameraStates, CameraBaseState> states = new Dictionary<CameraStates, CameraBaseState>();

        public CameraStateFactory(CamStateMachine currentContext)
        {
            context = currentContext;
            states[CameraStates.fps] = new CameraFpsState(context, this);
            states[CameraStates.aim] = new CameraAimState(context, this);
            states[CameraStates.wallcharge] = new CameraWallChargeState(context, this);
            states[CameraStates.glovecharge] = new CameraGloveChargeState(context, this);


        }

        public CameraBaseState Fps()
        {
            return states[CameraStates.fps];
        }

        public CameraBaseState Aim()
        {
            return states[CameraStates.aim];
        }

        public CameraBaseState Wallcharge()
        {
            return states[CameraStates.wallcharge];
        }

        public CameraBaseState GloveCharge()
        {
            return states[CameraStates.glovecharge];
        }



    }

}