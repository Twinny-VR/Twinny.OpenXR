using Concept.Core;
using System.Linq;
using System.Threading.Tasks;
using Twinny.Core;
using Twinny.Navigation;
using Twinny.UI;
using Twinny.XR.Anchoring;
using UnityEngine;
using UnityEngine.SceneManagement;
using static OVRHaptics;

namespace Twinny.XR
{


    public class SceneFeatureXR : SceneFeature
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
            //    CallbackHub.RegisterCallback<ITwinnyXRCallbacks>(this);
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

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            PassthroughFader.TogglePassthroughAction(sceneType == SceneType.MR, 100f);

            AnchorScene();

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
            //CallbackHub.UnregisterCallback<ITwinnyXRCallbacks>(this);
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
            Transform cameraRig = GameObject.FindAnyObjectByType<OVRCameraRig>().transform;

            SetupHDRI(landMarkIndex);

            if (landMarks.Length > 0 && landMarkIndex >= 0)
            {
                // cameraRig.position = new Vector3(Camera.main.transform.position.x,cameraRig.position.y,Camera.main.transform.position.z);
                if (_currentLandMark != null) _currentLandMark.node?.OnLandMarkUnselected?.Invoke();
                _currentLandMark = landMarks[landMarkIndex];
                var node = _currentLandMark.node;

                    worldTransform.localPosition = Vector3.zero;
                    worldTransform.localRotation = Quaternion.identity;
                    cameraRig.localPosition = Vector3.zero;
                    cameraRig.localRotation = Quaternion.identity;
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


                    float nodeYaw = GetYawRelativeToParent(node.transform, worldTransform.parent);

                    worldTransform.localRotation = Quaternion.Euler(0f, -nodeYaw, 0f);

                    Vector3 nodeLocalPos = worldTransform.parent.InverseTransformPoint(node.transform.position);

                    worldTransform.localPosition = -(worldTransform.localRotation * nodeLocalPos);

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

            Debug.LogWarning($"[AnchorManager][LandMark] WORLD: {worldTransform.position} {worldTransform.rotation.eulerAngles.y}º" +
                $" LOCAL: {worldTransform.localPosition} {worldTransform.localRotation.eulerAngles.y}º");


        }

        static float GetYawRelativeToParent(Transform target, Transform parent)
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
            Debug.LogWarning($"[SceneFeatureXR] SetHDRIRotation: {angle}°");

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
            Debug.LogWarning("[SceneFeatureXR] RecenterSkyBox: " + worldTransform.localRotation.eulerAngles);
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
            transform.position = AnchorManager.position;
            transform.rotation = AnchorManager.rotation;
            Debug.LogWarning($"[SceneFeature] Game Mode: {GameMode.currentMode}");
            Debug.LogWarning($"[SceneFeature] Scene Type: {sceneType}");
            if (sceneType == SceneType.VR && GameMode.currentMode is TwinnyXRSingleplayer) return;
#if !UNITY_EDITOR
             gameObject.AddComponent<OVRSpatialAnchor>();
#endif
        }
        #endregion


    }

}
