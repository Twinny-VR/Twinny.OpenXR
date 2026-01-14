using Concept.Core;
using System.Linq;
using System.Threading.Tasks;
using Twinny.Core;
using Twinny.XR.Navigation;
using Twinny.UI;
using Twinny.XR.Anchoring;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twinny.XR
{


    public class SceneFeatureXR : SceneFeature, ITwinnyXRCallbacks
    {
        #region Cached Components
        private Transform _transform;
        #endregion

        #region Fields
        public Transform worldTransform;
        [SerializeField] public SceneType sceneType;

        [SerializeField] public LandMark[] landMarks = new LandMark[0];
        [SerializeField] public bool enableNavigationMenu;
        //public GameObject extensionMenu;
        public bool isMenuStatic;
        private LandMark currentLandMark;
        private LandMark _currentLandMark;


        #endregion



        #region MonoBehaviour Methods   


#if UNITY_EDITOR

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
        private void OnEnable()
        {
            CallbackHub.RegisterCallback<ITwinnyXRCallbacks>(this);
        }


        //Awake is called before the script is started
        protected override void Awake()
        {
            base.Awake();
            _transform = transform;
            if (OVRManager.display != null)
            {
                OVRManager.TrackingAcquired += OnRecenterDetected;
                OVRManager.InputFocusAcquired += OnRecenterDetected;
                OVRManager.display.RecenteredPose += OnRecenterDetected;
            }
        }
        private Vector3 m_anchorStartPosition;
        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            PassthroughFader.TogglePassthroughAction(sceneType == SceneType.MR, 100f);

            AnchorScene();
            Debug.LogWarning($"[SceneFeature] RIG:{TwinnyXRManager.cameraRigTransform.position} {(int)TwinnyXRManager.cameraRigTransform.eulerAngles.y}º " +
                $"SAFE: {AnchorManager.position} {(int)AnchorManager.rotation.eulerAngles.y}º " +
                $"ANCH:{AnchorManager.currentAnchor.transform.position} {(int)AnchorManager.currentAnchor.transform.eulerAngles.y}º " +
                $"THIS:{transform.position} {(int)transform.eulerAngles.y}º" +
                $"WRLD:{worldTransform.position} {(int)worldTransform.eulerAngles.y}º" +
                $"LWRLD:{worldTransform.localPosition} {(int)worldTransform.localEulerAngles.y}º"
                          );

            m_anchorStartPosition = AnchorManager.position;

            if (fadeOnAwake)
            {
                //NetworkedLevelManager.Instance.RPC_FadingStatus(0);
                _ = CanvasTransition.FadeScreenAsync(false, TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime);
            }


            if (OVRManager.display != null)
                OVRManager.display.RecenteredPose += OnRecenterDetected;

            //if (extensionMenu) CallbackHub.CallAction<IUICallBacks>(callback => callback.OnLoadExtensionMenu(extensionMenu, isMenuStatic));


            int layer = LayerMask.NameToLayer("Character");

            GestureMonitor.SetHandForwardGestureRight(sceneType == SceneType.VR);
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

        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<ITwinnyXRCallbacks>(this);

            //    NavigationMenu.Instance.SetArrows(null);
        }

        private void OnDestroy()
        {
            // SetupHDRI(-1);
            //CallbackHub.CallAction<IUICallBacks>(callback => callback.OnLoadExtensionMenu(null));

            if (OVRManager.display != null)
            {
                OVRManager.TrackingAcquired -= OnRecenterDetected;
                OVRManager.InputFocusAcquired -= OnRecenterDetected;
                OVRManager.display.RecenteredPose -= OnRecenterDetected;
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
        /// This method change World Transform position to a especific landMark position.
        /// </summary>
        /// <param name="landMarkIndex">Index on landMarks array.</param>
        public override void TeleportToLandMark(int landMarkIndex)
        {
            Transform cameraRig = TwinnyXRManager.cameraRigTransform.transform;
            SetupHDRI(landMarkIndex);
            UndockScene();
            if (landMarks.Length > 0 && landMarkIndex >= 0)
            {
                // cameraRig.position = new Vector3(Camera.main.transform.position.x,cameraRig.position.y,Camera.main.transform.position.z);
                if (_currentLandMark != null) _currentLandMark.node?.OnLandMarkUnselected?.Invoke();
                _currentLandMark = landMarks[landMarkIndex];
                var node = _currentLandMark.node;

                //Debug.LogWarning($"MERDA ANCH:{AnchorManager.currentAnchor.transform.position} SA:{AnchorManager.position} PRNT:{worldTransform.parent.position} WP:{worldTransform.position} WLP:{worldTransform.localPosition}");

                worldTransform.position = m_anchorStartPosition;
                worldTransform.localPosition = Vector3.zero;
                worldTransform.rotation = AnchorManager.rotation;
                worldTransform.localRotation = Quaternion.identity;
                if (node.changeParent != null)
                {
                    Transform centerEye = Camera.main.transform;
                    var hmdOffset = centerEye.position - cameraRig.position;
                    hmdOffset.y = centerEye.position.y;
                    Vector3 desiredPosition = -node.transform.position - hmdOffset;
                    worldTransform.localPosition = desiredPosition;
                    // cameraRig.SetParent(node.changeParent);
                }
                else
                {
                    Vector3 nodeLocalPos = worldTransform.parent.InverseTransformPoint(node.transform.position);
                    //Vector3 desiredPosition = -(worldTransform.localRotation * nodeLocalPos);
                    Vector3 trackingDeltaLocal = Vector3.zero;
                    bool trackingDirty = (AnchorManager.position - m_anchorStartPosition).sqrMagnitude > 0.0001f;

                    if (trackingDirty)
                    {
                        Vector3 trackingDeltaWorld = AnchorManager.position - m_anchorStartPosition;

                        trackingDeltaLocal = worldTransform.parent.InverseTransformVector(trackingDeltaWorld);
                        // trackingDeltaLocal = worldRotation * trackingDeltaLocal;

                    }

                    float nodeYaw = GetYawRelativeToParent(node.transform, worldTransform.parent);
                    Quaternion invNodeYaw = Quaternion.Euler(0f, -nodeYaw, 0f);



                    Vector3 desiredPosition = -(invNodeYaw * nodeLocalPos) - trackingDeltaLocal;

                    worldTransform.localRotation = invNodeYaw;
                    worldTransform.localPosition = desiredPosition;

                    //Debug.LogWarning($"MERDA YAW:{nodeYaw} NLP:{nodeLocalPos} WP:{worldTransform.position} WLP:{worldTransform.localPosition}");
                    //   NavigationMenu.Instance?.SetArrows(enableNavigationMenu ? _currentLandMark.node : null);

                    SetHDRIRotation(worldTransform.localRotation.eulerAngles.y + transform.rotation.eulerAngles.y);
                    cameraRig.SetParent(null);
                    var activeScene = SceneManager.GetSceneByBuildIndex(0);
                    SceneManager.MoveGameObjectToScene(cameraRig.gameObject, activeScene);
                    cameraRig.position = Vector3.zero;
                    cameraRig.rotation = Quaternion.identity;
                }
                node?.OnLandMarkSelected?.Invoke();
            }
            else
            {
                _currentLandMark = null;
                if (worldTransform != null)
                {
                    worldTransform.position = AnchorManager.Instance.transform.position;
                    worldTransform.rotation = AnchorManager.Instance.transform.rotation;
                }
            }
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnTeleportToLandMark(landMarkIndex));

            if (worldTransform != null)
            {
                AnchorScene();
            }


            Debug.LogWarning($"[SceneFeature] OnTeleport RIG:{TwinnyXRManager.cameraRigTransform.position} {(int)TwinnyXRManager.cameraRigTransform.eulerAngles.y}º " +
                $"SAFE: {AnchorManager.position} {(int)AnchorManager.rotation.eulerAngles.y}º " +
                $"ANCH:{AnchorManager.currentAnchor.transform.position} {(int)AnchorManager.currentAnchor.transform.eulerAngles.y}º " +
                $"THIS:{transform.position} {(int)transform.eulerAngles.y}º" +
                $"WRLD:{worldTransform.position} {(int)worldTransform.eulerAngles.y}º" +
                $"LWRLD:{worldTransform.localPosition} {(int)worldTransform.localEulerAngles.y}º"
                          );

        }
        public void TeleportToLandMark_WORKING(int landMarkIndex)
        {
            Transform cameraRig = GameObject.FindAnyObjectByType<OVRCameraRig>().transform;
            SetupHDRI(landMarkIndex);
            UndockScene();
            if (landMarks.Length > 0 && landMarkIndex >= 0)
            {
                // cameraRig.position = new Vector3(Camera.main.transform.position.x,cameraRig.position.y,Camera.main.transform.position.z);
                if (_currentLandMark != null) _currentLandMark.node?.OnLandMarkUnselected?.Invoke();
                _currentLandMark = landMarks[landMarkIndex];
                var node = _currentLandMark.node;

                //Debug.LogWarning($"MERDA ANCH:{AnchorManager.currentAnchor.transform.position} SA:{AnchorManager.position} PRNT:{worldTransform.parent.position} WP:{worldTransform.position} WLP:{worldTransform.localPosition}");

                worldTransform.position = m_anchorStartPosition;
                worldTransform.localPosition = Vector3.zero;
                worldTransform.rotation = AnchorManager.rotation;
                worldTransform.localRotation = Quaternion.identity;
                if (node.changeParent != null)
                {
                    Transform centerEye = Camera.main.transform;
                    var hmdOffset = centerEye.position - cameraRig.position;
                    hmdOffset.y = centerEye.position.y;
                    Vector3 desiredPosition = -_currentLandMark.node.transform.position - hmdOffset;
                    worldTransform.localPosition = desiredPosition;
                    // cameraRig.SetParent(node.changeParent);
                }
                else
                {

                    Transform relative = worldTransform.parent;
                    float nodeYaw = GetYawRelativeToParent(node.transform, relative);

                    worldTransform.localRotation = Quaternion.Euler(0f, -nodeYaw, 0f);

                    Vector3 nodeLocalPos = relative.InverseTransformPoint(node.transform.position);
                    Vector3 desiredPosition = -(worldTransform.localRotation * nodeLocalPos);
                    bool trackingDirty = (AnchorManager.position - m_anchorStartPosition).sqrMagnitude > 0.0001f;

                    if (trackingDirty)
                    {
                        Vector3 trackingDeltaWorld = AnchorManager.position - m_anchorStartPosition;

                        Vector3 trackingDeltaLocal = relative.InverseTransformVector(trackingDeltaWorld);
                        desiredPosition -= trackingDeltaLocal;

                    }
                    worldTransform.localPosition = desiredPosition;
                    //Debug.LogWarning($"MERDA YAW:{nodeYaw} NLP:{nodeLocalPos} WP:{worldTransform.position} WLP:{worldTransform.localPosition}");
                    //   NavigationMenu.Instance?.SetArrows(enableNavigationMenu ? _currentLandMark.node : null);

                    SetHDRIRotation(worldTransform.localRotation.eulerAngles.y + transform.rotation.eulerAngles.y);
                    cameraRig.SetParent(null);
                    var activeScene = SceneManager.GetSceneByBuildIndex(0);
                    SceneManager.MoveGameObjectToScene(cameraRig.gameObject, activeScene);
                    cameraRig.position = Vector3.zero;
                    cameraRig.rotation = Quaternion.identity;
                }
                node?.OnLandMarkSelected?.Invoke();
            }
            else
            {
                _currentLandMark = null;
                if (worldTransform != null)
                {
                    worldTransform.position = AnchorManager.Instance.transform.position;
                    worldTransform.rotation = AnchorManager.Instance.transform.rotation;
                }
            }
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnTeleportToLandMark(landMarkIndex));

            if (worldTransform != null)
            {
                Debug.LogWarning($"[AnchorManager][LandMark] WORLD: {worldTransform.position} {worldTransform.rotation.eulerAngles.y}º" +
                    $" LOCAL: {worldTransform.localPosition} {worldTransform.localRotation.eulerAngles.y}º");
                AnchorScene();
            }

        }


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


        public LandMark GetLandMark(LandMarkNode node)
        {
            return landMarks.FirstOrDefault(o => o.node == node);
        }

        public int GetLandMarkIndex(LandMarkNode node)
        {
            for (int i = 0; i < landMarks.Length; i++)
            {
                if (landMarks[i].node == node)
                { return i; }
            }
            return -1;
        }

        public void OnRecenterDetected()
        {
            Debug.LogWarning($"[SceneFeature] Recenter Detected! RIG:{TwinnyXRManager.cameraRigTransform.position} {(int)TwinnyXRManager.cameraRigTransform.eulerAngles.y}º " +
                $"SAFE: {AnchorManager.position} {(int)AnchorManager.rotation.eulerAngles.y}º " +
                $"ANCH:{AnchorManager.currentAnchor.transform.position} {(int)AnchorManager.currentAnchor.transform.eulerAngles.y}º " +
                $"THIS:{transform.position} {(int)transform.eulerAngles.y}º" +
                $"WRLD:{worldTransform.position} {(int)worldTransform.eulerAngles.y}º" +
                $"LWRLD:{worldTransform.localPosition} {(int)worldTransform.localEulerAngles.y}º"
                );
            AnchorScene();
            _ = RecenterSkyBox();
        }


        #endregion

        #region Private Methods


        private void SetupHDRI(int landMarkIndex)
        {

            //TODO Melhorar o sistema de HDRI
            //TODO Arrumar ao troca de cena


            if (landMarkIndex < 0)//If no LandMark to set, reset skybox to Passthroug
            {
                PassthroughFader.TogglePassthroughAction(true, 100f);
                //RenderSettings.skybox = TwinnyRuntime.GetInstance<TwinnyXRRuntime>().defaultSkybox;
                return;
            }

            if (landMarks.Length > 0)
            {

                LandMark landMark = landMarks[landMarkIndex];
                SetHDRI(landMark.skyBoxMaterial);
                currentLandMark = landMark;
            }

        }



        private async void SetHDRIRotation(float angle)
        {
            await Task.Yield();
            await Task.Yield();
            if (!RenderSettings.skybox) { Debug.LogWarning("[SceneFeature] Warning! The Skybox Material has not been defined."); return; }

            angle -= currentLandMark.hdriOffsetRotation;

            angle = angle % 360;

            if (angle < 0)
            {
                angle += 360;
            }

            float rotationOffset = 0;

            if (angle > 0)
                rotationOffset = 360f - angle;
            else
                rotationOffset = angle + 360;


            rotationOffset = Mathf.Clamp(rotationOffset, 0, 360);

            RenderSettings.skybox.SetFloat("_Rotation", rotationOffset);
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
        private async Task RecenterSkyBox()
        {
            await Task.Yield();
            // Debug.LogWarning("[SceneFeatureXR] RecenterSkyBox: " + worldTransform.localRotation.eulerAngles);
            SetHDRIRotation(worldTransform.localRotation.eulerAngles.y + transform.rotation.eulerAngles.y);
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

        public void AnchorScene()
        {
            UndockScene();
            transform.position = AnchorManager.position;
            transform.rotation = AnchorManager.rotation;
            // if (sceneType == SceneType.VR && GameMode.currentMode is TwinnyXRSingleplayer) return;
#if !UNITY_EDITOR
             DockScene();
#endif
        }

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
        public void OnAnchorStateChanged(StateAnchorManager state)
        {

        }

        public void OnSetPassthrough(bool status)
        {

        }

        public void OnStartInteract(GameObject gameObject)
        {

        }

        public void OnStopInteract(GameObject gameObject)
        {

        }

        public void OnStartTeleport()
        {
            UndockScene();
        }


        public void OnTeleport()
        {
            DockScene();
        }

        public void OnPlatformInitializing()
        {

        }

        public void OnPlatformInitialized()
        {

        }

        public void OnExperienceReady()
        {

        }

        public void OnExperienceStarting()
        {

        }

        public void OnExperienceStarted()
        {

        }

        public void OnExperienceEnding()
        {

        }

        public void OnExperienceEnded(bool isRunning)
        {

        }

        public void OnSceneLoadStart(string sceneName)
        {

        }

        public void OnSceneLoaded(Scene scene)
        {

        }

        public void OnTeleportToLandMark(int landMarkIndex)
        {

        }
        #endregion


    }

}
