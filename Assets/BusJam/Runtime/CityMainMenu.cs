using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Rollic.BusJam
{
    public sealed class CityMainMenu : MonoBehaviour
    {
        readonly Color navy=new Color(.025f,.20f,.39f),blue=new Color(.06f,.43f,.79f),cyan=new Color(.49f,.92f,1),gold=new Color(1,.78f,.14f);
        readonly List<Texture2D> textures=new List<Texture2D>();readonly List<Sprite> sprites=new List<Sprite>();
        readonly Dictionary<string,Sprite> spriteCache=new Dictionary<string,Sprite>();
        Image clockShade;Text livesValue,livesTimer,rechargePopupTimer,rechargePopupCount;float nextLivesRefresh;
        Font font;RectTransform layout,page;GameObject homeLogo,playButton,homeHint,pageDim,rechargePopup;MenuCityView city;
        Action play,restart;Action<int> chooseLevel;int current,total,completed;float seconds;bool resumed;string levelTitle;float safeTop,safeBottom;int selectedTab=2;
        readonly List<Image> tabFaces=new List<Image>();
        readonly List<Text> tabCaptions=new List<Text>();
        readonly List<Vector2> nativeTabLines=new List<Vector2>();bool? navigationEnglish;
        public void Initialize(int level,int count,int done,string title,float time,bool hasProgress,Action onPlay,Action onRestart,Action<int> onLevel)
        {
            current=level;total=count;completed=done;seconds=time;resumed=hasProgress;levelTitle=title;play=onPlay;restart=onRestart;chooseLevel=onLevel;font=Resources.Load<Font>("Fonts/Nunito-Black")??Resources.Load<Font>("Fonts/Nunito-ExtraBold")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var backdrop=GetComponent<Image>();backdrop.color=new Color(.09f,.72f,.91f);
            layout=Rect(transform,"Mobile menu",new Vector2(.5f,0),new Vector2(.5f,1),Vector2.zero,new Vector2(900,0));
            Canvas.ForceUpdateCanvases();safeTop=(Screen.height-Screen.safeArea.yMax)/Mathf.Max(1,Screen.height)*layout.rect.height;safeBottom=Screen.safeArea.yMin/Mathf.Max(1,Screen.height)*layout.rect.height;
            city=MenuCityView.Acquire(layout);
            Header();Logo();HomeAction();Navigation();
        }
        RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max,Vector2 pos,Vector2 size)
        {
            var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        Sprite Surface(Color top,Color bottom,bool circle=false)
        {
            string key=ColorUtility.ToHtmlStringRGBA(top)+ColorUtility.ToHtmlStringRGBA(bottom)+(circle?"C":"R");if(spriteCache.TryGetValue(key,out var saved))return saved;
            const int n=128;var texture=new Texture2D(n,n,TextureFormat.RGBA32,false){name="Menu gradient",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var pixels=new Color[n*n];float radius=circle?63f:24f;
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                Vector2 q=new Vector2(Mathf.Abs(x-63.5f),Mathf.Abs(y-63.5f))-Vector2.one*(64-radius);float distance=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-radius;
                Color c=Color.Lerp(bottom,top,y/127f);c.a*=Mathf.Clamp01(.7f-distance);pixels[y*n+x]=c;
            }
            texture.SetPixels(pixels);texture.Apply();textures.Add(texture);var sprite=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,circle?Vector4.zero:Vector4.one*26);sprites.Add(sprite);spriteCache[key]=sprite;return sprite;
        }
        Image Plate(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,Color top,Color bottom,bool circle=false)
        {
            var r=Rect(parent,name,anchor,anchor,position,size);var image=r.gameObject.AddComponent<Image>();image.sprite=Surface(top,bottom,circle);image.type=circle?Image.Type.Simple:Image.Type.Sliced;image.raycastTarget=false;return image;
        }
        Text Text(Transform parent,string value,int size,Vector2 pos,Vector2 area,Color? color=null,bool outline=true)
        {
            if(parent==page&&(value=="NASIL OYNANIR?"||value=="GARAJ"||value=="YOLCULUĞUN"||value=="AYARLAR")){value="";}
            var r=Rect(parent,value,new Vector2(.5f,1),new Vector2(.5f,1),pos,area);var t=r.gameObject.AddComponent<Text>();t.font=font;t.text=GamePreferences.Translate(value);t.fontSize=size;t.fontStyle=FontStyle.Normal;t.alignment=TextAnchor.MiddleCenter;t.color=color??Color.white;t.raycastTarget=false;
            if(outline){var o=r.gameObject.AddComponent<Outline>();o.effectColor=new Color(.015f,.16f,.28f,.95f);o.effectDistance=new Vector2(1,-1);}
            return t;
        }
        MenuIconGraphic Icon(Transform parent,MenuIconGraphic.Symbol symbol,Vector2 anchor,Vector2 pos,float size,Color? tint=null)
        {
            var r=Rect(parent,"Icon "+symbol,anchor,anchor,pos,Vector2.one*size);if(!r.GetComponent<CanvasRenderer>())r.gameObject.AddComponent<CanvasRenderer>();var g=r.gameObject.AddComponent<MenuIconGraphic>();g.symbol=symbol;g.color=tint??Color.white;g.raycastTarget=false;var shadow=r.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.025f,.20f,.37f,.75f);shadow.effectDistance=new Vector2(2,-3);return g;
        }
        Sprite IllustratedButtonSprite(int style)
        {
            string key="Illustrated button "+style;if(spriteCache.TryGetValue(key,out var cached))return cached;
            var texture=Resources.Load<Texture2D>("MenuArt/MenuPopupButtons");if(!texture)return null;
            int[] tops={45,370,694},bottoms={364,692,1012};
            var rect=new Rect(texture.width*56f/1536,texture.height*(1024f-bottoms[style])/1024,texture.width*1420f/1536,texture.height*(bottoms[style]-tops[style])/1024);
            var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),200,0,SpriteMeshType.FullRect,new Vector4(140,40,140,40));sprites.Add(sprite);spriteCache[key]=sprite;return sprite;
        }
        void PopupGlyph(Transform parent,int icon,Vector2 position,float size)
        {
            Artwork(parent,"Popup category icon","MenuPopupIcons",new Vector2(.5f,1),position,Vector2.one*size,new Rect((icon%2)*.5f,icon<2?.5f:0,.5f,.5f));
        }
        IEnumerator OpenPopup(RectTransform root)
        {
            var fade=root.gameObject.AddComponent<CanvasGroup>();float elapsed=0;root.localScale=Vector3.one*.85f;fade.alpha=0;
            while(root&&elapsed<.25f){elapsed+=Mathf.Min(Time.unscaledDeltaTime,.05f);float t=Mathf.Clamp01(elapsed/.25f),back=t-1;float ease=1+2.70158f*back*back*back+1.70158f*back*back;root.localScale=Vector3.one*Mathf.LerpUnclamped(.85f,1,ease);fade.alpha=Mathf.Clamp01(t*1.5f);yield return null;}
            if(root){root.localScale=Vector3.one;fade.alpha=1;}
        }
        Button Button(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color top,Color bottom,Action action,int textSize=34)
        {
            if(size.x>=400&&Resources.Load<Texture2D>("MenuArt/MenuPopupButtons"))
            {
                int style=top.r>.85f&&top.b<.5f?2:top.g>.8f&&top.b<.35f?1:0;
                var r=Rect(parent,name,anchor,anchor,pos,size);var image=r.gameObject.AddComponent<Image>();image.sprite=IllustratedButtonSprite(style);image.type=Image.Type.Simple;image.raycastTarget=true;
                var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(()=>action?.Invoke());
                if(!string.IsNullOrEmpty(name))
                {
                    var caption=Text(r,name,textSize,new Vector2(0,-size.y*.5f+size.y*.015f),size-new Vector2(70,12),new Color(1,.99f,.93f));
                    caption.alignment=TextAnchor.MiddleCenter;caption.alignByGeometry=true;
                    if(name=="ANA SAYFAYA DÖN")
                    {
                        var labelRect=caption.rectTransform;labelRect.anchorMin=labelRect.anchorMax=labelRect.pivot=new Vector2(.5f,.5f);
                        labelRect.sizeDelta=size-new Vector2(70,12);labelRect.anchoredPosition=new Vector2(0,size.y*.04f);
                    }
                }
                return b;
            }
            Plate(parent,name+" shadow",anchor,pos+Vector2.down*9,size,new Color(.02f,.15f,.35f,.40f),new Color(.02f,.15f,.35f,.40f));
            var outer=Plate(parent,name,anchor,pos,size,new Color(1,.88f,.30f),new Color(.86f,.49f,.035f));outer.raycastTarget=true;var button=outer.gameObject.AddComponent<Button>();button.targetGraphic=outer;button.onClick.AddListener(()=>action?.Invoke());
            Plate(outer.transform,"Cyan rim",new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*6,new Color(1,.98f,.85f),new Color(1,.80f,.38f));
            Plate(outer.transform,"Color face",new Vector2(.5f,.5f),Vector2.zero,size-Vector2.one*16,top,bottom);
            if(!string.IsNullOrEmpty(name)){var caption=Text(outer.transform,name,textSize,new Vector2(0,-size.y*.5f),size-Vector2.one*26);caption.alignByGeometry=true;}
            return button;
        }
        Button CircleButton(Transform parent,string name,Vector2 pos,MenuIconGraphic.Symbol symbol,Action action)
        {
            var border=Plate(parent,name,new Vector2(.5f,1),pos,Vector2.one*108,navy,navy,true);border.raycastTarget=true;var b=border.gameObject.AddComponent<Button>();b.targetGraphic=border;b.onClick.AddListener(()=>action());Plate(border.transform,"Silver rim",new Vector2(.5f,.5f),Vector2.zero,Vector2.one*101,new Color(.63f,.94f,1),new Color(.12f,.44f,.93f),true);Plate(border.transform,"Blue badge",new Vector2(.5f,.5f),Vector2.zero,Vector2.one*87,new Color(.25f,.65f,1),new Color(.13f,.24f,.76f),true);Icon(border.transform,symbol,new Vector2(.5f,.5f),Vector2.zero,67);return b;
        }
        RectTransform Artwork(Transform parent,string name,string resource,Vector2 anchor,Vector2 pos,Vector2 size,Rect uv)
        {
            var r=Rect(parent,name,anchor,anchor,pos,size);var image=r.gameObject.AddComponent<RawImage>();
            image.texture=Resources.Load<Texture2D>("MenuArt/"+resource);image.uvRect=uv;image.raycastTarget=false;return r;
        }
        Button Hit(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Action action)
        {
            var r=Rect(parent,name,anchor,anchor,pos,size);var image=r.gameObject.AddComponent<Image>();image.color=Color.clear;image.raycastTarget=true;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.None;button.onClick.AddListener(()=>action?.Invoke());return button;
        }
        void Header()
        {
            var hud=Artwork(layout,"Illustrated HUD","TopHUD",new Vector2(.5f,1),new Vector2(0,-103-safeTop),new Vector2(840,141),new Rect(8f/2172,181f/724,2156f/2172,362f/724));
            Hit(hud,"Profil",new Vector2(.5f,.5f),new Vector2(-354,0),new Vector2(124,141),()=>ShowPage(2));
            Hit(hud,"HAK SÜRESİ",new Vector2(.5f,.5f),new Vector2(354,0),new Vector2(124,141),ShowLivesRecharge);
            clockShade=Plate(hud,"Recharge clock dim",new Vector2(.5f,.5f),new Vector2(354,0),new Vector2(128,134),Color.white,Color.white,true);
            var levelCaptionBack=Plate(hud,"Level caption background",new Vector2(.5f,1),new Vector2(-105,-29),new Vector2(155,38),new Color(.025f,.31f,.76f),new Color(.025f,.31f,.76f));
            levelCaptionBack.gameObject.AddComponent<EnglishOnlyLabel>();
            var levelTitle=Text(levelCaptionBack.transform,"BÖLÜM",24,new Vector2(0,-19),new Vector2(155,38),new Color(1,.94f,.76f));levelTitle.alignByGeometry=true;
            levelTitle.verticalOverflow=VerticalWrapMode.Truncate;
            var levelTitleOutline=levelTitle.GetComponent<Outline>();levelTitleOutline.effectColor=new Color(.24f,.13f,.055f,.95f);levelTitleOutline.effectDistance=new Vector2(1,-2);
            var levelTitleShadow=levelTitle.gameObject.AddComponent<Shadow>();levelTitleShadow.effectColor=new Color(.10f,.05f,.015f,.65f);levelTitleShadow.effectDistance=new Vector2(1,-3);
            var levelValue=Text(hud,(current+1)+" / "+total,32,new Vector2(-105,-70.5f),new Vector2(170,50));levelValue.alignByGeometry=true;
            var heartTitle=Text(hud,"KALAN HAK",24,new Vector2(95,-29),new Vector2(200,38),new Color(1,.94f,.76f));heartTitle.alignByGeometry=true;
            var titleOutline=heartTitle.GetComponent<Outline>();titleOutline.effectColor=new Color(.24f,.13f,.055f,.95f);titleOutline.effectDistance=new Vector2(1,-2);
            var titleShadow=heartTitle.gameObject.AddComponent<Shadow>();titleShadow.effectColor=new Color(.10f,.05f,.015f,.65f);titleShadow.effectDistance=new Vector2(1,-3);
            livesValue=Text(hud,LivesService.GetState().count.ToString(),32,new Vector2(95,-70.5f),new Vector2(170,50));livesValue.alignByGeometry=true;
            var timerPlate=Plate(hud,"Clock countdown inset",new Vector2(.5f,1),new Vector2(354,-70.5f),new Vector2(124,38),new Color(.025f,.24f,.49f),new Color(.015f,.13f,.34f));
            livesTimer=Text(hud,"",22,new Vector2(354,-70.5f),new Vector2(118,34));livesTimer.alignByGeometry=true;UpdateLives();
        }
        void Update(){if(navigationEnglish!=GamePreferences.English&&tabFaces.Count==5)RefreshNavigationLanguage();var backdrop=GetComponent<Image>();if(backdrop)backdrop.color=GamePreferences.Dark?new Color(.025f,.055f,.14f):new Color(.09f,.72f,.91f);if(Time.unscaledTime>=nextLivesRefresh){nextLivesRefresh=Time.unscaledTime+.5f;UpdateLives();}}
        void UpdateLives()
        {
            if(!livesValue)return;var state=LivesService.GetState();livesValue.text=state.count.ToString();
            livesTimer.text=state.count<LivesService.Maximum?LivesService.Remaining(state,LivesService.UtcNow):GamePreferences.Translate("DOLU");
            if(clockShade)clockShade.color=state.count<LivesService.Maximum?new Color(.015f,.035f,.08f,.50f):Color.clear;
            if(rechargePopupTimer)rechargePopupTimer.text=state.count<LivesService.Maximum?LivesService.Remaining(state,LivesService.UtcNow):GamePreferences.Translate("HAKLARIN DOLU");
            if(rechargePopupCount)rechargePopupCount.text=GamePreferences.Translate("Kalan hak: "+state.count+" / "+LivesService.Maximum);
        }
        void ShowLivesRecharge()
        {
            if(rechargePopup)return;
            if(city)city.SetObscured(true);
            var shade=Rect(layout,"Life recharge popup",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            rechargePopup=shade.gameObject;var dim=shade.gameObject.AddComponent<Image>();dim.color=new Color(.015f,.035f,.10f,.78f);dim.raycastTarget=true;
            var card=Artwork(shade,"Recharge card","MenuCategoryPopup",new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,900),new Rect(0,0,1,1));
            Icon(card,MenuIconGraphic.Symbol.Clock,new Vector2(.5f,1),new Vector2(0,-126),130,gold);
            Text(card,"YENİ HAK",46,new Vector2(0,-335),new Vector2(620,100));
            Text(card,"Her 4 saatte 1 hak kazanırsın.\nEn fazla 10 hak birikebilir.",29,new Vector2(0,-450),new Vector2(620,110),new Color(.93f,.98f,1),false);
            rechargePopupCount=Text(card,"",29,new Vector2(0,-555),new Vector2(620,55));
            rechargePopupTimer=Text(card,"",42,new Vector2(0,-630),new Vector2(620,85));
            UpdateLives();
            Button(card,"TAMAM",new Vector2(.5f,1),new Vector2(0,-775),new Vector2(510,105),new Color(.58f,.94f,.16f),new Color(.20f,.69f,.05f),()=>
            {
                if(!rechargePopup)return;
                if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                rechargePopup.SetActive(false);Destroy(rechargePopup);rechargePopup=null;rechargePopupTimer=null;rechargePopupCount=null;if(city)city.SetObscured(selectedTab!=2);
            });
            StartCoroutine(OpenPopup(card));
        }
        void StatCard(float x,float y,MenuIconGraphic.Symbol symbol,string caption,string value)
        {
            var plate=Plate(layout,caption,new Vector2(.5f,1),new Vector2(x,y),new Vector2(255,92),navy,navy);Plate(plate.transform,"Aqua edge",new Vector2(.5f,.5f),Vector2.zero,new Vector2(249,86),cyan,new Color(.11f,.55f,.83f));Plate(plate.transform,"Inset blue",new Vector2(.5f,.5f),Vector2.zero,new Vector2(237,74),new Color(.05f,.49f,.73f),new Color(.025f,.31f,.55f));
            var badge=Plate(plate.transform,"Gold badge",new Vector2(0,.5f),new Vector2(25,0),Vector2.one*86,new Color(1,.91f,.26f),new Color(1,.56f,.025f),true);Icon(badge.transform,symbol,new Vector2(.5f,.5f),Vector2.zero,58,new Color(1,.94f,.47f));
            Text(plate.transform,caption,15,new Vector2(35,-24),new Vector2(167,26),new Color(.78f,.96f,1),false);Text(plate.transform,value,34,new Vector2(35,-58),new Vector2(166,44));
        }
        void Logo()
        {
            var r=Rect(layout,"Home logo",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-310-safeTop),new Vector2(800,280));homeLogo=r.gameObject;
            var texture=Resources.Load<Texture2D>("MenuArt/BusJamLogo");
            if(texture)
            {
                var image=Rect(r,"Illustrated Bus Jam logo",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-110),new Vector2(630,630f*texture.height/texture.width)).gameObject.AddComponent<RawImage>();
                image.texture=texture;image.raycastTarget=false;
            }
            else Text(r,"BUS JAM",82,new Vector2(0,-110),new Vector2(780,105));
        }
        void HomeAction()
        {
            var button=Button(layout,resumed?"DEVAM ET":"OYNA",new Vector2(.5f,0),new Vector2(0,310+safeBottom),new Vector2(500,125),new Color(.58f,.94f,.16f),new Color(.20f,.69f,.05f),play,42);
            button.name=resumed?"DEVAM ET":"BÖLÜM "+(current+1);playButton=button.gameObject;
            homeHint=Rect(layout,"Home hint placeholder",new Vector2(.5f,0),new Vector2(.5f,0),Vector2.zero,Vector2.zero).gameObject;
        }
        Sprite NavigationCaptionSurface()
        {
            const string key="Localized navigation blue";if(spriteCache.TryGetValue(key,out var cached))return cached;
            var texture=new Texture2D(1,32,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<32;y++){float t=y/31f;texture.SetPixel(0,y,t<.25f?Color.Lerp(new Color(2f/255,95f/255,220f/255),new Color(1f/255,77f/255,194f/255),t*4):Color.Lerp(new Color(1f/255,77f/255,194f/255),new Color(1f/255,80f/255,195f/255),(t-.25f)/.75f));}
            texture.Apply();textures.Add(texture);var sprite=Sprite.Create(texture,new Rect(0,0,1,32),new Vector2(.5f,.5f));sprites.Add(sprite);spriteCache[key]=sprite;return sprite;
        }
        void Navigation()
        {
            var panel=Artwork(layout,"Illustrated navigation","NavigationBar",new Vector2(.5f,0),new Vector2(0,97+safeBottom),new Vector2(870,186),new Rect(14f/2172,125f/724,2145f/2172,458f/724));
            string[] labels={"REHBER","GARAJ","ANA SAYFA","BÖLÜMLER","AYARLAR"};
            // Visible letter bounds in the navigation artwork, excluding its frame.
            float[] wordLeft={146,566,914,1336,1759},wordRight={395,787,1261,1653,2043};
            for(int i=0;i<5;i++)
            {
                int tab=i;float x=(i-2)*174;
                Hit(panel,labels[i],new Vector2(.5f,.5f),new Vector2(x,0),new Vector2(174,186),()=>ShowPage(tab));
                float artworkScale=870f/2145f;
                float underlineX=((wordLeft[i]+wordRight[i])*.5f-14)*artworkScale-435;
                float underlineWidth=(wordRight[i]-wordLeft[i]+1)*artworkScale;
                // Match the illustration's blue gradient, with opaque coverage for its baked Turkish letters.
                var captionBack=Rect(panel,"Navigation caption background",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(underlineX,44),new Vector2(underlineWidth+24,44));
                var captionFill=captionBack.gameObject.AddComponent<Image>();captionFill.sprite=NavigationCaptionSurface();captionFill.raycastTarget=false;
                captionBack.gameObject.AddComponent<EnglishOnlyLabel>();
                var caption=Text(captionBack.transform,labels[i],28,new Vector2(0,-22),new Vector2(164,44),new Color(1,.98f,.87f));
                caption.alignByGeometry=true;caption.verticalOverflow=VerticalWrapMode.Overflow;tabCaptions.Add(caption);
                var indicator=Plate(panel,"Selected tab "+i,new Vector2(.5f,0),new Vector2(underlineX,27),new Vector2(underlineWidth,7),gold,gold);
                indicator.color=i==2?gold:Color.clear;tabFaces.Add(indicator);nativeTabLines.Add(new Vector2(underlineX,underlineWidth));
            }
        }
        void RefreshNavigationLanguage()
        {
            navigationEnglish=GamePreferences.English;
            string[] labels={"REHBER","GARAJ","ANA SAYFA","BÖLÜMLER","AYARLAR"};
            for(int i=0;i<tabCaptions.Count;i++)
            {
                tabCaptions[i].text=GamePreferences.Translate(labels[i]);
                var rect=tabFaces[i].rectTransform;
                rect.anchoredPosition=new Vector2(nativeTabLines[i].x,27);
                rect.sizeDelta=new Vector2(GamePreferences.English?Mathf.Min(160,Mathf.Ceil(tabCaptions[i].preferredWidth)):nativeTabLines[i].y,7);
            }
        }
        public void ShowPage(int tab)
        {
            if(city)city.SetObscured(tab!=2);
            selectedTab=tab;if(page){page.gameObject.SetActive(false);Destroy(page.gameObject);}homeLogo.SetActive(tab==2);playButton.SetActive(tab==2);homeHint.SetActive(tab==2);
            for(int i=0;i<tabFaces.Count;i++)tabFaces[i].color=i==tab?gold:Color.clear;
            if(pageDim){pageDim.SetActive(false);Destroy(pageDim);pageDim=null;}
            if(tab==2)return;
            var dim=Rect(layout,"Menu page dim",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);pageDim=dim.gameObject;var dimImage=pageDim.AddComponent<Image>();dimImage.color=new Color(.015f,.035f,.10f,.55f);dimImage.raycastTarget=true;dim.SetSiblingIndex(1);
            page=Artwork(layout,"Menu page","MenuCategoryPopup",new Vector2(.5f,.5f),new Vector2(0,-50),new Vector2(790,940),new Rect(0,0,1,1));
            page.GetComponent<RawImage>().raycastTarget=true;
            PopupGlyph(page,tab==0?0:tab==1?1:tab==3?2:3,new Vector2(0,-131),145);
            StartCoroutine(OpenPopup(page));
            switch(tab){case 0:Help();break;case 1:Garage();break;case 3:Levels();break;case 4:Settings();break;}
        }
        void Help()
        {
            Text(page,"NASIL OYNANIR?",43,new Vector2(0,-70),new Vector2(720,90));
            string[] titles={"YOLU AÇ","RENGİNİ EŞLEŞTİR","OTOBÜSÜ DOLDUR"};string[] descriptions={"Yolu açık yolculara dokun.","Otobüsle aynı renkte olanlar biner.\nDiğerleri bekleme alanına geçer.","Süre bitmeden tüm yolcuları bindir."};
            const float titleHeight=55f,descriptionGap=8f,stepGap=32f;
            float stepTop=-294f;
            for(int i=0;i<3;i++)
            {
                Text(page,titles[i],31,new Vector2(0,stepTop-titleHeight*.5f),new Vector2(470,titleHeight));
                var description=Text(page,descriptions[i],25,Vector2.zero,new Vector2(470,94),new Color(.84f,.96f,1),false);
                description.alignment=TextAnchor.UpperCenter;
                float descriptionHeight=description.preferredHeight;
                description.rectTransform.sizeDelta=new Vector2(470,descriptionHeight);
                description.rectTransform.anchoredPosition=new Vector2(0,stepTop-titleHeight-descriptionGap-descriptionHeight*.5f);
                float stepHeight=titleHeight+descriptionGap+descriptionHeight;
                var badge=Plate(page,"Step "+(i+1),new Vector2(.5f,1),new Vector2(-280,stepTop-stepHeight*.5f),Vector2.one*64,gold,new Color(1,.57f,.04f),true);
                Text(badge.transform,(i+1).ToString(),34,new Vector2(0,-32),Vector2.one*60);
                stepTop-=stepHeight+stepGap;
            }
            Button(page,"OYUNA DEVAM ET",new Vector2(.5f,1),new Vector2(0,-810),new Vector2(570,90),new Color(.58f,.94f,.16f),new Color(.20f,.69f,.05f),play);
        }
        void Garage()
        {
            Text(page,"GARAJ",50,new Vector2(0,-70),new Vector2(720,90));Text(page,"Vitrin otobüsünün rengini seç",27,new Vector2(0,-290),new Vector2(700,60),new Color(.85f,.97f,1),false);
            string[] colors={"KIRMIZI","TURKUAZ","YEŞİL","SARI","MOR","PEMBE"};
            for(int i=0;i<6;i++)
            {
                int choice=i;float x=(i%3-1)*210,y=-435-(i/3)*220;var color=MenuCityView.Colors[i];var b=Button(page,"",new Vector2(.5f,1),new Vector2(x,y),new Vector2(180,160),Color.Lerp(color,Color.white,.20f),color,()=>{city.SetColor(choice);ShowPage(2);});b.name="Preview color "+i;Icon(b.transform,MenuIconGraphic.Symbol.Bus,new Vector2(.5f,.5f),new Vector2(0,15),100,Color.white);Text(b.transform,colors[i],22,new Vector2(0,-130),new Vector2(190,40));
            }
            Button(page,"ANA SAYFAYA DÖN",new Vector2(.5f,1),new Vector2(0,-815),new Vector2(570,90),new Color(.18f,.74f,1),blue,()=>ShowPage(2),29);
        }
        void Levels()
        {
            Text(page,"YOLCULUĞUN",44,new Vector2(0,-70),new Vector2(720,90));Text(page,completed+" / "+total+" bölüm tamamlandı",27,new Vector2(0,-293),new Vector2(710,55),new Color(.87f,.97f,1),false);
            var viewport=Rect(page,"Level viewport",new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-550),new Vector2(660,440));
            var touch=viewport.gameObject.AddComponent<Image>();touch.color=Color.clear;viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect(viewport,"Level content",new Vector2(.5f,1),new Vector2(.5f,1),Vector2.zero,new Vector2(720,Mathf.CeilToInt(total/3f)*220+20));content.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=35;
            for(int i=0;i<total;i++)
            {
                int index=i;float x=(i%3-1)*210,y=-110-(i/3)*220;bool unlocked=IsLevelUnlocked(i);bool active=i==current;
                var b=Button(content,unlocked?(i+1).ToString():"",new Vector2(.5f,1),new Vector2(x,y),new Vector2(180,170),active?new Color(.61f,.98f,.17f):unlocked?new Color(.24f,.76f,1):new Color(.29f,.51f,.67f),active?new Color(.20f,.69f,.04f):new Color(.04f,.38f,.68f),()=>SelectLevel(index),54);b.name="Level "+(i+1);b.interactable=unlocked;
                if(!unlocked)Icon(b.transform,MenuIconGraphic.Symbol.Lock,new Vector2(.5f,.5f),new Vector2(0,5),80,new Color(.71f,.87f,.94f));
                else if(i<completed)Icon(b.transform,MenuIconGraphic.Symbol.Star,new Vector2(.5f,0),new Vector2(0,32),38,gold);
            }
            Button(page,"ANA SAYFAYA DÖN",new Vector2(.5f,1),new Vector2(0,-810),new Vector2(570,85),new Color(.18f,.74f,1),blue,()=>ShowPage(2),29);
        }
        bool IsLevelUnlocked(int index)=>index>=0&&index<total;
        void SelectLevel(int index)
        {
            if(!IsLevelUnlocked(index))return;
            if(index==current){play?.Invoke();return;}
            if(resumed)Confirm("BÖLÜM "+(index+1),"Mevcut bölüm kaydının yerine\nseçtiğin bölüm başlatılacak.",()=>chooseLevel?.Invoke(index));else chooseLevel?.Invoke(index);
        }
        void Settings()
        {
            Text(page,"AYARLAR",48,new Vector2(0,-75),new Vector2(720,90));
            var top=new Color(.22f,.77f,1);var amber=new Color(1,.72f,.20f);
            void Option(string caption,int row,Action action,bool warning=false)
            {
                var button=Button(page,caption,new Vector2(.5f,1),new Vector2(0,-350-row*150),new Vector2(630,100),warning?amber:top,warning?new Color(.95f,.43f,.06f):blue,action,34);
                var label=button.GetComponentInChildren<Text>();
                label.fontSize=34;label.resizeTextForBestFit=false;label.alignment=TextAnchor.MiddleCenter;label.alignByGeometry=true;
                var rect=label.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
                rect.sizeDelta=new Vector2(560,88);rect.anchoredPosition=new Vector2(0,1.5f);
            }
            Option(GamePreferences.English?"DİL: İNGİLİZCE":"DİL: TÜRKÇE",0,()=>{GamePreferences.Set("Language",GamePreferences.English?0:1);ShowPage(4);});
            Option(GamePreferences.Sound?"SES: AÇIK":"SES: KAPALI",1,()=>{GamePreferences.Set("Sound",GamePreferences.Sound?0:1);ShowPage(4);});
            Option(GamePreferences.Dark?"TEMA: KARANLIK":"TEMA: AYDINLIK",2,()=>{GamePreferences.Set("Dark",GamePreferences.Dark?0:1);ShowPage(4);});
            Option("ANA SAYFAYA DÖN",3,()=>ShowPage(2));
        }
        void Confirm(string heading,string detail,Action action)
        {
            var shade=Plate(layout,"Confirm overlay",new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,2400),new Color(.015f,.10f,.25f,.78f),new Color(.015f,.10f,.25f,.78f));shade.raycastTarget=true;
            var card=Artwork(shade.transform,"Confirmation","MenuCategoryPopup",new Vector2(.5f,.5f),Vector2.zero,new Vector2(740,880),new Rect(0,0,1,1));var warning=Text(card,"!",84,new Vector2(0,-124),new Vector2(130,150),new Color(1,.83f,.12f));warning.alignByGeometry=true;
            var warningShadow=warning.gameObject.AddComponent<Shadow>();warningShadow.effectColor=new Color(.53f,.27f,.015f,.85f);warningShadow.effectDistance=new Vector2(2,-3);
            Text(card,heading,40,new Vector2(0,-285),new Vector2(620,90));Text(card,detail,29,new Vector2(0,-410),new Vector2(620,150),Color.white,false);StartCoroutine(OpenPopup(card));
            Button(card.transform,"BAŞLAT",new Vector2(.5f,1),new Vector2(0,-600),new Vector2(600,100),new Color(.60f,.98f,.16f),new Color(.20f,.69f,.04f),action);Button(card.transform,"VAZGEÇ",new Vector2(.5f,1),new Vector2(0,-745),new Vector2(600,85),new Color(.23f,.73f,1),blue,()=>{shade.gameObject.SetActive(false);Destroy(shade.gameObject);});
        }
        void OnDisable(){if(city)city.Release(layout);}
        void OnDestroy(){if(city)city.Release(layout);foreach(var sprite in sprites)if(sprite)Destroy(sprite);foreach(var texture in textures)if(texture)Destroy(texture);}
    }
}
