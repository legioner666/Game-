using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ParasiteEvolution
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public readonly List<EnemyActor> Enemies = new List<EnemyActor>();
        public ProfileData Profile => store?.Current;
        public SlimeActor Player { get; private set; }
        public GameHud Hud { get; private set; }
        public bool Paused { get; private set; }
        public bool ShopOpen { get; private set; }
        public string FatalError { get; private set; }
        public bool InSafe => Player != null && WorldBuilder.IsSafe(Player.transform.position);
        public string Toast { get; private set; }
        public float ToastUntil { get; private set; }
        public float HurtFlash { get; private set; }
        public EnemyActor ActiveWarden { get; private set; }
        ProfileStore store;
        Camera cameraView;
        Vector3 cameraFocus;
        float saveTimer, castLock, depositTimer, shake, lastSavedHealth;
        Vector3 lastSavedPosition;
        bool baseLatch, dying, depositWarned;
        readonly List<EnemyActor> targets = new List<EnemyActor>();
        readonly DistanceOrder order = new DistanceOrder();
        static readonly Vector3 CameraOffset = new Vector3(0, 19, -14);

        sealed class DistanceOrder : IComparer<EnemyActor>
        {
            public Vector3 Origin;
            public int Compare(EnemyActor a, EnemyActor b) =>
                (a.transform.position - Origin).sqrMagnitude.CompareTo((b.transform.position - Origin).sqrMagnitude);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        { if (Instance == null) new GameObject("Parasite Evolution").AddComponent<GameSession>(); }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; Time.timeScale = 1;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false; Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
            Hud = new GameObject("Interface").AddComponent<GameHud>(); Hud.Initialize(this);
            try
            {
                store = new ProfileStore(Path.Combine(Application.persistentDataPath, "parasite-profile.json"));
                new WorldBuilder(this);
                foreach (var enemy in Enemies) if (enemy.Warden) ActiveWarden = enemy;
                Player = new GameObject("Slime organism").AddComponent<SlimeActor>(); Player.Initialize(this);
                cameraView = new GameObject("Camera").AddComponent<Camera>();
                cameraView.tag = "MainCamera"; cameraView.orthographic = true; cameraView.orthographicSize = 11;
                cameraView.nearClipPlane = .1f; cameraView.farClipPlane = 100;
                cameraView.clearFlags = CameraClearFlags.SolidColor;
                cameraView.backgroundColor = new Color(.17f, .22f, .28f);
                cameraView.transform.rotation = Quaternion.Euler(54, 0, 0);
                cameraFocus = Player.transform.position;
                cameraView.transform.position = cameraFocus + CameraOffset;
                lastSavedPosition = Player.transform.position; lastSavedHealth = Player.Health;
                if (Profile.health <= 0) Die();
                Note("Рухайся, збирай зразки та повертайся на базу.");
            }
            catch (Exception ex) { Fail(ex); }
        }

        void Update()
        {
            if (Paused || Player == null) return;
            var dt = Time.deltaTime;
            castLock = Mathf.Max(0, castLock - dt); saveTimer += dt; depositTimer -= dt;
            HurtFlash = Mathf.Max(0, HurtFlash - dt * 2.5f);
            var safe = InSafe;
            if (safe)
            {
                Player.Health = Mathf.MoveTowards(Player.Health, Balance.MaxHealth(Profile), dt * 8);
                if (Profile.cargoUnits > 0 && depositTimer <= 0)
                {
                    depositTimer = 1;
                    var value = Profile.cargoValue;
                    if (Transact(Economy.Deposit)) { Note("Зразки перероблено: +" + value + " геному"); depositWarned = false; }
                    else if (FatalError == null && !depositWarned) { Note("Сховище геному переповнене."); depositWarned = true; }
                }
                if (!baseLatch && Profile.questStage < Balance.QuestRewards.Length &&
                    Balance.QuestProgress(Profile) >= Balance.QuestGoals[Profile.questStage]) Note("Завдання виконано — забери нагороду.");
            }
            baseLatch = safe;
            var bit = 1 << WorldBuilder.ZoneAt(Player.transform.position);
            if ((Profile.visitedZones & bit) == 0) Transact(p => { p.visitedZones |= bit; return true; });
            var work = Player.Visual.WorkTargets;
            work.Clear();
            if (!safe && !Player.Dashing)
            {
                FindTargets(Balance.Radius(Profile));
                var channels = Balance.Channels(Profile);
                var used = 0;
                for (var i = 0; i < targets.Count && used < channels; i++)
                {
                    var enemy = targets[i];
                    if (enemy.Units > Balance.Capacity(Profile) - Profile.cargoUnits) continue;
                    used++;
                    work.Add(enemy.transform.position + Vector3.up * (enemy.Warden ? 1.2f : .65f));
                    enemy.Damage(Balance.CaptureDamage(Profile) * enemy.CaptureMultiplier * dt);
                    if (Paused) break;
                }
                foreach (var enemy in Enemies)
                    if (enemy.Alive && enemy.Pulled && work.Count < 3) work.Add(enemy.transform.position + Vector3.up * .65f);
            }
            if (saveTimer >= 5)
            {
                saveTimer = 0;
                // Periodic snapshot only when the organism actually moved or changed health.
                if ((Player.transform.position - lastSavedPosition).sqrMagnitude > .25f || Mathf.Abs(Player.Health - lastSavedHealth) > .5f)
                    Checkpoint();
            }
        }

        void LateUpdate()
        {
            if (cameraView == null || Player == null || Paused) return;
            var dt = Time.deltaTime;
            // Look ahead in the movement direction so threats appear before the organism reaches them.
            var lead = Player.Aim * (2.2f * Player.Speed01);
            var focus = Player.transform.position + lead;
            focus.x = Mathf.Clamp(focus.x, -26, 26); focus.z = Mathf.Clamp(focus.z, -17, 17);
            cameraFocus = Vector3.Lerp(cameraFocus, focus, 1 - Mathf.Exp(-6 * dt));
            var wardenNear = ActiveWarden != null && ActiveWarden.Alive && ActiveWarden.Brain != null &&
                ActiveWarden.Brain.State != WardenState.Idle && ActiveWarden.Brain.State != WardenState.Return;
            var zoom = wardenNear ? 13f : Player.Dashing ? 12f : 11f;
            cameraView.orthographicSize = Mathf.Lerp(cameraView.orthographicSize, zoom, 1 - Mathf.Exp(-3 * dt));
            shake = Mathf.Max(0, shake - dt * 2.2f);
            var jitter = shake > 0 ? new Vector3(Mathf.PerlinNoise(Time.time * 31, 0) - .5f, 0, Mathf.PerlinNoise(0, Time.time * 31) - .5f) * shake : Vector3.zero;
            cameraView.transform.position = cameraFocus + CameraOffset + jitter;
        }

        void FindTargets(float radius)
        {
            targets.Clear();
            var origin = Player.transform.position;
            foreach (var enemy in Enemies)
                if (enemy.Alive && Vector3.Distance(origin, enemy.transform.position) <= radius &&
                    WorldBuilder.ClearLine(origin, enemy.transform.position)) targets.Add(enemy);
            order.Origin = origin;
            targets.Sort(order);
        }

        public bool Collect(EnemyActor enemy)
        {
            var level = Balance.Level(Profile);
            var success = Transact(p => Economy.Capture(p, enemy.Units, enemy.Value, enemy.Experience, enemy.Warden));
            if (success)
            {
                if (enemy.Warden) Note("Наглядача поглинуто!");
                else if (Balance.Level(Profile) > level) Note("Новий рівень: " + Balance.Level(Profile) + ". Перевір мутації на базі.");
                else if (Profile.cargoUnits == Balance.Capacity(Profile)) Note("Вантаж заповнений. Повернися на базу.");
            }
            else if (FatalError == null) Note("Не вистачає місця для вантажу.");
            return success;
        }

        public void Cast(string id)
        {
            if (Paused || Player == null || castLock > 0 || Profile == null) return;
            if (!Profile.abilities.Contains(id)) { OpenShop(); return; }
            if (InSafe) { Note("База захищена. Здібності доступні за її межами."); return; }
            if (id == "AB-PULL")
            {
                if (Player.PullRemaining > 0) return;
                FindTargets(8 + (Balance.Has(Profile, MutationKind.LongLimbs) ? 1.5f : 0));
                if (targets.Count == 0) { Note("Немає доступної цілі."); return; }
                targets[0].Pull(); Player.PullRemaining = Balance.Cooldown(Profile, id);
            }
            else if (id == "AB-MOVE")
            {
                if (Player.MoveRemaining > 0) return;
                if (!Player.StartDash(Balance.DashDistance(Profile))) { Note("Немає безпечної точки ривка."); return; }
                Player.MoveRemaining = Balance.Cooldown(Profile, id);
            }
            else if (id == "AB-DAMAGE")
            {
                if (Player.DamageRemaining > 0) return;
                const float radius = 4.5f;
                FindTargets(radius);
                Player.Visual.BeginStrike(radius);
                Player.DamageRemaining = Balance.Cooldown(Profile, id);
                Ring(Player.transform.position, radius, new Color(.8f, .3f, 1));
                Shake(.2f);
                var damage = Balance.StrikeDamage(Profile);
                var copy = targets.ToArray();
                foreach (var enemy in copy)
                {
                    enemy.Knock(enemy.transform.position - Player.transform.position, 7);
                    enemy.Damage(damage);
                    if (Paused) break;
                }
                if (copy.Length == 0) Note("Удар у порожнечу — цілі поза радіусом.");
            }
            else return;
            castLock = .2f; Checkpoint();
        }

        public bool Transact(Func<ProfileData, bool> action)
        {
            if (store == null || FatalError != null) return false;
            try { return store.Apply(p => { Snapshot(p); return action(p); }); }
            catch (Exception ex) { Fail(ex); return false; }
        }

        void Snapshot(ProfileData p)
        {
            if (Player == null) return;
            p.x = Player.transform.position.x; p.z = Player.transform.position.z;
            p.health = Mathf.Clamp(Player.Health, 0, Balance.MaxHealth(p));
            p.pullRemaining = Player.PullRemaining; p.moveRemaining = Player.MoveRemaining; p.damageRemaining = Player.DamageRemaining;
        }

        public void Checkpoint()
        {
            if (store == null || Player == null) return;
            if (Transact(p => true)) { lastSavedPosition = Player.transform.position; lastSavedHealth = Player.Health; }
        }

        public void Die()
        {
            if (dying || Profile == null) return; dying = true;
            var serial = Profile.deathSerial + 1; var before = Profile.bank;
            var success = Transact(p =>
            {
                if (!Economy.SettleDeath(p, serial, Balance.DeathPenaltyBps)) return false;
                p.health = Balance.MaxHealth(p); p.x = WorldBuilder.SafePoint.x; p.z = WorldBuilder.SafePoint.z;
                return true;
            });
            if (success)
            {
                Player.Health = Profile.health; Player.Teleport(WorldBuilder.SafePoint); Player.Protect(2.5f);
                foreach (var enemy in Enemies) enemy.ResetToHome();
                Note("Організм відновився на базі. Втрачено геному: " + (before - Profile.bank));
                baseLatch = false; HurtFlash = 1;
            }
            dying = false;
        }

        public void OnPlayerHurt(float amount)
        {
            HurtFlash = Mathf.Clamp01(HurtFlash + .25f + amount / 40f);
            Shake(Mathf.Clamp(amount / 40f, .08f, .3f));
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        public void Buy(UpgradeKind kind)
        {
            if (!ShopOpen || !InSafe) return;
            var oldMax = Balance.MaxHealth(Profile);
            if (Transact(p => Economy.Upgrade(p, kind)))
            { Player.Health += Balance.MaxHealth(Profile) - oldMax; Note("Покращення придбано."); }
            else if (FatalError == null) Note("Недостатньо геному або потрібен вищий рівень.");
        }

        public void Mutate(MutationKind kind)
        {
            if (!ShopOpen || !InSafe) return;
            var index = (int)kind;
            if (Transact(p => Economy.Mutate(p, kind))) Note("Мутація закріпилася в тілі.");
            else if (FatalError == null)
                Note(Balance.Has(Profile, kind) ? "Мутація вже є." : Balance.Level(Profile) < Balance.MutationLevels[index]
                    ? "Потрібен рівень " + Balance.MutationLevels[index] + "." : "Бракує геному.");
        }

        public void Unlock(string id)
        {
            if (!ShopOpen || !InSafe) return;
            if (Transact(p => Economy.Unlock(p, id))) Note("Здібність відкрита.");
            else if (FatalError == null) Note("Здібність уже відкрита або бракує геному.");
        }

        public void ClaimQuest()
        {
            if (!InSafe) { Note("Повернися до термінала на базі."); return; }
            if (Transact(Economy.ClaimQuest)) Note("Нагороду отримано.");
            else if (FatalError == null) Note("Завдання ще не виконано.");
        }

        public void Unstick()
        {
            if (Player == null || FatalError != null) return;
            ClosePanel();
            Note(Player.Unstick() ? "Організм вивільнено поруч." : "Організм не застряг.");
            Checkpoint();
        }

        public void OpenShop()
        {
            if (FatalError != null) return;
            if (!InSafe) { Note("Покращення, мутації й здібності — на базі."); return; }
            Checkpoint(); if (FatalError != null) return;
            ShopOpen = true; SetPaused(true);
        }

        public void ClosePanel()
        { if (FatalError != null) return; ShopOpen = false; SetPaused(false); }

        public void TogglePause()
        {
            if (FatalError != null) return;
            if (Paused) ClosePanel(); else { Checkpoint(); SetPaused(true); }
        }

        void SetPaused(bool value)
        { Paused = value; Time.timeScale = value ? 0 : 1; Hud?.ResetInput(); }

        public void Note(string message) { Toast = message; ToastUntil = Time.unscaledTime + 4; }

        void Fail(Exception ex)
        {
            Debug.LogException(ex); FatalError = "Не вдалося прочитати або зберегти прогрес. Файли збережено для відновлення.";
            ShopOpen = false; SetPaused(true);
        }

        void OnApplicationPause(bool value)
        {
            if (!value || Player == null) return;
            Checkpoint(); ShopOpen = false; SetPaused(true);
        }

        void OnApplicationQuit() { if (FatalError == null) Checkpoint(); }
        void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1; } }

        public void Ring(Vector3 center, float radius, Color color)
        {
            var material = Visuals.Material(color); var line = Visuals.Line(null, material, .07f); line.positionCount = 49;
            for (var i = 0; i <= 48; i++)
            { var a = i / 48f * Mathf.PI * 2; line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, .1f, Mathf.Sin(a) * radius)); }
            Destroy(line.gameObject, .5f); Destroy(material, .6f);
        }

        public void Beam(Vector3 from, Vector3 to, Color color)
        {
            var material = Visuals.Glow(color, 2.5f); var line = Visuals.Line(null, material, .18f); line.positionCount = 2;
            line.SetPosition(0, from); line.SetPosition(1, to);
            Destroy(line.gameObject, .18f); Destroy(material, .25f);
        }
    }
}
