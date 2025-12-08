#if UNITY_EDITOR
using Concept.UI;
using Twinny.XR;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    [UxmlElement]
    public partial class SceneFeatureTab : VisualElement
    {
        private AdvicePanel m_advicePanel;
        private VisualElement m_sceneFeaturePanel;
        public SceneFeatureTab  () {
            var visualTree = Resources.Load<VisualTreeAsset>(GetType().Name);
            if (visualTree == null)
            {
                Debug.LogError(GetType().Name + " Resource not found!");
                return;
            }

            visualTree.CloneTree(this);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            m_advicePanel = this.Q<AdvicePanel>();
            m_sceneFeaturePanel = this.Q<VisualElement>("SceneFeaturePanel");


            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            });


        }


        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            ValidateScene();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            ValidateScene();
        }



        private void ValidateScene()
        {

            TwinnyXRManager twinnyXRManager = GameObject.FindAnyObjectByType<TwinnyXRManager>();

            bool validScene = false;

            if (twinnyXRManager == null)
            {
                m_advicePanel.ShowAdvice("Active Scene is not a valid OpenXR Scene", AdviceType.ERROR, "Fix Scene", () => FixScene(false), "Create New", () => FixScene(true));
            }

            m_sceneFeaturePanel.style.display = (validScene) ? DisplayStyle.Flex : DisplayStyle.None;


        }

        private void FixScene(bool createNew)
        {
            CreateScenePanel createScenePanel = new CreateScenePanel();
            createScenePanel.panelType = createNew? CreateScenePanel.PanelType.Create : CreateScenePanel.PanelType.Fix;
            createScenePanel.OnClose += (saved) =>
            {
                SetupGuideWindow.DisposeOverlay();
            };
            SetupGuideWindow.InjectOverlay(createScenePanel);

        }

    }
}
#endif