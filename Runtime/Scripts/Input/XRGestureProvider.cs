using System;
using Concept.Core;
using Concept.Helpers;
using Oculus.Interaction.HandGrab;
using Twinny.XR.Input;
using Twinny.XR.Interactables;
using UnityEngine;
using UnityEngine.Events;

namespace TWE26.OpenXR.Input
{
    /// <summary>
    /// XR-specific input provider responsible for detecting hand gestures, 
    /// interaction focus (ray-based tracing), and XR-only actions such as pinch,
    /// grab, teleport gestures, and menu input.
    ///
    /// This class does NOT dispatch generic input directly.
    /// Instead, it forwards XR events through CallbackHub using IXRInputCallbacks.
    ///
    /// Intended to live inside the twe26.openxr package.
    /// </summary>
    public class XRGestureProvider : TSingleton<XRGestureProvider>
    {

        #region Fields

        // Main XR camera used for ray-based interactable tracing (HMD camera)
        private Camera m_camera;

        // Reference to the left tracked hand
        private OVRHand m_leftHand;

        // Reference to the right tracked hand
        private OVRHand m_rightHand;

        // Internal state flag used to detect pinch "edge" on the left hand
        private bool m_wasPinchingLeft;

        // Internal state flag used to detect pinch "edge" on the right hand
        private bool m_wasPinchingRight;

        // Internal state flag used to detect teleport gesture edge trigger
        private bool m_wasTeleporting;

        [Header("Hand Grab Interactors")]

        // Hand grab interactor for the left hand
        [SerializeField]
        private HandGrabInteractor m_handGrabInteractorLeft;

        // Hand grab interactor for the right hand
        [SerializeField]
        private HandGrabInteractor m_handGrabInteractorRight;

        [Header("Interactable Tracing")]

        // Enables or disables ray-based interactable tracing
        [SerializeField]
        private bool m_traceInteractables = true;

        // Time (in seconds) the user must observe an interactable before focus is triggered
        [SerializeField]
        private float m_closeUpTime = 1f;

        // Maximum distance for the interactable raycast
        [SerializeField]
        private float m_rayDistance = 10f;

        // Layer mask used to filter valid interactables
        [SerializeField]
        private LayerMask m_interactableLayer;

        // Currently focused interactable
        private Interactable m_target;

        // Internal timer used to track how long the current interactable is being observed
        private float m_observeTime;

        [SerializeField] private GameObject m_handForwardGestureRight;

        #endregion

        #region Embeded Events
        public UnityEvent<bool> OnPinchLeftEvent = new UnityEvent<bool>();
        public UnityEvent<bool> OnPinchRightEvent = new UnityEvent<bool>();
        public UnityEvent<Transform> OnGrabEvent = new UnityEvent<Transform>();
        public UnityEvent<bool> OnTeleportEvent = new UnityEvent<bool>();
        public UnityEvent OnMenuPressedEvent = new UnityEvent();
        public UnityEvent<Interactable> OnTargetFocusEvent = new UnityEvent<Interactable>();
        #endregion

        #region MonoBehaviour

        /// <summary>
        /// Initializes the XR gesture provider and locates required XR references
        /// such as the main camera and tracked hands.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_camera = Camera.main;
            FindHands();

            if (!m_leftHand)
                Debug.LogWarning("[XRGestureProvider] Left hand not found in scene.");

            if (!m_rightHand)
                Debug.LogWarning("[XRGestureProvider] Right hand not found in scene.");
        }

        /// <summary>
        /// Main update loop responsible for:
        /// - Tracing interactables
        /// - Detecting pinch gestures
        /// - Detecting teleport gestures
        /// - Detecting grab state
        /// - Detecting menu button input
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (m_traceInteractables)
                TraceInteractables();

            if (!m_leftHand || !m_rightHand)
                return;

            HandlePinch(m_leftHand, ref m_wasPinchingLeft);
            HandlePinch(m_rightHand, ref m_wasPinchingRight);

            HandleTeleportGesture(m_rightHand);

            HandleGrab();

            HandleMenu();
        }

        #endregion

        #region Hand Detection

        /// <summary>
        /// Searches the scene for OVRHand components and assigns
        /// references to left and right tracked hands.
        /// </summary>
        private void FindHands()
        {
            var hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);

            foreach (var hand in hands)
            {
                if (hand.GetHand() == OVRPlugin.Hand.HandLeft)
                    m_leftHand = hand;
                else if (hand.GetHand() == OVRPlugin.Hand.HandRight)
                    m_rightHand = hand;
            }
        }

        #endregion

        #region Gesture Handling

        /// <summary>
        /// Handles pinch detection for a given hand.
        /// Uses edge detection to ensure the event is only fired
        /// when the pinch starts, not while it is being held.
        /// </summary>
        /// <param name="hand">Tracked hand reference.</param>
        /// <param name="wasPinching">
        /// Internal state flag storing whether the hand
        /// was pinching in the previous frame.
        /// </param>
        private void HandlePinch(OVRHand hand, ref bool wasPinching)
        {
            if (hand == null || !hand.IsTracked)
                return;

            bool isPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

            // Determina a mão
            bool isLeft = hand.GetHand() == OVRPlugin.Hand.HandLeft;

            if (isPinching && !wasPinching)
            {
                wasPinching = true;

                if (isLeft)
                    OnPinchLeftEvent?.Invoke(true);
                else
                    OnPinchRightEvent?.Invoke(true);

                CallbackHub.CallAction<IXRInputCallbacks>(cb => cb.OnPinch(hand.GetHand(), true)); // global
            }
            else if (!isPinching && wasPinching)
            {
                wasPinching = false;

                if (isLeft)
                    OnPinchLeftEvent?.Invoke(false);
                else
                    OnPinchRightEvent?.Invoke(false);

                CallbackHub.CallAction<IXRInputCallbacks>(cb => cb.OnPinch(hand.GetHand(), false));
            }
        }


        /// <summary>
        /// Detects teleport gesture using hand posture and orientation.
        /// This method uses edge detection to avoid spamming events
        /// while the gesture is being held.
        /// </summary>
        /// <param name="hand">Tracked hand reference (usually right hand).</param>
        private void HandleTeleportGesture(OVRHand hand)
        {
            if (hand == null || !hand.IsTracked)
                return;

            bool isTeleporting = IsTeleportGesture(hand);

            // Only react when the state changes (start or stop pointing)
            if (isTeleporting == m_wasTeleporting)
                return;

            m_wasTeleporting = isTeleporting;
            OnTeleportEvent?.Invoke(isTeleporting);
            CallbackHub.CallAction<IXRInputCallbacks>(
                cb => cb.OnTeleportPointing(isTeleporting)
            );
        }


        /// <summary>
        /// Detects grab state from both hand grab interactors
        /// and forwards the transform of the hand currently grabbing.
        /// </summary>
        private void HandleGrab()
        {
            Transform grabbedBy = null;

            if (m_handGrabInteractorLeft != null && m_handGrabInteractorLeft.IsGrabbing)
                grabbedBy = m_handGrabInteractorLeft.transform;
            else if (m_handGrabInteractorRight != null && m_handGrabInteractorRight.IsGrabbing)
                grabbedBy = m_handGrabInteractorRight.transform;

            OnGrabEvent?.Invoke(grabbedBy);
            CallbackHub.CallAction<IXRInputCallbacks>(
                cb => cb.OnGrab(grabbedBy)
            );
        }

        /// <summary>
        /// Detects menu button input from the XR controller.
        /// </summary>
        private void HandleMenu()
        {
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                OnMenuPressedEvent?.Invoke();
                CallbackHub.CallAction<IXRInputCallbacks>(
                    cb => cb.OnMenuPressed()
                );
            }
        }

        #endregion

        #region Interactable Tracing

        /// <summary>
        /// Performs a forward raycast from the XR camera to detect
        /// interactable objects.
        ///
        /// Highlights the current target and tracks how long the user
        /// has been observing it. Once the observation time exceeds
        /// the configured close-up time, a focus event is fired.
        /// </summary>
        private void TraceInteractables()
        {
            Ray ray = new Ray(m_camera.transform.position, m_camera.transform.forward);

            if (Physics.Raycast(ray, out var hit, m_rayDistance, m_interactableLayer))
            {
                var interactable = hit.collider.GetComponent<Interactable>();

                if (interactable != null)
                {
                    if (m_target == null || m_target != interactable)
                    {
                        m_target?.SetHighLight(false);
                        m_target = interactable;
                        m_target.SetHighLight();
                        m_observeTime = 0f;
                    }

                    m_observeTime += Time.deltaTime;

                    if (m_observeTime >= m_closeUpTime)
                    {
                        OnTargetFocusEvent?.Invoke(m_target);
                        CallbackHub.CallAction<IXRInputCallbacks>(
                            cb => cb.OnTargetFocus(m_target)
                        );

                        m_observeTime = 0f;
                    }

                    return;
                }
            }

            m_target?.SetHighLight(false);
            m_target = null;
            m_observeTime = 0f;
        }

        #endregion

        #region Gesture Utilities

        /// <summary>
        /// Determines whether the palm of the hand is facing upward.
        /// </summary>
        /// <param name="hand">Tracked hand reference.</param>
        /// <returns>True if the palm is facing up, otherwise false.</returns>
        private bool IsPalmUp(OVRHand hand)
        {
            Vector3 palmUp = hand.transform.rotation * Vector3.up;
            return palmUp.y > 0f;
        }

        /// <summary>
        /// Determines whether the hand is in a pointing posture.
        /// All fingers except the index must be closed.
        /// </summary>
        /// <param name="hand">Tracked hand reference.</param>
        /// <returns>True if the hand is pointing, otherwise false.</returns>
        private bool IsPointing(OVRHand hand)
        {
            return !hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) &&
                   !hand.GetFingerIsPinching(OVRHand.HandFinger.Ring) &&
                   !hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky) &&
                   IsPalmUp(hand);
        }

        /// <summary>
        /// Determines whether the current hand posture represents
        /// a teleport gesture.
        /// </summary>
        /// <param name="hand">Tracked hand reference.</param>
        /// <returns>True if the teleport gesture is active.</returns>
        private bool IsTeleportGesture(OVRHand hand)
        {
            return IsPointing(hand) && IsPalmUp(hand);
        }

        public static void SetHandForwardGestureRight(bool status) => Instance.m_handForwardGestureRight?.SetActive(status);


        #endregion
    }
}
