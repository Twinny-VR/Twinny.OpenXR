#if OBSOLETE
using Concept.Core;
using UnityEngine;

namespace Twinny.XR
{
    public class TwinnyXRActionDispatcher : MonoBehaviour
    {

        public void StartExperience() => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnStartExperienceAction());
        public void ChangeScene(string sceneName) => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnChangeSceneAction(sceneName));
        public void ChangeScene(int sceneBuildIndex) => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnChangeSceneAction(sceneBuildIndex));
        public void NavigateTo(int landMarkIndex) => CallbackHub.CallAction<ITwinnyXRCallbacks>(callback => callback.OnNavigateAction(landMarkIndex));


    }
}
#endif