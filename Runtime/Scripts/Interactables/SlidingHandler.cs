using UnityEngine;

[RequireComponent(typeof(Animator))]
public class SlidingHandler : MonoBehaviour
{

    public enum GateStatus
    {
        OPENED,
        CLOSED,
        LOCKED,
        BUSY
    }

    private Animator m_animator;

    [SerializeField]
    private GateStatus m_status = GateStatus.CLOSED;

    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_animator.SetBool("closed", m_status == GateStatus.CLOSED);
   }

    private void Start()
    {
    }


    [ContextMenu("SWITCH SLIDER")]
    public void SwitchState()
    {
        if (m_status == GateStatus.LOCKED || m_status == GateStatus.BUSY) return;
        m_animator.SetBool("closed", m_status == GateStatus.OPENED);
        m_status = GateStatus.BUSY;

    }


    public void SetClosed() => m_status = GateStatus.CLOSED;
    public void SetOpended() => m_status = GateStatus.OPENED;
    
}
