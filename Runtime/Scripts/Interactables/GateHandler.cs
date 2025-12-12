#if false
using DG.Tweening;
using UnityEngine;

public class GateHandler : MonoBehaviour
{

    public enum GateStatus
    {
        OPENED,
        CLOSED,
        LOCKED,
        BUSY
    }

    private DOTweenAnimation[] m_animations;

    [SerializeField]
    private GateStatus m_status = GateStatus.CLOSED;

    private void Awake()
    {
        m_animations = GetComponentsInChildren<DOTweenAnimation>();
    }

    private void Start()
    {
    }


    [ContextMenu("SWITCH GATE")]
    public void SwitchState()
    {
        if (m_status == GateStatus.LOCKED || m_status == GateStatus.BUSY) return;

        int total = m_animations.Length;
        int finished = 0;

        foreach (var animation in m_animations)
        {

            if (m_status == GateStatus.CLOSED)
                animation.DORestart(); // abre
            else
            if (m_status == GateStatus.OPENED)
                animation.DOPlayBackwards(); // fecha

            m_status = GateStatus.BUSY;
            
            animation.tween.OnComplete(() =>
            {
                finished++;
                if (finished == total) m_status = GateStatus.OPENED;
            });
            animation.tween.OnRewind(() =>
            {
                finished++;
                if (finished == total) m_status = GateStatus.CLOSED;
            });
        }
    }
}
#endif