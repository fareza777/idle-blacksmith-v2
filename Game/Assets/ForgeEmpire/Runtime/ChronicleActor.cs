using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Art poses and equipment layers share one grounded origin. Gameplay owns all timing.
    public sealed class ChronicleActor
    {
        [System.Serializable] public sealed class GripMark { public float x,y,offX,offY; }
        [System.Serializable] public sealed class GripSheet { public string name; public GripMark[] frames; }
        [System.Serializable] public sealed class GripAtlas { public GripSheet[] sheets; }
        static readonly System.Collections.Generic.Dictionary<string,GripMark[]> handMaps=new System.Collections.Generic.Dictionary<string,GripMark[]>();
        static GripMark GripFor(string sheet,int pose)
        {
            if(handMaps.Count==0)
            {
                var data=Resources.Load<TextAsset>("Art/HandDrawn/painted-grips");
                if(!data)throw new System.InvalidOperationException("Missing measured painted hand attachments");
                foreach(var item in JsonUtility.FromJson<GripAtlas>(data.text).sheets)handMaps.Add(item.name,item.frames);
            }
            if(!handMaps.TryGetValue(sheet,out var frames)||frames.Length!=20)throw new System.InvalidOperationException("Incomplete painted hand attachments: "+sheet);
            return frames[pose];
        }
        readonly RawImage body,weapon,secondWeapon,barrier;
        readonly PaintedBodyImage paintedBody;
        readonly BowStringGraphic bowString;
        readonly RectTransform root;
        readonly int weaponId,heroId;
        readonly string bodySheet;
        readonly HandDrawnArt.Layout bodyLayout;
        readonly float size;
        readonly Vector2 feet;
        string action="";
        int lowContactPose=-1;
        float actionAt,attackX,attackY,returnX,returnY,attackScale=1,returnScale=1,attackAim,returnAt=-10,flashUntil;
        Color flashColor=Color.white;
        Vector2 primaryJoint,offhandJoint;
        public RawImage Body { get { return body; } }
        public string BodySheet { get { return bodySheet; } }
        public Vector2 Feet { get { return feet; } }
        public Vector2 Position { get { return new Vector2(root.anchoredPosition.x,-root.anchoredPosition.y); } }
        public Vector2 ContactPoint { get { var world=weapon?weapon.rectTransform.position:root.TransformPoint(new Vector2(0,size*.5f));var p=(Vector2)root.parent.InverseTransformPoint(world);return new Vector2(p.x,-p.y); } }
        public Vector2 HandPosition { get { var p=(Vector2)root.parent.InverseTransformPoint(root.TransformPoint(primaryJoint));return new Vector2(p.x,-p.y); } }
        public Vector2 WeaponTip { get { if(!weapon)return ContactPoint;var r=weapon.rectTransform;var p=(Vector2)root.parent.InverseTransformPoint(r.TransformPoint(new Vector2(r.rect.center.x,r.rect.yMax)));return new Vector2(p.x,-p.y); } }
        public ChronicleActor(string name,Transform parent,int hero,CraftedItem equippedWeapon,CraftedItem equippedArmor,Vector2 position,float height)
        {
            size=height;feet=position;attackX=position.x;attackY=position.y;heroId=hero;weaponId=equippedWeapon==null?-1:equippedWeapon.RecipeId;
            bodySheet=ChronicleWardrobe.Sheet(hero,equippedArmor==null?-1:equippedArmor.RecipeId);
            bodyLayout=JsonUtility.FromJson<HandDrawnArt.Layout>(Resources.Load<TextAsset>("Art/HandDrawn/"+bodySheet+"-layout").text);
            root=ForgeUI.Rect(name,parent,position.x,position.y,1,1);
            ForgeUI.Light(name+"GroundedShadow",root,-height*.23f,-height*.025f,height*.46f,height*.055f).color=new Color(0,0,0,.42f);
            barrier=ForgeUI.Light(name+"Barrier",root,-height*.46f,-height*.94f,height*.92f,height*.92f);barrier.color=Color.clear;
            paintedBody=ForgeUI.Rect(name+"Body",root,0,0,height,height).gameObject.AddComponent<PaintedBodyImage>();paintedBody.raycastTarget=false;body=paintedBody;
            HandDrawnArt.Pose(body,bodySheet,0,Vector2.zero,height,height);
            if(equippedWeapon!=null)
            {
                var kind=GameContent.Recipes[weaponId].Kind;
                float factor=kind==WeaponKind.Greatsword?.82f:kind==WeaponKind.Spear?1.05f:kind==WeaponKind.Staff?.94f:kind==WeaponKind.Daggers?.34f:kind==WeaponKind.Bow?.64f:.64f;
                weapon=HandDrawnArt.Piece(name+"Weapon",root,"held-weapons",(int)kind,0,0,height*factor,height*factor);
                if(kind==WeaponKind.Daggers)secondWeapon=HandDrawnArt.Piece(name+"Offhand",root,"held-weapons",5,0,0,height*.31f,height*.31f);
                if(kind==WeaponKind.Bow)
                {
                    var bowUV=weapon.uvRect;weapon.uvRect=new Rect(bowUV.x+bowUV.width,bowUV.y,-bowUV.width,bowUV.height);
                    var stringRect=ForgeUI.Rect(name+"BowString",weapon.transform,0,0,1,1);stringRect.anchorMin=Vector2.zero;stringRect.anchorMax=Vector2.one;stringRect.offsetMin=stringRect.offsetMax=Vector2.zero;
                    bowString=stringRect.gameObject.AddComponent<BowStringGraphic>();bowString.color=ForgeUI.Cream;bowString.raycastTarget=false;
                }
            }
            Pose("ready",0,true);
        }
        public void SetStrikeTarget(float x){attackX=x;attackY=feet.y;attackScale=1;attackAim=0;lowContactPose=-1;}
        public void SetStrikeTarget(Vector2 point){attackX=point.x;attackY=point.y;attackScale=1;attackAim=0;lowContactPose=-1;}
        public void SetContactTarget(Vector2 point,float targetHeight=0,float paintedHeight=0)
        {
            if(!weapon){SetStrikeTarget(point);return;}
            var kind=GameContent.Recipes[weaponId].Kind;
            if(kind==WeaponKind.Bow||kind==WeaponKind.Staff){SetStrikeTarget(point);return;}
            // Solve the impact pose from the painted hand and actual held tip, not the actor origin.
            var rig=WeaponPoseRig.Evaluate(kind,3,heroId,0,false);var frame=bodyLayout.frames[rig.BodyPose];
            float scale=Mathf.Min(size/frame.referenceWidth,size/frame.referenceHeight);
            var mark=GripFor(bodySheet,rig.BodyPose);
            attackScale=targetHeight>0?Mathf.Clamp(targetHeight/330,.65f,1):1;
            float contactHeight=paintedHeight>0?paintedHeight:targetHeight,angle=(float)rig.Angle,tipLength=weapon.rectTransform.rect.height*.84f;
            float handHeight=((1-mark.y)-frame.pivotY)*frame.h*bodyLayout.height*scale;lowContactPose=-1;
            if(kind==WeaponKind.Daggers&&contactHeight>0&&(handHeight-tipLength*.9f)*attackScale>contactHeight*.9f)
            {
                // A short blade needs a grounded crouch to reach a low creature.
                lowContactPose=13;frame=bodyLayout.frames[lowContactPose];mark=GripFor(bodySheet,lowContactPose);
                scale=Mathf.Min(size/frame.referenceWidth,size/frame.referenceHeight);
                handHeight=((1-mark.y)-frame.pivotY)*frame.h*bodyLayout.height*scale;
            }
            float grip=(mark.x-frame.pivotX)*frame.w*bodyLayout.width*scale;
            if(contactHeight>0)
            {
                angle=-Mathf.Acos(Mathf.Clamp((contactHeight*.78f/attackScale-handHeight)/tipLength,-.98f,.98f))*Mathf.Rad2Deg;
            }
            attackAim=angle-(float)rig.Angle;float tip=-Mathf.Sin(angle*Mathf.Deg2Rad)*tipLength;
            attackX=point.x-(grip+(float)rig.Reach*size+tip)*attackScale;attackY=point.y;
        }
        public void Flash(Color color,float until){flashColor=color;flashUntil=until;}
        public void ShowBarrier(double amount,double maximum){barrier.color=new Color(.45f,.78f,1,.22f*Mathf.Sqrt(Mathf.Clamp01((float)(amount/System.Math.Max(1,maximum)))));}
        public void Pose(string next,float time,bool reduced,float walkDistance=0,float anticipation=.4f)
        {
            if(action!=next)
            {
                if(action=="attack"||action=="windup"){returnX=root.anchoredPosition.x;returnY=-root.anchoredPosition.y;returnScale=root.localScale.x;returnAt=time;}
                action=next;actionAt=time;
            }
            float elapsed=Mathf.Max(0,time-actionAt);int pose=0;
            bool ranged=weaponId>=0&&(GameContent.Recipes[weaponId].Kind==WeaponKind.Bow||GameContent.Recipes[weaponId].Kind==WeaponKind.Staff);
            float x=feet.x,y=feet.y,depth=1;
            if(next=="windup")
            {
                pose=reduced?2:elapsed<.12f?1:2;
                if(!ranged&&!reduced&&(Vector2.Distance(new Vector2(attackX,attackY),feet)>1||Mathf.Abs(attackScale-1)>.001f))
                {
                    float step=Mathf.Clamp(anticipation*.65f,.26f,.55f),travel=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/step));x=Mathf.Lerp(feet.x,attackX,travel);y=Mathf.Lerp(feet.y,attackY,travel);depth=Mathf.Lerp(1,attackScale,travel);
                    if(elapsed<step)pose=14+Mathf.Min(5,(int)(elapsed/(step/6)));
                }
            }
            else if(next=="attack"){pose=reduced?3:elapsed<.13f?3:elapsed<.26f?4:5;if(!ranged&&!reduced){x=attackX;y=attackY;depth=attackScale;}}
            else if(next=="brace")pose=6;else if(next=="parry")pose=7;else if(next=="hurt")pose=8;else if(next=="stagger")pose=9;
            else if(next=="cast")pose=10;else if(next=="heal")pose=11;else if(next=="victory")pose=12;else if(next=="defeat")pose=13;
            else if(next=="walk")pose=14+(int)(walkDistance/8)%6;
            if(next!="windup"&&next!="attack"&&!reduced&&time-returnAt<.42f&&Vector2.Distance(new Vector2(returnX,returnY),feet)>1)
            {float settle=Mathf.SmoothStep(0,1,(time-returnAt)/.42f);x=Mathf.Lerp(returnX,feet.x,settle);y=Mathf.Lerp(returnY,feet.y,settle);depth=Mathf.Lerp(returnScale,1,settle);if(next=="ready")pose=19-Mathf.Min(5,(int)((time-returnAt)/.07f));}
            var weaponKind=weaponId<0?WeaponKind.None:GameContent.Recipes[weaponId].Kind;
            var rig=WeaponPoseRig.Evaluate(weaponKind,pose,heroId,elapsed,reduced);
            if(!reduced&&lowContactPose>=0&&(next=="attack"&&(pose==3||pose==4)||next=="windup"&&elapsed>=.12f))rig.BodyPose=lowContactPose;
            HandDrawnArt.Pose(body,bodySheet,rig.BodyPose,Vector2.zero,size,size);
            paintedBody.SetJoints((float)rig.Lean,(float)rig.Sway,reduced?0:Mathf.Sin(time*2.3f+heroId)*.55f);
            body.color=time<flashUntil?Color.Lerp(Color.white,flashColor,.42f):Color.white;
            float nudge=reduced?0:next=="attack"?Mathf.Sin(Mathf.Clamp01(elapsed/.3f)*Mathf.PI)*8:next=="hurt"?-6*Mathf.Exp(-elapsed*9):next=="walk"?Mathf.Min(0,-48+walkDistance):0;
            // Thrust moves the hand and held shaft together; never slide a weapon out of the palm.
            if(next=="attack"&&!ranged&&!reduced)nudge+=(float)rig.Reach*size*depth;
            root.anchoredPosition=new Vector2(x+nudge,-y);
            root.localScale=new Vector3(depth,depth,1);
            var rect=body.rectTransform;float w=rect.sizeDelta.x,h=rect.sizeDelta.y;
            var mark=GripFor(bodySheet,rig.BodyPose);
            primaryJoint=paintedBody.JointPoint(new Vector2((mark.x-rect.pivot.x)*w,((1-mark.y)-rect.pivot.y)*h));
            offhandJoint=paintedBody.JointPoint(new Vector2((mark.offX-rect.pivot.x)*w,((1-mark.offY)-rect.pivot.y)*h));
            if(weapon)
            {
                bool casting=pose==10||pose==11,down=pose==13;
                weapon.rectTransform.pivot=weaponKind==WeaponKind.Bow?new Vector2(.5f,.5f):new Vector2(.5f,.16f);
                weapon.rectTransform.anchoredPosition=primaryJoint;
                float aim=!reduced&&!ranged&&(next=="attack"&&(pose==3||pose==4)||next=="windup")?attackAim:0;
                weapon.rectTransform.localEulerAngles=new Vector3(0,0,(float)rig.Angle+aim);
                weapon.gameObject.SetActive(!down&&(!casting||weaponKind==WeaponKind.Staff||weaponKind==WeaponKind.Bow));
                if(bowString)bowString.SetDraw((float)rig.Draw);
                if(secondWeapon)
                {
                    secondWeapon.rectTransform.pivot=new Vector2(.5f,.16f);secondWeapon.rectTransform.anchoredPosition=offhandJoint;
                    secondWeapon.rectTransform.localEulerAngles=new Vector3(0,0,pose==3?-48:-125);secondWeapon.gameObject.SetActive(!down&&!casting&&Vector2.Distance(primaryJoint,offhandJoint)>size*.08f);
                }
            }
        }
    }
}
