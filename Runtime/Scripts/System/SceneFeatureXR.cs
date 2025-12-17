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

        //Awake is called before the script is started
        protected override void Awake()
        {
            base.Awake();
            _transform = transform;
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
            //    NavigationMenu.Instance.SetArrows(null);
        }

        private void OnDestroy()
        {
            SetupHDRI(-1);
            //CallbackHub.CallAction<IUICallBacks>(callback => callback.OnLoadExtensionMenu(null));

            if (OVRManager.display != null)
                OVRManager.display.RecenteredPose -= OnRecenterDetected;
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
            SetupHDRI(landMarkIndex);
            if (GameMode.currentMode is TwinnyXRSingleplayer) return;


                if (landMarks.Length > 0 && landMarkIndex >= 0)
            {
                Transform cameraRig = GameObject.FindAnyObjectByType<OVRCameraRig>().transform;

                if (_currentLandMark != null) _currentLandMark.node?.OnLandMarkUnselected?.Invoke();
                _currentLandMark = landMarks[landMarkIndex];

                cameraRig.position = Vector3.zero;
                cameraRig.rotation = Quaternion.identity;
                worldTransform.localPosition = Vector3.zero;
                worldTransform.localRotation = Quaternion.identity;

                float desiredAngle = transform.eulerAngles.y + _currentLandMark.node.transform.eulerAngles.y;

                Vector3 desiredPosition = -_currentLandMark.node.transform.localPosition;
                worldTransform.localPosition = desiredPosition;
                worldTransform.RotateAround(AnchorManager.Instance.transform.position, Vector3.up, -_currentLandMark.node.transform.localRotation.eulerAngles.y);

                //   NavigationMenu.Instance?.SetArrows(enableNavigationMenu ? _currentLandMark.node : null);

                SetHDRIRotation(worldTransform.localRotation.eulerAngles.y + transform.rotation.eulerAngles.y);

                _currentLandMark.node?.OnLandMarkSelected?.Invoke();

                bool turnParent = _currentLandMark.node.changeParent;


                if (turnParent)
                    cameraRig.SetParent(_currentLandMark.node.newParent);
                else
                {
                    cameraRig.SetParent(null);
                    cameraRig.position = Vector3.zero;
                    var activeScene = SceneManager.GetSceneByBuildIndex(0);
                    SceneManager.MoveGameObjectToScene(cameraRig.gameObject, activeScene);
                }
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
                PassthroughFader.TogglePassthroughAction(true,100f);
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

        private void SetHDRIRotation(float angle)
        {
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
        private async Task RecenterSkyBox()
        {
            await Task.Yield();
            Debug.Log("[SceneFeatureXR] RecenterSkyBox: " + worldTransform);
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
            transform.position = AnchorManager.Instance.transform.position;
            transform.rotation = AnchorManager.Instance.transform.rotation;
            if (sceneType == SceneType.VR && GameMode.currentMode is TwinnyXRSingleplayer) return;
#if !UNITY_EDITOR
             gameObject.AddComponent<OVRSpatialAnchor>();
#endif
        }

        #endregion
    }

}
