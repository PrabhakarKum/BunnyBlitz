using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace BunnyBlitz
{
    public class HealthbarHandler : MonoBehaviour
    {
        private UIDocument m_Document;
        private VisualElement m_HealthbarFill;

        private SortingGroup m_SortingGroup;
    
    
        private void OnEnable()
        {
            m_Document = GetComponent<UIDocument>();
            m_SortingGroup = GetComponent<SortingGroup>();

            m_HealthbarFill = m_Document.rootVisualElement.Q<VisualElement>("healthbar-fill");
        }

        public void SetBarFill(float percent)
        {
            m_HealthbarFill.style.width = Length.Percent(percent);
        }
    }
}