using Concept.Core;
using Concept.Helpers;
using System;
using System.Collections;
using Twinny.XR;
using Twinny.XR.Anchoring;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Twinny.UI
{
    [Serializable] public class OnPlayerListEvent : UnityEvent<int> { }


    /// <summary>
    /// This is a singleton class to control H.U.D Anchor elements
    /// </summary>
    public class HUDManagerXR : MonoBehaviour, IUICallBacks, ITwinnyXRCallbacks
    {
        #region Fields

        [Header("COMPONENTS")]

        [Space]
        [Tooltip("H.U.D Estática")]
        [SerializeField] private GameObject _staticHud;

        [Space]
        [Tooltip("H.U.D Dinâmica segue o F.O.V")]
        [SerializeField] private GameObject _dynamicHud;
        [SerializeField] private Transform _canvasRoot;
        [SerializeField] private GameObject _configMenu;
        [SerializeField] private GameObject _mainMenu;
        // [SerializeField] private Transform _mainMenu2;
        [Space]
        [Tooltip("Ângulo limite de visão até iniciar rotação.")]
        [SerializeField] private float _dynamicTreesholdAngle = 30f;
        [Tooltip("Velocidade de rotação da HUD.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _dynamicRotationSpeed = .5f;

        [Space]
        [Tooltip("H.U.D Dinâmica de Navegação")]
        [SerializeField] private GameObject _navigationHud;


        private bool _isFollowing;


        private Coroutine _fadeCoroutine;
        private Coroutine _fadeDynamicCoroutine;
        private Coroutine _fadeNavCoroutine;
        private Vector3 _previousCameraPos;

        #endregion
        private Transform _mainCameraTransform;
        #region Delegates

        public delegate void onPlayerListEvent(int count);
        public onPlayerListEvent OnPlayerListEvent;

        #endregion

        [SerializeField] public UnityEvent OnInternetConnectedEvent;
        [SerializeField] public UnityEvent OnInternetDisconnectedEvent;
        [SerializeField] public UnityEvent OnSingleplayerConnectEvent;
        [SerializeField] public UnityEvent OnMultiplayerConnectEvent;
        [SerializeField] public UnityEvent OnDisconnectEvent;



        #region MonoBehaviour Methods

        private void OnEnable()
        {
            CallbackHub.RegisterCallback<IUICallBacks>(this);
            CallbackHub.RegisterCallback<ITwinnyXRCallbacks>(this);
            
        }

        //Awake is called before the script is started
        private void Awake()
        {
        }

        // Start is called before the first frame update
        void Start()
        {
/*
            NetworkUtils.OnInternetConnectedEvent.AddListener(OnInternetConnected);
            NetworkUtils.OnInternetDisconnectedEvent.AddListener(OnInternetDisconnected);
*/ 
            //_banner.transform.SetParent(AnchorManager.Instance.transform);
            _staticHud.transform.SetParent(AnchorManager.Instance.transform);
            _staticHud.transform.localPosition = Vector3.zero;
            _staticHud.transform.localRotation = Quaternion.identity;

            _mainCameraTransform = Camera.main.transform;
            _previousCameraPos = _mainCameraTransform.position;
           // _mainMenu.SetActive(false);

/*
            bool hasInternet = NetworkUtils.IsWiFiConnected();

            if (hasInternet)
                OnInternetConnected();
            else
                OnInternetDisconnected();

            if (NetworkRunnerHandler.runner != null)
                OnConnected(NetworkRunnerHandler.runner.GameMode);
*/
        }

        // Update is called once per frame
        void Update()
        {
            if (_dynamicHud) FollowUser(_dynamicHud.transform, _dynamicRotationSpeed, _dynamicTreesholdAngle);
            if (_navigationHud) FollowUser(_navigationHud.transform, 0);

            float distance = Vector3.Distance(_previousCameraPos, _mainCameraTransform.position);
            distance = Mathf.Round(distance * 100f) / 100f;
            if (distance > .1f)
            {
                _previousCameraPos = _mainCameraTransform.position;
                FadeHud(true);
            }
            else
                FadeHud(false);
            //if (_configMenu != null) FollowUser2(_configMenu.transform, 0.5f);
        }

        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<IUICallBacks>(this);
            CallbackHub.UnregisterCallback<ITwinnyXRCallbacks>(this);
/*
             NetworkUtils.OnInternetConnectedEvent.RemoveListener(OnInternetConnected);
            NetworkUtils.OnInternetDisconnectedEvent.RemoveListener(OnInternetDisconnected);
*/
        }

        private void OnDestroy()
        {
            Destroy(_staticHud);
        }
        #endregion

        #region Public Methods



        public void FadeHud(bool status, float limit = 0f)
        {
            //TODO Criar sistema de esmaecer controles
            // if (status) Debug.Log("MOVIMENTANDO");
        }


        public void HideHud(bool status, float limit = 0f)
        {
            _canvasRoot.gameObject.SetActive(!status);
            //TODO Criar sistema de esmaecer controles
            // if (status) Debug.Log("MOVIMENTANDO");
        }

        /*
        /// <summary>
        /// Set all handeables UI elements active.
        /// </summary>
        /// <param name="elements">Array of elements that only will be activated.</param>
        public void SetElementActive(string[] elements = null)
        {

            foreach (HudElement item in _hudElements)
            {
                item.element.SetActive(false);
                if (elements != null)
                    foreach (var element in elements)
                    {
                        Debug.LogWarning(element+"|"+item.key);
                        if (element == item.key)
                        {

                            item.element.SetActive(true);
                        }
                    }
            }
        }


        /// <summary>
        /// This method insert a new loaded scene menus
        /// </summary>
        /// <param name="menu">Menu UI object</param>
        /// <param name="isStatic">Default:false</param>
        private void LoadExtensionMenu(UnityEngine.GameObject menu, bool isStatic = false)
        {


            if (_extensionMenu != null)
            {
                Destroy(_extensionMenu.gameObject);
            }

            if (menu != null)
            {
                _extensionMenu = Instantiate(menu, isStatic ? _staticHud.transform : _canvasRoot);

          //      _extensionMenu.SetActive(NetworkedLevelManager.IsManager);
                HudElement he = new HudElement();
                he.key = menu.name;
                he.element = _extensionMenu;
            }
        }

        */

        #endregion


        #region Private Methods


        /// <summary>
        /// This method makes HUD follow the user orbit.
        /// </summary>
        /// <param name="tracer">Tracer object.</param>
        /// <param name="speed">Trajectory velocity.</param>
        /// <param name="treesholdAngle">F.O.V Angle Limit until following.</param>
        private void FollowUser(Transform tracer, float speed, float treesholdAngle = 0f)
        {
            if (speed > 0)
            {

                Quaternion targetRotation = Quaternion.Euler(0f, _mainCameraTransform.eulerAngles.y, 0f);

                float angleDifference = Quaternion.Angle(tracer.rotation, targetRotation);

                if (angleDifference > treesholdAngle || _isFollowing)
                {

                    tracer.rotation = Quaternion.Slerp(tracer.rotation, targetRotation, (10f * speed) * Time.deltaTime);
                    //Debug.Log(angleDifference);
                    _isFollowing = (angleDifference > 1f);
                }
            }
            Vector3 desiredPosition = _mainCameraTransform.position;
            desiredPosition.y = 0f;

            tracer.position = desiredPosition;
        }

        private void OnInternetConnected()
        {
            OnInternetConnectedEvent?.Invoke();
        }

        private void OnInternetDisconnected()
        {
            OnInternetDisconnectedEvent?.Invoke();
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// This coroutine fades the canvas element smoothly
        /// </summary>
        /// <param name="canvas">Canvas Object</param>
        /// <param name="targetAlpha">Desired alpha value</param>
        /// <param name="delay">Delay before start motion</param>
        /// <param name="duration">Motion time</param>
        /// <param name="callback">bool return: Callback function (true for hided, false for showing)</param>
        /// <returns></returns>
        private IEnumerator FadeCanvas(CanvasGroup canvas, float targetAlpha, float delay, float duration, Action<bool> callback = null)
        {
            float startAlpha = canvas.alpha;
            float elapsedTime = 0f;

            yield return new WaitForSeconds(delay);

            // Smooth fade progress
            while (elapsedTime < duration)
            {
                canvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / duration);

                elapsedTime += Time.deltaTime;

                yield return null;  // Espera o próximo frame
            }

            //Force final alpha result
            canvas.alpha = targetAlpha;

            //Send callback to caller function
            if (callback != null)
            {
                callback((canvas.alpha == 0));
            }
        }

        #endregion

        #region UI Buttons Actions
        public void HandleAnchor()
        {
           AnchorManager.HandleAnchorPlacement();
        }

        /*
        public void CreateAnchor()
        {
            AnchorManager.CreateAnchor();
        }
        */
        #endregion

        #region CallBack Methods

        private void OnSafeAreaEntered2(bool status)
        {

        }

        /*
        public void OnSwitchManager(int source)
        {
            Debug.LogWarning("Switch Manager chamado no HUDManager!");


            if (source == NetworkRunnerHandler.runner.LocalPlayer.PlayerId)
                AsyncOperationExtensions.CallDelayedAction(() =>
                {

                    _extensionMenu.SetActive(true);
                    var feature = SceneFeature.Instance as SceneFeatureXR;
                    if (feature.enableNavigationMenu)
                        NavigationMenu.Instance?.SetArrows(feature?.landMarks[NetworkedLevelManager.Instance.currentLandMark].node);
                }, 500);

            Debug.LogError("[HUDManagerXR] Error impossible to connect without a multiplayer system installed.");

        }



        public void OnLoadExtensionMenu(GameObject menu, bool isStatic)
        {
            LoadExtensionMenu(menu, isStatic);

        }

        */
        public void OnSceneLoadStart(string sceneName)
        {

        }


        public void OnHudStatusChanged(bool status)
        {
            HideHud(status);
        }

        public void OnSceneLoaded(Scene scene)
        {

            AsyncOperationExtensions.CallDelayedAction(() =>
            {

                //TODO Make inactive and fadeout H.U.D

                bool active = true;
                //active = NetworkedLevelManager.IsManager;

                //if (_extensionMenu) _extensionMenu.SetActive(active); else _mainMenu?.SetActive(true);

                //_navigationHud.SetActive((SceneFeature.Instance as SceneFeatureXR).enableNavigationMenu);
                _dynamicHud.SetActive(true);

            }, 500);
        }

        public void OnUnloadSceneFeature()
        {

        }

        public void OnLoadScene()
        {
        }

        public void OnAnchorStateChanged(StateAnchorManager state)
        {
            Debug.LogWarning("OnAnchorStateChanged");

            bool isActive = state == StateAnchorManager.DISABLED || state == StateAnchorManager.ANCHORED;
            //TODO Make inactive and fadeout H.U.D
                _mainMenu?.SetActive(isActive);
        //    var feature = SceneFeature.Instance as SceneFeatureXR;
         //   _navigationHud?.SetActive(isActive && feature && feature.enableNavigationMenu);

        }

        public void OnPlatformInitializing() { }

        public void OnPlatformInitialized()
        {
 //           AlertViewHUD.PostMessage("CONECTANDO...", AlertViewHUD.MessageType.Warning, RuntimeXR.GetInstance<RuntimeXR>().connectionTimeout);
        }

        public void OnExperienceStarting() {
            _mainMenu.SetActive(false);
        }
        public void OnExperienceStarted() {

       //     _mainMenu.SetActive(true);

        }
        public void OnExperienceReady()
        {
            AlertViewHUD.CancelMessage();
            //_mainMenu.SetActive(true);
        }

        public void OnExperienceEnding() { }

        public void OnExperienceEnded(bool isRunning)
        {
            _dynamicHud?.SetActive(isRunning);
            _mainMenu?.SetActive(isRunning);
        }

        public void OnCameraChanged(Transform camera, string type) { }

        public void OnCameraLocked(Transform target) { }

        public void OnStandby(bool status) { }

        public void OnPlayerList(int count)
        {
            OnPlayerListEvent?.Invoke(count);
        }

        public void OnSetPassthrough(bool status)
        {
        }

        public void OnStartInteract(GameObject gameObject)
        {
            _mainMenu.SetActive(false);
        }

        public void OnStopInteract(GameObject gameObject)
        {
            _mainMenu.SetActive(true);
        }

        public void OnTeleportToLandMark(int landMarkIndex)
        {
        }

        public void OnStartTeleport() { }
        public void OnTeleport()
        {
            Transform rig = FindAnyObjectByType<OVRCameraRig>().transform;
            var newPos = rig.position;
            newPos.y = 0;
            rig.position = newPos;

           // Debug.LogWarning($"[HUDManagerXR] RIG:{rig.position} CanvasRoot:{_canvasRoot.position}");
        }

        public void OnSkyboxHDRIChanged(Material material) { }


        /*
        public void OnConnected(GameMode gameMode)
        {
            if (gameMode == GameMode.Single)
                OnSingleplayerConnectEvent?.Invoke();
            else
                OnMultiplayerConnectEvent?.Invoke();
        }

        public void OnDisconnected()
        {
                OnDisconnectEvent?.Invoke();
        }
        */
        #endregion
    }


    [Serializable]
    public class HudElement
    {
        public string key;
        public GameObject element;

    }
}