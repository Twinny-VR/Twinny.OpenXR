using Twinny.Core;
using UnityEngine;
using static Twinny.Core.GameMode;

namespace Twinny.XR
{
    public class IdleState : IState
    {

        private TwinnyXRManager m_manager;
        public IdleState(TwinnyXRManager managerOwner) => m_manager = managerOwner;

        public void Enter()
        {
            SetGameMode();
        }

        public void Exit()
        {
            throw new System.NotImplementedException();
        }

        public void Update()
        {
            throw new System.NotImplementedException();
        }


        private void SetGameMode()
        {
#if FUSION2
//TODO Check if we are in multiplayer session
            ChangeState(new TwinnyXRMultiplayer());
            return;
#endif
            ChangeState(new TwinnyXRSingleplayer(m_manager));
        }
    }
}
