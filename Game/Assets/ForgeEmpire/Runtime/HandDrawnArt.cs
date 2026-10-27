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
            public int columns,rows,width,height;
            public Frame[] frames;
        }
        sealed class Sheet { public Texture2D Texture; public Layout Layout; }
        static readonly Dictionary<string,Sheet> sheets=new Dictionary<string,Sheet>();
        static Sheet Get(string name)
        {
            if(sheets.TryGetValue(name,out var sheet))return sheet;
            var texture=Resources.Load<Texture2D>("Art/HandDrawn/"+name);
            if(!texture)throw new InvalidOperationException("Missing illustrated art: "+name);
            var definition=Resources.Load<TextAsset>("Art/HandDrawn/"+name+"-layout");
            sheet=new Sheet{Texture=texture,Layout=definition?JsonUtility.FromJson<Layout>(definition.text):null};
            sheets.Add(name,sheet);return sheet;
        }
        public static Texture2D Texture(string name){return Get(name).Texture;}
        static Frame GetFrame(Sheet sheet,int frame)
        {
            if(sheet.Layout==null||sheet.Layout.frames==null||sheet.Layout.frames.Length==0)throw new InvalidOperationException("Missing illustrated atlas metadata");
            return sheet.Layout.frames[Mathf.Clamp(frame,0,sheet.Layout.frames.Length-1)];
        }
        public static RawImage Person(string name,Transform parent,string sheet,int frame,Vector2 feet,float width,float height,bool faceRight=true)
        {
            var image=ForgeUI.Picture(name,parent,Get(sheet).Texture,0,0,width,height);
            Pose(image,sheet,frame,feet,width,height,faceRight);return image;
        }
        public static void Pose(RawImage image,string name,int frame,Vector2 feet,float width,float height,bool faceRight=true)
        {
            var sheet=Get(name);var f=GetFrame(sheet,frame);
            image.texture=sheet.Texture;
            image.uvRect=faceRight?new Rect(f.x,f.y,f.w,f.h):new Rect(f.x+f.w,f.y,-f.w,f.h);
            var rect=image.rectTransform;rect.pivot=new Vector2(faceRight?f.pivotX:1-f.pivotX,f.pivotY);
            float scale=Mathf.Min(width/f.referenceWidth,height/f.referenceHeight);
            int sourceWidth=sheet.Layout.width>0?sheet.Layout.width:sheet.Texture.width,sourceHeight=sheet.Layout.height>0?sheet.Layout.height:sheet.Texture.height;
            rect.sizeDelta=new Vector2(f.w*sourceWidth*scale,f.h*sourceHeight*scale);
            rect.anchoredPosition=new Vector2(feet.x,-feet.y);
        }
        public static RawImage Icon(string name,Transform parent,int frame,float x,float y,float width,float height)
        {
            var image=ForgeUI.Picture(name,parent,Texture("equipment"),0,0,width,height);
            EquipmentFrame(image,frame,width,height);
            image.rectTransform.anchoredPosition=new Vector2(x+width*.5f,-y-height*.5f);return image;
        }
        public static void EquipmentFrame(RawImage image,int frame,float width,float height)
        {
            var sheet=Get(frame<20?"equipment":"gear-"+((frame-20)/20));var f=GetFrame(sheet,frame<20?frame:(frame-20)%20);
            image.texture=sheet.Texture;image.uvRect=new Rect(f.x,f.y,f.w,f.h);
            int sourceWidth=sheet.Layout.width>0?sheet.Layout.width:sheet.Texture.width,sourceHeight=sheet.Layout.height>0?sheet.Layout.height:sheet.Texture.height;
            float w=f.w*sourceWidth,h=f.h*sourceHeight,scale=Mathf.Min(width/w,height/h);
            image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.sizeDelta=new Vector2(w*scale,h*scale);
        }
        static readonly string[] regions={"emberwood","marsh","frostpeak","caldera","starfall","dawnforge"};
        public static RawImage Landscape(string name,Transform parent,int region,float x,float y,float width,float height,bool battle=false)
        {
            var texture=Texture(regions[Mathf.Clamp(region,0,5)]);
            var image=ForgeUI.Picture(name,parent,texture,x,y,width,height);
            float source=texture.width/(float)texture.height,target=width/height;
            float cropWidth=Mathf.Min(1,target/source),cropHeight=Mathf.Min(1,source/target);
            float top=battle?Mathf.Clamp(.82f-.82f*cropHeight,0,1-cropHeight):Mathf.Clamp(.43f-cropHeight*.5f,0,1-cropHeight);
            image.uvRect=new Rect((1-cropWidth)*.5f,1-top-cropHeight,cropWidth,cropHeight);return image;
        }
        public static RawImage Prop(string name,Transform parent,int frame,float x,float y,float width,float height)
        {
            var sheet=Get("props");var f=sheet.Layout.frames[frame];
            var image=ForgeUI.Picture(name,parent,sheet.Texture,x,y,width,height);image.uvRect=new Rect(f.x,f.y,f.w,f.h);return image;
        }
        public static RawImage Piece(string name,Transform parent,string atlas,int frame,float x,float y,float width,float height)
        {
            var sheet=Get(atlas);var f=GetFrame(sheet,frame);var image=ForgeUI.Picture(name,parent,sheet.Texture,0,0,width,height);
            int sourceWidth=sheet.Layout.width>0?sheet.Layout.width:sheet.Texture.width,sourceHeight=sheet.Layout.height>0?sheet.Layout.height:sheet.Texture.height;
            image.uvRect=new Rect(f.x,f.y,f.w,f.h);float w=f.w*sourceWidth,h=f.h*sourceHeight,scale=Mathf.Min(width/w,height/h);
            image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.sizeDelta=new Vector2(w*scale,h*scale);image.rectTransform.anchoredPosition=new Vector2(x+width*.5f,-y-height*.5f);return image;
        }
    }
}
