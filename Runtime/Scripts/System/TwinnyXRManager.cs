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



        [SerializeField]
        private PlayerLocomotor m_locomotor;
        public static PlayerLocomotor locomotor { get; private set; }


        private void Awake()
        {
            if (cameraRig == null) cameraRig = FindAnyObjectByType<OVRCameraRig>();

            if (m_locomotor == null) m_locomotor = FindAnyObjectByType<PlayerLocomotor>();
            locomotor = m_locomotor;
        }

        private void Start()
            {
                Initialize();
            }
        private void OnDestroy()
        {
                locomotor = null;
        }
        public void Initialize()
            {
                StateMachine.ChangeState(new IdleState(this));
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnSetPassthrough(true));


        }
    }
}