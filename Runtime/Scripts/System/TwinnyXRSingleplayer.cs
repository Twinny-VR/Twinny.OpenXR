using System;
using System.Threading.Tasks;
using Concept.Core;
using Twinny.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twinny.XR
{
    public class TwinnyXRSingleplayer : IGameMode
    {
        private TwinnyXRManager m_manager;
        public TwinnyXRSingleplayer(TwinnyXRManager managerOwner) => m_manager = managerOwner;

        public void Enter()
        {
            Initialize();
        }

        public void Exit()
        {
            throw new System.NotImplementedException();
        }



        private async void Initialize()
        {
            CallbackHub.CallAction<ICallbacks>(callback => callback.OnPlatformInitializing());
            await ChangeScene(1);
            CallbackHub.CallAction<ICallbacks>(callback => callback.OnPlatformInitialized());
        }

        public async Task<Scene> ChangeScene(int buildIndex, Action<float> onSceneLoading = null) {
            Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);
            if (!scene.IsValid()) {
                Debug.LogError($"[TwinnyXRSingleplayer] Invalid '{buildIndex}' scene build index.");
                return default;
            }
            return await ChangeScene(scene.name,onSceneLoading);
        } 
        public async Task<Scene> ChangeScene(string sceneName, Action<float> onSceneLoading = null)
        {
            CallbackHub.CallAction<ICallbacks>(callback => callback.OnSceneLoadStart(sceneName));
            AsyncOperation async  =  SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!async.isDone) { 
                await Task.Yield();
                onSceneLoading?.Invoke(async.progress);
            }

            Scene newScene = SceneManager.GetSceneByName(sceneName);

            if (!newScene.IsValid())
            {
                Debug.LogError($"[TwinnyXRSingleplayer] Invalid '{sceneName}' scene name!");
                return default;
            }

            CallbackHub.CallAction<ICallbacks>(callback => callback.OnSceneLoaded(newScene));
            return newScene;
        }


        public void NavigateTo(int landMarkIndex)
        {
            throw new System.NotImplementedException();
        }

        public void StartExperience()
        {
            throw new System.NotImplementedException();
        }

        public void Update()
        {
            throw new System.NotImplementedException();
        }
    }
}
