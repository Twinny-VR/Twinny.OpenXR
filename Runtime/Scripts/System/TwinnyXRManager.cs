using Concept.Core;
using Concept.Helpers;
using Oculus.Interaction.Locomotion;
using Twinny.Core;
using UnityEngine;

namespace Twinny.XR
{

    public class TwinnyXRManager: MonoBehaviour
    {
        [SerializeField]
        private OVRCameraRig m_cameraRig;
        public static OVRCameraRig cameraRig;
        public static Transform cameraRigTransform => cameraRig?.transform;
        public static Transform headTransform => Camera.main?.transform;


        private void Awake()
        {
            if (cameraRig == null) cameraRig = FindAnyObjectByType<OVRCameraRig>();

        }

        private void Start()
            {
                Initialize();
            }
        private void OnDestroy()
        {
        }
        public void Initialize()
            {
                StateMachine.ChangeState(new IdleState(this));
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnSetPassthrough(true));


        }
    }
}