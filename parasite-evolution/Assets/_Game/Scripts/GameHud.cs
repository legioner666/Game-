using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ParasiteEvolution
{
    // Landscape mobile HUD built in code: floating stick on the left, three abilities on the right,
    // bars instead of numbers where possible, every element kept inside Screen.safeArea.
    public sealed class GameHud : MonoBehaviour
    {
        GameSession session;
        RectTransform safe;
        Font font;
        VirtualJoystick joystick;
        Text stats, quest, toast, modalTitle, modalQuest, zoneLabel, wardenLabel;
        readonly Text[] abilityText = new Text[3], upgradeText = new Text[6], unlockText = new Text[2], mutationText = new Text[Balance.MutationCount];
        readonly Button[] abilityButtons = new Button[3];
        readonly RectTransform[] cooldownMask = new RectTransform[3];
        RectTransform healthFill, cargoFill, wardenFill;
        GameObject modal, shop, pause, claimButton, wardenBar;
        Image vignette;
        Rect lastSafe; Vector2Int lastScreen;
        float refresh;
        readonly float[] remaining = new float[3];
        static readonly string[] AbilityIds = { "AB-PULL", "AB-MOVE", "AB-DAMAGE" };
        static readonly string[] AbilityNames = { "Хватка", "Ривок", "Удар шипами" };
        static readonly string[] UpgradeNames = { "Швидкість захоплення", "Місткість", "Швидкість руху", "Радіус захоплення", "Канали захоплення", "Здоров'я" };
        static readonly string[] MutationNames = { "Хітинові шипи", "Кислотні залози", "Довгі кінцівки", "Щільна мембрана", "Рефлекс хижака", "Еластичний шлунок" };
        static readonly string[] MutationEffects = { "удар ×1,5", "захоплення +25%", "радіус і ривок довші", "шкода −25%", "перезарядка −20%", "вантаж +4" };
        static readonly string[] QuestNames = { "Віднови сили: захопи 4 цілі", "Двічі здай зразки на базі", "Придбай покращення",
            "Досліди три зони", "Зупини Наглядача", "Закріпи першу мутацію", "Здай вантаж 10 разів" };
        public Vector2 Move => joystick != null ? joystick.Value : Vector2.zero;

        public void Initialize(GameSession game)
        {
            session = game; font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false); root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
            vignette = Stretch(root.transform, new Color(.8f, 0, .08f, 0)).GetComponent<Image>(); vignette.raycastTarget = false;
            safe = Rect("Safe area", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            if (EventSystem.current == null)
                new GameObject("Input events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            BuildStatus();
            BuildControls();
            BuildModal();
            modal.SetActive(false); FitSafeArea();
        }

        void BuildStatus()
        {
            healthFill = Bar(safe, new Vector2(0, 1), new Vector2(175, -30), new Vector2(310, 22), new Color(.75f, .1f, .25f));
            cargoFill = Bar(safe, new Vector2(0, 1), new Vector2(175, -58), new Vector2(310, 14), new Color(.35f, .85f, .65f));
            stats = Label(safe, "", 19, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(530, -46), new Vector2(380, 60));
            quest = Label(safe, "", 18, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(225, -100), new Vector2(420, 50));
            zoneLabel = Label(safe, "", 18, TextAnchor.MiddleRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-170, -88), new Vector2(300, 30));
            toast = Label(safe, "", 21, TextAnchor.MiddleCenter, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -160), new Vector2(760, 60));
            wardenBar = new GameObject("Warden bar", typeof(RectTransform)); wardenBar.transform.SetParent(safe, false);
            var wardenRect = (RectTransform)wardenBar.transform; wardenRect.anchorMin = wardenRect.anchorMax = new Vector2(.5f, 1);
            wardenRect.anchoredPosition = new Vector2(0, -112); wardenRect.sizeDelta = new Vector2(420, 40);
            wardenFill = Bar(wardenBar.transform, new Vector2(.5f, .5f), new Vector2(0, -8), new Vector2(420, 14), new Color(1, .48f, .12f));
            wardenLabel = Label(wardenBar.transform, "Наглядач", 17, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 12), new Vector2(420, 24));
            wardenBar.SetActive(false);
            ButtonAt(safe, "База", new Vector2(1, 1), new Vector2(-190, -40), new Vector2(110, 52), () => session.OpenShop());
            ButtonAt(safe, "Пауза", new Vector2(1, 1), new Vector2(-65, -40), new Vector2(110, 52), () => session.TogglePause());
        }

        void BuildControls()
        {
            var zone = Rect("Move zone", safe, new Vector2(0, 0), new Vector2(.42f, .72f), Vector2.zero, Vector2.zero);
            zone.offsetMin = Vector2.zero; zone.offsetMax = Vector2.zero;
            var catcher = zone.gameObject.AddComponent<Image>(); catcher.color = new Color(0, 0, 0, 0);
            joystick = zone.gameObject.AddComponent<VirtualJoystick>();
            joystick.Base = Panel(zone, new Color(.08f, .18f, .2f, .55f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(190, 190));
            joystick.Base.GetComponent<Image>().raycastTarget = false;
            joystick.Knob = Panel(joystick.Base, new Color(.34f, .85f, .68f, .85f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(74, 74));
            joystick.Knob.GetComponent<Image>().raycastTarget = false;
            joystick.Range = 80;
            // Primary heavy strike is the biggest; grab and dash sit on an arc around it.
            var positions = new[] { new Vector2(-300, 95), new Vector2(-150, 255), new Vector2(-120, 105) };
            var sizes = new[] { new Vector2(140, 110), new Vector2(140, 110), new Vector2(170, 150) };
            for (var i = 0; i < 3; i++)
            {
                var id = AbilityIds[i];
                abilityButtons[i] = ButtonAt(safe, AbilityNames[i], new Vector2(1, 0), positions[i], sizes[i], () => session.Cast(id));
                abilityText[i] = abilityButtons[i].GetComponentInChildren<Text>();
                var mask = Rect("Cooldown", abilityButtons[i].transform, Vector2.zero, new Vector2(1, 0), Vector2.zero, Vector2.zero);
                mask.offsetMin = Vector2.zero; mask.offsetMax = Vector2.zero;
                var shade = mask.gameObject.AddComponent<Image>(); shade.color = new Color(0, 0, 0, .55f); shade.raycastTarget = false;
                mask.SetAsFirstSibling();
                cooldownMask[i] = mask;
            }
            claimButton = ButtonAt(safe, "Забрати нагороду", new Vector2(.5f, 0), new Vector2(0, 48), new Vector2(280, 54), () => session.ClaimQuest()).gameObject;
        }

        void BuildModal()
        {
            modal = Stretch(safe, new Color(.02f, .04f, .06f, .95f));
            var modalRect = (RectTransform)modal.transform; modalRect.offsetMin = new Vector2(24, 24); modalRect.offsetMax = new Vector2(-24, -24);
            modalTitle = Label(modal.transform, "", 27, TextAnchor.MiddleCenter, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -36), new Vector2(900, 56));
            shop = new GameObject("Shop", typeof(RectTransform)); shop.transform.SetParent(modal.transform, false);
            var shopRect = (RectTransform)shop.transform; shopRect.anchorMin = Vector2.zero; shopRect.anchorMax = Vector2.one;
            shopRect.offsetMin = Vector2.zero; shopRect.offsetMax = Vector2.zero;
            Label(shop.transform, "Покращення", 20, TextAnchor.MiddleCenter, new Vector2(.17f, 1), new Vector2(.17f, 1), new Vector2(0, -84), new Vector2(340, 30));
            Label(shop.transform, "Мутації", 20, TextAnchor.MiddleCenter, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -84), new Vector2(340, 30));
            Label(shop.transform, "Здібності та завдання", 20, TextAnchor.MiddleCenter, new Vector2(.83f, 1), new Vector2(.83f, 1), new Vector2(0, -84), new Vector2(340, 30));
            for (var i = 0; i < 6; i++)
            {
                var kind = (UpgradeKind)i;
                upgradeText[i] = ButtonAt(shop.transform, "", new Vector2(.17f, 1), new Vector2(0, -132 - i * 64), new Vector2(330, 56), () => session.Buy(kind)).GetComponentInChildren<Text>();
                var mutation = (MutationKind)i;
                mutationText[i] = ButtonAt(shop.transform, "", new Vector2(.5f, 1), new Vector2(0, -132 - i * 64), new Vector2(330, 56), () => session.Mutate(mutation)).GetComponentInChildren<Text>();
            }
            for (var i = 0; i < 2; i++)
            {
                var id = AbilityIds[i + 1];
                unlockText[i] = ButtonAt(shop.transform, "", new Vector2(.83f, 1), new Vector2(0, -136 - i * 76), new Vector2(330, 66), () => session.Unlock(id)).GetComponentInChildren<Text>();
            }
            modalQuest = Label(shop.transform, "", 19, TextAnchor.MiddleCenter, new Vector2(.83f, 1), new Vector2(.83f, 1), new Vector2(0, -320), new Vector2(330, 90));
            ButtonAt(shop.transform, "Отримати нагороду", new Vector2(.83f, 1), new Vector2(0, -400), new Vector2(330, 56), () => session.ClaimQuest());
            pause = new GameObject("Pause", typeof(RectTransform)); pause.transform.SetParent(modal.transform, false);
            var pauseRect = (RectTransform)pause.transform; pauseRect.anchorMin = Vector2.zero; pauseRect.anchorMax = Vector2.one;
            pauseRect.offsetMin = Vector2.zero; pauseRect.offsetMax = Vector2.zero;
            Label(pause.transform, "Організм у сплячці. Прогрес збережено.", 24, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 60), new Vector2(700, 80));
            ButtonAt(pause.transform, "Застряг? Вивільнити поруч", new Vector2(.5f, .5f), new Vector2(0, -30), new Vector2(360, 56), () => session.Unstick());
            ButtonAt(modal.transform, "Продовжити", new Vector2(.5f, 0), new Vector2(0, 44), new Vector2(300, 56), () => session.ClosePanel());
        }

        void Update()
        {
            if (session == null) return;
            if (lastSafe != Screen.safeArea || lastScreen != new Vector2Int(Screen.width, Screen.height)) FitSafeArea();
            var p = session.Profile;
            var player = session.Player;
            vignette.color = new Color(.8f, 0, .08f, .45f * session.HurtFlash);
            if (p != null && player != null && session.FatalError == null)
            {
                SetFill(healthFill, player.Health / Mathf.Max(1, Balance.MaxHealth(p)));
                SetFill(cargoFill, p.cargoUnits / (float)Mathf.Max(1, Balance.Capacity(p)));
                remaining[0] = player.PullRemaining; remaining[1] = player.MoveRemaining; remaining[2] = player.DamageRemaining;
                for (var i = 0; i < 3; i++)
                {
                    var total = Balance.Cooldown(p, AbilityIds[i]);
                    var m = cooldownMask[i]; var max = m.anchorMax; max.y = total > 0 ? Mathf.Clamp01(remaining[i] / total) : 0; m.anchorMax = max;
                }
                var warden = session.ActiveWarden;
                var showWarden = warden != null && warden.Alive && warden.Brain != null && warden.Brain.State != WardenState.Idle;
                if (wardenBar.activeSelf != showWarden) wardenBar.SetActive(showWarden);
                if (showWarden) SetFill(wardenFill, warden.HealthFraction);
            }
            refresh -= Time.unscaledDeltaTime; if (refresh > 0) return; refresh = .1f;
            modal.SetActive(session.Paused);
            shop.SetActive(session.ShopOpen && session.FatalError == null);
            pause.SetActive(!session.ShopOpen && session.FatalError == null);
            if (session.FatalError != null)
            { modalTitle.text = session.FatalError; stats.text = "Помилка збереження"; return; }
            if (p == null || player == null) return;
            var zone = WorldBuilder.ZoneAt(player.transform.position) + 1;
            var level = Balance.Level(p);
            stats.text = "Геном " + p.bank + "   Рівень " + level + " (" + Balance.LevelProgress(p) + "/" + level * 30 + ")\n" +
                "HP " + Mathf.CeilToInt(player.Health) + "/" + Balance.MaxHealth(p) + "   Вантаж " + p.cargoUnits + "/" + Balance.Capacity(p) + " (+" + p.cargoValue + ")";
            zoneLabel.text = session.InSafe ? "Сейв-зона" : "Небезпека зони " + zone;
            var stage = p.questStage;
            var ready = stage < QuestNames.Length && Balance.QuestProgress(p) >= Balance.QuestGoals[stage];
            var text = stage < QuestNames.Length ? QuestNames[stage] + " · " + Mathf.Min(Balance.QuestProgress(p), Balance.QuestGoals[stage]) + "/" + Balance.QuestGoals[stage] +
                (ready ? " — готово" : "") : "Досліджуй світ і розвивай організм";
            quest.text = text; modalQuest.text = text;
            claimButton.SetActive(ready && session.InSafe && !session.Paused);
            modalTitle.text = session.ShopOpen ? "Переробка й адаптація · Геном " + p.bank + " · Рівень " + level : "Пауза";
            toast.text = Time.unscaledTime < session.ToastUntil ? session.Toast : "";
            for (var i = 0; i < 3; i++)
            {
                var owned = p.abilities.Contains(AbilityIds[i]);
                abilityText[i].text = AbilityNames[i] + (owned ? (remaining[i] > 0 ? "\n" + remaining[i].ToString("0.0") + " с" : "") :
                    "\nВідкрити: " + Balance.AbilityPrice(AbilityIds[i]));
                abilityButtons[i].interactable = !session.Paused && (!owned || remaining[i] <= 0);
            }
            if (!session.ShopOpen) return;
            for (var i = 0; i < 6; i++)
            {
                var locked = (i == (int)UpgradeKind.Channels || i == (int)UpgradeKind.Radius) && level < 2;
                upgradeText[i].text = UpgradeNames[i] + " · " + p.upgrades[i] + "\n" +
                    (locked ? "Потрібен рівень 2" : p.upgrades[i] >= Balance.UpgradeLimit((UpgradeKind)i) ? "Максимум" : "Геном: " + Balance.UpgradePrice(p, (UpgradeKind)i));
                var kind = (MutationKind)i;
                mutationText[i].text = MutationNames[i] + " · " + MutationEffects[i] + "\n" +
                    (Balance.Has(p, kind) ? "Закріплено" : level < Balance.MutationLevels[i] ? "Потрібен рівень " + Balance.MutationLevels[i]
                        : "Геном: " + Balance.MutationPrices[i]);
            }
            for (var i = 0; i < 2; i++) unlockText[i].text = AbilityNames[i + 1] + "\n" +
                (p.abilities.Contains(AbilityIds[i + 1]) ? "Відкрито" : "Відкрити за " + Balance.AbilityPrice(AbilityIds[i + 1]) + " геному");
        }

        static void SetFill(RectTransform fill, float fraction)
        {
            var max = fill.anchorMax; max.x = Mathf.Clamp01(fraction); fill.anchorMax = max;
        }

        void FitSafeArea()
        {
            lastSafe = Screen.safeArea; lastScreen = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safe.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safe.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
            safe.offsetMin = Vector2.zero; safe.offsetMax = Vector2.zero; ResetInput();
        }

        public void ResetInput() { if (joystick != null) joystick.ResetInput(); }

        RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform; rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }

        GameObject Stretch(Transform parent, Color color)
        {
            var rect = Rect("Overlay", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            rect.gameObject.AddComponent<Image>().color = color; return rect.gameObject;
        }

        RectTransform Panel(Transform parent, Color color, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var rect = Rect("Panel", parent, anchor, anchor, position, size); var image = rect.gameObject.AddComponent<Image>(); image.color = color; return rect;
        }

        // Background plus a fill whose right anchor encodes the fraction (no sprite needed).
        RectTransform Bar(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var back = Panel(parent, new Color(0, 0, 0, .55f), anchor, position, size); back.GetComponent<Image>().raycastTarget = false;
            var fill = Rect("Fill", back, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fill.offsetMin = new Vector2(2, 2); fill.offsetMax = new Vector2(-2, -2);
            var image = fill.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return fill;
        }

        Text Label(Transform parent, string value, int size, TextAnchor alignment, Vector2 min, Vector2 max, Vector2 pos, Vector2 dimensions)
        {
            var rect = Rect("Text", parent, min, max, pos, dimensions); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.color = new Color(.89f, .96f, .94f); text.alignment = alignment;
            text.text = value; text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; return text;
        }

        Button ButtonAt(Transform parent, string title, Vector2 anchor, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(parent, new Color(.10f, .29f, .28f, .97f), anchor, pos, size); var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(action);
            Label(rect, title, 19, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-12, -6)); return button;
        }
    }
}
