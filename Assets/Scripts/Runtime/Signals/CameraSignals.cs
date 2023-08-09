using Runtime.Enums;
using Runtime.Extentions;
using UnityEngine.Events;

namespace Runtime.Signals
{
    public class CameraSignals : MonoSingleton<CameraSignals>
    {
        public UnityAction<CameraStates> onChangeCameraState = delegate { };
        public UnityAction<CameraTargetState> onSetCinemachineTarget = delegate { };
    }
}