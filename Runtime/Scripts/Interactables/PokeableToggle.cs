using System.Collections;
using UnityEngine;

public class PokeableToggle : MonoBehaviour
{
    [SerializeField] RectTransform m_dragger;
    [SerializeField] float duration = 0.18f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] Vector2 m_fromToPosition;
    Coroutine m_moveRoutine;

    [SerializeField]
    private bool m_checked;
    public bool Checked
    {
        get => m_checked; set
        {
            if (m_checked == value) return;

                m_checked = value;
            Slide();
        }
    }

    [ContextMenu("SWITCH")]
    public void Switch()
    {
        Checked = !m_checked;
    }

    private void Slide()
    {
        if(m_moveRoutine != null)
            StopCoroutine(m_moveRoutine);

        m_moveRoutine = StartCoroutine(MoveToX(m_checked ? m_fromToPosition.y : m_fromToPosition.x));
    }

    IEnumerator MoveToX(float targetX)
    {
        if (m_dragger == null)
        {
            Debug.LogError("m_dragger é nulo!");
            yield break;
        }

        // Verifique se já existe uma corrotina em execução
        if (m_moveRoutine != null)
        {
            StopCoroutine(m_moveRoutine);
        }

        float startX = m_dragger.anchoredPosition.x;

        Debug.Log($"Iniciando movimento: {startX} -> {targetX} (diferença: {targetX - startX})");

        float startTime = Time.unscaledTime;

        while (Time.unscaledTime - startTime < duration)
        {
            float progress = (Time.unscaledTime - startTime) / duration;

            // Progressão linear
            float newX = Mathf.Lerp(startX, targetX, progress);
            m_dragger.anchoredPosition = new Vector2(newX, m_dragger.anchoredPosition.y);

            yield return null;
        }

        // Garantir posição final exata
        m_dragger.anchoredPosition = new Vector2(targetX, m_dragger.anchoredPosition.y);
        m_moveRoutine = null;

        Debug.Log("Movimento concluído");
    }

}
