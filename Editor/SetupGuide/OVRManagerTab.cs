#if UNITY_EDITOR
using Concept.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    [UxmlElement]
    public partial class OVRManagerTab : VisualElement
    {
        private TabNavigation m_tabNavigation;
        public OVRManagerTab() {
            var visualTree = Resources.Load<VisualTreeAsset>(GetType().Name);
            if (visualTree == null)
            {
                Debug.LogError(GetType().Name + " Resource not found!");
                return;
            }

            visualTree.CloneTree(this);

            dataSource = OVRProjectConfig.CachedProjectConfig;

            m_tabNavigation = this.Q<TabNavigation>("TabNavigation");
            m_tabNavigation.SetTabsContent(new List<(string, VisualElement)>()
            {
                ("General",this.Q<VisualElement>("GeneralTab")),
                ("Build Settings",this.Q<VisualElement>("BuildSettingsTab")),
                ("Security",this.Q<VisualElement>("SecurityTab")),
                ("Experimental",this.Q<VisualElement>("ExperimentalTab")),
            });

        }

    }
}
#endif