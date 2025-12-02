#if UNITY_EDITOR
using Concept.SmartTools;
using Concept.UI;
using System;
using Twinny.XR;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Twinny.Editor
{

    [InitializeOnLoad]
    public static class OepnXRWindowRegister
    {
        static OepnXRWindowRegister()
        {
            var pkgInfo = SmartTools.GetPackageInfo(typeof(OpenXRWindow));
            SetupGuideWindow.RegisterModule(pkgInfo.name, typeof(OpenXRWindow));
        }
    }


    [UxmlElement]
    public partial class OpenXRWindow : VisualElement, IModuleSetup
    {

        private SetupGuideWindow m_guideWindow;

        private TabNavigation m_tabNavigation;
        private ScrollView m_runtimeConfigTabs;

        private ProjectTab m_projectTab;
        private OVRManagerTab m_ovrTab;
        private SceneFeatureTab m_sceneTab;

        private Label m_sceneNameLabel;

        public OpenXRWindow()
        {
            var visualTree = Resources.Load<VisualTreeAsset>(GetType().Name);
            if (visualTree == null)
            {
                Debug.LogError(GetType().Name + " Resource not found!");
                return;
            }

            visualTree.CloneTree(this);

            m_runtimeConfigTabs = this.Q<ScrollView>("RuntimeConfigTabs");
            m_projectTab = new ProjectTab();
            m_runtimeConfigTabs.Add(m_projectTab);
            m_ovrTab = new OVRManagerTab();
            m_runtimeConfigTabs.Add(m_ovrTab);
            m_sceneTab = new SceneFeatureTab();
            m_runtimeConfigTabs.Add(m_sceneTab);

            m_sceneNameLabel = this.Q<Label>("SceneNameLabel");

            m_tabNavigation = this.Q<TabNavigation>("TabNavigation");
            m_tabNavigation.ClearTabs();
            m_tabNavigation.AddTab(("Project", m_projectTab));
            m_tabNavigation.AddTab(("OVR", m_ovrTab));
            m_tabNavigation.AddTab(("Scene", m_sceneTab));
#if TWINNY_NETWORK
        private NetworkTab m_networkTab;
            m_runtimeConfigTabs.Add(m_networkTab);
            m_tabNavigation.AddTab(("Network",m_networkTab));
#endif
#if TWINNY_AVATARS
        private AvatarsTab m_avatarsTab;
            m_runtimeConfigTabs.Add(m_avatarsTab);
            m_tabNavigation.AddTab(("Avatars",m_avatarsTab));
#endif

            m_tabNavigation.SelectIndex(0);


            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;
            RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            });
        }



        public void OnShowSection(SetupGuideWindow guideWindow, int tabIndex)
        {
            m_guideWindow = guideWindow;
            ValidateScene();
        }

        public void OnApply()
        {
            throw new NotImplementedException();
        }
        private void OnActiveSceneChanged(Scene previous, Scene next)
        {
            ValidateScene();
        }

        public void ValidateScene()
        {
            TwinnyXRManager twinnyXRManager = GameObject.FindAnyObjectByType<TwinnyXRManager>();

            bool isValidScene = false;


            if (twinnyXRManager == null)
            {
                m_sceneNameLabel.text = "(Active Scene is not a valid OpenXR Scene)";
            }else
                m_sceneNameLabel.text = $"({EditorSceneManager.GetActiveScene().name})";

            m_sceneNameLabel.EnableInClassList("valid", isValidScene);
            m_sceneNameLabel.EnableInClassList("error", !isValidScene);
        }

    }

}
#endif