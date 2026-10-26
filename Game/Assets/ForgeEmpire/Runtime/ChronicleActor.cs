using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Art poses and equipment layers share one grounded origin. Gameplay owns all timing.
    public sealed class ChronicleActor
    {
        readonly RawImage body,weapon,barrier;
        readonly RectTransform root;
        readonly int weaponId;
        readonly string bodySheet;
        readonly float size;
        readonly Vector2 feet;
        string action="";
        float actionAt,attackX,returnX,returnAt=-10,flashUntil;
        Color flashColor=Color.white;
        public RawImage Body { get { return body; } }
        public string BodySheet { get { return bodySheet; } }
        public Vector2 Feet { get { return feet; } }
        public Vector2 Position { get { return new Vector2(root.anchoredPosition.x,-root.anchoredPosition.y); } }
        public ChronicleActor(string name,Transform parent,int hero,CraftedItem equippedWeapon,CraftedItem equippedArmor,Vector2 position,float height)
        {
            size=height;feet=position;attackX=position.x;weaponId=equippedWeapon==null?-1:equippedWeapon.RecipeId;
            bodySheet=ChronicleWardrobe.Sheet(hero,equippedArmor==null?-1:equippedArmor.RecipeId);
            root=ForgeUI.Rect(name,parent,position.x,position.y,1,1);
            barrier=ForgeUI.Light(name+"Barrier",root,-height*.46f,-height*.94f,height*.92f,height*.92f);barrier.color=Color.clear;
            body=HandDrawnArt.Person(name+"Body",root,bodySheet,0,Vector2.zero,height,height);
            if(equippedWeapon!=null)
            {
                var kind=GameContent.Recipes[weaponId].Kind;
                float factor=kind==WeaponKind.Greatsword?.76f:kind==WeaponKind.Spear?.84f:kind==WeaponKind.Staff?.72f:kind==WeaponKind.Daggers?.38f:.56f;
                weapon=HandDrawnArt.Icon(name+"Weapon",root,equippedWeapon.RecipeId,0,0,height*factor,height*factor);
            }
            Pose("ready",0,true);
        }
        public void SetStrikeTarget(float x){attackX=x;}
        public void Flash(Color color,float until){flashColor=color;flashUntil=until;}
        public void ShowBarrier(double amount,double maximum){barrier.color=new Color(.45f,.78f,1,.22f*Mathf.Sqrt(Mathf.Clamp01((float)(amount/System.Math.Max(1,maximum)))));}
        public void Pose(string next,float time,bool reduced,float walkDistance=0,float anticipation=.4f)
        {
            if(action!=next)
            {
                if(action=="attack"||action=="windup"){returnX=root.anchoredPosition.x;returnAt=time;}
                action=next;actionAt=time;
            }
            float elapsed=Mathf.Max(0,time-actionAt);int pose=0;
            bool ranged=weaponId>=0&&(GameContent.Recipes[weaponId].Kind==WeaponKind.Bow||GameContent.Recipes[weaponId].Kind==WeaponKind.Staff);
            float x=feet.x;
            if(next=="windup")
            {
                pose=reduced?2:elapsed<.12f?1:2;
                if(!ranged&&!reduced&&attackX>feet.x)
                {
                    float step=Mathf.Clamp(anticipation*.65f,.26f,.55f),travel=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/step));x=Mathf.Lerp(feet.x,attackX,travel);
                    if(elapsed<step)pose=14+Mathf.Min(5,(int)(elapsed/(step/6)));
                }
            }
            else if(next=="attack"){pose=reduced?3:elapsed<.13f?3:elapsed<.26f?4:5;if(!ranged&&!reduced)x=attackX;}
            else if(next=="brace")pose=6;else if(next=="parry")pose=7;else if(next=="hurt")pose=8;else if(next=="stagger")pose=9;
            else if(next=="cast")pose=10;else if(next=="heal")pose=11;else if(next=="victory")pose=12;else if(next=="defeat")pose=13;
            else if(next=="walk")pose=14+(int)(walkDistance/8)%6;
            if(next!="windup"&&next!="attack"&&!reduced&&time-returnAt<.42f&&returnX>feet.x+1)
            {x=Mathf.Lerp(returnX,feet.x,Mathf.SmoothStep(0,1,(time-returnAt)/.42f));if(next=="ready")pose=19-Mathf.Min(5,(int)((time-returnAt)/.07f));}
            HandDrawnArt.Pose(body,bodySheet,pose,Vector2.zero,size,size);
            body.color=time<flashUntil?Color.Lerp(Color.white,flashColor,.42f):Color.white;
            float nudge=reduced?0:next=="attack"?Mathf.Sin(Mathf.Clamp01(elapsed/.3f)*Mathf.PI)*8:next=="hurt"?-6*Mathf.Exp(-elapsed*9):next=="walk"?Mathf.Min(0,-48+walkDistance):0;
            root.anchoredPosition=new Vector2(x+nudge,-feet.y);
            var rect=body.rectTransform;float w=rect.sizeDelta.x,h=rect.sizeDelta.y;
            if(weapon)
            {
                bool casting=pose==10||pose==11,down=pose==13;
                float handX=pose==3?.88f:pose==4?.89f:pose==2?.48f:pose==6?.55f:pose==7?.73f:.69f;
                float handY=pose==3?.36f:pose==4?.38f:pose==2?.15f:pose==6?.36f:pose==7?.32f:.58f;
                var kind=GameContent.Recipes[weaponId].Kind;
                weapon.rectTransform.pivot=kind==WeaponKind.Bow?new Vector2(.5f,.5f):new Vector2(.18f,.12f);
                weapon.rectTransform.anchoredPosition=new Vector2((handX-rect.pivot.x)*w,((1-handY)-rect.pivot.y)*h);
                float angle=pose==2?55:pose==3||pose==4?-43:pose==6?-10:pose==7?10:-88;
                if(kind==WeaponKind.Bow)angle=pose==3?-15:-55;
                if(kind==WeaponKind.Staff)angle=pose==10?-15:-10;
                weapon.rectTransform.localEulerAngles=new Vector3(0,0,angle);
                weapon.gameObject.SetActive(!casting&&!down||kind==WeaponKind.Staff&&!down);
            }
        }
    }
}
