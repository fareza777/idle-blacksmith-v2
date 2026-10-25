using UnityEngine;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Art poses and equipment layers share one grounded origin. Gameplay owns all timing.
    public sealed class ChronicleActor
    {
        static readonly string[] Sheets={"rowan","lyra","kael","mira","toren","edda"};
        readonly RawImage body,weapon,armor,barrier;
        readonly RectTransform root;
        readonly int heroId,weaponId;
        readonly float size;
        readonly Vector2 feet;
        string action="";
        float actionAt;
        public RawImage Body { get { return body; } }
        public ChronicleActor(string name,Transform parent,int hero,CraftedItem equippedWeapon,CraftedItem equippedArmor,Vector2 position,float height)
        {
            heroId=hero;size=height;feet=position;weaponId=equippedWeapon==null?-1:equippedWeapon.RecipeId;
            root=ForgeUI.Rect(name,parent,position.x,position.y,1,1);
            barrier=ForgeUI.Light(name+"Barrier",root,-height*.46f,-height*.94f,height*.92f,height*.92f);barrier.color=Color.clear;
            body=HandDrawnArt.Person(name+"Body",root,"hero-"+Sheets[hero],0,Vector2.zero,height,height);
            if(equippedArmor!=null)armor=HandDrawnArt.Icon(name+"Armor",root,equippedArmor.RecipeId,0,0,height*.36f,height*.4f);
            if(equippedWeapon!=null)weapon=HandDrawnArt.Icon(name+"Weapon",root,equippedWeapon.RecipeId,0,0,height*.62f,height*.62f);
            Pose("ready",0,true);
        }
        public void ShowBarrier(double amount,double maximum){barrier.color=new Color(.45f,.78f,1,.22f*Mathf.Sqrt(Mathf.Clamp01((float)(amount/System.Math.Max(1,maximum)))));}
        public void Pose(string next,float time,bool reduced,float walkDistance=0)
        {
            if(action!=next){action=next;actionAt=time;}float elapsed=time-actionAt;int pose=0;
            if(next=="attack")pose=reduced?3:elapsed<.04f?1:elapsed<.10f?2:elapsed<.16f?3:elapsed<.22f?4:5;
            else if(next=="brace")pose=6;else if(next=="parry")pose=7;else if(next=="hurt")pose=8;else if(next=="stagger")pose=9;
            else if(next=="cast")pose=10;else if(next=="heal")pose=11;else if(next=="victory")pose=12;else if(next=="defeat")pose=13;
            else if(next=="walk")pose=14+(int)(walkDistance/8)%6;
            HandDrawnArt.Pose(body,"hero-"+Sheets[heroId],pose,Vector2.zero,size,size);
            float nudge=reduced?0:next=="attack"?Mathf.Sin(Mathf.Clamp01(elapsed/.3f)*Mathf.PI)*12:next=="hurt"?-6*Mathf.Exp(-elapsed*9):next=="walk"?Mathf.Min(0,-48+walkDistance):0;
            root.anchoredPosition=new Vector2(feet.x+nudge,-feet.y);
            var rect=body.rectTransform;float w=rect.sizeDelta.x,h=rect.sizeDelta.y;
            float torsoX=pose==3||pose==4?.46f:pose==8||pose==9?.46f:.48f,torsoY=pose==13?.5f:.39f;
            if(armor)
            {
                armor.gameObject.SetActive(pose!=13);
                armor.rectTransform.anchoredPosition=new Vector2((torsoX-rect.pivot.x)*w,((1-torsoY)-rect.pivot.y)*h);
                armor.rectTransform.localEulerAngles=new Vector3(0,0,pose==8||pose==9?16:pose==3||pose==4?-8:0);
                armor.color=new Color(1,1,1,.96f);
            }
            if(weapon)
            {
                bool casting=pose==10||pose==11,down=pose==13;
                float handX=pose==3?.88f:pose==4?.89f:pose==2?.48f:pose==6?.55f:pose==7?.73f:.69f;
                float handY=pose==3?.36f:pose==4?.38f:pose==2?.15f:pose==6?.36f:pose==7?.32f:.58f;
                weapon.rectTransform.pivot=new Vector2(.18f,.12f);
                weapon.rectTransform.anchoredPosition=new Vector2((handX-rect.pivot.x)*w,((1-handY)-rect.pivot.y)*h);
                var kind=GameContent.Recipes[weaponId].Kind;
                float angle=pose==2?55:pose==3||pose==4?-43:pose==6?-10:pose==7?10:-88;
                if(kind==WeaponKind.Bow)angle=pose==3?-15:-55;
                if(kind==WeaponKind.Staff)angle=pose==10?-15:-10;
                weapon.rectTransform.localEulerAngles=new Vector3(0,0,angle);
                weapon.gameObject.SetActive(!casting&&!down||kind==WeaponKind.Staff&&!down);
            }
        }
    }
}
