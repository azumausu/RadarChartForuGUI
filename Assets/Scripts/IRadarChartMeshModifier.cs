using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Graph
{
    /// <summary>
    /// 通常のMeshModifierではインデックスバッファー反映後の頂点しか取得できなかったので独自のModifierを定義
    /// </summary>
    public interface IRadarChartMeshModifier
    {
        void ModifyMesh(VertexHelper toFill, IReadOnlyCollection<Vector2> vertices);
    }
}