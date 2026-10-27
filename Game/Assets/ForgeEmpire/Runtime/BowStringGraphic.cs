using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed class BowStringGraphic : MaskableGraphic
    {
        float draw;
        public void SetDraw(float amount){if(Mathf.Abs(draw-amount)<.001f)return;draw=amount;SetVerticesDirty();}
        void Line(VertexHelper vh,Vector2 a,Vector2 b,float width)
        {
            Vector2 side=(b-a).normalized;side=new Vector2(-side.y,side.x)*width*.5f;int n=vh.currentVertCount;
            vh.AddVert(a-side,color,Vector2.zero);vh.AddVert(a+side,color,Vector2.zero);vh.AddVert(b+side,color,Vector2.zero);vh.AddVert(b-side,color,Vector2.zero);vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();var nock=new Vector2(r.center.x-r.width*(.16f+.32f*draw),r.center.y);
            Line(vh,new Vector2(r.xMin+r.width*.28f,r.yMin+r.height*.04f),nock,1.6f);Line(vh,nock,new Vector2(r.xMin+r.width*.28f,r.yMax-r.height*.04f),1.6f);
            if(draw>.05f){var tip=new Vector2(r.xMax+r.width*.40f,r.center.y);Line(vh,nock,tip,2.1f);Line(vh,tip,tip+new Vector2(-9,4),2);Line(vh,tip,tip+new Vector2(-9,-4),2);}
        }
    }
}
