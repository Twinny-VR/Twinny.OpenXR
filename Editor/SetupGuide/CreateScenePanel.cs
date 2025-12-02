using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    [UxmlElement]
    public partial class CreateScenePanel : VisualElement
    {

        public enum PanelType
        {
            Create,
            Fix
        }

        private const string USSClassName = "scene-panel";
        private const float DOUBLE_CLICK_TIME = 0.25f; // 250 ms, padrão comum

        private float _lastClickTime = -1f;

        private Label m_headerLabel;

        private TextField m_sceneNameField;

        private Button m_closeButton;
        private Button m_saveButton;
        private Button m_cancelButton;


        private List<(VisualElement, Action)> m_templates;

        private VisualElement m_currentTemplate;

        private PanelType m_panelType;
        public PanelType panelType
        {
            get => m_panelType;
            set
            {
                m_panelType = value;

                m_sceneNameField.style.display = (value == PanelType.Create) ? DisplayStyle.Flex : DisplayStyle.None;

                switch (value)
                {
                    case PanelType.Create:
                        m_headerLabel.text = "Create New OpenXR Scene";
                        m_saveButton.text = "Create";
                        m_sceneNameField.Focus();
                        break;
                    case PanelType.Fix:
                        m_headerLabel.text = "Configure Current Scene";
                        m_saveButton.text = "Fix";
                        break;
                    default:
                        break;
                }

            }
        }

        public event Action<bool> OnClose;

        public CreateScenePanel()
        {
            var visualTree = Resources.Load<VisualTreeAsset>(GetType().Name);
            if (visualTree == null)
            {
                Debug.LogError(GetType().Name + " Resource not found!");
                return;
            }

            visualTree.CloneTree(this);
            AddToClassList(USSClassName);

            m_headerLabel = this.Q<Label>("HeaderLabel");
            m_sceneNameField = this.Q<TextField>("SceneNameField");
            m_sceneNameField.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                m_saveButton.SetEnabled(Validate());
            });

            m_closeButton = this.Q<Button>("CloseButton");
            m_closeButton.clicked += OnCancelClicked;


            m_saveButton = this.Q<Button>("SaveButton");
            m_saveButton.clicked += OnSaveClicked;

            m_cancelButton = this.Q<Button>("CancelButton");
            m_cancelButton.clicked += OnCancelClicked;

            RegisterCallback<AttachToPanelEvent>((evt) =>
            {
                if (m_panelType == PanelType.Create) m_sceneNameField.Focus();
            });

            m_templates = new List<(VisualElement, Action)>()
            {
                (this.Q<VisualElement>("SingleplayerTemplate"),OnSingleplayerSelected),
                (this.Q<VisualElement>("MultiplayerTemplate"),OnMultiplayerSelected),
                (this.Q<VisualElement>("StartSceneTemplate"),OnStartSceneSelected),
                (this.Q<VisualElement>("MockupTemplate"),OnMockupSelected),
                (this.Q<VisualElement>("LinearTemplate"),OnLinearSelected),
                (this.Q<VisualElement>("InteractiveTemplate"),OnInteractiveSelected)

            };

            SetupTemplateCallbacks();

        }

        private void OnCancelClicked()
        {
            OnClose?.Invoke(false);
        }

        private void OnSaveClicked()
        {
            if (m_panelType == PanelType.Create)
            {
                if (!CreateNewScene(m_sceneNameField.value)) return;
            }

            var action = m_templates.FirstOrDefault(t => t.Item1 == m_currentTemplate).Item2;
            action?.Invoke();
            OnClose?.Invoke(true);

        }

        private void SetupTemplateCallbacks()
        {
            foreach (var (element, action) in m_templates)
            {
                element.RegisterCallback<ClickEvent>(evt =>
                {
                    float time = Time.realtimeSinceStartup;

                    if (time - _lastClickTime < DOUBLE_CLICK_TIME)
                    {
                        if (m_panelType != PanelType.Create)
                            OnSaveClicked();
                    }
                    else
                    {
                        SelectTemplate(element);
                    }

                    _lastClickTime = time;
                });
            }
        }

        private void SelectTemplate(VisualElement template)
        {
            foreach (var (element, action) in m_templates)
            {
                if (element == template && m_currentTemplate != template)
                {
                    m_currentTemplate = element;
                    element.AddToClassList("selected");
                    continue;
                }
                if (m_currentTemplate == element) m_currentTemplate = null;

                element.RemoveFromClassList("selected");
            }

            m_saveButton.SetEnabled(Validate());
        }

        private bool Validate()
        {
            bool isValid = false;

            switch (m_panelType)
            {
                case PanelType.Create:
                    isValid = !string.IsNullOrEmpty(m_sceneNameField.value) && m_currentTemplate != null;
                    break;
                case PanelType.Fix:
                    isValid = m_currentTemplate != null;
                    break;
            }

            return isValid;
        }

        private void OnSingleplayerSelected()
        {
            Debug.LogWarning("Singleplayer Scene Selected");
        }
        private void OnMultiplayerSelected()
        {
            Debug.LogWarning("Multiplayer Scene Selected");
        }
        private void OnStartSceneSelected()
        {
            Debug.LogWarning("Start Scene Selected");
        }
        private void OnMockupSelected()
        {
            Debug.LogWarning("Mockup Scene Selected");
        }
        private void OnLinearSelected()
        {
            Debug.LogWarning("Linear Scene Selected");
        }
        private void OnInteractiveSelected()
        {
            Debug.LogWarning("Interactive Scene Selected");
        }

        private bool CreateNewScene(string sceneName)
        {
            throw new NotImplementedException();
        }
    }
}
