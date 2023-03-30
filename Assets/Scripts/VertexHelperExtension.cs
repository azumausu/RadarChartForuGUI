using UnityEngine;
using UnityEngine.UI;

namespace Graph
{
    public static class VertexHelperExtension
    {
        public static void ToFillSquarePoint(this VertexHelper self, Vector2 centerPoint, float thickness)
        {
            var halfThickness = thickness * 0.5f;
            var count = self.currentVertCount;
            self.AddVert(new Vector3(centerPoint.x, centerPoint.y + halfThickness, 0f), Color.black, new Vector2(0, 0));
            self.AddVert(new Vector3(centerPoint.x + halfThickness, centerPoint.y, 0f), Color.black, new Vector2(0, 0)); 
            self.AddVert(new Vector3(centerPoint.x, centerPoint.y - halfThickness, 0f), Color.black, new Vector2(0, 0));
            self.AddVert(new Vector3(centerPoint.x - halfThickness, centerPoint.y, 0f), Color.black, new Vector2(0, 0));
            
            self.AddTriangle(count, count + 3, count + 1);
            self.AddTriangle(count + 2, count + 1, count + 3);
        }
    }
}