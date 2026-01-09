using System;
using Oculus.Interaction.Locomotion;
using UnityEngine;
using UnityEngine.Events;


namespace Twinny.XR.Navigation
{
    public class LandMarkNode : MonoBehaviour
    {
        public Transform changeParent;

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

        public void Select()
        {
            if (TwinnyXRManager.locomotor == null) return;

            Debug.LogWarning("[LandMarkNode] TEM CARBURADOR!");

            Pose targetPose = new Pose(transform.position,Quaternion.Euler(0f, transform.eulerAngles.y, 0f));

            LocomotionEvent evt = new LocomotionEvent(
                0,
                targetPose,
                LocomotionEvent.TranslationType.Absolute,
                LocomotionEvent.RotationType.Absolute
            );

            TwinnyXRManager.locomotor.enabled = false;
            TwinnyXRManager.locomotor.HandleLocomotionEvent( evt );
            TwinnyXRManager.locomotor.enabled = true;
            OnLandMarkSelected?.Invoke();
        }
        public void Unselect()
        {
            OnLandMarkUnselected?.Invoke();
        }

    }

}