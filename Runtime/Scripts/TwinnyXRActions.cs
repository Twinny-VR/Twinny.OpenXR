using Concept.Core;
using NUnit.Framework.Internal;
using System.Collections.Generic;
using System.Threading.Tasks;
using Twinny.Core;
using Twinny.XR.Anchoring;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Twinny.XR
{
    public class TwinnyXRActions : MonoBehaviour, ITwinnyXRCallbacks
    {

        private IGameMode m_gameMode => GameMode.currentMode;
        // Start is called once before the first execution of Update after the MonoBehaviour is created

        #region Event Actions

        [SerializeField] private UnityEvent OnAnchoringEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnAnchoredEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnActivePassthroughEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnPassthroughDisabledEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnPlatformInitializingEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnPlatformInitializedEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnExperienceReadyEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnExperienceStartingEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnExperienceStartedEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnExperienceEndingEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnExperienceEndedEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnSceneLoadStartEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnSceneLoadedEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnStartInteractEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnStopInteractEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnStartTeleportEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnTeleportEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnTeleportToLandMarkEvent = new UnityEvent();
        [SerializeField] private UnityEvent OnSkyboxHDRIChangedEvent = new UnityEvent();

        #endregion

        private void OnEnable()
        {
            CallbackHub.RegisterCallback<ITwinnyXRCallbacks>(this);
        }

        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnDisable()
        {
            CallbackHub.UnregisterCallback<ITwinnyXRCallbacks>(this);
        }


        #region UI Callback Actions
        public void StartExperience(string sceneName, int buildIndex) => m_gameMode?.StartExperience(sceneName, buildIndex);
        public void RestartExperience() => m_gameMode?.RestartExperience();

        public void ChangeScene(string sceneName) => m_gameMode?.ChangeScene(sceneName);

        public void ChangeScene(int sceneBuildIndex) => m_gameMode?.ChangeScene(sceneBuildIndex);

        public void NavigateTo(int landMarkIndex) => m_gameMode?.NavigateTo(landMarkIndex);

        public void StartInteract(GameObject gameObject) => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnStartInteract(gameObject));
        public void StopInteract(GameObject gameObject) => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnStopInteract(gameObject));

        public void StartTeleportCallback() => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnStartTeleport());
        public void TeleportCallback() => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnTeleport());
        
        public void SetHDRI(Material material) => TwinnyManager.SetHDRI(material);

        public async void SetHDRIRotation(float angle)
        {
            await Task.Yield();
            await Task.Yield();
            TwinnyManager.SetHDRIRotation(angle);
        }
        #endregion


        public void OnAnchorStateChanged(StateAnchorManager state)
        {
            if(state == StateAnchorManager.ANCHORING)
            OnAnchoringEvent?.Invoke();
            else
            OnAnchoredEvent?.Invoke();

        }

        public void OnSetPassthrough(bool status)
        {
            if (status)
                OnActivePassthroughEvent?.Invoke();
            else
                OnPassthroughDisabledEvent?.Invoke();
        }
        public void OnPlatformInitializing() => OnPlatformInitializingEvent?.Invoke();

        public void OnPlatformInitialized() => OnPlatformInitializedEvent?.Invoke();

        public void OnExperienceReady() => OnExperienceReadyEvent?.Invoke();

        public void OnExperienceStarting() => OnExperienceStartingEvent?.Invoke();

        public void OnExperienceStarted() => OnExperienceStartedEvent?.Invoke();

        public void OnExperienceEnding() => OnExperienceEndingEvent?.Invoke();

        public void OnExperienceEnded(bool isRunning) => OnExperienceEndedEvent?.Invoke();

        public void OnSceneLoadStart(string sceneName) => OnSceneLoadStartEvent?.Invoke();

        public void OnSceneLoaded(Scene scene) => OnSceneLoadedEvent?.Invoke();
        public void OnStartInteract(GameObject gameObject) => OnStartInteractEvent?.Invoke();

        public void OnStopInteract(GameObject gameObject) => OnStartInteractEvent?.Invoke();
        public void OnTeleportToLandMark(int landMarkIndex) => OnTeleportToLandMarkEvent?.Invoke();

        public void OnStartTeleport() => OnStartTeleportEvent?.Invoke();
        public void OnTeleport() => OnTeleportEvent?.Invoke();
        public void OnSkyboxHDRIChanged(Material material) => OnSkyboxHDRIChangedEvent?.Invoke();


#if UNITY_EDITOR
        [ContextMenu("Start")]
        public void StartMockup()
        {
            StartExperience("OpenXRMockupScene",-1);
        }
        [ContextMenu("Imersive")]
        public void StartImersive()
        {
            ChangeScene("OpenXRExperienceScene");
            NavigateTo(0);
        }

#endif
    }
}
