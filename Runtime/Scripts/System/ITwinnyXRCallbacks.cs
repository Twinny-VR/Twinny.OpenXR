using Twinny.Core;
using Twinny.XR.Anchoring;
using UnityEngine;

namespace Twinny.XR
{
    public interface ITwinnyXRCallbacks : ICallbacks
    {
        void OnAnchorStateChanged(StateAnchorManager state);
        void OnSetPassthrough(bool status);

        void OnStartInteract(GameObject gameObject);
        void OnStopInteract(GameObject gameObject);
        void OnStartTeleport();
        void OnTeleport();
    }
}
