using Meta.XR.BuildingBlocks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Meta.XR.MultiplayerBlocks.Shared;
using Twinny.XR;
using Concept.Helpers;
using Twinny.Core;
using Concept.Core;

namespace Twinny.XR.Anchoring
{

    public enum StateAnchorManager
    {
        DISABLED,
        ANCHORED,
        ANCHORING
    }

    /// <summary>
    /// This class handles the Anchors Management
    /// </summary>
    public class AnchorManager : TSingleton<AnchorManager>
    {

        #region Cached Components
        private Transform _transform;
        #endregion

        public static Vector3 position { get => Instance._transform.position; }
        public static Quaternion rotation { get => Instance._transform.rotation; }


        #region Fields
        private Transform _mainCamera;
        [SerializeField] private Transform _cameraRig;
        [SerializeField] private OVRSpatialAnchor _currentAnchor;
        public static OVRSpatialAnchor currentAnchor { get { return Instance?._currentAnchor; } } //set { _currentAnchor = value; } }

        [SerializeField] private SpatialAnchorCoreBuildingBlock _spatialAnchorCore;
        [SerializeField] private SpatialAnchorLoaderBuildingBlock _spatialAnchorLoader;
        [SerializeField] private SpatialAnchorSpawnerBuildingBlock _spatialAnchorSpawner;
        [SerializeField] private float _maxDistance = 5f;

        [SerializeField] private bool _usePinchToAnchor = false;


        [SerializeField]
        private Component _alignCameraToAnchor;



        [SerializeField]
        private GameObject _colocationPrefab;

        [SerializeField]
        private GameObject _colocation;

        //[Tooltip("Width/Height")]
        private Vector2 _safeAreaSize;

        [SerializeField]
        private StateAnchorManager _stateAnchorManager = StateAnchorManager.DISABLED;
        private StateAnchorManager _state
        {
            get { return _stateAnchorManager; }
            set
            {

                CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnAnchorStateChanged(value));

                _stateAnchorManager = value;

                if (value == StateAnchorManager.ANCHORING)
                {
                    RemoveColocation();
                    _transform.SetParent(null);
                    _spatialAnchorCore.EraseAllAnchors();



                    /*
                    MethodInfo getComponentMethod = typeof(GameObject).GetMethod("GetComponent", new Type[] { typeof(Type) });

                    if(getComponentMethod != null)
                    {
                        var component = getComponentMethod.Invoke(Camera.main.gameObject, new object[] { alignCameraToAnchorType });
                        
                        if(component != null)
                        {
                            Debug.LogWarning("ACHOU O COMPONENTE!");
                        }else
                        Debug.LogWarning("NAO ACHOU O COMPONENTE");


                    }
                    else
                        Debug.LogWarning("NAO ACHOU METODO!");
                    */
                    /*
                    OVRSpatialAnchor currentAnchor;
                    TryGetComponent<OVRSpatialAnchor>(out currentAnchor);
                    if (currentAnchor != null) LevelManager.CallDelayedAction(() => { Destroy(GetComponent<OVRSpatialAnchor>()); },.5f);
                    */

                }


            }
        }

        public StateAnchorManager state { get => _stateAnchorManager; }

        public bool isInSafeArea;

        #endregion

        #region Delegates
        public delegate void onSafeAreaEntered(bool status);
        public static onSafeAreaEntered OnSafeAreaEntered;
        #endregion

        #region MonoBehaviour Methods


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
            _safeAreaSize = TwinnyRuntime.GetInstance<TwinnyXRRuntime>().safeAreaSize;

            //Set callbacks listeners
            _spatialAnchorCore.OnAnchorsLoadCompleted.AddListener(OnAnchorsLoadCompleted);
            _spatialAnchorCore.OnAnchorCreateCompleted.AddListener(OnAnchorCreateCompleted);
            _spatialAnchorCore.OnAnchorEraseCompleted.AddListener(OnAnchorEraseCompleted);
            //Set callbacks delegates
            GestureMonitor.Instance.OnPinchLeft += OnPinchLeft;
            GestureMonitor.Instance.OnPinchRight += OnPinchRight;

            _spatialAnchorLoader.LoadAnchorsFromDefaultLocalStorage();

            _mainCamera = Camera.main.transform;

            StartCoroutine(CheckPlayerInSafeArea());
            Instance.StartCoroutine(Instance.GetAlignCameraToAnchorCoroutine());
        }

        protected override void Update()
        {
            base.Update();

            //Anchor placement system
            if (_state != StateAnchorManager.ANCHORING) return;
            Handling();
        }

        //OnDestroy is called when component is removed        
        private void OnDestroy()
        {
            //Unset Delegates
            GestureMonitor.Instance.OnPinchLeft -= OnPinchLeft;
            GestureMonitor.Instance.OnPinchRight -= OnPinchRight;
            //Unset listeners
            _spatialAnchorCore.OnAnchorsLoadCompleted.RemoveListener(OnAnchorsLoadCompleted);
            _spatialAnchorCore.OnAnchorCreateCompleted.RemoveListener(OnAnchorCreateCompleted);
            _spatialAnchorCore.OnAnchorEraseCompleted.RemoveListener(OnAnchorEraseCompleted);

        }
        #endregion



        #region Public Methods

        /// <summary>
        /// This method switch Anchor Placement ON/OFF
        /// </summary>
        /// <param name="status">Is handling switch.</param>
        public static void HandleAnchorPlacement()
        {

            if (Instance._state == StateAnchorManager.DISABLED || Instance._state == StateAnchorManager.ANCHORED)
            {
                Instance._state = StateAnchorManager.ANCHORING;
            }
            else
                if (Instance._state == StateAnchorManager.ANCHORING)
            {
                CreateAnchor();
            }

            //            Instance._state = (Instance._state == StateAnchorManager.DISABLED) ? StateAnchorManager.ANCHORING : StateAnchorManager.DISABLED;
        }

        /// <summary>
        /// Creates an anchor from SafeArea transform 
        /// </summary>
        public static void CreateAnchor()
        {
            SharedSpatialAnchorCore core = new SharedSpatialAnchorCore();
            Instance.transform.SetParent(null);
            Instance._spatialAnchorCore.EraseAllAnchors();
            Vector3 desiredPosition = Instance._transform.position;
            desiredPosition.y = 0;
            Instance._spatialAnchorSpawner.SpawnSpatialAnchor(desiredPosition, Instance._transform.rotation);
        }

        public async static void Recolocation()
        {
            return;
            await Task.Delay(500);

            if (Instance._colocation != null)
            {
                Destroy(Instance._colocation);
                await Task.Delay(500);
                Debug.LogWarning("[AnchorManager] Colocation retargeting.");
                SpawnColocation();
                //LevelManager.CallDelayedAction(() =>{}, .5f);
            }
        }

        #endregion

        #region Private Methods

        public static void SpawnColocation()
        {
#if FUSION2
            if (GameMode.currentMode is TwinnyXRMultiplayer)
            {
                if (Instance._colocation == null)
                {
                    Instance._colocation = Instantiate(Instance._colocationPrefab);
                    ColocationController controller = Instance._colocation.GetComponent<ColocationController>();
                    if (controller != null)
                    {
                        controller.ColocationReadyCallbacks.AddListener(OnColocationReady);
                    }
                    else Debug.LogWarning("[AnchorManager] ColocationController not found!");

                }

                Instance.StartCoroutine(Instance.GetAlignCameraToAnchorCoroutine());
            }
#endif
        }

        private void RemoveColocation()
        {
            return;
            if (_colocation != null)
            {
                ColocationController controller = _colocation.GetComponent<ColocationController>();
                if (controller != null)
                {

                    controller.ColocationReadyCallbacks.RemoveListener(OnColocationReady);
                }

                Destroy(_colocation.gameObject);
            }
            if (_alignCameraToAnchor != null)
            {
                // ((Behaviour)_alignCameraToAnchor).enabled = status;
                Debug.LogWarning("DESTRÓI AlignCameraToAnchor");
                Destroy(_alignCameraToAnchor);
            }
            else
                Debug.LogWarning("NÃO ENCONTROU AlignCameraToAnchor");
        }


        private static void OnColocationReady()
        {
            Debug.LogWarning("[AnchorManager] COLOCATION READY!");
        }


        /// <summary>
        /// This method is a callback for when SpacialAnchorCoreBuildingBlock finishes the Anchors Loading
        /// </summary>
        /// <param name="loadedAnchors">A list of OVRSpatialAnchors received by Core</param>
        private void OnAnchorsLoadCompleted(List<OVRSpatialAnchor> loadedAnchors)
        {
            if (loadedAnchors.Count == 0)
            {
                Debug.LogWarning($"[{nameof(AnchorManager)}] None anchor loaded.");
                _stateAnchorManager = StateAnchorManager.DISABLED;
                return;
            }
            OVRSpatialAnchor anchor = loadedAnchors[0];
            Debug.LogWarning($"[{nameof(AnchorManager)}] Anchors loaded successfully! ({anchor.transform.position})");



            PlaceSafeArea(anchor);
            // _stateAnchorManager = StateAnchorManager.ANCHORED;

            // _transform.gameObject.AddComponent<OVRSpatialAnchor>();
        }

        public static void PlaceSafeArea(OVRSpatialAnchor anchor)
        {

            PlaceSafeArea(anchor.transform.position, Quaternion.Euler(0, anchor.transform.rotation.eulerAngles.y, 0), anchor.transform);
            if (Instance != null)
                Instance._currentAnchor = anchor;

        }
        public static void PlaceSafeArea(Vector3 position, Quaternion rotation) => PlaceSafeArea(position, rotation, null);
        public static void PlaceSafeArea(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (Instance == null)
            {
                Debug.LogError("[AnchorManager] Instace not found!");
                return;
            }
            Instance.transform.SetParent(null);

            Instance._transform.SetPositionAndRotation(position, rotation);

            Instance._transform.SetParent(parent);
        }

        public static void ResetSafeArea()
        {
            if (Instance == null)
            {
                Debug.LogError("[AnchorManager] Instace not found!");
                return;
            }

            if (Instance._currentAnchor != null) return;

            PlaceSafeArea(Instance._cameraRig.transform.position, Instance._cameraRig.transform.rotation, null);
        }

        /// <summary>
        /// This method is a callback for when SpacialAnchorCoreBuildingBlock creates an anchor.
        /// </summary>
        /// <param name="anchor">Anchor created by Core.</param>
        /// <param name="result">Possible results of various AnchorCore operations.</param>
        private void OnAnchorCreateCompleted(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
        {
            /*
            OVRSpatialAnchor currentAnchor;
            TryGetComponent<OVRSpatialAnchor>(out currentAnchor);

            if (currentAnchor == null)
                _transform.gameObject.AddComponent<OVRSpatialAnchor>();

            */

            _state = (result.IsSuccess()) ? StateAnchorManager.ANCHORED : StateAnchorManager.DISABLED;
            if (result.IsSuccess())
            {
                _transform.SetParent(anchor.transform);
                _currentAnchor = anchor;
                SpawnColocation();
                Debug.LogWarning("[AnchorManager] Anchor create successfully!");



            }
            else
                Debug.LogError($"[AnchorManager] Failed to create anchor! Result: {result}");

        }

        private void OnAnchorEraseCompleted(OVRSpatialAnchor anchor, OVRSpatialAnchor.OperationResult result)
        {
            _currentAnchor = anchor;

        }

        /// <summary>
        /// This method align the Anchor and SafeArea acording the spectate view
        /// </summary>
        private void Handling()
        {
            Vector3 forwardDirection = _mainCamera.transform.forward;
            forwardDirection.y = 0;
            forwardDirection.Normalize();

            float cameraPitch = _mainCamera.transform.eulerAngles.x;

            if (cameraPitch > 180)
            {
                cameraPitch -= 360;  // Limits the X axis rotation between -180 and 180 degrees.
            }

            float targetDistance = Mathf.Lerp(_maxDistance, 0f, Mathf.InverseLerp(-70f, 90f, cameraPitch));
            Vector3 targetPosition =
    _mainCamera.transform.position +
    forwardDirection * targetDistance +
    forwardDirection * TwinnyRuntime.GetInstance<TwinnyXRRuntime>().anchoringDistanceOffset;

            // Vector3 targetPosition = _mainCamera.transform.position + forwardDirection * targetDistance;
            Quaternion targetRotation = Quaternion.LookRotation(forwardDirection);

            //Sets Visual Anchor and Safe Area placement by the anchor
            _transform.SetPositionAndRotation(new Vector3(targetPosition.x, 0, targetPosition.z), targetRotation);
        }


        /// <summary>
        /// This Method is a callback from GestureMonitor Hand Left Pinch Action
        /// </summary>
        private void OnPinchLeft()
        {
            if (!_usePinchToAnchor || _state != StateAnchorManager.ANCHORED) return;
            _state = StateAnchorManager.ANCHORING;
        }


        /// <summary>
        /// This Method is a callback from GestureMonitor Hand Right Pinch Action
        /// </summary>
        private void OnPinchRight()
        {
            if (!_usePinchToAnchor || _state != StateAnchorManager.ANCHORING) return;
            CreateAnchor();

        }


        bool IsPlayerInSafeArea()
        {
            // Get SafeArea center
            Vector3 safeAreaCenter = _transform.position;

            float halfWidth = _safeAreaSize.x / 2f;
            float halfHeight = _safeAreaSize.y / 2f;

            // Check if is inside Safe Area
            bool withinWidth = _mainCamera.position.x >= safeAreaCenter.x - halfWidth && _mainCamera.position.x <= safeAreaCenter.x + halfWidth;
            bool withinHeight = _mainCamera.position.z >= safeAreaCenter.z - halfHeight && _mainCamera.position.z <= safeAreaCenter.z + halfHeight;

            return withinWidth && withinHeight;
        }


        #endregion
        #region Coroutines

        IEnumerator GetAlignCameraToAnchorCoroutine()
        {
            while (_alignCameraToAnchor == null)
            {
                Component[] components = _cameraRig.GetComponents<Component>();
                yield return new WaitForSeconds(1f);
                foreach (Component comp in components)
                {
                    if (comp.GetType().Name == "AlignCameraToAnchor")
                        _alignCameraToAnchor = comp;
                }
            }
        }

        IEnumerator CheckPlayerInSafeArea()
        {
            while (true)
            {

                if (isInSafeArea != IsPlayerInSafeArea())
                {
                    isInSafeArea = IsPlayerInSafeArea();
                    OnSafeAreaEntered?.Invoke(isInSafeArea);
                }
                yield return new WaitForSeconds(.1f);

            }
        }



        #endregion

    }

}
