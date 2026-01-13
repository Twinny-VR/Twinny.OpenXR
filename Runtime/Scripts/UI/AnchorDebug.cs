using System.Collections;
using TMPro;
using Twinny.Core;
using Twinny.XR;
using UnityEngine;
using Concept.SmartTools;

namespace Twinny.UI
{
    [ExecuteInEditMode]
    public class AnchorDebug : MonoBehaviour
    {
        private Transform _transform;
        [SerializeField] private GameObject _debugVisual;

        [SerializeField]
        private string _displayText;
        public string displayText { get => _displayText; set => _displayText = value;  }

        [SerializeField] private bool _showInfo = false;
        public bool showInfo { get => _showInfo; set { if (TwinnyRuntime.GetInstance<TwinnyXRRuntime>().ambientType == BuildType.RELEASE) return;  _showInfo = value; _debugInfo.SetActive(value); }
        }
        [SerializeField] private GameObject _debugInfo;
        [SerializeField] private TextMeshProUGUI TMP_Info;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _debugInfo?.SetActive(_showInfo);
        }
#endif

        // Start is called before the first frame update
        void Start()
        {
            //  Debug.LogWarning("[AnchorDebug] Active:" + (TwinnyRuntime.GetInstance<TwinnyXRRuntime>().ambientType != BuildType.RELEASE));
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SetDebugVisual(true);
                return;
            } 
#endif
            SetDebugVisual(TwinnyRuntime.GetInstance<TwinnyXRRuntime>().ambientType != BuildType.RELEASE);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetDebugVisual(bool status)
        {
            if(string.IsNullOrEmpty(_displayText)) _displayText = transform.parent.name;
            _debugVisual?.SetActive(status);
            _transform = transform;
            _debugInfo?.SetActive(_showInfo);
            if (_showInfo)
                StartCoroutine(SetCoordinatesText());
        }

        IEnumerator SetCoordinatesText()
        {
            while (_showInfo)
            {
                TMP_Info.text = $"<b><color=#0F0>[{_displayText}]</color></b>\nP:({_transform.position})\nR:({_transform.eulerAngles})"; 

                yield return new WaitForSeconds(1f);
            }
        }
    }
}
