using Twinny.Core;
using UnityEngine;

namespace Twinny.XR
{
    public class TwinnyXRSingleplayer : IGameMode
    {
        private TwinnyXRManager m_manager;
        public TwinnyXRSingleplayer(TwinnyXRManager managerOwner)
        {
            m_manager = managerOwner;
        }


        public void ChangeScene(string sceneName)
        {
            throw new System.NotImplementedException();
        }

        public void ChangeScene(int sceneBuildIndex)
        {
            throw new System.NotImplementedException();
        }

        public void Enter()
        {
            throw new System.NotImplementedException();
        }

        public void Exit()
        {
            throw new System.NotImplementedException();
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
