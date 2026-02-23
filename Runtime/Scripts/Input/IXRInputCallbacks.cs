using Twinny.Core.Input;
using Twinny.XR.Interactables;
using UnityEngine;

namespace Twinny.XR.Input
{
    /// <summary>
    /// Callbacks específicos para XR/OpenXR.
    /// Herdam os eventos comuns do core IInputCallbacks.
    /// </summary>
    public interface IXRInputCallbacks : IInputCallbacks
    {
        /// <summary>
        /// Chamado quando ocorre um pinch em qualquer mão.
        /// </summary>
        void OnPinch(OVRPlugin.Hand hand, bool status);

        /// <summary>
        /// Chamado quando o usuário segura/interage com algum objeto.
        /// </summary>
        /// <param name="target">Transform do objeto que está sendo segurado.</param>
        void OnGrab(Transform target);

        /// <summary>
        /// Chamado quando o menu do headset é pressionado (ex: botão Start do Oculus)
        /// </summary>
        void OnMenuPressed();

        /// <summary>
        /// Called when the user maintains visual focus on an interactable object
        /// for a configured amount of time, indicating an intentional gaze-based
        /// selection or attention state in XR.
        /// </summary>
        /// <param name="target">
        /// The interactable object currently being focused by the user's gaze.
        /// This reference can be used to display contextual UI, preload interactions,
        /// or prepare selection logic.
        /// </param>
        void OnTargetFocus(Interactable target);


        /// <summary>
        /// Called when the teleport pointing gesture state changes.
        /// </summary>
        /// <param name="isPointing">
        /// True when the user starts pointing (teleport gesture active),
        /// false when the user stops pointing.
        /// </param>
        void OnTeleportPointing(bool isPointing);

    }
}
