using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twinny.XR
{
    public static partial class TwinnyXRSceneManager
    {
        #region Singleplayer Methods
        public static class Singleplayer
        {
            public static async Task LoadAdditiveSceneAsync(object scene, int landMarkIndex)
            {
                if(SceneManager.sceneCount > 2) await UnloadAdditivesScenes();
                
                if (scene is int index) await SceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
                else
                if (scene is string name) await SceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
                else
                {
                    Debug.LogErrorFormat("[TwinnyXRSceneManager] Scene format error: <b>{0}</b> is not a valid format!", scene.GetType().Name);
                    return;
                }

            }

            public static async Task UnloadAdditivesScenes()
            {
                await Task.Yield(); // Similar "yield return new WaitForEndFrame()"

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene loadedScene = SceneManager.GetSceneAt(i);
                    if (loadedScene.buildIndex > 1)
                    {
                        if (loadedScene.IsValid() && loadedScene.isLoaded)
                        await SceneManager.UnloadSceneAsync(loadedScene);
                    }
                }
                await Resources.UnloadUnusedAssets();

            }


        }
        #endregion
    }
}
