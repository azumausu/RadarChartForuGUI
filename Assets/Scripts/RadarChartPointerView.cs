using UnityEngine;
using UnityEngine.EventSystems;

namespace Graph
{
    public abstract class RadarChartPointerView : UIBehaviour
    {
        public abstract void UpdatePointerPosition(Vector2 anchoredPosition);
    }
}