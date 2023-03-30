using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Graph
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class RadarChartView : MaskableGraphic
    {
        [Header("n角形化")]
        [Range(3, 20)]
        [SerializeField] int _cornerCount = 3;
        
        /// <summary>
        /// 各頂点の長さのスケールを表す。0 - 1の値を持ち、i番目の頂点までの距離は
        /// direction[i] * radius * _meterScale[i]
        /// で計算される
        /// </summary>
        private readonly List<float> _meterScales = new();

        public RadarChartView()
        {
            useLegacyMeshGeneration = false;
        }

        public void UpdateView(IReadOnlyCollection<float> values)
        {
            _meterScales.Clear();
            _meterScales.AddRange(values.Select(Mathf.Clamp01));
            if (_meterScales.Count != _cornerCount)
                throw new ArgumentException($"与えられたパラメータ数と設定された値が異なります。Parameter: {_meterScales.Count}個, {nameof(_cornerCount)}: {_cornerCount}個");
            
            // 頂点を更新
            this.SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (_meterScales.Count != _cornerCount) return;
            
            GenerateFilledPolygon(toFill);
        }

        private void GenerateFilledPolygon(VertexHelper toFill)
        {
            var vertices = ListPool<Vector2>.Get();
            toFill.Clear();

            // Rectの小さい方の長さを半径に持つ円に、内接する多角形を求める
            var rect = rectTransform.rect;
            var horizontalRadius = rect.width * 0.5f;
            var verticalRadius = rect.height * 0.5f;
            
            // doubleで計算しないと計算誤差が生じて図形がおかしくなる
            double radian = (double)360 / _cornerCount * Mathf.Deg2Rad;
            // 開始頂点を90度回転してわかりやすいように上始まりにする
            double offset = 90 * Mathf.Deg2Rad;

            Color32 color32 = this.color;

            // 円の中心から多角形の頂点へのベクトルを求める
            // 中心の頂点を追加する
            vertices.Add(new Vector2(0, 0));
            toFill.AddVert(new Vector3(0, 0, 0), color32, new Vector2(0.5f, 0.5f));
            for (var i = 0; i < _cornerCount; i++)
            {
                // 時計回りにするために - をつける
                var normalizedX = Math.Cos(radian * -i + offset);
                var normalizedY = Math.Sin(radian * -i + offset);
                var x = (_meterScales[i] * horizontalRadius) * normalizedX;
                var y = (_meterScales[i] * verticalRadius) * normalizedY;
                toFill.AddVert(new Vector3((float)x, (float)y, 0), color32, new Vector2((float)normalizedX, (float)normalizedY));
                vertices.Add(new Vector2((float)x, (float)y));
            }

            // MEMO: このように三角形分割しないと特定条件下で表示が正しくなくなるので多角形の中心を基準に三角形分割
            // https://blog.terresquall.com/2020/12/drawing-radar-charts-for-stat-uis-in-unity/
            // 自分で図を書くのが面倒だったので、例えば上のURLのような分割をすると頂点1が極端に小さい歪な5角形の時に0-2の直線より内側に頂点１が入ってしまう
            // 数学的に言うなら、URLは凸包出ないと成り立たないポリゴンの取り方になっている。
            // 開始位置から最後の三角形以外を追加
            for (var i = 0; i < _cornerCount - 1; i++) toFill.AddTriangle(i + 1, 0, i + 2);
            // 最後の三角形を追加
            toFill.AddTriangle(1, 0, _cornerCount);

            // 他のコンポーネントによるフック処理を呼び出す。
            var components = ListPool<Component>.Get();
            GetComponents(typeof(IRadarChartMeshModifier), components);
            for (var i = 0; i < components.Count; i++)
                ((IRadarChartMeshModifier)components[i]).ModifyMesh(toFill, vertices);
            
            ListPool<Component>.Release(components);
            ListPool<Vector2>.Release(vertices);
        }
        
#if UNITY_EDITOR
        [Header("Debug用")]
        [SerializeField] private List<float> _testMeterValues = new List<float>{1, 1, 1};

        protected override void OnValidate()
        {
            base.OnValidate();
            if (_testMeterValues.Count > _cornerCount)
            {
                while (_testMeterValues.Count != _cornerCount) 
                    _testMeterValues.RemoveAt(_testMeterValues.Count - 1);
            }
            else if (_testMeterValues.Count < _cornerCount)
            {
                while (_testMeterValues.Count != _cornerCount) _testMeterValues.Add(1f); 
            }

            UpdateView(_testMeterValues);
        }
#endif
    }
}