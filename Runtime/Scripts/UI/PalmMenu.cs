using Oculus.Interaction;
using TWE26.OpenXR.Input;
using UnityEngine;

namespace Twinny.XR
{
    public class PalmMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_mainMenu;

        [SerializeField] private bool m_isToggle;

        private SelectorUnityEventWrapper m_eventWrapper;

        private void OnEnable()
        {
            m_eventWrapper = GetComponent<SelectorUnityEventWrapper>();
            m_eventWrapper.WhenUnselected.AddListener(HideMenu);
            XRGestureProvider.Instance.OnMenuPressedEvent.AddListener(m_isToggle? ToggleMenu : ShowMenu);
        }

        private void OnDisable()
        {
            m_eventWrapper.WhenUnselected.RemoveListener(HideMenu);
            XRGestureProvider.Instance.OnMenuPressedEvent.RemoveListener(m_isToggle ? ToggleMenu : ShowMenu);
        }

        private void Start()
        {
            HideMenu();
        }

        public void HideMenu() => m_mainMenu.SetActive(false);
        public void ShowMenu() => m_mainMenu.SetActive(true);

        public void ToggleMenu() => ToggleMenu(!m_mainMenu.activeSelf);
        public void ToggleMenu(bool status) => m_mainMenu.SetActive(status); 
        

    }
}
