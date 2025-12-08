using UnityEngine;
using static Twinny.Core.GameMode;


namespace Twinny.XR
{

public class TwinnyXRManager: MonoBehaviour
{

        private void Start()
        {
            InitializeGameMode();
        }

        public void InitializeGameMode()
        {
#if FUSION2
//TODO Check if we are in multiplayer session
            ChangeState(new TwinnyXRMultiplayer());
            return;
#endif
            ChangeState(new TwinnyXRSingleplayer(this));
        }


        #region UI Callback Actions
        public void StartExperience() => currentMode.StartExperience();

        public void ChangeScene(string sceneName) => currentMode.ChangeScene(sceneName);

        public void ChangeScene(int sceneBuildIndex) => currentMode.ChangeScene(sceneBuildIndex);

        public void NavigateTo(int landMarkIndex) => currentMode.NavigateTo(landMarkIndex);
        #endregion
    }

}