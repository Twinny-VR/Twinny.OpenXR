using Concept.Helpers;
using UnityEngine;
using Twinny.Core;

namespace Twinny.XR
{

    public class TwinnyXRManager: MonoBehaviour
    {
            private IGameMode m_gameMode => GameMode.currentMode;

            private void Start()
            {
                Initialize();
            }

            public void Initialize()
            {
                StateMachine.ChangeState(new IdleState(this));

            }


            #region UI Callback Actions
            public void StartExperience() => m_gameMode?.StartExperience();

            public void ChangeScene(string sceneName) => m_gameMode?.ChangeScene(sceneName);

            public void ChangeScene(int sceneBuildIndex) => m_gameMode?.ChangeScene(sceneBuildIndex);

            public void NavigateTo(int landMarkIndex) => m_gameMode?.NavigateTo(landMarkIndex);
            #endregion
        }
}