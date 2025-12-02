using Concept.UI;
using System.Collections.Generic;
using Twinny.Core;
using Twinny.XR;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    [UxmlElement]
    public partial class ProjectTab : VisualElement
    {

        private AdvicePanel m_advicePanel;
        private VisualElement m_projectPanel;
        private TabNavigation m_tabNavigation;
        private VisualElement m_generalTab;
        private VisualElement m_openXRTab;
        private Toggle m_forceFRToggle;
        private VisualElement m_targetFRPanel;
        public ProjectTab()
        {
            var visualTree = Resources.Load<VisualTreeAsset>(GetType().Name);
            if (visualTree == null)
            {
                Debug.LogError(GetType().Name + " Resource not found!");
                return;
            }

            visualTree.CloneTree(this);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);

            m_advicePanel = this.Q<AdvicePanel>("AdvicePanel");
            m_projectPanel = this.Q<VisualElement>("ProjectPanel");
            m_generalTab = this.Q<VisualElement>("GeneralTab");
            m_openXRTab = this.Q<VisualElement>("OpenXRTab");


            m_tabNavigation = this.Q<TabNavigation>();
            m_tabNavigation.SetTabsContent(new List<(string, VisualElement)>()
            {
                ("General",m_generalTab),
                ("OpenXR", m_openXRTab)

            });


            m_targetFRPanel = m_generalTab.Q<VisualElement>("TargetFRPanel");
            m_forceFRToggle = m_generalTab.Q<Toggle>("ForceFRToggle");
            m_targetFRPanel.visible = m_forceFRToggle.value;

            m_forceFRToggle.RegisterValueChangedCallback(evt => {

                m_targetFRPanel.visible = evt.newValue;            
            });

        }


        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var config = TwinnyRuntime.GetInstance<TwinnyXRRuntime>();

            m_projectPanel.style.display = (config == null) ? DisplayStyle.None : DisplayStyle.Flex;
            m_advicePanel.style.display = (config != null) ? DisplayStyle.None : DisplayStyle.Flex;

            if (config == null)
            {
                m_advicePanel.ShowAdvice("The current project doesn't have a RuntimeConfig file defined!", AdviceType.ERROR, "Create", () => {

                    config = TwinnyRuntime.CreateAsset<TwinnyXRRuntime>();
                    OnAttachToPanel(evt);
                });
            }

            dataSource = config;
        }




    }
}
