using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Events settle in the simulation. This view only paints their recipients and pending flights.
    public sealed class ChronicleBattleFX
    {
        sealed class Mark
        {
            public Text Label;
            public RawImage Paint;
            public Vector2 LabelPoint,Point;
            public float At=-10,Life;
            public Color Tint;
            public int Frame;
        }
        sealed class Flight
        {
            public RawImage Paint;
            public Vector2 Origin,Destination;
            public float Progress;
            public Color Tint;
            public bool Active;
            public int Frame;
        }
        readonly Mark[] marks=new Mark[18];
        readonly Flight[] flights=new Flight[4];
        readonly ChronicleActor[] actors;
        readonly Vector2[] enemies;
        readonly HandDrawnArt.Layout layout;
        readonly float stageHeight;
        int next;
        public int PresentedSerial { get; private set; }
        public int ActiveCount { get; private set; }
        public int FlyingCount { get; private set; }
        public int LastEnemyRecipient { get; private set; }=-1;
        public int LastEnemySource { get; private set; }=-1;
        public ChronicleBattleFX(Transform stage,ChronicleActor[] party,Vector2 enemyFeet,int serial):this(stage,party,new[]{enemyFeet-new Vector2(0,145)},serial){}
        public ChronicleBattleFX(Transform stage,ChronicleActor[] party,Vector2[] enemyPoints,int serial)
        {
            actors=party;enemies=enemyPoints;PresentedSerial=serial;stageHeight=((RectTransform)stage).rect.height;
            layout=JsonUtility.FromJson<HandDrawnArt.Layout>(Resources.Load<TextAsset>("Art/HandDrawn/forge-fx-layout").text);
            var layer=ForgeUI.Rect("SagaImpactFX",stage,0,0,1020,stageHeight);
            for(int i=0;i<flights.Length;i++)
            {
                var f=new Flight();flights[i]=f;f.Paint=HandDrawnArt.Piece("SagaProjectile"+i,layer,"forge-fx",8,0,0,76,76);f.Paint.color=Color.clear;
            }
            for(int i=0;i<marks.Length;i++)
            {
                var m=new Mark();marks[i]=m;m.Paint=HandDrawnArt.Piece("ImpactPaint"+i,layer,"forge-fx",0,0,0,110,110);m.Paint.color=Color.clear;
                m.Label=ForgeUI.Label("ImpactValue"+i,layer,"",0,0,330,110,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold,TextAnchor.MiddleCenter);ForgeUI.TextShadow(m.Label);
            }
        }
        Vector2 EnemyPoint(int slot){return slot>=0&&slot<enemies.Length?enemies[slot]:enemies.Length>0?enemies[0]:new Vector2(815,stageHeight*.6f);}
        Vector2 PartyPoint(int slot)
        {
            if(slot<0||slot>=actors.Length||actors[slot]==null)return new Vector2(310,stageHeight*.65f);
            var a=actors[slot];return Vector2.Lerp(a.Position,a.ContactPoint,.8f);
        }
        void PaintFrame(RawImage image,int frame,Vector2 center,float width,float height)
        {
            var f=layout.frames[Mathf.Clamp(frame,0,layout.frames.Length-1)];image.uvRect=new Rect(f.x,f.y,f.w,f.h);
            float w=f.w*layout.width,h=f.h*layout.height,scale=Mathf.Min(width/w,height/h);image.rectTransform.pivot=new Vector2(.5f,.5f);
            image.rectTransform.sizeDelta=new Vector2(w*scale,h*scale);image.rectTransform.anchoredPosition=new Vector2(center.x,-center.y);
        }
        void Emit(string value,Vector2 point,Color tint,float time,float life=1.05f,int frame=0)
        {
            var m=marks[next++%marks.Length];m.Point=point;m.At=time;m.Life=life;m.Tint=tint;m.Frame=frame;m.Label.text=value;
            m.Label.rectTransform.sizeDelta=new Vector2(value.Length>9?720:330,110);
            m.Label.rectTransform.sizeDelta=new Vector2(Mathf.Clamp(Mathf.Ceil(m.Label.preferredWidth)+24,90,960),Mathf.Max(m.Label.fontSize*1.4f,m.Label.preferredHeight+8));m.Label.rectTransform.pivot=new Vector2(.5f,.5f);PlaceLabel(m,point,time);
        }
        void PlaceLabel(Mark mark,Vector2 point,float time)
        {
            Vector2 size=mark.Label.rectTransform.rect.size;
            for(int row=0;row<6;row++)for(int column=0;column<7;column++)
            {
                int side=column==0?0:(column+1)/2*(column%2==1?-1:1);
                var candidate=new Vector2(Mathf.Clamp(point.x+side*(size.x+18),size.x*.5f,1020-size.x*.5f),Mathf.Clamp(point.y-row*(size.y+18),size.y*.5f+42,stageHeight-size.y*.5f));bool clear=true;
                foreach(var other in marks)
                {
                    float age=time-other.At;if(other==mark||age<0||age>=other.Life)continue;Vector2 otherSize=other.Label.rectTransform.rect.size;
                    if(Mathf.Abs(candidate.x-other.LabelPoint.x)<(size.x+otherSize.x)*.5f+8&&Mathf.Abs(candidate.y-other.LabelPoint.y)<(size.y+otherSize.y)*.5f+50){clear=false;break;}
                }
                mark.LabelPoint=candidate;if(clear)return;
            }
            // A crowded compact arena defers a value to the journal instead of overprinting another value.
            mark.Life=0;
        }
        public void Consume(SagaRunState run,float time,bool sound,Action<string> play)
        {
            foreach(var e in run.Events)
            {
                if(e.Id<=PresentedSerial)continue;PresentedSerial=e.Id;
                if(e.Enemy&&(e.Kind==SagaEventKind.Hit||e.Kind==SagaEventKind.Heal||e.Kind==SagaEventKind.Block))LastEnemyRecipient=e.EnemySlot;
                if(!e.Enemy&&e.SourceSlot<0&&e.Kind==SagaEventKind.Hit)LastEnemySource=e.EnemySlot;
                Vector2 point=e.Enemy?EnemyPoint(e.EnemySlot):e.TargetSlot>=0?PartyPoint(e.TargetSlot):e.SourceSlot<0?EnemyPoint(e.EnemySlot):PartyPoint(e.SourceSlot);
                if(e.Kind==SagaEventKind.Hit&&e.Amount>0)
                {
                    if(e.SourceSlot>=0&&e.SourceSlot<run.Units.Length)
                    {
                        int recipe=run.Units[e.SourceSlot].WeaponRecipe;var kind=recipe>=0&&recipe<GameContent.Recipes.Length?GameContent.Recipes[recipe].Kind:WeaponKind.None;
                        if(sound)play(kind==WeaponKind.Bow?"impact-wood":kind==WeaponKind.Staff?"impact-crystal":kind==WeaponKind.Hammer?"impact-stone":"impact-iron");
                    }
                    Emit("−"+Math.Ceiling(e.Amount),point,e.Enemy?ForgeUI.Cream:ForgeUI.Orange,time);
                    if(!e.Enemy&&e.TargetSlot>=0&&e.TargetSlot<actors.Length&&actors[e.TargetSlot]!=null)actors[e.TargetSlot].Flash(ForgeUI.Orange,time+.18f);
                }
                else if(e.Kind==SagaEventKind.Heal&&e.Amount>0)Emit("+"+Math.Ceiling(e.Amount),point,ForgeUI.Teal,time,1.05f,8);
                else if(e.Kind==SagaEventKind.Block)
                {
                    Emit(e.Amount>0?(e.Enemy?"Shield ":"Block ")+Math.Ceiling(e.Amount):e.Text.Contains("disrupted")?"Interrupted":"Guard",point,ForgeUI.Teal,time,.9f,8);
                    if(!e.Enemy&&e.TargetSlot>=0&&e.TargetSlot<actors.Length&&actors[e.TargetSlot]!=null)actors[e.TargetSlot].Flash(ForgeUI.Teal,time+.2f);
                }
                else if(e.Kind==SagaEventKind.Break)Emit("GUARD BROKEN",point,ForgeUI.Gold,time,1.6f,8);
                else if(e.Kind==SagaEventKind.Phase)Emit("PHASE "+(run.BossPhase+1),EnemyPoint(e.EnemySlot),ForgeUI.Orange,time,1.6f,8);
                else if(e.Kind==SagaEventKind.Skill)
                {
                    if(e.SourceSlot>=0&&e.SourceSlot<actors.Length&&actors[e.SourceSlot]!=null)actors[e.SourceSlot].Flash(ForgeUI.Gold,time+.18f);
                    if(e.Enemy&&e.Text.Contains("shatter"))Emit("SHATTER",EnemyPoint(e.EnemySlot),ForgeUI.Teal,time,.95f,8);
                    else if(e.Enemy&&e.Text.Contains("surge"))Emit("RUNE SURGE",EnemyPoint(e.EnemySlot),ForgeUI.Gold,time,.95f,8);
                }
            }
            // Flight progress comes from the current announced schedule, never from a post-hit tween.
            for(int slot=0;slot<3;slot++)
            {
                var f=flights[slot];f.Active=false;var u=run.Units[slot];int recipe=u.WeaponRecipe;
                if(run.Phase!=SagaPhase.Combat||u.HeroId<0||u.HP<=0||u.ImpactTick<=run.RoomTick||u.TargetEnemy<0||recipe<0||recipe>=GameContent.Recipes.Length||actors[slot]==null)continue;
                var kind=GameContent.Recipes[recipe].Kind;if(kind!=WeaponKind.Bow&&kind!=WeaponKind.Staff)continue;
                f.Active=true;f.Origin=actors[slot].ContactPoint;f.Destination=EnemyPoint(u.TargetEnemy);f.Progress=Progress(run,u.WindupTick,u.ImpactTick);f.Tint=kind==WeaponKind.Staff?ForgeUI.Teal:ForgeUI.Gold;f.Frame=kind==WeaponKind.Staff?8:0;
            }
            var shot=flights[3];shot.Active=run.Phase==SagaPhase.Combat&&run.ThreatAction=="ranged"&&run.ThreatImpactTick>run.RoomTick&&run.ThreatEnemy>=0;
            if(shot.Active){shot.Origin=EnemyPoint(run.ThreatEnemy);shot.Destination=PartyPoint(run.ThreatTarget);shot.Progress=Progress(run,run.ThreatStartTick,run.ThreatImpactTick);shot.Tint=ForgeUI.Orange;shot.Frame=0;}
        }
        static float Progress(SagaRunState run,int start,int end)
        {return Mathf.Clamp01((float)((run.RoomTick+Math.Min(.099,Math.Max(0,run.TickRemainder))*10-start)/Math.Max(1,end-start)));}
        public void Tick(float time,bool reduced)
        {
            int active=0,flying=0;
            foreach(var f in flights)
            {
                if(!f.Active||reduced){f.Paint.color=Color.clear;continue;}flying++;
                var point=Vector2.Lerp(f.Origin,f.Destination,f.Progress);point.y-=Mathf.Sin(f.Progress*Mathf.PI)*22;
                PaintFrame(f.Paint,f.Frame+Mathf.Clamp((int)(f.Progress*4),0,3),point,f.Frame==0?100:70,f.Frame==0?30:70);
                f.Paint.rectTransform.localEulerAngles=new Vector3(0,0,f.Frame==0?Mathf.Atan2(-(f.Destination.y-f.Origin.y),f.Destination.x-f.Origin.x)*Mathf.Rad2Deg:0);f.Paint.color=new Color(f.Tint.r,f.Tint.g,f.Tint.b,.92f);
            }
            foreach(var m in marks)
            {
                float age=time-m.At;if(age<0||age>=m.Life){m.Label.color=Color.clear;m.Paint.color=Color.clear;continue;}active++;
                float fade=Mathf.Clamp01((m.Life-age)/.38f);m.Label.rectTransform.anchoredPosition=new Vector2(m.LabelPoint.x,-m.LabelPoint.y+(reduced?0:Mathf.Min(42,age*42)));m.Label.color=new Color(m.Tint.r,m.Tint.g,m.Tint.b,fade);
                PaintFrame(m.Paint,m.Frame+(reduced?1:Mathf.Clamp((int)(age/.075f),0,3)),m.Point,130,130);m.Paint.color=new Color(m.Tint.r,m.Tint.g,m.Tint.b,age<.30f?.85f*(1-age/.30f):0);
            }
            FlyingCount=flying;ActiveCount=active;
        }
    }

    public sealed class ChroniclePauseGlyph : Graphic
    {
        bool paused;
        public void ShowPaused(bool value){if(paused==value)return;paused=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();
            if(paused)
            {
                vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.center.y),color,Vector2.zero);vh.AddTriangle(0,1,2);return;
            }
            for(int i=0;i<2;i++)
            {
                float x=r.xMin+r.width*(i==0?.12f:.62f),w=r.width*.26f;int start=i*4;
                vh.AddVert(new Vector3(x,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(x,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(x+w,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(x+w,r.yMin),color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
