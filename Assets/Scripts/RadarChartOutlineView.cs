using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Graph
{
    [RequireComponent(typeof(RadarChartView))]
    public class RadarChartOutlineView : UIBehaviour, IRadarChartMeshModifier
    {
        [Range(0, 100)]
        [SerializeField] private float _outlineThickness = 1.0f;
        [SerializeField] private Color _outlineColor = Color.black;
        [SerializeField] private OutlineType _outlineType;

        // Editorでも動くようようにAwakeでのキャッシュではなく動的なキャッシュにする
        private RectTransform _rectTransform;
        private RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null) _rectTransform = transform as RectTransform;
                return _rectTransform;
            }
        }

        void IRadarChartMeshModifier.ModifyMesh(VertexHelper toFill, IReadOnlyCollection<Vector2> vertices)
        {
            if (!isActiveAndEnabled) return;
            
            ToFillOutline(toFill, vertices);
        }

        void ToFillOutline(VertexHelper verts, IReadOnlyCollection<Vector2> vertices)
        {
            // 中心をSkipした頂点を作成しなおす。
            var cornerVertices = ListPool<Vector2>.Get();
            var lineList = ListPool<Line>.Get();
            
            // Debug用
            // var allVertices = ListPool<Vector2>.Get();
            // allVertices.AddRange(vertices);


            cornerVertices.AddRange(vertices.Skip(1).Select(x => new Vector2(x.x, x.y)));
            Color32 color32 = _outlineColor;
            var rect = RectTransform.rect;
            var cornerCount = cornerVertices.Count;

            // 多角形の外周の各直線のパラメタ
            var normalRotationQuaternion = Quaternion.Euler(0, 0, 90);
            for (var i = 0; i < cornerVertices.Count; i++)
            {
                var i1 = (i - 1 + cornerVertices.Count) % cornerVertices.Count;
                var i2 = i;
                var x1 = cornerVertices[i1].x;
                var y1 = cornerVertices[i1].y;
                var x2 = cornerVertices[i2].x;
                var y2 = cornerVertices[i2].y;
                var y21 = y2 - y1;
                var x21 = x2 - x1;

                var a = y21;
                var b = -x21;
                var c = y1 * x21 - x1 * y21;

                // y = ax + b を ax + by + c = 0 の形に変形
                var normalVector = (normalRotationQuaternion * (cornerVertices[i2] - cornerVertices[i1])).normalized;
                var line = _outlineType switch
                {
                    OutlineType.Inner => new Line()
                        { A = a, B = b, C = c, InnerTranslatePosition = -normalVector * _outlineThickness },
                    OutlineType.Outer => new Line()
                        { A = a, B = b, C = c, OuterTranslatePosition = normalVector * _outlineThickness },
                    OutlineType.Center => new Line()
                    {
                        A = a, B = b, C = c, InnerTranslatePosition = -normalVector * _outlineThickness * 0.5f,
                        OuterTranslatePosition = normalVector * _outlineThickness * 0.5f,
                    },
                };
                lineList.Add(line);
            }
            
            // 頂点の作成
            if (_outlineType is OutlineType.Outer or OutlineType.Inner)
            {
                // OuterかInnerの場合は頂点をそのまま追加する（頂点カラー反映のため）
                
                for (var i = 0; i < cornerVertices.Count; i++)
                {
                    // 頂点カラーをOutline用にするために色を追加しておく
                    var pos = new Vector2(cornerVertices[i].x, cornerVertices[i].y);
                    var uv = new Vector2(cornerVertices[i].x / rect.width, cornerVertices[i].y / rect.height);
                    verts.AddVert(pos, color32, uv);
                    // allVertices.Add(pos);
                }
                
                // 押出したOutlineの頂点を追加
                for (var i = 0; i < lineList.Count; i++)
                {
                    var i1 = i;
                    var i2 = (i + 1) % lineList.Count;
                    var point = _outlineType switch
                    {
                        OutlineType.Inner => lineList[i1].InnerTranslate()
                            .CalculateIntersectionPoint(lineList[i2].InnerTranslate()),
                        OutlineType.Outer => lineList[i1].OuterTranslate()
                            .CalculateIntersectionPoint(lineList[i2].OuterTranslate()),
                    };
                    verts.AddVert(new Vector3(point.x, point.y, 0), color32, new Vector2(point.x / rect.width, point.y / rect.height));
                    // allVertices.Add(point);
                }
            }
            else
            {
                // Inlineの頂点追加
                for (var i = 0; i < lineList.Count; i++)
                {
                    var i1 = i;
                    var i2 = (i + 1) % lineList.Count;
                    var point = lineList[i1].InnerTranslate().CalculateIntersectionPoint(lineList[i2].InnerTranslate());
                    verts.AddVert(new Vector3(point.x, point.y, 0), color32, new Vector2(point.x / rect.width, point.y / rect.height));
                    // allVertices.Add(point);
                } 
                
                // Inlineの頂点追加
                for (var i = 0; i < lineList.Count; i++)
                {
                    var i1 = i;
                    var i2 = (i + 1) % lineList.Count;
                    var point = lineList[i1].OuterTranslate().CalculateIntersectionPoint(lineList[i2].OuterTranslate());
                    verts.AddVert(new Vector3(point.x, point.y, 0), color32, new Vector2(point.x / rect.width, point.y / rect.height));
                    // allVertices.Add(point);
                } 
            }

            // ポリゴンの作成
            for (int i = 0; i < cornerCount; i++)
            {
                var inlineIndex = vertices.Count + i;
                var inlineNextIndex = (i + 1) % cornerCount + vertices.Count;
                var outlineIndex = cornerCount + inlineIndex;
                var outlineNextIndex = cornerCount + inlineNextIndex;

                verts.AddTriangle(outlineIndex, inlineIndex, outlineNextIndex);
                verts.AddTriangle(outlineNextIndex, inlineIndex, inlineNextIndex);
            }

            // foreach (var vert in allVertices) verts.ToFillSquarePoint(vert, 20.0f);
            
            // ListPool<Vector2>.Release(allVertices);
            ListPool<Vector2>.Release(cornerVertices);
            ListPool<Line>.Release(lineList);
        }


        /// <summary>
        /// 一般系
        /// </summary>
        private struct Line
        {
            public float A;
            public float B;
            public float C;
            public Vector2? InnerTranslatePosition;
            public Vector2? OuterTranslatePosition;

            public Line InnerTranslate()
            {
                return new Line()
                {
                    A = this.A,
                    B = this.B,
                    C = this.C - this.A * InnerTranslatePosition.Value.x - this.B * InnerTranslatePosition.Value.y,
                }; 
            }

            public Line OuterTranslate()
            {
                
                return new Line()
                {
                    A = this.A,
                    B = this.B,
                    C = this.C - this.A * OuterTranslatePosition.Value.x - this.B * OuterTranslatePosition.Value.y,
                }; }

            public Vector2 CalculateIntersectionPoint(Line otherLine)
            {
                var a1 = this.A;
                var b1 = this.B;
                var c1 = this.C;
                var a2 = otherLine.A;
                var b2 = otherLine.B;
                var c2 = otherLine.C;

                var a1b2a2b1 = (a1 * b2 - a2 * b1);
                var intersectionX = (b1 * c2 - b2 * c1) / a1b2a2b1;
                var intersectionY = (a2 * c1 - a1 * c2) / a1b2a2b1;

                return new Vector2(intersectionX, intersectionY);
            }
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