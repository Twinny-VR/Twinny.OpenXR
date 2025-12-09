using System;
using System.Threading.Tasks;
using Twinny.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Twinny.XR
{
    public class TwinnyXRMultiplayer : IGameMode
    {
        public Task<Scene> ChangeScene(int buildIndex, Action<float> onSceneLoading = null)
        {
            throw new NotImplementedException();
        }

        public Task<Scene> ChangeScene(string sceneName, Action<float> onSceneLoading = null)
        {
            throw new NotImplementedException();
        }

        public void Enter()
        {
            throw new NotImplementedException();
        }

        public void Exit()
        {
            throw new NotImplementedException();
        }

        public void NavigateTo(int landMarkIndex)
        {
            throw new NotImplementedException();
        }

        public void StartExperience()
        {
            throw new NotImplementedException();
        }

        public void Update()
        {
            throw new NotImplementedException();
        }
    }
}
