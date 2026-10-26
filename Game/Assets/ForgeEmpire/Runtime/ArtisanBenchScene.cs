using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Presentation only: the simulation owns every payment, strike and delivery.
    sealed class ArtisanBenchScene
    {
        public enum Cue { None,Heat,Strike,Quench,Rune,Reveal }
        readonly RectTransform root,piece,reveal;
        readonly RawImage surface,heat,contact,sparks,steam,hammer,rune;
        readonly Vector2 hammerRest;
        readonly float height,revealAmount;
        readonly ArtisanStage stage;
        readonly bool target;
        Cue cue;
        float cueAt=-100;

        public bool IsAlive { get { return root; } }

        public ArtisanBenchScene(Transform parent,CraftedItem item,ArtisanStage currentStage,int strikes,float sceneHeight,bool targetPreview=false,bool result=false)
        {
            height=sceneHeight;stage=currentStage;target=targetPreview;
            root=ForgeUI.Rect("ArtisanBenchScene",parent,0,0,1020,height);
            root.gameObject.AddComponent<RectMask2D>();
            var backdrop=ForgeUI.Picture("ArtisanWorkbench",root,HandDrawnArt.Texture("forge-closeup"),0,0,1020,height);
            float crop=height/574f;
            backdrop.uvRect=new Rect(0,Mathf.Clamp(.5f-crop*.5f,0,1-crop),1,crop);
            heat=ForgeUI.Light("ArtisanHeat",root,290,height*.10f,430,height*.8f);
            var shadow=ForgeUI.Light("ArtisanPieceShadow",root,325,height*.43f,370,height*.32f);shadow.color=new Color(0,0,0,.85f);
            piece=ForgeUI.Rect("ArtisanItemOnAnvil",root,510,height*.49f,380,height*.70f);
            piece.pivot=new Vector2(.5f,.5f);piece.anchoredPosition=new Vector2(510,-height*.49f);
            piece.localRotation=Quaternion.Euler(0,0,GameContent.Recipes[item.RecipeId].Slot==GearSlot.Weapon?-24:4);
            var silhouette=HandDrawnArt.Icon("ArtisanSilhouette",piece,item.RecipeId,0,0,380,height*.70f);
            silhouette.color=new Color(.23f,.24f,.23f,1);
            reveal=ForgeUI.Rect("ArtisanSurfaceReveal",piece,0,0,380,height*.70f);reveal.gameObject.AddComponent<RectMask2D>();
            surface=HandDrawnArt.Icon(result?"CraftResultItem":"ArtisanPiece",reveal,item.RecipeId,0,0,380,height*.70f);
            revealAmount=target||stage>=ArtisanStage.Ready?1:stage==ArtisanStage.Heat?.08f:stage==ArtisanStage.Shape?.24f+strikes*.19f:stage==ArtisanStage.Quench?.82f:.94f;
            reveal.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,380*revealAmount);
            steam=ForgeUI.Picture("ArtisanQuenchSteam",root,Resources.Load<Texture2D>("Art/smoke"),414,height*.03f,192,height*.78f);
            steam.uvRect=new Rect(0,0,1f/6,1);
            contact=ForgeUI.Light("ArtisanContact",root,410,height*.26f,210,height*.45f);
            sparks=ForgeUI.Picture("ArtisanContactSparks",root,Resources.Load<Texture2D>("Art/sparks"),392,height*.11f,240,height*.60f);
            sparks.uvRect=new Rect(0,0,.2f,1);
            // The real painted hammer from the existing tool-rack prop, without its rack.
            hammer=ForgeUI.Picture("ArtisanHammer",root,HandDrawnArt.Texture("props"),0,0,100,height*.54f);
            hammer.uvRect=new Rect(.700f,.690f,.087f,.162f);hammer.rectTransform.pivot=new Vector2(.5f,0);
            hammerRest=new Vector2(640,-height*.82f);hammer.rectTransform.anchoredPosition=hammerRest;
            rune=ForgeUI.Glyph("ArtisanRuneMark",piece,item.Rune==1?"strike":item.Rune==2?"lock":"heal",170,height*.30f,40,ForgeUI.Gold);
            rune.gameObject.SetActive(item.Rune>0&&stage>=ArtisanStage.Ready);
            ForgeUI.Fade("ArtisanBenchFoot",root,0,height-64,1020,64,ForgeUI.Background,true);
            Tick(0,true);
        }

        public void Play(Cue action,float time) { cue=action;cueAt=time; }

        public void Tick(float time,bool reducedMotion)
        {
            if(!root)return;
            float age=time-cueAt;
            bool fresh=age>=0&&age<1.1f;
            bool hot=!target&&stage<=ArtisanStage.Quench;
            float warm=hot?.52f:0;
            if(fresh&&cue==Cue.Heat)warm+=.5f*(1-age/1.1f);
            heat.color=new Color(1,.63f,.27f,warm);
            surface.color=hot?new Color(1,.52f,.24f,1):Color.white;
            bool strike=fresh&&cue==Cue.Strike;
            float contactAge=age-.15f;
            bool impact=strike&&contactAge>=0&&contactAge<.38f;
            contact.color=new Color(1,.85f,.49f,impact?(1-contactAge/.38f)*.9f:0);
            sparks.gameObject.SetActive(impact&&!reducedMotion);
            if(impact)sparks.uvRect=new Rect(Mathf.Clamp((int)(contactAge/.076f),0,4)/5f,0,.2f,1);
            hammer.gameObject.SetActive(strike&&!reducedMotion&&age<.65f);
            if(hammer.gameObject.activeSelf)
            {
                float angle=age<.15f?Mathf.Lerp(-24,39,Mathf.Pow(age/.15f,2)):Mathf.Lerp(39,-9,Mathf.Clamp01((age-.15f)/.5f));
                hammer.rectTransform.localRotation=Quaternion.Euler(0,0,angle);
                hammer.rectTransform.anchoredPosition=hammerRest;
            }
            bool quench=fresh&&cue==Cue.Quench;
            steam.gameObject.SetActive(quench);
            if(quench)
            {
                steam.uvRect=new Rect((reducedMotion?2:Mathf.Clamp((int)(age/.18f),0,5))/6f,0,1f/6,1);
                steam.color=new Color(.76f,.88f,.91f,.8f*(1-age/1.1f));
                steam.rectTransform.anchoredPosition=new Vector2(414,-height*(reducedMotion?.03f:.03f-age*.13f));
            }
            bool engraving=fresh&&cue==Cue.Rune;
            rune.color=engraving?Color.Lerp(ForgeUI.Cream,ForgeUI.Gold,Mathf.Clamp01(age/.8f)):ForgeUI.Gold;
            bool unveiling=fresh&&cue==Cue.Reveal;
            if(unveiling)
            {
                heat.color=new Color(1,.82f,.48f,.65f*(1-age/1.1f));
                if(!reducedMotion)reveal.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,380*Mathf.Lerp(.08f,1,Mathf.SmoothStep(0,1,age/.65f)));
            }
            else reveal.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,380*revealAmount);
            piece.localScale=Vector3.one;
            if(impact&&!reducedMotion)piece.localScale=new Vector3(1+Mathf.Sin(contactAge/.38f*Mathf.PI)*.018f,1,1);
        }
    }
}
