using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Graph
{
    [RequireComponent(typeof(RadarChartView))]
    public class RadarChartPointerController : UIBehaviour, IRadarChartMeshModifier
    {
        [SerializeField] private RadarChartPointerView[] _pointerViews;
        
        void IRadarChartMeshModifier.ModifyMesh(VertexHelper toFill, IReadOnlyCollection<Vector2> vertices)
        {
            foreach (var entry in vertices.Skip(1).Select((x, index) => new { Vertex = x, Index = index }))
                _pointerViews[entry.Index].UpdatePointerPosition(entry.Vertex);
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate ();

            var graphics = base.GetComponent<Graphic> ();
            graphics.SetVerticesDirty();
        }
#endif
    }
}