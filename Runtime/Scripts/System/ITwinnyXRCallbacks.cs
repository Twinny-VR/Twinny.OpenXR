#if OBSOLETE
using UnityEngine;

namespace Twinny.XR
{
    public interface ITwinnyXRCallbacks 
    {
        void OnStartExperienceAction();
        void OnChangeSceneAction(string sceneName);
        void OnChangeSceneAction(int sceneBuildIndex);
        void OnNavigateAction(int landMarkIndex);
    }
}
#endif