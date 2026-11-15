using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ParasiteEvolution
{
    public sealed class GameHud : MonoBehaviour
    {
        GameSession session;
        RectTransform safe;
        Font font;
        VirtualJoystick joystick;
        Text stats,quest,toast,modalTitle,modalQuest;
        Text[] abilityText=new Text[3],upgradeText=new Text[6],unlockText=new Text[2];
        Button[] abilityButtons=new Button[3];
        GameObject modal,shop,pause;
        Rect lastSafe;Vector2Int lastScreen;
        float refresh;
        readonly string[] abilityIds={"AB-PULL","AB-MOVE","AB-DAMAGE"};
        readonly string[] abilityNames={"Хватка","Ривок","Сильний удар"};
        readonly string[] upgradeNames={"Швидкість захоплення","Місткість","Швидкість руху","Радіус захоплення","Канали захоплення","Здоров'я"};
        readonly string[] questNames={"Віднови сили: захопи 4 цілі","Двічі здай зразки на базі","Придбай покращення","Досліди три зони","Зупини Наглядача"};
        public Vector2 Move => joystick!=null?joystick.Value:Vector2.zero;

        public void Initialize(GameSession game)
        {
            session=game;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root=new GameObject("HUD Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            safe=Rect("Safe area",root.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            if(EventSystem.current==null)
                new GameObject("Input events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            stats=Label(safe,"",21,TextAnchor.MiddleLeft,new Vector2(0,1),new Vector2(1,1),new Vector2(-120,-42),new Vector2(-290,65));
            ButtonAt(safe,"База",new Vector2(1,1),new Vector2(-190,-40),new Vector2(110,48),()=>session.OpenShop());
            ButtonAt(safe,"Пауза",new Vector2(1,1),new Vector2(-65,-40),new Vector2(110,48),()=>session.TogglePause());
            quest=Label(safe,"",19,TextAnchor.MiddleLeft,new Vector2(0,1),new Vector2(0,1),new Vector2(355,-102),new Vector2(660,65));
            toast=Label(safe,"",20,TextAnchor.MiddleCenter,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-178),new Vector2(760,60));
            var pad=Panel(safe,new Color(.08f,.18f,.2f,.8f),new Vector2(0,0),new Vector2(145,135),new Vector2(185,185));
            joystick=pad.gameObject.AddComponent<VirtualJoystick>();
            joystick.Knob=Panel(pad,new Color(.34f,.85f,.68f,.8f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(64,64));
            joystick.Knob.GetComponent<Image>().raycastTarget=false;
            for(var i=0;i<3;i++)
            {
                var id=abilityIds[i];
                abilityButtons[i]=ButtonAt(safe,abilityNames[i],new Vector2(1,0),new Vector2(-135,80+i*78),
                    new Vector2(220,65),()=>session.Cast(id));
                abilityText[i]=abilityButtons[i].GetComponentInChildren<Text>();
            }
            ButtonAt(safe,"Нагорода завдання",new Vector2(.5f,0),new Vector2(0,48),new Vector2(270,48),()=>session.ClaimQuest());
            modal=Panel(safe,new Color(.02f,.04f,.06f,.94f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(980,590)).gameObject;
            modalTitle=Label(modal.transform,"",28,TextAnchor.MiddleCenter,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-42),new Vector2(880,65));
            shop=new GameObject("Shop",typeof(RectTransform));shop.transform.SetParent(modal.transform,false);
            var shopRect=(RectTransform)shop.transform;shopRect.anchorMin=Vector2.zero;shopRect.anchorMax=Vector2.one;
            shopRect.offsetMin=Vector2.zero;shopRect.offsetMax=Vector2.zero;
            for(var i=0;i<6;i++)
            {
                var kind=(UpgradeKind)i;
                var button=ButtonAt(shop.transform,"",new Vector2(0,1),new Vector2(260,-128-i*65),new Vector2(440,56),()=>session.Buy(kind));
                upgradeText[i]=button.GetComponentInChildren<Text>();
            }
            for(var i=0;i<2;i++)
            {
                var id=abilityIds[i+1];
                var button=ButtonAt(shop.transform,"",new Vector2(1,1),new Vector2(-235,-135-i*83),new Vector2(390,70),()=>session.Unlock(id));
                unlockText[i]=button.GetComponentInChildren<Text>();
            }
            modalQuest=Label(shop.transform,"",21,TextAnchor.MiddleCenter,new Vector2(1,1),new Vector2(1,1),
                new Vector2(-235,-330),new Vector2(390,120));
            ButtonAt(shop.transform,"Отримати нагороду",new Vector2(1,1),new Vector2(-235,-430),new Vector2(390,56),()=>session.ClaimQuest());
            pause=new GameObject("Pause",typeof(RectTransform));pause.transform.SetParent(modal.transform,false);
            Label(pause.transform,"Продовжити дослідження",25,TextAnchor.MiddleCenter,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,140));
            ButtonAt(modal.transform,"Продовжити",new Vector2(.5f,0),new Vector2(0,42),new Vector2(300,52),()=>session.ClosePanel());
            modal.SetActive(false);FitSafeArea();
        }
        void Update()
        {
            if(session==null)return;
            if(lastSafe!=Screen.safeArea||lastScreen!=new Vector2Int(Screen.width,Screen.height))FitSafeArea();
            refresh-=Time.unscaledDeltaTime;if(refresh>0)return;refresh=.1f;
            var p=session.Profile;
            modal.SetActive(session.Paused);
            shop.SetActive(session.ShopOpen&&session.FatalError==null);
            pause.SetActive(!session.ShopOpen);
            if(session.FatalError!=null)
            {modalTitle.text=session.FatalError;stats.text="Помилка збереження";return;}
            if(p==null||session.Player==null)return;
            var zone=WorldBuilder.ZoneAt(session.Player.transform.position)+1;
            stats.text="HP "+Mathf.CeilToInt(session.Player.Health)+"/"+Balance.MaxHealth(p)+
                "   Геном "+p.bank+"   Біомаса "+Balance.LevelProgress(p)+"/"+(Balance.Level(p)*30)+
                "   Рівень "+Balance.Level(p)+"\nВантаж "+p.cargoUnits+"/"+Balance.Capacity(p)+" (+"+p.cargoValue+" геному)"+
                (session.InSafe?"   Сейв-зона":"   Небезпека зони "+zone);
            var stage=p.questStage;
            var text=stage<questNames.Length?questNames[stage]+" · "+Mathf.Min(Balance.QuestProgress(p),Balance.QuestGoals[stage])+"/"+Balance.QuestGoals[stage]:"Досліджуй світ і розвивай організм";
            quest.text=text;modalQuest.text=text;
            modalTitle.text=session.ShopOpen?"Переробка й адаптація · Геном "+p.bank:"Пауза";
            toast.text=Time.unscaledTime<session.ToastUntil?session.Toast:"";
            var cooldowns=new[]{session.Player.PullRemaining,session.Player.MoveRemaining,session.Player.DamageRemaining};
            for(var i=0;i<3;i++)
            {
                var owned=p.abilities.Contains(abilityIds[i]);
                abilityText[i].text=abilityNames[i]+(owned?(cooldowns[i]>0?" · "+cooldowns[i].ToString("0.0")+" с":""):
                    "\nВідкрити: "+Balance.AbilityPrice(abilityIds[i]));
                abilityButtons[i].interactable=!session.Paused&&(!owned||cooldowns[i]<=0);
            }
            for(var i=0;i<6;i++)
            {
                var locked=(i==(int)UpgradeKind.Channels||i==(int)UpgradeKind.Radius)&&Balance.Level(p)<2;
                upgradeText[i].text=upgradeNames[i]+" · "+p.upgrades[i]+"\n"+
                    (locked?"Потрібен рівень 2":p.upgrades[i]>=Balance.UpgradeLimit((UpgradeKind)i)?"Максимум":"Геном: "+Balance.UpgradePrice(p,(UpgradeKind)i));
            }
            for(var i=0;i<2;i++)unlockText[i].text=abilityNames[i+1]+"\n"+
                (p.abilities.Contains(abilityIds[i+1])?"Відкрито":"Відкрити за "+Balance.AbilityPrice(abilityIds[i+1])+" геному");
        }
        void FitSafeArea()
        {
            lastSafe=Screen.safeArea;lastScreen=new Vector2Int(Screen.width,Screen.height);
            if(Screen.width<=0||Screen.height<=0)return;
            safe.anchorMin=new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);
            safe.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);
            safe.offsetMin=Vector2.zero;safe.offsetMax=Vector2.zero;ResetInput();
        }
        public void ResetInput(){if(joystick!=null)joystick.ResetInput();}
        RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 position,Vector2 size)
        {
            var rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);
            rect.anchorMin=min;rect.anchorMax=max;rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        RectTransform Panel(Transform parent,Color color,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var rect=Rect("Panel",parent,anchor,anchor,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=color;return rect;
        }
        Text Label(Transform parent,string value,int size,TextAnchor alignment,Vector2 min,Vector2 max,Vector2 pos,Vector2 dimensions)
        {
            var rect=Rect("Text",parent,min,max,pos,dimensions);var text=rect.gameObject.AddComponent<Text>();
            text.font=font;text.fontSize=size;text.color=new Color(.89f,.96f,.94f);text.alignment=alignment;
            text.text=value;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;return text;
        }
        Button ButtonAt(Transform parent,string title,Vector2 anchor,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var rect=Panel(parent,new Color(.10f,.29f,.28f,.97f),anchor,pos,size);var button=rect.gameObject.AddComponent<Button>();
            button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener(action);
            Label(rect,title,20,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-12,-6));return button;
        }
    }
}
