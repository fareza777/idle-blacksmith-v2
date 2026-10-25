using System.Collections.Generic;
using UnityEngine;

namespace ForgeEmpire
{
    // Small interface marks share a smooth ink stroke; content art remains generated paintings.
    public static class IllustratedGlyphs
    {
        static Vector2[] Path(params float[] coordinates)
        {
            var points=new Vector2[coordinates.Length/2];
            for(int i=0;i<points.Length;i++)points[i]=new Vector2(coordinates[i*2],coordinates[i*2+1]);
            return points;
        }
        static Vector2[] Circle(float x,float y,float radius)
        {
            var points=new Vector2[33];
            for(int i=0;i<points.Length;i++){float angle=i*Mathf.PI/16;points[i]=new Vector2(x+Mathf.Cos(angle)*radius,y+Mathf.Sin(angle)*radius);}
            return points;
        }
        public static Texture2D Create(string kind)
        {
            var paths=new List<Vector2[]>();
            switch(kind)
            {
                case "back":paths.Add(Path(30,13,11,32,30,51));paths.Add(Path(12,32,55,32));break;
                case "check":paths.Add(Path(10,33,25,47,54,16));break;
                case "heal":paths.Add(Path(32,12,32,52));paths.Add(Path(12,32,52,32));break;
                case "strike":
                    paths.Add(Path(24,40,44,15,56,7,51,22,30,46));paths.Add(Path(17,36,38,55));paths.Add(Path(23,46,12,57));paths.Add(Circle(10,57,3));break;
                case "hero":
                    paths.Add(Circle(32,18,8));paths.Add(Path(13,55,15,43,22,35,32,39,42,35,49,43,51,55,13,55));paths.Add(Path(23,36,26,52));paths.Add(Path(41,36,38,52));break;
                case "map":
                    paths.Add(Path(7,16,23,9,41,16,57,9,57,49,41,56,23,49,7,56,7,16));paths.Add(Path(23,10,23,49));paths.Add(Path(41,16,41,55));break;
                case "book":
                    paths.Add(Path(32,19,25,13,17,11,7,12,7,49,18,48,25,50,32,55,39,50,47,48,57,49,57,12,47,11,39,13,32,19,32,55));
                    paths.Add(Path(14,23,22,23,27,26));paths.Add(Path(14,33,22,33,27,36));paths.Add(Path(37,26,42,23,50,23));paths.Add(Path(37,36,42,33,50,33));break;
                case "crew":
                    paths.Add(Circle(19,18,6));paths.Add(Circle(45,18,6));paths.Add(Circle(32,32,7));
                    paths.Add(Path(6,39,8,31,14,27,23,27));paths.Add(Path(41,27,50,27,56,31,58,39));paths.Add(Path(16,57,18,47,25,41,39,41,46,47,48,57));break;
                case "bag":
                    paths.Add(Path(22,19,22,13,26,8,38,8,42,13,42,19));paths.Add(Path(17,19,47,19,53,28,53,53,48,57,16,57,11,53,11,28,17,19));
                    paths.Add(Path(12,32,52,32));paths.Add(Path(26,30,26,38,38,38,38,30));paths.Add(Path(19,47,45,47));break;
                case "lock":
                    paths.Add(Path(20,27,20,18,23,11,29,8,35,8,41,11,44,18,44,27));paths.Add(Path(14,27,50,27,50,54,46,58,18,58,14,54,14,27));paths.Add(Circle(32,39,3));paths.Add(Path(32,42,32,49));break;
                default:
                    paths.Add(Path(7,19,51,19,57,24,49,31,39,34,24,34,18,30,11,27,7,19));
                    paths.Add(Path(25,35,27,44,23,49,18,51,18,56,46,56,46,51,40,48,37,43,38,35));paths.Add(Path(16,14,44,14));break;
            }
            const int size=64;var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var point=new Vector2(x+.5f,y+.5f);float distance=100;
                foreach(var path in paths)for(int i=1;i<path.Length;i++)
                {
                    var span=path[i]-path[i-1];float along=Mathf.Clamp01(Vector2.Dot(point-path[i-1],span)/Mathf.Max(.0001f,span.sqrMagnitude));
                    distance=Mathf.Min(distance,Vector2.Distance(point,path[i-1]+span*along));
                }
                pixels[(size-1-y)*size+x]=new Color(1,1,1,Mathf.Clamp01(2.3f-distance));
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="InkGlyph-"+kind,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels(pixels);texture.Apply(false,true);return texture;
        }
    }
}
