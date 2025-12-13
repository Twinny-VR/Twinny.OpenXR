using System;
using System.IO;
using System.Threading.Tasks;
using Concept.Core;
using Twinny.Core;
using Twinny.UI;
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
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnPlatformInitializing());
            await ChangeScene(1);
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnPlatformInitialized());
        }

        public async Task<Scene> ChangeScene(int buildIndex, int landMarkIndex = -1, Action<float> onSceneLoading = null) {
            
            string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex)); 
            return await ChangeScene(sceneName, landMarkIndex, onSceneLoading);
        } 
        public async Task<Scene> ChangeScene(string sceneName, int landMarkIndex = -1, Action<float> onSceneLoading = null)
        {
            await CanvasTransition.FadeScreenAsync(true, TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime);


            if (SceneManager.sceneCount > 1) 
                await UnloadAdditivesScenes();


            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnSceneLoadStart(sceneName));
            AsyncOperation async  =  SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!async.isDone) { 
                await Task.Yield();
                onSceneLoading?.Invoke(async.progress);
            }

            SceneFeatureXR.Instance?.TeleportToLandMark(landMarkIndex);
            Scene newScene = SceneManager.GetSceneByName(sceneName);

            if (!newScene.IsValid())
            {
                Debug.LogError($"[TwinnyXRSingleplayer] Invalid '{sceneName}' scene name!");
                return default;
            }

            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnSceneLoaded(newScene));
            await CanvasTransition.FadeScreenAsync(false, TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime);
            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnExperienceStarted());
            return newScene;
        }


        public async void NavigateTo(int landMarkIndex)
        {
            await CanvasTransition.FadeScreenAsync(true, TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime);
            SceneFeatureXR.Instance.TeleportToLandMark(landMarkIndex);
            await CanvasTransition.FadeScreenAsync(false, TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime);
        }

        public async Task StartExperience(int buildIndex, int landMarkIndex = -1)
        {
            string sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(buildIndex));
            await StartExperience(sceneName, landMarkIndex);
        }
        public async Task StartExperience(string sceneName, int landMarkIndex = -1)
        {

            CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnExperienceStarting());
            await ChangeScene(sceneName, landMarkIndex);
        }

        public void Update()
        {
            throw new System.NotImplementedException();
        }

        public static async Task UnloadAdditivesScenes()
        {
            if (SceneManager.sceneCount <= 1) return;
 
            await Task.Yield(); // Similar "yield return new WaitForEndFrame()"

                for (int i = 1; i < SceneManager.sceneCount; i++)
            {
                Scene loadedScene = SceneManager.GetSceneAt(i);
                    if (loadedScene.IsValid() && loadedScene.isLoaded)
                        await SceneManager.UnloadSceneAsync(loadedScene);
            }
            await Resources.UnloadUnusedAssets();

        }

        public async void RestartExperience()
        {
            await CanvasTransition.FadeScreenAsync(true, TwinnyRuntime.GetInstance<TwinnyXRRuntime>().fadeTime);
            SceneFeatureXR.SetPassthrough(true);
            await UnloadAdditivesScenes();
            await StartExperience(1);
        }

        public void Quit()
        {
            throw new NotImplementedException();
        }
    }
}
