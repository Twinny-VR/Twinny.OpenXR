using System.Collections.Generic;
using Concept.Core;
using Concept.Helpers;
using Oculus.Interaction.HandGrab;
using Twinny.Helpers;
using Twinny.UI;
using Twinny.XR.Interactables;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


namespace Twinny.XR
{


    /// <summary>
    /// This class is used to monitoring the hand gestures with delegates callbacks
    /// </summary>
    public class GestureMonitor : TSingleton<GestureMonitor>
    {

        #region Fields
        private Camera _camera;
        private OVRHand _leftHand, _rightHand;
        private bool _wasPinchingLeft, _wasPinchingRight = false;  // Left and Right hand status Flag
        [Header("Interactions")]
        [SerializeField] private HandGrabInteractor _handGrabInteractorLeft;
        [SerializeField] private HandGrabInteractor _handGrabInteractorRight;


        [Header("Trace Interactables")]
        [SerializeField] private bool _traceInteractables;
        [SerializeField] private float _closeUpTime = 1f;

        [SerializeField] private float _rayDistance = 10f;  // Distância do raio
        [SerializeField] private LayerMask _interactableLayer;
        private Interactable _target;
        private float _observeTime = 0f;

        [SerializeField] private GameObject m_handForwardGestureRight;



        #endregion

        #region Events
        public static UnityEvent OnPinchLeftEvent = new UnityEvent();
        public static UnityEvent OnPinchRightEvent = new UnityEvent();
        public static UnityEvent<bool> OnGrabbingEvent = new UnityEvent<bool>();
        public static UnityEvent OnMenuPressedEvent = new UnityEvent();
        #endregion

        #region MonoBehaviour Methods
        private void OnEnable()
        {
        }
        private void OnDisable()
        {
        }
        protected override void Start()
        {
            base.Start();
            OnGrabbingEvent.AddListener(OnGrabbingCallBack);

            _camera = Camera.main;
            FindHands();
            if (!_leftHand)
                Debug.LogWarning("[GestureMonitor] Left Hand NOT FOUND");
            if (!_rightHand)
                Debug.LogWarning("[GestureMonitor] Right Hand NOT FOUND");

        }

        
        protected override void Update()
        {
            base.Update();

            if (_traceInteractables) TraceInteractables();

            if (!_leftHand || !_rightHand) return;

            if (IsPinching(_leftHand, ref _wasPinchingLeft)) { 
                OnPinchLeftEvent?.Invoke();

                if (DebugPanel.Instance != null) DebugPanel.Instance.SetFold();
            }
            if (IsPinching(_rightHand, ref _wasPinchingRight))  OnPinchRightEvent?.Invoke();
            

            if (OVRInput.GetDown(OVRInput.Button.Start))
                OnMenuPressedEvent?.Invoke();

            OnGrabbingEvent?.Invoke(_handGrabInteractorLeft.IsGrabbing || _handGrabInteractorRight.IsGrabbing);
        }
        #endregion


        #region Privates Methods

        // Função para encontrar a mão associada ao controlador
        private void FindHands()
        {
            // Encontrar todas as mãos (OVRHands) na cena
            OVRHand[] hands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);

            // Iterar sobre as mãos para associar a mão direita ou esquerda com base no controlador
            foreach (OVRHand hand in hands)
            {
                if (hand.GetHand() == OVRPlugin.Hand.HandLeft)
                    _leftHand = hand;
                if (hand.GetHand() == OVRPlugin.Hand.HandRight)
                    _rightHand = hand;
            }
        }


        private bool IsPinching(OVRHand hand, ref bool wasPinching)
        {
            bool isPinching = false;

            // Verificar se a mão está rastreada e detectar pinça
            if (hand != null && hand.IsTracked)
            {
                isPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            }

            // Verifique se houve uma transição do estado de "não pinçando" para "pinçando"
            if (isPinching && !wasPinching)
            {
                wasPinching = true;  // Marque que agora a mão está pinçando
                return true; // Retorne true apenas no momento que começou o pinçamento
            }
            else if (!isPinching)
            {
                wasPinching = false; // Se a mão não está pinçando, resetar o estado
            }

            return false; // Se não for o momento de iniciar o pinçamento, retorne false
        }


        private bool IsPalmUp(OVRHand hand)
        {
            if (hand != null && hand.IsTracked)
            {
                // Obter a rotação da mão
                Quaternion palmRotation = hand.transform.rotation;

                // Obter o vetor da direção para cima a partir da rotação da mão
                Vector3 palmUp = palmRotation * Vector3.up;

                // Se a direção da palma (palmUp) tiver um componente Y positivo, a palma está para cima
                return palmUp.y > 0;
            }

            return false;
        }

        private bool IsPalmDown(OVRHand hand)
        {
            if (hand != null && hand.IsTracked)
            {
                // Obter a rotação da mão
                Quaternion palmRotation = hand.transform.rotation;

                // Obter o vetor da direção para cima a partir da rotação da mão
                Vector3 palmUp = palmRotation * Vector3.up;

                // Se a direção da palma (palmUp) tiver um componente Y negativo, a palma está para baixo
                return palmUp.y < 0;
            }

            return false;
        }



        private bool IsPointing(OVRHand hand)
        {
            if (hand != null && hand.IsTracked)
            {
                bool isPointing = hand.GetFingerIsPinching(OVRHand.HandFinger.Index) == false &&
                                  hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) == false &&
                                  hand.GetFingerIsPinching(OVRHand.HandFinger.Ring) == false &&
                                  hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky) == false;

                // Certifique-se de que o dedo indicador está esticado e a palma da mão está voltada para cima
                return isPointing && IsPalmUp(hand);
            }

            return false;
        }



        private bool IsFist(OVRHand hand)
        {
            if (hand != null && hand.IsTracked)
            {
                // Verifique se todos os dedos estão fechados (não estão pinçando)
                bool isFist = hand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                              hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) &&
                              hand.GetFingerIsPinching(OVRHand.HandFinger.Ring) &&
                              hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky);

                return isFist;
            }

            return false;
        }

        private bool IsOpenHand(OVRHand hand)
        {
            if (hand != null && hand.IsTracked)
            {
                // Verifique se nenhum dedo está fechando (não estão pinçando)
                bool isOpen = !hand.GetFingerIsPinching(OVRHand.HandFinger.Index) &&
                              !hand.GetFingerIsPinching(OVRHand.HandFinger.Middle) &&
                              !hand.GetFingerIsPinching(OVRHand.HandFinger.Ring) &&
                              !hand.GetFingerIsPinching(OVRHand.HandFinger.Pinky);

                return isOpen;
            }

            return false;
        }

        private bool IsTeleportGesture(OVRHand hand)
        {
            if (hand != null && hand.IsTracked)
            {
                return IsPointing(hand) && IsPalmUp(hand);
            }

            return false;
        }
        private void TraceInteractables()
        {
            RaycastHit hit;
            Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);
            if (Physics.Raycast(ray, out hit, _rayDistance, _interactableLayer))
            {
                if (hit.collider.isTrigger)
                {
                    Debug.Log("Olhando para: " + hit.collider.name);
                }
                Interactable interactableTarget = hit.collider.GetComponent<Interactable>();

                if (interactableTarget != null)
                {
                    if (_target == null || _target != interactableTarget)
                    {
                        if (_target != null) _target.SetHighLight(false);
                        _target = interactableTarget;
                        _target.SetHighLight();
                        _observeTime = 0f;
                    }

                    _observeTime += Time.deltaTime;
                    if (_observeTime > _closeUpTime)
                    {
                        Debug.Log("Olhando para: " + interactableTarget.name);
                        _observeTime = 0f;
                    }

                }
                else
                {
                    if (_target != null) _target.SetHighLight(false);
                    _target = null;
                    _observeTime = 0f;

                }
            }
            else
            {
                if (_target != null) _target.SetHighLight(false);
                _target = null;
                _observeTime = 0f;

            }
        }


        #endregion

        #region CallBacks


        public void OnGrabbingCallBack(bool status)
        {
            CallbackHub .CallAction<IUICallBacks>(callback => callback.OnHudStatusChanged(status));

        }

        #endregion

        public static void SetHandForwardGestureRight(bool status) => Instance.m_handForwardGestureRight?.SetActive(status);
    }

}