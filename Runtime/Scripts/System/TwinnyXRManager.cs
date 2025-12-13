using Concept.Core;
using Concept.Helpers;
using Twinny.Core;
using UnityEngine;

namespace Twinny.XR
{

    public class TwinnyXRManager: MonoBehaviour
    {

            private void Start()
            {
                Initialize();
            }

            public void Initialize()
            {
                StateMachine.ChangeState(new IdleState(this));
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnSetPassthrough(true));


        }
    }
}