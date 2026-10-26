using System;
using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Read-only event presentation. The simulation has already settled every impact.
    public sealed class ChronicleBattleFX
    {
        sealed class Mark
        {
            public Text Label;
            public RawImage Glow;
            public Image Stroke;
            public Vector2 Origin,Destination,LabelPoint;
            public float At,Life;
            public Color Tint;
            public bool Projectile;
        }
        readonly Mark[] marks=new Mark[18];
        readonly ChronicleActor[] actors;
        readonly Vector2 enemy;
        readonly float stageHeight;
        int next;
        public int PresentedSerial { get; private set; }
        public int ActiveCount { get; private set; }
        public ChronicleBattleFX(Transform stage,ChronicleActor[] party,Vector2 enemyFeet,int serial)
        {
            actors=party;enemy=enemyFeet;PresentedSerial=serial;stageHeight=((RectTransform)stage).rect.height;
            var layer=ForgeUI.Rect("SagaImpactFX",stage,0,0,1020,stageHeight);
            for(int i=0;i<marks.Length;i++)
            {
                var mark=new Mark();marks[i]=mark;
                mark.Glow=ForgeUI.Light("ImpactHalo"+i,layer,0,0,94,94);mark.Glow.color=Color.clear;
                mark.Stroke=ForgeUI.Fill("ImpactStroke"+i,layer,0,0,90,4,Color.clear);mark.Stroke.rectTransform.pivot=new Vector2(.5f,.5f);
                mark.Label=ForgeUI.Label("ImpactValue"+i,layer,"",0,0,330,110,ForgeUI.Small,ForgeUI.Cream,FontStyle.Bold,TextAnchor.MiddleCenter);ForgeUI.TextShadow(mark.Label);
                mark.At=-10;
            }
        }
        Vector2 PartyPoint(int slot)
        {
            if(slot<0||slot>=actors.Length||actors[slot]==null)return new Vector2(310,enemy.y-130);
            var p=actors[slot].Position;return new Vector2(p.x,p.y-135);
        }
        void Emit(string value,Vector2 point,Color tint,float time,float life=1.05f,bool projectile=false,Vector2 from=default(Vector2))
        {
            var m=marks[next++%marks.Length];m.Origin=projectile?from:point;m.Destination=point;m.At=time;m.Life=life;m.Tint=tint;m.Projectile=projectile;
            m.Label.text=value;m.Label.rectTransform.sizeDelta=new Vector2(value.Length>9?720:330,110);
            m.Label.rectTransform.sizeDelta=new Vector2(Mathf.Clamp(Mathf.Ceil(m.Label.preferredWidth)+24,90,960),Mathf.Max(m.Label.fontSize*1.4f,m.Label.preferredHeight+8));
            m.Label.rectTransform.pivot=new Vector2(.5f,.5f);
            PlaceLabel(m,point,time);
        }
        void PlaceLabel(Mark mark,Vector2 point,float time)
        {
            Vector2 size=mark.Label.rectTransform.rect.size;
            for(int row=0;row<5;row++)for(int column=0;column<7;column++)
            {
                int side=column==0?0:(column+1)/2*(column%2==1?-1:1);
                var candidate=new Vector2(Mathf.Clamp(point.x+side*(size.x+18),size.x*.5f,1020-size.x*.5f),Mathf.Clamp(point.y-row*(size.y+18),size.y*.5f+42,stageHeight-size.y*.5f));
                bool clear=true;
                foreach(var other in marks)
                {
                    float age=time-other.At;if(other==mark||age<0||age>=other.Life)continue;
                    Vector2 otherSize=other.Label.rectTransform.rect.size;
                    // Reserve the entire rise path so consecutive impacts remain separate as they drift.
                    if(Mathf.Abs(candidate.x-other.LabelPoint.x)<(size.x+otherSize.x)*.5f+8&&Mathf.Abs(candidate.y-other.LabelPoint.y)<(size.y+otherSize.y)*.5f+50){clear=false;break;}
                }
                mark.LabelPoint=candidate;if(clear)return;
            }
        }
        public void Consume(SagaRunState run,float time,bool sound,Action<string> play)
        {
            foreach(var e in run.Events)
            {
                if(e.Id<=PresentedSerial)continue;PresentedSerial=e.Id;
                Vector2 point=e.Enemy?new Vector2(enemy.x,enemy.y-145):PartyPoint(e.TargetSlot);
                if(e.Kind==SagaEventKind.Hit&&e.Amount>0)
                {
                    bool ranged=false;
                    if(e.SourceSlot>=0)
                    {
                        var u=run.Units[e.SourceSlot];var kind=GameContent.Recipes[u.WeaponRecipe].Kind;ranged=kind==WeaponKind.Bow||kind==WeaponKind.Staff;
                        if(sound)play(kind==WeaponKind.Bow?"impact-wood":kind==WeaponKind.Staff?"impact-crystal":kind==WeaponKind.Hammer?"impact-stone":"impact-iron");
                    }
                    Emit("−"+Math.Ceiling(e.Amount),point,e.Enemy?ForgeUI.Cream:ForgeUI.Orange,time,1.05f,ranged,PartyPoint(e.SourceSlot));
                    if(!e.Enemy&&e.TargetSlot>=0&&actors[e.TargetSlot]!=null)actors[e.TargetSlot].Flash(ForgeUI.Orange,time+.18f);
                }
                else if(e.Kind==SagaEventKind.Heal&&e.Amount>0)Emit("+"+Math.Ceiling(e.Amount),point,ForgeUI.Teal,time);
                else if(e.Kind==SagaEventKind.Block)
                {
                    Emit(e.Amount>0?"Block "+Math.Ceiling(e.Amount):e.Text.Contains("disrupted")?"Interrupted":"Guard",point,ForgeUI.Teal,time,.9f);
                    if(!e.Enemy&&e.TargetSlot>=0&&actors[e.TargetSlot]!=null)actors[e.TargetSlot].Flash(ForgeUI.Teal,time+.2f);
                }
                else if(e.Kind==SagaEventKind.Break)Emit("GUARD BROKEN",point,ForgeUI.Gold,time,1.6f);
                else if(e.Kind==SagaEventKind.Phase)Emit("PHASE "+(run.BossPhase+1),new Vector2(680,95),ForgeUI.Orange,time,1.6f);
                else if(e.Kind==SagaEventKind.Skill&&e.SourceSlot>=0&&actors[e.SourceSlot]!=null)actors[e.SourceSlot].Flash(ForgeUI.Gold,time+.18f);
            }
        }
        public void Tick(float time,bool reduced)
        {
            int active=0;
            foreach(var m in marks)
            {
                float age=time-m.At;if(age<0||age>=m.Life){m.Label.color=Color.clear;m.Glow.color=Color.clear;m.Stroke.color=Color.clear;continue;}
                active++;float fade=Mathf.Clamp01((m.Life-age)/.38f),flight=m.Projectile&&!reduced?Mathf.Clamp01(age/.17f):1;
                Vector2 point=Vector2.Lerp(m.Origin,m.Destination,Mathf.SmoothStep(0,1,flight));
                m.Label.rectTransform.anchoredPosition=new Vector2(m.LabelPoint.x,-m.LabelPoint.y+(reduced?0:Mathf.Min(42,age*42)));
                m.Label.color=new Color(m.Tint.r,m.Tint.g,m.Tint.b,flight<1?0:fade);
                m.Glow.rectTransform.anchoredPosition=new Vector2(point.x-47,-point.y+47);m.Glow.color=new Color(m.Tint.r,m.Tint.g,m.Tint.b,age<.28f?.45f*(1-age/.28f):0);
                m.Stroke.rectTransform.anchoredPosition=new Vector2(point.x,-point.y);
                m.Stroke.rectTransform.localEulerAngles=new Vector3(0,0,m.Projectile?Mathf.Atan2(-(m.Destination.y-m.Origin.y),m.Destination.x-m.Origin.x)*Mathf.Rad2Deg:-32);
                m.Stroke.color=new Color(m.Tint.r,m.Tint.g,m.Tint.b,!reduced&&age<.2f?.9f*(1-age/.2f):0);
            }
            ActiveCount=active;
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
