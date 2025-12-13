using Oculus.Interaction;
using Concept.Helpers;
using UnityEngine;
using Twinny.Core;
using Twinny.XR;
using Twinny.XR.Anchoring;



namespace Twinny.UI
{

    public enum ButtonType
    {
        START,
        QUIT,
        SETTINGS,
        CHANGE_SCENE,
        NAVIGATION,
        ACTION,
        RESTART
    }

    [RequireComponent(typeof(PointableUnityEventWrapper))]
    public class ButtonActionXR : MonoBehaviour
    {
        public ButtonType type;
        [SerializeField] private PointableUnityEventWrapper _pointable;
        public string parameter;
        public int landMarkIndex;


        #region MonoBehaviour Methods
        protected virtual void Awake()
        {
            if(!_pointable)
            _pointable = GetComponent<PointableUnityEventWrapper>();
            _pointable.WhenRelease.AddListener((pointerEvent) => OnRelease());
        }


        #endregion
        //[ContextMenu("CLICK")]

        public void OnRelease()
        {
            //TODO Criar um sistema de configurações
            if (!TwinnyRuntime.GetInstance<TwinnyXRRuntime>().allowClickSafeAreaOutside && !AnchorManager.Instance.isInSafeArea)
            {
                AlertViewHUD.PostMessage("Volte para a Safe Área!", AlertViewHUD.MessageType.Warning, 5f);
                return;
            }
            switch (type)
            {
                case ButtonType.START:
                    GameMode.currentMode.StartExperience(parameter);
                    break;
                    case ButtonType.QUIT:
                    GameMode.currentMode.Quit();
                    break;
                case ButtonType.RESTART:
                    GameMode.currentMode.RestartExperience();
                    break;
                case ButtonType.CHANGE_SCENE:
                    GameMode.currentMode.ChangeScene(parameter, landMarkIndex);
                    break;
                case ButtonType.NAVIGATION:
                    GameMode.currentMode.NavigateTo(landMarkIndex);
                    break;
                case ButtonType.ACTION:
                    ActionManager.CallAction(parameter);
                    break;
            }
        }

    }

}