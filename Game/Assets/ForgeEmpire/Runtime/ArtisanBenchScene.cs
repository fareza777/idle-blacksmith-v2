using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Presentation only: every workpiece stage comes from a paid, saved simulation step.
    sealed class ArtisanBenchScene
    {
        public enum Cue { None,Heat,Strike,Quench,Rune,Reveal }
        readonly RectTransform root,piece;
        readonly RawImage surface,heat,contact,sparks,steam,hammer,rune,revealGlow,workMark;
        readonly Vector2 contactPoint,nextPoint;
        readonly float height;
        readonly ArtisanStage stage;
        readonly ArtisanMethod method;
        readonly bool target,finished;
        readonly HandDrawnArt.Layout fxLayout;
        Cue cue;
        float cueAt=-100;

        public bool IsAlive { get { return root; } }
        public static float WorkAreaAim(int recipeId,int strike)
        {
            int index=Mathf.Clamp(strike,0,2);var method=WorkshopSimulation.ArtisanMethodFor(recipeId);
            // Fine work follows its scored target; weapon contacts move along the piece while retaining center timing.
            return method==ArtisanMethod.Strike?new[]{.30f,.52f,.72f}[index]:(float)WorkshopSimulation.ArtisanTimingTarget(recipeId,index);
        }
        public static string WorkAreaName(int recipeId,int strike)
        {
            int index=Mathf.Clamp(strike,0,2);var method=WorkshopSimulation.ArtisanMethodFor(recipeId);
            if(method==ArtisanMethod.Fitting)return new[]{"left seam","right seam","center fit"}[index];
            if(method==ArtisanMethod.Engraving)return new[]{"outer channel","inner channel","center seal"}[index];
            if(GameContent.Recipes[recipeId].Kind==WeaponKind.Bow)return new[]{"lower limb","upper limb","grip"}[index];
            return new[]{"heel","middle","tip"}[index];
        }

        public ArtisanBenchScene(Transform parent,CraftedItem item,ArtisanStage currentStage,int strikes,float sceneHeight,bool targetPreview=false,bool result=false)
        {
            height=sceneHeight;stage=currentStage;target=targetPreview;finished=result||stage>=ArtisanStage.Ready;method=WorkshopSimulation.ArtisanMethodFor(item.RecipeId);
            root=ForgeUI.Rect("ArtisanBenchScene",parent,0,0,1020,height);
            root.gameObject.AddComponent<RectMask2D>();
            var backdrop=ForgeUI.Picture("ArtisanWorkbench",root,HandDrawnArt.Texture("forge-closeup"),0,0,1020,height);
            float crop=Mathf.Min(1,height/574f);
            backdrop.uvRect=new Rect(0,Mathf.Clamp(.5f-crop*.5f,0,1-crop),1,crop);
            heat=ForgeUI.Light("ArtisanHeat",root,290,height*.10f,430,height*.8f);
            var shadow=ForgeUI.Light("ArtisanPieceShadow",root,295,height*.43f,430,height*.31f);shadow.color=new Color(0,0,0,.76f);
            // Child artwork is centered around this point. A sized, center-pivot parent would
            // shift its children's top-left anchors half a workpiece away from the tool contact.
            piece=ForgeUI.Rect("ArtisanItemOnAnvil",root,510,height*.53f,1,1);
            int family=(int)WorkshopSimulation.ArtisanWorkpieceFor(item.RecipeId);
            string sheet=family<4?"workpieces-metal":"workpieces-fine";int row=family<4?family:family-4;
            // A real unfinished painting is chosen; a finished equipment icon is never masked into a blank.
            surface=finished?HandDrawnArt.Icon(result?"CraftResultItem":"ArtisanPiece",piece,item.RecipeId,-200,-height*.26f,400,height*.52f):
                HandDrawnArt.Piece("ArtisanWorkpiece",piece,sheet,row*5+WorkshopSimulation.ArtisanWorkpieceStage(stage,strikes),-200,-height*.26f,400,height*.52f);
            if(!finished)surface.rectTransform.localRotation=Quaternion.Euler(42,0,-12);
            nextPoint=new Vector2(510+(WorkAreaAim(item.RecipeId,strikes)-.5f)*surface.rectTransform.sizeDelta.x,height*.53f);
            contactPoint=new Vector2(510+(WorkAreaAim(item.RecipeId,Mathf.Max(0,strikes-1))-.5f)*surface.rectTransform.sizeDelta.x,height*.53f);
            workMark=ForgeUI.Glyph("ArtisanWorkArea",root,"strike",nextPoint.x-18,nextPoint.y-18,36,ForgeUI.Gold);
            workMark.gameObject.SetActive(!target&&stage==ArtisanStage.Shape);
            fxLayout=JsonUtility.FromJson<HandDrawnArt.Layout>(Resources.Load<TextAsset>("Art/HandDrawn/forge-fx-layout").text);
            sparks=HandDrawnArt.Piece("ArtisanContactSparks",root,"forge-fx",0,contactPoint.x-120,contactPoint.y-height*.35f,240,height*.54f);
            steam=HandDrawnArt.Piece("ArtisanQuenchSteam",root,"forge-fx",4,350,height*.05f,320,height*.76f);
            revealGlow=HandDrawnArt.Piece("ArtisanRevealPaint",root,"forge-fx",8,320,height*.05f,380,height*.85f);
            contact=ForgeUI.Light("ArtisanContact",root,contactPoint.x-62,contactPoint.y-46,124,92);
            // Reuse the painted tool-rack hammer. Its head pivot lands directly on the work mark.
            hammer=ForgeUI.Picture(method==ArtisanMethod.Engraving?"ArtisanEngravingTool":"ArtisanHammer",root,HandDrawnArt.Texture("props"),0,0,100,height*.54f);
            if(method==ArtisanMethod.Engraving)
            {
                // A narrow painted wooden burnisher from the same tool prop keeps fine work light.
                hammer.uvRect=new Rect(.916f,.704f,.020f,.10f);hammer.rectTransform.sizeDelta=new Vector2(22,height*.38f);hammer.rectTransform.pivot=new Vector2(.5f,.06f);
            }
            else {hammer.uvRect=new Rect(.700f,.690f,.087f,.162f);hammer.rectTransform.pivot=new Vector2(.34f,.80f);}
            rune=ForgeUI.Glyph("ArtisanRuneMark",root,item.Rune==1?"strike":item.Rune==2?"lock":"heal",494,height*.47f,32,ForgeUI.Gold);
            rune.gameObject.SetActive(item.Rune>0&&finished);
            ForgeUI.Fade("ArtisanBenchFoot",root,0,height-48,1020,48,ForgeUI.Background,true);
            Tick(0,true);
        }

        public void Play(Cue action,float time) { cue=action;cueAt=time; }
        void PaintFrame(RawImage image,int frame,Vector2 center,float width,float boundsHeight)
        {
            var f=fxLayout.frames[frame];image.uvRect=new Rect(f.x,f.y,f.w,f.h);
            float w=f.w*fxLayout.width,h=f.h*fxLayout.height,scale=Mathf.Min(width/w,boundsHeight/h);
            image.rectTransform.pivot=new Vector2(.5f,.5f);image.rectTransform.sizeDelta=new Vector2(w*scale,h*scale);image.rectTransform.anchoredPosition=new Vector2(center.x,-center.y);
        }
        public void Tick(float time,bool reducedMotion)
        {
            if(!root)return;
            float age=time-cueAt;bool fresh=age>=0&&age<1.1f;
            bool hot=!target&&stage<=ArtisanStage.Quench;float warm=hot?.38f:0;
            if(fresh&&cue==Cue.Heat)warm+=.4f*(1-age/1.1f);
            heat.color=new Color(1,.63f,.27f,warm);
            surface.color=hot?new Color(1,.77f,.55f,1):Color.white;
            bool strike=fresh&&cue==Cue.Strike;float contactAge=age-.15f;
            bool impact=strike&&contactAge>=0&&contactAge<.38f;
            contact.color=new Color(1,.85f,.49f,impact?(1-contactAge/.38f)*.8f:0);
            workMark.color=new Color(1,.82f,.4f,strike&&age<.65f?0:1);
            sparks.gameObject.SetActive(impact&&method!=ArtisanMethod.Engraving);
            if(impact)PaintFrame(sparks,reducedMotion?1:Mathf.Clamp((int)(contactAge/.095f),0,3),contactPoint,220,height*.5f);
            hammer.gameObject.SetActive(strike&&!reducedMotion&&age<.65f);
            if(hammer.gameObject.activeSelf)
            {
                float progress=age<.15f?1-Mathf.Pow(age/.15f,2):Mathf.Clamp01((age-.15f)/.5f)*.7f;
                Vector2 point=contactPoint+new Vector2(45*progress,-height*.27f*progress);
                hammer.rectTransform.anchoredPosition=new Vector2(point.x,-point.y);
                hammer.rectTransform.localRotation=Quaternion.Euler(0,0,method==ArtisanMethod.Engraving?Mathf.Lerp(-24,-17,progress):Mathf.Lerp(4,-38,progress));
            }
            bool quench=fresh&&cue==Cue.Quench;steam.gameObject.SetActive(quench);
            if(quench)
            {
                PaintFrame(steam,4+(reducedMotion?1:Mathf.Clamp((int)(age/.24f),0,3)),new Vector2(510,height*(reducedMotion?.39f:.39f-age*.13f)),320,height*.7f);
                steam.color=new Color(1,1,1,.9f*(1-age/1.1f));
            }
            bool engraving=fresh&&(cue==Cue.Rune||cue==Cue.Strike&&method==ArtisanMethod.Engraving);
            bool unveiling=fresh&&cue==Cue.Reveal;
            revealGlow.gameObject.SetActive(engraving||unveiling);
            if(engraving||unveiling)
            {
                PaintFrame(revealGlow,8+(reducedMotion?1:Mathf.Clamp((int)(age/.24f),0,3)),engraving?contactPoint:new Vector2(510,height*.47f),unveiling?420:170,height*(unveiling?.85f:.44f));
                revealGlow.color=new Color(1,1,1,.85f*(1-age/1.1f));
            }
            rune.color=engraving?Color.Lerp(ForgeUI.Cream,ForgeUI.Gold,Mathf.Clamp01(age/.8f)):ForgeUI.Gold;
            piece.localScale=Vector3.one;
            if(impact&&!reducedMotion)piece.localScale=new Vector3(1+Mathf.Sin(contactAge/.38f*Mathf.PI)*.012f,1,1);
        }
    }
}
