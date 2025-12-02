using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    [UxmlElement]
    public partial class NetworkTab : VisualElement
    {

        public NetworkTab() {
            var visualTree = Resources.Load<VisualTreeAsset>(GetType().Name);
            if (visualTree == null)
            {
                Debug.LogError(GetType().Name + " Resource not found!");
                return;
            }

            visualTree.CloneTree(this);
        }

    }
}
