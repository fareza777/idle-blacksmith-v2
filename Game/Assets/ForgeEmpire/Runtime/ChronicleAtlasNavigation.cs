using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeEmpire
{
    // Atlas movement is presentation only. Expedition position always comes from the saved run.
    public sealed class ChronicleAtlasNavigation : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        RectTransform viewport,content;
        ScrollRect page;
        float zoom=1,pinchDistance;
        bool restorePage;
        public float Zoom { get { return zoom; } }
        public Vector2 Pan { get { return content?content.anchoredPosition:Vector2.zero; } }
        public void Configure(RectTransform view,RectTransform painting,ScrollRect outer)
        {
            viewport=view;content=painting;page=outer;SetZoom(1);
        }
        public void SetZoom(float value)
        {
            if(!content)return;
            float next=Mathf.Clamp(value,1,2.4f),ratio=next/zoom;
            Vector2 centre=new Vector2(viewport.rect.width*.5f,-viewport.rect.height*.5f);
            content.anchoredPosition=centre+(content.anchoredPosition-centre)*ratio;
            zoom=next;content.localScale=Vector3.one*zoom;ClampPan();
        }
        public void ZoomBy(float amount){SetZoom(zoom+amount);}
        public void PanBy(float x,float y)
        {
            if(!content)return;content.anchoredPosition+=new Vector2(x,y);ClampPan();
        }
        public void Focus(Vector2 point)
        {
            content.anchoredPosition=new Vector2(viewport.rect.width*.5f-point.x*content.rect.width*zoom,point.y*content.rect.height*zoom-viewport.rect.height*.5f);ClampPan();
        }
        void ClampPan()
        {
            var p=content.anchoredPosition;p.x=Mathf.Clamp(p.x,Mathf.Min(0,viewport.rect.width-content.rect.width*zoom),0);
            p.y=Mathf.Clamp(p.y,0,Mathf.Max(0,content.rect.height*zoom-viewport.rect.height));content.anchoredPosition=p;
        }
        public void OnBeginDrag(PointerEventData data)
        {
            if(page){restorePage=page.enabled;page.StopMovement();page.enabled=false;}
        }
        public void OnDrag(PointerEventData data)
        {
            Vector2 now,before;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,data.position,data.pressEventCamera,out now)&&RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport,data.position-data.delta,data.pressEventCamera,out before))PanBy(now.x-before.x,now.y-before.y);
        }
        public void OnEndDrag(PointerEventData data){RestorePage();}
        public void OnScroll(PointerEventData data){ZoomBy(data.scrollDelta.y*.12f);}
        void RestorePage(){if(page&&restorePage)page.enabled=true;restorePage=false;}
        void OnDisable(){RestorePage();pinchDistance=0;}
        void Update()
        {
            if(!viewport||Input.touchCount!=2){pinchDistance=0;return;}
            var first=Input.GetTouch(0);var second=Input.GetTouch(1);
            if(!RectTransformUtility.RectangleContainsScreenPoint(viewport,first.position)||!RectTransformUtility.RectangleContainsScreenPoint(viewport,second.position)){pinchDistance=0;return;}
            float distance=Vector2.Distance(first.position,second.position);
            if(pinchDistance>1)SetZoom(zoom*distance/pinchDistance);pinchDistance=distance;
        }
    }
}
