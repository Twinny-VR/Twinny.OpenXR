using Concept.Core;
using System.Linq;
using System.Threading.Tasks;
using Twinny.Core;
using Twinny.XR.Navigation;
using Twinny.UI;
using Twinny.XR.Anchoring;
using UnityEngine;
using UnityEngine.SceneManagement;
using TWE26.OpenXR.Input;
using System;

namespace Twinny.XR
{
    /// <summary>
    /// Scene layout type VR(Virtual), MR(Mixed), MOBILE(Mobile)
    /// </summary>
    [Serializable]
    public enum SceneType
    {
        VR, //Virtual Reallity
        MR //Mixed Reallity
    }

    public class SceneFeatureXR : SceneFeature, ITwinnyXRCallbacks
    {
        #region Cached Components
        private Transform _transform;
        #endregion

        #region Fields
        // Root transform that contains the scene content that gets repositioned and rotated.
        public Transform worldTransform;
        // Active scene presentation mode (VR or MR).
        [SerializeField] public SceneType sceneType;

        // Landmark targets available for teleport and scene alignment.
        [SerializeField] public LandMark[] landMarks = new LandMark[0];
        [SerializeField] public bool enableNavigationMenu;
        //public GameObject extensionMenu;
        public bool isMenuStatic;
        // Landmark currently used to drive HDRI configuration.
        private LandMark currentLandMark;
        // Landmark currently selected for navigation state.
        private LandMark _currentLandMark;


        #endregion



        #region MonoBehaviour Methods   


#if UNITY_EDITOR

        /// <summary>
        /// Ensures editor-time references are valid and synchronizes landmark display names.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            if (worldTransform == null) worldTransform = transform.Find("World");
            if (worldTransform == null) worldTransform = new GameObject("World").transform;
            worldTransform.SetParent(transform);
            if (landMarks == null) return;

            foreach (var mark in landMarks)
            {
                if (mark.node != null)
                {
                    mark.landName = mark.node.name;
                }
                else
                    mark.landName = "Empty LandMark";
            }

        }
#endif
        /// <summary>
        /// Registers this component to receive XR callback events.
        /// </summary>
        private void OnEnable()
        {
            CallbackHub.RegisterCallback<ITwinnyXRCallbacks>(this);
        }


        //Awake is called before the script is started
        /// <summary>
        /// Caches references and subscribes to OVR focus and tracking lifecycle events.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            _transform = transform;
            if (OVRManager.display != null)
            {
                OVRManager.InputFocusLost += OnInputFocusLost;
                OVRManager.InputFocusAcquired += OnInputFocusAcquired;
                OVRManager.TrackingAcquired += OnTrackingAcquired;
                //OVRManager.display.RecenteredPose += OnRecenterDetected2;
            }
        }
        // Anchor baseline used by tracking compensation when resolving landmark teleport.
        private Vector3 m_anchorStartPosition;
        // Start is called before the first frame update
        /// <summary>
        /// Initializes anchoring, passthrough mode, baseline tracking state, and gesture setup.
        /// </summary>
        protected override void Start()
        {
            Debug.LogWarning($"[SceneFeature] {gameObject.scene.name} Started");
            base.Start();
            PassthroughFader.TogglePassthroughAction(sceneType == SceneType.MR, 100f);

            AnchorScene();
            /*
                        Debug.LogWarning($"[SceneFeature] RIG:{TwinnyXRManager.cameraRigTransform.position} {(int)TwinnyXRManager.cameraRigTransform.eulerAngles.y}º " +
                            $"SAFE: {AnchorManager.position} {(int)AnchorManager.rotation.eulerAngles.y}º " +
                            $"ANCH:{AnchorManager.currentAnchor.transform.position} {(int)AnchorManager.currentAnchor.transform.eulerAngles.y}º " +
                            $"THIS:{transform.position} {(int)transform.eulerAngles.y}º" +
                            $"WRLD:{worldTransform.position} {(int)worldTransform.eulerAngles.y}º" +
                            $"LWRLD:{worldTransform.localPosition} {(int)worldTransform.localEulerAngles.y}º"
                            );
            */

            m_anchorStartPosition = AnchorManager.position;

            if (fadeOnAwake)
            {
                //NetworkedLevelManager.Instance.RPC_FadingStatus(0);
                _ = CanvasTransition.FadeScreenAsync(false, TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime);
            }


            //if (OVRManager.display != null) OVRManager.display.RecenteredPose += OnRecenterDetected;

            //if (extensionMenu) CallbackHub.CallAction<IUICallBacks>(callback => callback.OnLoadExtensionMenu(extensionMenu, isMenuStatic));


            int layer = LayerMask.NameToLayer("Character");

            XRGestureProvider.SetHandForwardGestureRight(sceneType == SceneType.VR);
            if (layer == -1) return;


            if (sceneType == SceneType.VR)
            {
                // AvatarSpawner.SpawnAvatar();
                //                Camera.main.cullingMask |= (1 << layer);
            }
            else
            {
                //AvatarSpawner.DespawnAvatar();
                //              Camera.main.cullingMask &= ~(1 << layer);

            }

            //CheckGameMode();


        }

        /// <summary>
        /// Unregisters this component from XR callback events.
        /// </summary>
        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<ITwinnyXRCallbacks>(this);

            //    NavigationMenu.Instance.SetArrows(null);
        }

        /// <summary>
        /// Unsubscribes from OVR events to avoid dangling handlers.
        /// </summary>
        private void OnDestroy()
        {
            // SetupHDRI(-1);
            //CallbackHub.CallAction<IUICallBacks>(callback => callback.OnLoadExtensionMenu(null));

            if (OVRManager.display != null)
            {
                OVRManager.TrackingAcquired -= OnTrackingAcquired;
                OVRManager.InputFocusLost -= OnInputFocusLost;
                OVRManager.InputFocusAcquired -= OnInputFocusAcquired;
               // OVRManager.display.RecenteredPose -= OnRecenterDetected2;
            }
            /*
            if (NetworkedLevelManager.Instance.currentLandMark < 0
                && NetworkRunnerHandler.runner.IsConnectedToServer
                && NetworkRunnerHandler.runner.SessionInfo != null)
                CallbackHub.CallAction<IUICallBacks>(callback => callback.OnUnloadSceneFeature());
        */

        }

        #endregion

        #region Overrided Methods


        /// <summary>
        /// Repositions and rotates the world to align a target landmark with the user, including tracking-drift compensation.
        /// Also handles reset flow when <paramref name="landMarkIndex"/> is negative.
        /// </summary>
        /// <param name="landMarkIndex">Target landmark index; use -1 to reset to anchor reference.</param>
        public override void TeleportToLandMark(int landMarkIndex)
        {
            if (worldTransform == null) return;

            Transform cameraRig = TwinnyXRManager.cameraRigTransform.transform;
            UndockScene();
            if (landMarks.Length > 0 && landMarkIndex >= 0)
            {
                // cameraRig.position = new Vector3(Camera.main.transform.position.x,cameraRig.position.y,Camera.main.transform.position.z);
                if (_currentLandMark != null) _currentLandMark.node?.Unselect();
                _currentLandMark = landMarks[landMarkIndex];
                var node = _currentLandMark.node;

                //Debug.LogWarning($"MERDA ANCH:{AnchorManager.currentAnchor.transform.position} SA:{AnchorManager.position} PRNT:{worldTransform.parent.position} WP:{worldTransform.position} WLP:{worldTransform.localPosition}");

                worldTransform.position = m_anchorStartPosition;
                worldTransform.localPosition = Vector3.zero;
                worldTransform.rotation = GetYawRotation(AnchorManager.rotation);
                worldTransform.localRotation = Quaternion.identity;
                Vector3 nodeLocalPos = worldTransform.parent.InverseTransformPoint(node.transform.position);
                //Vector3 desiredPosition = -(worldTransform.localRotation * nodeLocalPos);
                // TRACKING COMPENSATION: compare current anchor with baseline captured at scene start/reset.
                Vector3 trackingDeltaLocal = Vector3.zero;
                bool trackingDirty = (AnchorManager.position - m_anchorStartPosition).sqrMagnitude > 0.0001f;
                if (trackingDirty)
                {
                    // Convert world-space drift to parent-local space used by worldTransform alignment.
                    Vector3 trackingDeltaWorld = AnchorManager.position - m_anchorStartPosition;

                    trackingDeltaLocal = worldTransform.parent.InverseTransformVector(trackingDeltaWorld);
                    // trackingDeltaLocal = worldRotation * trackingDeltaLocal;

                }

                float nodeYaw = GetYawRelativeToParent(node.transform, worldTransform.parent);
                Quaternion invNodeYaw = Quaternion.Euler(0f, -nodeYaw, 0f);



                // Final landmark position compensated by tracking drift (if any).
                Vector3 desiredPosition = -(invNodeYaw * nodeLocalPos) - trackingDeltaLocal;

                worldTransform.localRotation = invNodeYaw;
                worldTransform.localPosition = desiredPosition;

                //Debug.LogWarning($"MERDA YAW:{nodeYaw} NLP:{nodeLocalPos} WP:{worldTransform.position} WLP:{worldTransform.localPosition}");
                //   NavigationMenu.Instance?.SetArrows(enableNavigationMenu ? _currentLandMark.node : null);

                    cameraRig.position = Vector3.zero;
                    cameraRig.rotation = Quaternion.identity;

                node?.Select();
            }
            else
            {
                _currentLandMark = null;
                if (worldTransform != null)
                {
                    worldTransform.position = AnchorManager.Instance.transform.position;
                    worldTransform.rotation = GetYawRotation(AnchorManager.rotation);
                }

                // On reset/restart, also clear locomotion rig drift before next scene/landmark teleport.
                cameraRig.position = Vector3.zero;
                cameraRig.rotation = Quaternion.identity;

                // Reset baseline so future landmark teleports start from a clean tracking state.
                m_anchorStartPosition = AnchorManager.position;
            }
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnTeleportToLandMark(landMarkIndex));

            if (worldTransform != null)
            {
                AnchorScene();
            }


            SetupHDRI(landMarkIndex);

            RecenterSkyBox();


        }

        /// <summary>
        /// Computes yaw angle between a node forward vector and its parent forward vector on the horizontal plane.
        /// </summary>
        /// <param name="node">Node transform used as yaw source.</param>
        /// <param name="parent">Parent transform used as yaw reference.</param>
        /// <returns>Signed yaw angle in degrees.</returns>
        private float GetYawRelativeToParent(Transform node, Transform parent)
        {
            Vector3 forward = node.forward;
            Vector3 parentForward = parent.forward;

            forward.y = 0;
            parentForward.y = 0;

            forward.Normalize();
            parentForward.Normalize();

            return Vector3.SignedAngle(parentForward, forward, Vector3.up);
        }



        /// <summary>
        /// Alternative yaw computation in parent space using projected forward vector.
        /// </summary>
        /// <param name="target">Target transform used as yaw source.</param>
        /// <param name="parent">Parent transform used as yaw reference.</param>
        /// <returns>Yaw angle in degrees.</returns>
        static float GetYawRelativeToParent2(Transform target, Transform parent)
        {
            Vector3 forward = target.forward;

            // remove pitch/roll
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up).normalized;

            // traz para o espaço do parent
            if (parent != null)
                flatForward = parent.InverseTransformDirection(flatForward);

            return Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;
        }


        #endregion

        #region Public Methods


        /// <summary>
        /// Returns the landmark associated with a given landmark node.
        /// </summary>
        /// <param name="node">Landmark node to look up.</param>
        /// <returns>Matching landmark, or null when not found.</returns>
        public LandMark GetLandMark(LandMarkNode node)
        {
            return landMarks.FirstOrDefault(o => o.node == node);
        }

        /// <summary>
        /// Returns the index of the landmark associated with a given node.
        /// </summary>
        /// <param name="node">Landmark node to look up.</param>
        /// <returns>Landmark index, or -1 when not found.</returns>
        public int GetLandMarkIndex(LandMarkNode node)
        {
            for (int i = 0; i < landMarks.Length; i++)
            {
                if (landMarks[i].node == node)
                { return i; }
            }
            return -1;
        }


        // Cached world position in anchor local space while app focus is lost.
        Vector3 _worldLocalPosToRig;
        // Cached world rotation in anchor local space while app focus is lost.
        Quaternion _worldLocalRotToRig;
        // Indicates whether focus-loss snapshot data was captured.
        bool _hasRigSnapshot;


        /// <summary>
        /// Captures world transform relative to anchor before input focus is lost.
        /// </summary>
        public void OnInputFocusLost()
        {
            _ = CanvasTransition.FadeScreenAsync(
                true,
                TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime
            );

            Transform anchor = AnchorManager.Instance.transform;
            Transform world = worldTransform;

            // posição relativa ao rig
            _worldLocalPosToRig = anchor.InverseTransformPoint(world.position);

            // rotação relativa ao rig
            _worldLocalRotToRig = Quaternion.Inverse(anchor.rotation) * world.rotation;

            _hasRigSnapshot = true;

            Debug.LogWarning("[SceneFeature] Snapshot salvo: World relativo ao CameraRig");
        }
        /// <summary>
        /// Restores view transition after input focus returns.
        /// </summary>
        public async void OnInputFocusAcquired()
        {
            await Task.Delay(500);
            _ = CanvasTransition.FadeScreenAsync(false, 1.5f);
        }


        /// <summary>
        /// Restores world transform from cached anchor-relative snapshot after tracking is reacquired.
        /// </summary>
        public async void OnTrackingAcquired()
        {
            await Task.Delay(500);
            _ = CanvasTransition.FadeScreenAsync(false, 1.5f);

            UndockScene();

            transform.rotation = GetForwardDirection(AnchorManager.rotation);

            Transform anchor = AnchorManager.Instance.transform;
            Transform world = worldTransform;

            // restaura posição no espaço atual do rig
            world.position = anchor.TransformPoint(_worldLocalPosToRig);

            // restaura rotação relativa ao rig
            world.rotation = anchor.rotation * _worldLocalRotToRig;

#if !UNITY_EDITOR
    DockScene();
#endif

            RecenterSkyBox();

            Debug.LogWarning("[SceneFeature] Snapshot restaurado com sucesso: " + _hasRigSnapshot);
        }



        /// <summary>
        /// Legacy fallback tracking-reacquired handler.
        /// </summary>
        public async void OnTrackingAcquired2()
        {
            await CanvasTransition.FadeScreenAsync(false, 1.5f, 1f);
            UndockScene();
            transform.rotation = GetForwardDirection(AnchorManager.rotation);
            //transform.position = AnchorManager.position;
            // if (sceneType == SceneType.VR && GameMode.currentMode is TwinnyXRSingleplayer) return;
#if !UNITY_EDITOR
             DockScene();
#endif
            RecenterSkyBox();
        }


        #endregion

        #region Private Methods


        /// <summary>
        /// Applies HDRI configuration for a target landmark, or enables passthrough when index is invalid.
        /// </summary>
        /// <param name="landMarkIndex">Landmark index used to resolve HDRI settings.</param>
        private void SetupHDRI(int landMarkIndex)
        {
            if (landMarkIndex < 0)//If no LandMark to set, reset skybox to Passthroug
            {
                PassthroughFader.TogglePassthroughAction(true, 100f);
                //RenderSettings.skybox = TwinnyRuntime.GetInstance<TwinnyXRRuntime>().defaultSkybox;
                return;
            }

            if (landMarks.Length > 0)
            {

                LandMark landMark = landMarks[landMarkIndex];
                TwinnyManager.SetHDRI(landMark.skyBoxMaterial);
                currentLandMark = landMark;
            }

        }





        /*
        private void CheckGameMode()
        {

            bool active = (NetworkRunnerHandler.runner.GameMode != Fusion.GameMode.Single);
            NetworkTransform[] networks = _transform.GetComponentsInChildren<NetworkTransform>();
            foreach (var item in networks)
            {
                item.enabled = active;
            }

        }
        */
        /// <summary>
        /// Debug helper used to inspect recentering state transitions.
        /// </summary>
        private async Task Recenter2()
        {
            Debug.LogWarning($"[SceneFeature] STARTING RECENTERING");
            Debug.LogWarning($"[SceneFeature] Recenter. Anchor " +
                $"P:{AnchorManager.Instance.transform.position} " +
                $"R:{AnchorManager.Instance.transform.rotation.eulerAngles}" +
                $"LR:{AnchorManager.Instance.transform.localRotation.eulerAngles}");
            Debug.LogWarning($"[SceneFeature] Recenter. World " +
                $"P:{worldTransform.position} " +
                $"R:{worldTransform.rotation.eulerAngles}" +
                $"LR:{worldTransform.localRotation.eulerAngles}");


            Debug.LogWarning($"[SceneFeature] Recenter. Anchor R:{AnchorManager.Instance.transform.position}");
            await Task.Yield();



            //Vector3 desiredPosition = -_currentLandMark.node.transform.localPosition;
            //worldTransform.localPosition = desiredPosition;
            //worldTransform.RotateAround(AnchorManager.Instance.transform.position, Vector3.up, -_currentLandMark.node.transform.localRotation.eulerAngles.y);

            Debug.LogWarning($"[SceneFeature] RECENTERED");
            Debug.LogWarning($"[SceneFeature] Recenter. Anchor " +
                $"P:{AnchorManager.Instance.transform.position} " +
                $"R:{AnchorManager.Instance.transform.rotation.eulerAngles}" +
                $"LR:{AnchorManager.Instance.transform.localRotation.eulerAngles}");
            Debug.LogWarning($"[SceneFeature] Recenter. World " +
                $"P:{worldTransform.position} " +
                $"R:{worldTransform.rotation.eulerAngles}" +
                $"LR:{worldTransform.localRotation.eulerAngles}");


        }


        /// <summary>
        /// Recomputes skybox yaw based on world local rotation and optional landmark offset.
        /// </summary>
        private async void RecenterSkyBox()
        {
            await Task.Yield();
           // Debug.LogWarning("[SceneFeatureXR] RecenterSkyBox: " + worldTransform.localRotation.eulerAngles);
            float offset = currentLandMark != null ? currentLandMark.hdriOffsetRotation : 0;
            TwinnyManager.SetHDRIRotation(worldTransform.localRotation.eulerAngles.y + transform.rotation.eulerAngles.y - offset);
        }
        /*

        public static void SetPassthrough(bool status)
        {
            Debug.LogWarning("SetPassthrough: " + status);
            Camera.main.backgroundColor = Color.clear;
            if (status)
            {
                RenderSettings.skybox = TwinnyRuntime.GetInstance<TwinnyXRRuntime>().defaultSkybox;
                Camera.main.clearFlags = CameraClearFlags.SolidColor;
            }
            else
            {
                Camera.main.clearFlags = CameraClearFlags.Skybox;

            }

            OVRPassthroughLayer passThrough = FindAnyObjectByType<OVRPassthroughLayer>(FindObjectsInactive.Include);

            if (passThrough)
            {
                passThrough.enabled = status;
                passThrough.gameObject.SetActive(status);

            }
            else
            {
                Debug.LogWarning("[LevelManagerXR] SetPassthrough was not effective. Cause: 'Passthrough not found'");
                return;
            }

            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnSetPassthrough(status));

        }
        */

        /// <summary>
        /// Aligns this scene root with anchor position and forward direction.
        /// </summary>
        public void AnchorScene()
        {
            UndockScene();
            transform.rotation = GetForwardDirection(AnchorManager.rotation);
            transform.position = AnchorManager.position;
            // if (sceneType == SceneType.VR && GameMode.currentMode is TwinnyXRSingleplayer) return;
#if !UNITY_EDITOR
             DockScene();
#endif
        }

        /// <summary>
        /// Converts a rotation to a yaw-only forward quaternion.
        /// </summary>
        /// <param name="rotation">Source rotation.</param>
        /// <returns>Yaw-only quaternion.</returns>
        public Quaternion GetForwardDirection(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward; // Pega a direção que o Anchor olha
            forward.y = 0; // Mata a inclinação vertical
            return Quaternion.LookRotation(forward); // Cria a rotação baseada nesse vetor "plano"
        }

        /// <summary>
        /// Extracts yaw from a rotation and returns a yaw-only quaternion.
        /// </summary>
        /// <param name="rotation">Source rotation.</param>
        /// <returns>Yaw-only quaternion.</returns>
        public Quaternion GetYawRotation(Quaternion rotation)
        {
            float yaw = rotation.eulerAngles.y;
            return Quaternion.Euler(0f, yaw, 0f);
        }

        /// <summary>
        /// Ensures this scene root has an OVR spatial anchor component when anchoring is available.
        /// </summary>
        public void DockScene()
        {
            if (!this || !gameObject) return;

#if !UNITY_EDITOR
            if (AnchorManager.currentAnchor == null) return;
            OVRSpatialAnchor anchor = gameObject.GetComponent<OVRSpatialAnchor>();
            if (anchor == null) 
             gameObject.AddComponent<OVRSpatialAnchor>();
#endif
        }
        /// <summary>
        /// Removes OVR spatial anchor component from this scene root.
        /// </summary>
        public void UndockScene()
        {
            if (!this || !gameObject) return;

#if !UNITY_EDITOR
            if (AnchorManager.currentAnchor == null) return;
            OVRSpatialAnchor anchor = gameObject.GetComponent<OVRSpatialAnchor>();
            if (anchor != null) { Destroy(anchor); }
#endif
        }

        #endregion

        #region ITwinnyXRCallbacks
        /// <summary>
        /// Callback invoked when anchor manager state changes.
        /// </summary>
        /// <param name="state">New anchor manager state.</param>
        public void OnAnchorStateChanged(StateAnchorManager state)
        {

        }

        /// <summary>
        /// Callback invoked when passthrough status changes.
        /// </summary>
        /// <param name="status">True when passthrough is enabled.</param>
        public void OnSetPassthrough(bool status)
        {

        }

        /// <summary>
        /// Callback invoked when interaction starts on an object.
        /// </summary>
        /// <param name="gameObject">Interacted object.</param>
        public void OnStartInteract(GameObject gameObject)
        {

        }

        /// <summary>
        /// Callback invoked when interaction ends on an object.
        /// </summary>
        /// <param name="gameObject">Interacted object.</param>
        public void OnStopInteract(GameObject gameObject)
        {

        }

        /// <summary>
        /// Callback invoked when teleport operation starts.
        /// </summary>
        public void OnStartTeleport()
        {
            UndockScene();
        }


        /// <summary>
        /// Callback invoked when teleport operation completes.
        /// </summary>
        public void OnTeleport()
        {
            DockScene();
        }

        /// <summary>
        /// Callback invoked during platform initialization.
        /// </summary>
        public void OnPlatformInitializing()
        {

        }

        /// <summary>
        /// Callback invoked after platform initialization completes.
        /// </summary>
        public void OnPlatformInitialized()
        {

        }

        /// <summary>
        /// Callback invoked when experience enters ready state.
        /// </summary>
        public void OnExperienceReady()
        {

        }

        /// <summary>
        /// Callback invoked when experience start sequence begins.
        /// </summary>
        public void OnExperienceStarting()
        {

        }

        /// <summary>
        /// Callback invoked when experience start sequence completes.
        /// </summary>
        public void OnExperienceStarted()
        {

        }

        /// <summary>
        /// Callback invoked when experience end sequence begins.
        /// </summary>
        public void OnExperienceEnding()
        {

        }

        /// <summary>
        /// Callback invoked when experience end sequence completes.
        /// </summary>
        /// <param name="isRunning">True when runtime remains active after end flow.</param>
        public void OnExperienceEnded(bool isRunning)
        {

        }

        /// <summary>
        /// Callback invoked before additive scene loading starts.
        /// </summary>
        /// <param name="sceneName">Scene name being loaded.</param>
        public void OnSceneLoadStart(string sceneName)
        {

        }

        /// <summary>
        /// Callback invoked when additive scene loading completes.
        /// </summary>
        /// <param name="scene">Loaded scene instance.</param>
        public void OnSceneLoaded(Scene scene)
        {

        }

        /// <summary>
        /// Callback invoked after teleport to a landmark index is processed.
        /// </summary>
        /// <param name="landMarkIndex">Processed landmark index.</param>
        public void OnTeleportToLandMark(int landMarkIndex)
        {

        }

        /// <summary>
        /// Callback invoked when HDRI material changes and skybox yaw must be recentered.
        /// </summary>
        /// <param name="material">New HDRI material.</param>
        public void OnSkyboxHDRIChanged(Material material) => RecenterSkyBox();


        #endregion


    }

}
