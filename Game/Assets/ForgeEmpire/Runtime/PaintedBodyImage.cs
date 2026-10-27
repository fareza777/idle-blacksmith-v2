using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Small 2D joint mesh: feet stay planted while torso, shoulders and cloth settle.
    public sealed class PaintedBodyImage : RawImage
    {
        float lean,sway,breath;
        public void SetJoints(float torsoLean,float clothSway,float breathing)
        {
            if(Mathf.Abs(lean-torsoLean)+Mathf.Abs(sway-clothSway)+Mathf.Abs(breath-breathing)<.002f)return;
            lean=torsoLean;sway=clothSway;breath=breathing;SetVerticesDirty();
        }
        public Vector2 JointPoint(Vector2 local)
        {
            var bounds=GetPixelAdjustedRect();float h=Mathf.Max(1,bounds.height),up=Mathf.Clamp01((local.y-bounds.yMin)/h);
            float torso=Mathf.SmoothStep(0,1,Mathf.Clamp01((up-.12f)/.55f));
            float edge=Mathf.Abs((local.x-bounds.center.x)/Mathf.Max(1,bounds.width)) * 2;
            return local+new Vector2(Mathf.Sin(lean*Mathf.Deg2Rad)*(local.y-bounds.yMin)*torso+sway*edge*Mathf.Sin(up*Mathf.PI),breath*Mathf.Sin(up*Mathf.PI));
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=GetPixelAdjustedRect();const int cols=8,rows=12;
            for(int y=0;y<=rows;y++)for(int x=0;x<=cols;x++)
            {
                float u=x/(float)cols,v=y/(float)rows;var point=JointPoint(new Vector2(rect.xMin+u*rect.width,rect.yMin+v*rect.height));
                vh.AddVert(point,color,new Vector2(uvRect.x+u*uvRect.width,uvRect.y+v*uvRect.height));
            }
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++){int a=y*(cols+1)+x;vh.AddTriangle(a,a+cols+1,a+1);vh.AddTriangle(a+1,a+cols+1,a+cols+2);}
        }
    }
}
