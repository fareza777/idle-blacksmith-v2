using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public static class ForgeUI
    {
        public static readonly Color Background = Hex("181c1c");
        public static readonly Color Panel = Hex("242a28");
        public static readonly Color Line = Hex("55564a");
        public static readonly Color Gold = Hex("e5be76");
        public static readonly Color Cream = Hex("f8eed8");
        public static readonly Color Muted = Hex("bcc1b3");
        public static readonly Color Teal = Hex("9fd1b5");
        public static readonly Color Orange = Hex("f0a46c");
        public static readonly Color Surface = Hex("303a34");
        public const int Body = 48, Small = 42, Heading = 72, Subheading = 56;
        public static float TextScale = 1;
        public static bool ReducedMotion;
        static Font font, displayFont;
        static Texture2D icons;
        static Texture2D softLight;
        static Sprite buttonShape, circleShape, ringShape;
        static Texture2D fadeTexture;
        static readonly System.Collections.Generic.Dictionary<string,Texture2D> glyphs = new System.Collections.Generic.Dictionary<string,Texture2D>();
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }

        public static int Size(int size) { return Mathf.RoundToInt(size * TextScale); }
        public static float Height(string text, float width, int size = Body, FontStyle style = FontStyle.Normal)
        {
            LoadFonts();
            var settings = new TextGenerationSettings {
                font=size>=Subheading?displayFont:font,fontSize=Size(size),fontStyle=style,lineSpacing=1.12f,scaleFactor=1,
                generationExtents=new Vector2(width,0),textAnchor=TextAnchor.UpperLeft,
                horizontalOverflow=HorizontalWrapMode.Wrap,verticalOverflow=VerticalWrapMode.Overflow,
                color=Color.white,pivot=Vector2.zero,richText=false,generateOutOfBounds=true
            };
            return Mathf.Ceil(Mathf.Max(Size(size)*1.22f,new TextGenerator().GetPreferredHeight(text,settings)+6));
        }

        static void LoadFonts()
        {
            if(!font)font=Resources.Load<Font>("Fonts/Lato-Regular")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if(!displayFont)displayFont=Resources.Load<Font>("Fonts/Spectral-SemiBold")??font;
        }

        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static Image Fill(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(string name, Transform parent, string text, float x, float y, float w, float h, int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.MiddleLeft)
        {
            LoadFonts();
            var label = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Text>();
            label.font = size>=Subheading?displayFont:font; label.text = text; label.fontSize = Size(size); label.color = color;
            label.fontStyle = style; label.alignment = align; label.raycastTarget = false;
            label.supportRichText = false; label.lineSpacing = 1.12f;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        public static Button Button(string name, Transform parent, string text, float x, float y, float w, float h, Color fill, Color textColor, Action action, int size = Body)
        {
            if(fill==Panel||fill==Surface)fill=Color.clear;
            var bg = Fill(name, parent, x, y, w, h, fill); bg.raycastTarget = true;
            if(fill.a>.01f){EnsureShapes();bg.sprite=buttonShape;bg.type=Image.Type.Sliced;}
            var button = bg.gameObject.AddComponent<Button>(); button.targetGraphic = bg;
            var colors = button.colors; colors.highlightedColor = new Color(1.12f,1.1f,1.03f,1); colors.selectedColor = Color.white; colors.pressedColor = new Color(.78f,.85f,.8f,1); colors.disabledColor = new Color(.55f,.59f,.55f,.65f); button.colors = colors;
            button.navigation = new Navigation { mode = string.IsNullOrEmpty(text) ? Navigation.Mode.None : Navigation.Mode.Automatic };
            Label("Label", bg.transform, text, 24, 0, w - 48, h, size, textColor, FontStyle.Bold, TextAnchor.MiddleCenter);
            button.onClick.AddListener(() => action());
            bg.gameObject.AddComponent<ForgeButtonFeedback>();
            return button;
        }

        static void EnsureShapes()
        {
            if(buttonShape)return;
            var round=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var circle=new Texture2D(128,128,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var ring=new Texture2D(128,128,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color[4096];
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){
                float distance=new Vector2(Mathf.Max(18-x,x-45),Mathf.Max(18-y,y-45)).magnitude;
                bool inside=(x>=18&&x<=45)||(y>=18&&y<=45)||distance<=18;
                pixels[y*64+x]=inside?Color.white:Color.clear;
            }
            round.SetPixels(pixels);round.Apply(false,true);
            var discPixels=new Color[16384];var ringPixels=new Color[16384];
            for(int y=0;y<128;y++)for(int x=0;x<128;x++){
                float radius=new Vector2(x-63.5f,y-63.5f).magnitude;
                discPixels[y*128+x]=new Color(1,1,1,Mathf.Clamp01(63-radius));
                ringPixels[y*128+x]=new Color(1,1,1,Mathf.Clamp01(63-radius)*Mathf.Clamp01(radius-60));
            }
            circle.SetPixels(discPixels);circle.Apply(false,true);ring.SetPixels(ringPixels);ring.Apply(false,true);
            buttonShape=Sprite.Create(round,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(20,20,20,20));
            circleShape=Sprite.Create(circle,new Rect(0,0,128,128),new Vector2(.5f,.5f));
            ringShape=Sprite.Create(ring,new Rect(0,0,128,128),new Vector2(.5f,.5f));
        }

        public static Button Medallion(string name,Transform parent,float x,float y,float diameter,Action action,string glyph=null)
        {
            EnsureShapes();
            var button=Button(name,parent,"",x,y,diameter,diameter,new Color(.075f,.09f,.08f,.88f),Cream,action);
            var face=button.GetComponent<Image>();face.sprite=circleShape;face.type=Image.Type.Simple;
            var edge=Fill("BrassRim",button.transform,0,0,diameter,diameter,new Color(Gold.r,Gold.g,Gold.b,.65f));edge.sprite=ringShape;
            if(glyph!=null)Glyph("Symbol",button.transform,glyph,diameter*.28f,diameter*.28f,diameter*.44f,Gold);
            button.navigation=new Navigation{mode=Navigation.Mode.Automatic};
            return button;
        }

        public static void TextShadow(Text label)
        {
            var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.025f,.035f,.03f,.95f);shadow.effectDistance=new Vector2(0,-3);shadow.useGraphicAlpha=true;
        }

        public static RawImage Fade(string name,Transform parent,float x,float y,float w,float h,Color color,bool upward=false)
        {
            if(!fadeTexture){
                fadeTexture=new Texture2D(1,128,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[128];for(int i=0;i<128;i++)pixels[i]=new Color(1,1,1,Mathf.SmoothStep(0,1,i/127f));
                fadeTexture.SetPixels(pixels);fadeTexture.Apply(false,true);
            }
            var fade=Picture(name,parent,fadeTexture,x,y,w,h);fade.color=color;
            fade.uvRect=upward?new Rect(0,1,1,-1):new Rect(0,0,1,1);return fade;
        }

        public static void Ornament(Transform parent,float x,float y,float width)
        {
            var tone=new Color(Gold.r,Gold.g,Gold.b,.35f);
            Fill("Hairline",parent,x,y,width,2,tone);
            var diamond=Fill("Diamond",parent,x+width*.5f-5,y-4,10,10,Gold);diamond.rectTransform.localRotation=Quaternion.Euler(0,0,45);
        }

        public static RawImage Icon(string name, Transform parent, int index, float x, float y, float size)
        {
            if (!icons) icons = Resources.Load<Texture2D>("Art/icons");
            var image = Rect(name, parent, x, y, size, size).gameObject.AddComponent<RawImage>();
            image.texture = icons; image.uvRect = new Rect(index / 8f, 0, 1f / 8f, 1);
            image.raycastTarget = false; return image;
        }

        public static RawImage Picture(string name, Transform parent, Texture2D texture, float x, float y, float w, float h)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false; return image;
        }

        public static RawImage Glyph(string name, Transform parent, string kind, float x, float y, float size, Color color)
        {
            if(!glyphs.TryGetValue(kind,out var texture)){texture=IllustratedGlyphs.Create(kind);glyphs[kind]=texture;}
            var icon=Picture(name,parent,texture,x,y,size,size);icon.color=color;return icon;
        }

        public static RawImage Light(string name,Transform parent,float x,float y,float w,float h)
        {
            if(!softLight)
            {
                softLight=new Texture2D(64,64,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[4096];
                for(int row=0;row<64;row++)for(int col=0;col<64;col++)
                {
                    float radius=new Vector2((col-31.5f)/31.5f,(row-31.5f)/31.5f).magnitude;
                    pixels[row*64+col]=new Color(1,.72f,.36f,Mathf.Pow(Mathf.Clamp01(1-radius),2)*.44f);
                }
                softLight.SetPixels(pixels);softLight.Apply(false,true);
            }
            return Picture(name,parent,softLight,x,y,w,h);
        }

        public static void Frame(Transform parent, float w, float h, Color color)
        {
            Fill("BorderTop",parent,0,0,w,2,color); Fill("BorderBottom",parent,0,h-2,w,2,color);
            Fill("BorderLeft",parent,0,0,2,h,color); Fill("BorderRight",parent,w-2,0,2,h,color);
        }
    }
}
