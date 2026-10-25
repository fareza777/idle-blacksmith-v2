using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForgeEmpire
{
    public sealed class ForgeButtonFeedback : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        Button button;
        RectTransform[] visuals;
        Vector2[] origins;
        bool pressed;
        float weight;
        Image focusLine;
        void Start()
        {
            button=GetComponent<Button>();visuals=new RectTransform[transform.childCount];origins=new Vector2[visuals.Length];
            for(int i=0;i<visuals.Length;i++){visuals[i]=transform.GetChild(i) as RectTransform;origins[i]=visuals[i].anchoredPosition;}
        }
        void Update()
        {
            float before=weight;
            float target=pressed&&button&&button.IsInteractable()&&!ForgeUI.ReducedMotion?1:0;
            weight=Mathf.MoveTowards(weight,target,Time.unscaledDeltaTime*(target>0?20:12));
            if(visuals==null)return;
            for(int i=0;i<visuals.Length;i++)if(visuals[i])
            {
                visuals[i].localScale=Vector3.one*(1-.018f*weight);
                if(weight>0)visuals[i].anchoredPosition=origins[i]+Vector2.down*(2*weight);
                else if(before>0)visuals[i].anchoredPosition=origins[i];
            }
        }
        public void OnPointerDown(PointerEventData data){pressed=true;if(focusLine)focusLine.gameObject.SetActive(false);}
        public void OnPointerUp(PointerEventData data){pressed=false;}
        public void OnPointerExit(PointerEventData data){pressed=false;}
        public void OnSelect(BaseEventData data)
        {
            if(data is PointerEventData||Application.isMobilePlatform)return;
            if(!focusLine)
            {
                var rect=(RectTransform)transform;
                focusLine=ForgeUI.Fill("KeyboardFocus",transform,12,rect.rect.height-6,Mathf.Max(12,rect.rect.width-24),3,ForgeUI.Gold);
            }
            focusLine.gameObject.SetActive(true);
        }
        public void OnDeselect(BaseEventData data){if(focusLine)focusLine.gameObject.SetActive(false);}
        void OnDisable(){pressed=false;weight=0;if(focusLine)focusLine.gameObject.SetActive(false);}
    }
}
