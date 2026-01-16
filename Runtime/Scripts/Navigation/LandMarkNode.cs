using System;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


namespace Twinny.XR.Navigation
{
    public class LandMarkNode : MonoBehaviour
    {
        public Transform followTarget;

        private bool m_selected;

        private Transform m_cameraRig;

        [Header("NAVIGATION")]
        public LandMarkNode north;
        public LandMarkNode south;
        public LandMarkNode east;
        public LandMarkNode west;


        [Header("CALLBACK ACTIONS")]
        public UnityEvent OnLandMarkSelected = new UnityEvent();
        public UnityEvent OnLandMarkUnselected = new UnityEvent();

        void Awake()
        {
        }

        private void Start()
        {
            m_cameraRig = TwinnyXRManager.cameraRigTransform.transform;

        }

        private void Update()
        {
            if (m_selected && followTarget != null) FollowTarget();
        }


        public void Select()
        {
            m_selected = true;
            OnLandMarkSelected?.Invoke();
        }
        public void Unselect()
        {
            m_selected = false;
            OnLandMarkUnselected?.Invoke();
        }


        private void FollowTarget()
        {
            Vector3 desiredPosition = followTarget.position;
            desiredPosition.y = m_cameraRig.position.y;
            m_cameraRig.position = desiredPosition;
        }

    }

}