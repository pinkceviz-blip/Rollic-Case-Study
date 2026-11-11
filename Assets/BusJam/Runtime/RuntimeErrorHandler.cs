using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace Rollic.BusJam
{
    // Unity log callbacks may run off the main thread. Only queue a flag there.
    public sealed class RuntimeErrorHandler : MonoBehaviour
    {
        int pending;
        GameObject popup;
        BusJamGame game;
        public bool IsShowing=>popup;
        void OnEnable(){game=GetComponent<BusJamGame>();Application.logMessageReceivedThreaded+=OnLog;}
        void OnDisable(){Application.logMessageReceivedThreaded-=OnLog;Interlocked.Exchange(ref pending,0);if(popup)Destroy(popup);}
        void OnLog(string message,string stack,LogType type)
        {
#if UNITY_EDITOR
            // Editor search failures are outside the running game.
            if(stack!=null&&stack.Contains("UnityEditor.Search.")&&!stack.Contains("Rollic.BusJam"))return;
#endif
            if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)Interlocked.Exchange(ref pending,1);
        }
        void Update()
        {
            if(Interlocked.Exchange(ref pending,0)==0||popup)return;
            // Mark the overlay before constructing it, so another log cannot stack dialogs.
            popup=new GameObject("Runtime error popup",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            try{if(game)game.SuspendForError();Build();}
            catch(Exception e){Debug.LogException(e);}
        }
        RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max,Vector2 pos,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        void Build()
        {
            var canvas=popup.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
            var scaler=popup.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(900,1600);scaler.matchWidthOrHeight=0;
            var events=EventSystem.current;if(!events)events=new GameObject("Error popup input",typeof(EventSystem)).GetComponent<EventSystem>();
            var input=events.GetComponent<InputSystemUIInputModule>();if(!input)input=events.gameObject.AddComponent<InputSystemUIInputModule>();if(!input.actionsAsset)input.AssignDefaultActions();
            var shade=Rect(popup.transform,"Error dim",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<Image>();shade.color=new Color(.025f,.045f,.12f,.80f);
            var texture=Resources.Load<Texture2D>("MenuArt/MenuCategoryPopup");
            var card=Rect(shade.transform,"Error card",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(720,900));
            var art=card.gameObject.AddComponent<RawImage>();art.texture=texture;art.raycastTarget=false;
            Font font=Resources.Load<Font>("Fonts/Nunito-Black")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            void Text(string name,int size,float y,Vector2 area,Color color)
            {
                var r=Rect(card,name,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-y),area);var t=r.gameObject.AddComponent<UnityEngine.UI.Text>();t.text=name;t.font=font;t.fontSize=size;t.alignment=TextAnchor.MiddleCenter;t.alignByGeometry=true;t.color=color;t.raycastTarget=false;
                var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.025f,.12f,.26f,.9f);outline.effectDistance=new Vector2(1,-1);
            }
            Text("!",90,126,new Vector2(170,180),new Color(1,.82f,.12f));
            Text("HATA OLUŞTU",46,325,new Vector2(620,95),Color.white);
            Text("Beklenmeyen bir sorun oluştu.\nLütfen yeniden dene.",28,440,new Vector2(610,120),new Color(.93f,.98f,1));
            void Action(string label,float y,int style,bool restart)
            {
                var r=Rect(card,label,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-y),new Vector2(510,105));
                var image=r.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("MenuArt/MenuPopupButtons");image.uvRect=style==2?new Rect(56f/1536,12f/1024,1420f/1536,318f/1024):new Rect(56f/1536,660f/1024,1420f/1536,319f/1024);
                var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>Recover(restart));
                Text(label,29,y-2,new Vector2(470,80),Color.white);
            }
            Action("YENİDEN BAŞLAT",600,2,true);Action("ANA MENÜ",745,0,false);
            StartCoroutine(Appear(card));
        }
        IEnumerator Appear(RectTransform card)
        {
            var group=card.gameObject.AddComponent<CanvasGroup>();float elapsed=0;
            while(card&&elapsed<.3f){elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/.3f),b=t-1;card.localScale=Vector3.one*Mathf.LerpUnclamped(.8f,1,1+2.70158f*b*b*b+1.70158f*b*b);group.alpha=Mathf.Clamp01(t*1.5f);yield return null;}
            if(card){card.localScale=Vector3.one;group.alpha=1;}
        }
        void Recover(bool restart)
        {
            try
            {
                if(!game)return;
                game.RecoverFromError(restart);
                Interlocked.Exchange(ref pending,0);popup.SetActive(false);Destroy(popup);popup=null;
            }
            catch(Exception e){Debug.LogException(e);}
        }
    }
}
