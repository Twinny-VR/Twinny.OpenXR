using UnityEngine;
using UnityEngine.UI;
using Concept.Helpers;




#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace Twinny.UI
{


    public class AlertViewHUD : TSingleton<AlertViewHUD>
    {
        public enum MessageType
        {
            Info = 0,
            Warning = 1,
            Error = 2
        }

        internal float _hideAfterSec = 10f;

        [SerializeField] internal bool _centerInCamera = true;


        [SerializeField] private GameObject _panel;
        [SerializeField] private Sprite _warningIcon;
        [SerializeField] private Sprite _errorIcon;
        [SerializeField] private Sprite _infoIcon;
        [SerializeField] private Text _messageTextField;
        [SerializeField] private Text _messageTypeTextField;
        [SerializeField] private Image _messageTypeIconField;

        [SerializeField]
        private Transform _centerEyeTransform;
        private bool Hidden => !_panel.activeSelf;

        private float _initialTime;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private float _speed = 7f;


        protected override void Awake()
        {
            base.Awake();
            _initialTime = Time.time;
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            Hide();
        }

        protected override void Start()
        {
            base.Start();
        OVRCameraRig cameraRig = FindFirstObjectByType<OVRCameraRig>();
            _centerEyeTransform = cameraRig ? cameraRig.centerEyeAnchor : Camera.main.transform;
        }

        public static void PostMessage(string message, MessageType messageType = MessageType.Warning, float time = 10f)
        {
            if (Instance == null)
            {
                return;
            }
            Instance._hideAfterSec = time;
            Instance.Post(message, messageType);
        }

        public static void CancelMessage()
        {
            Instance._hideAfterSec = 0f;
        }

        private void Post(string message, MessageType type)
        {
            switch (type)
            {
                case MessageType.Info:
                    _messageTypeIconField.sprite = _infoIcon;
                    _messageTypeTextField.text = "%Info";
                    break;
                case MessageType.Warning:
                    _messageTypeIconField.sprite = _warningIcon;
                    _messageTypeTextField.text = "%Warning";
                    break;
                case MessageType.Error:
                    _messageTypeIconField.sprite = _errorIcon;
                    _messageTypeTextField.text = "%Error";
                    break;
            }
            _messageTextField.text = message + "\n";
            Reset();
        }

        private void ClearMessage() => _messageTextField.text = "";

        protected override void Update()
        {
            base.Update();
        CalculateHideAfterMessage();
            FollowCamera();
        }

        private void CalculateHideAfterMessage()
        {
            if (_hideAfterSec == -1f || Hidden)
            {
                return;
            }

            if (Time.time - _initialTime >= _hideAfterSec)
            {
                Hide();
            }
        }

        private void Reset()
        {
            _initialTime = Time.time;
            _panel.SetActive(true);
        }

        private void Hide() => _panel.SetActive(false);

        private void FollowCamera()
        {
            if (_centerEyeTransform == null || Hidden || !_centerInCamera)
            {
                return;
            }

            var targetPosition = _centerEyeTransform.TransformPoint(_initialPosition);
            var targetRotation = _centerEyeTransform.rotation * _initialRotation;

            var p = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * _speed);
            var r = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * _speed);
            transform.SetPositionAndRotation(p, r);
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(AlertViewHUD))]
    public class AlertViewHUDEditor : Editor
    {
        private bool _fold;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(AlertViewHUD._centerInCamera)));

            _fold = EditorGUILayout.Foldout(_fold, "References");
            if (_fold)
            {
                DrawPropertiesExcluding(serializedObject,
                    nameof(AlertViewHUD._centerInCamera));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif // UNITY_EDITOR

}