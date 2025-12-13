using System;
using System.Collections;
using Concept.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace Twinny.UI
{


    public class CarouselMenu : MonoBehaviour
    {

        [SerializeField] private ScrollRect m_ScrollRect;
        [SerializeField] private float m_ScrollDuration = .5f;
        public int count { get; private set; }
        public int currentPage { get; private set; }
        private void Awake()
        {
            if (m_ScrollRect == null) m_ScrollRect = GetComponentInChildren<ScrollRect>();
        }

        private void OnEnable()
        {
            ActionManager.RegisterAction("CAROUSEL_PREV", PreviousPage);
            ActionManager.RegisterAction("CAROUSEL_NEXT", NextPage);
            
        }

        private void OnDisable()
        {
            ActionManager.RemoveAction("CAROUSEL_PREV");
            ActionManager.RemoveAction("CAROUSEL_NEXT");
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            count = m_ScrollRect.content.childCount;
        }

        // Update is called once per frame
        void Update()
        {

        }


        [ContextMenu("PREVIOUS")]
        public void PreviousPage()
        {
            count = m_ScrollRect.content.childCount;
            if (count == 0) return;

            if (currentPage > 0)
            {
                currentPage--;
                ScrollToPage(currentPage);
            }
        }

        [ContextMenu("NEXT")]
        public void NextPage()
        {
            count = m_ScrollRect.content.childCount;
            if (currentPage < count - 1)
            {

                currentPage++;
                ScrollToPage(currentPage);
            }
        }

        public void ScrollToPage(int page)
        {

            HorizontalLayoutGroup layout = m_ScrollRect.content.GetComponent<HorizontalLayoutGroup>();

            float paddingLeft = layout.padding.left;
            float spacing = layout.spacing;

            LayoutRebuilder.ForceRebuildLayoutImmediate(m_ScrollRect.content);


            RectTransform child = m_ScrollRect.content.GetChild(0).GetComponent<RectTransform>();
            float itemWidth = child.sizeDelta.x;

            float pageStep = itemWidth + spacing;

            currentPage = Mathf.Clamp(page, 0, count - 1);
            float viewportWidth = m_ScrollRect.viewport.rect.width;
            float contentWidth = m_ScrollRect.content.rect.width;

            float offset = paddingLeft + page * pageStep;
            float centeredOffset = offset - (viewportWidth - itemWidth) / 2f;

            centeredOffset = Mathf.Clamp(centeredOffset, 0f, Mathf.Max(0f, contentWidth - viewportWidth));

            Vector2 targetPos = new Vector2(-centeredOffset, m_ScrollRect.content.anchoredPosition.y);

            StartCoroutine(SmoothScrollTo(targetPos));
            Debug.Log($"Scrolling to page {page}, offset = {offset}, itemWidth = {itemWidth}, padding = {paddingLeft}");
        }


        IEnumerator SmoothScrollTo(Vector2 target)
        {
            float elapsed = 0f;
            Vector2 start = m_ScrollRect.content.anchoredPosition;

            while (elapsed < m_ScrollDuration)
            {
                m_ScrollRect.content.anchoredPosition = Vector2.Lerp(start, target, elapsed / m_ScrollDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            m_ScrollRect.content.anchoredPosition = target;
        }
    }

}