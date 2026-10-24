using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public static class HandDrawnArt
    {
        [Serializable] public sealed class Frame
        {
            public float x,y,w,h,pivotX,pivotY,referenceWidth,referenceHeight;
        }
        [Serializable] public sealed class Layout
        {
            public int columns,rows;
            public Frame[] frames;
        }
        sealed class Sheet { public Texture2D Texture; public Layout Layout; }
        static readonly Dictionary<string,Sheet> sheets=new Dictionary<string,Sheet>();
        static Sheet Get(string name)
        {
            if(sheets.TryGetValue(name,out var sheet))return sheet;
            sheet=new Sheet{Texture=Resources.Load<Texture2D>("Art/HandDrawn/"+name),Layout=JsonUtility.FromJson<Layout>(Resources.Load<TextAsset>("Art/HandDrawn/"+name+"-layout").text)};
            sheets.Add(name,sheet);return sheet;
        }
        public static RawImage Person(string name,Transform parent,string sheet,int frame,Vector2 feet,float width,float height,bool faceRight=true)
        {
            var image=ForgeUI.Picture(name,parent,Get(sheet).Texture,0,0,width,height);
            Pose(image,sheet,frame,feet,width,height,faceRight);return image;
        }
        public static void Pose(RawImage image,string name,int frame,Vector2 feet,float width,float height,bool faceRight=true)
        {
            var sheet=Get(name);var f=sheet.Layout.frames[Mathf.Clamp(frame,0,sheet.Layout.frames.Length-1)];
            image.texture=sheet.Texture;
            image.uvRect=faceRight?new Rect(f.x,f.y,f.w,f.h):new Rect(f.x+f.w,f.y,-f.w,f.h);
            var rect=image.rectTransform;rect.pivot=new Vector2(faceRight?f.pivotX:1-f.pivotX,f.pivotY);
            float scale=Mathf.Min(width/f.referenceWidth,height/f.referenceHeight);
            rect.sizeDelta=new Vector2(f.w*sheet.Texture.width*scale,f.h*sheet.Texture.height*scale);
            rect.anchoredPosition=new Vector2(feet.x,-feet.y);
        }
        public static RawImage Prop(string name,Transform parent,int frame,float x,float y,float width,float height)
        {
            var sheet=Get("props");var f=sheet.Layout.frames[frame];
            var image=ForgeUI.Picture(name,parent,sheet.Texture,x,y,width,height);image.uvRect=new Rect(f.x,f.y,f.w,f.h);return image;
        }
    }
}
