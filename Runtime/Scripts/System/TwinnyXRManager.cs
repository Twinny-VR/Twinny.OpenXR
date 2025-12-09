using Concept.Helpers;
using Twinny.Core;
using UnityEngine;
using static Twinny.Core.GameMode;


namespace Twinny.XR
{

public class TwinnyXRManager: MonoBehaviour
{

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            StateMachine.ChangeState(new IdleState(this));

        }


        #region UI Callback Actions
        public void StartExperience() => currentMode.StartExperience();

        public void ChangeScene(string sceneName) => currentMode.ChangeScene(sceneName);

        public void ChangeScene(int sceneBuildIndex) => currentMode.ChangeScene(sceneBuildIndex);

        public void NavigateTo(int landMarkIndex) => currentMode.NavigateTo(landMarkIndex);
        #endregion
    }

}