using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ParasiteEvolution
{
    public sealed class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public readonly List<EnemyActor> Enemies=new List<EnemyActor>();
        public ProfileData Profile => store?.Current;
        public SlimeActor Player { get; private set; }
        public GameHud Hud { get; private set; }
        public bool Paused { get; private set; }
        public bool ShopOpen { get; private set; }
        public string FatalError { get; private set; }
        public bool InSafe => Player!=null&&WorldBuilder.IsSafe(Player.transform.position);
        public float RelayRemaining => relayRemaining;
        public string Toast { get; private set; }
        public float ToastUntil { get; private set; }
        ProfileStore store;
        Camera cameraView;
        float saveTimer,castLock,relayRemaining;
        bool baseLatch,dying;
        readonly List<EnemyActor> targets=new List<EnemyActor>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        { if(Instance==null)new GameObject("Parasite Evolution").AddComponent<GameSession>(); }
        void Awake()
        {
            if(Instance!=null){Destroy(gameObject);return;}
            Instance=this;Time.timeScale=1;
            Application.targetFrameRate=60;
            Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
            Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
            Screen.orientation=ScreenOrientation.AutoRotation;
            Hud=new GameObject("Interface").AddComponent<GameHud>();Hud.Initialize(this);
            try
            {
                store=new ProfileStore(Path.Combine(Application.persistentDataPath,"parasite-profile.json"));
                relayRemaining=store.Current.relayRemaining;
                new WorldBuilder(this);
                Player=new GameObject("Slime organism").AddComponent<SlimeActor>();Player.Initialize(this);
                cameraView=new GameObject("Camera").AddComponent<Camera>();
                cameraView.tag="MainCamera";cameraView.orthographic=true;cameraView.orthographicSize=11;
                cameraView.nearClipPlane=.1f;cameraView.farClipPlane=100;
                cameraView.backgroundColor=new Color(.17f,.22f,.28f);
                cameraView.transform.rotation=Quaternion.Euler(54,0,0);
                cameraView.transform.position=Player.transform.position+new Vector3(0,19,-14);
                if(Profile.health<=0)Die();
                Note("Рухайся, збирай зразки та повертайся на базу.");
            }
            catch(Exception ex){Fail(ex);}
        }
        void Update()
        {
            if(Paused||Player==null)return;
            var dt=Time.deltaTime;castLock=Mathf.Max(0,castLock-dt);saveTimer+=dt;
            relayRemaining=Mathf.Max(0,relayRemaining-dt);
            var safe=InSafe;
            if(safe)
            {
                Player.Health=Mathf.MoveTowards(Player.Health,Balance.MaxHealth(Profile),dt*8);
                if(!baseLatch&&Profile.cargoUnits>0)
                {
                    var value=Profile.cargoValue;
                    if(Transact(Economy.Deposit))Note("Зразки перероблено: +"+value+" геному");
                }
            }
            baseLatch=safe;
            var bit=1<<WorldBuilder.ZoneAt(Player.transform.position);
            if((Profile.visitedZones&bit)==0)Transact(p=>{p.visitedZones|=bit;return true;});
            Player.Visual.WorkTargets.Clear();
            if(!safe)
            {
                // Organic relay moves already captured cargo, avoiding repetitive walks to base.
                // It cannot create resources and is idle while the game is paused.
                if(Profile.relayLevel>0&&relayRemaining<=0&&Profile.cargoUnits>0)
                {
                    var delivered=Profile.cargoValue;
                    if(Transact(p=>
                    {
                        if(!Economy.RelayTransfer(p))return false;
                        p.relayRemaining=Balance.RelayInterval(p);
                        return true;
                    }))
                    {
                        relayRemaining=Profile.relayRemaining;
                        Note("Органічне реле передало: +"+delivered+" геному");
                    }
                }
                if(Paused)return;
                FindTargets(Balance.Radius(Profile),true);
                var channels=Balance.Channels(Profile);
                for(var i=0;i<targets.Count&&i<channels;i++)
                {
                    var enemy=targets[i];
                    Player.Visual.WorkTargets.Add(enemy.transform.position+Vector3.up*.65f);
                    enemy.Damage(Balance.CaptureDamage(Profile)*dt);
                    if(Paused)break;
                }
                foreach(var enemy in Enemies)
                    if(enemy.Alive&&enemy.Pulled&&Player.Visual.WorkTargets.Count<3)
                        Player.Visual.WorkTargets.Add(enemy.transform.position+Vector3.up*.65f);
            }
            if(saveTimer>=5){saveTimer=0;Checkpoint();}
        }
        void LateUpdate()
        {
            if(cameraView==null||Player==null||Paused)return;
            var desired=Player.transform.position+new Vector3(0,19,-14);
            cameraView.transform.position=Vector3.Lerp(cameraView.transform.position,desired,1-Mathf.Exp(-7*Time.deltaTime));
        }
        void FindTargets(float radius,bool mustFit=false)
        {
            targets.Clear();
            foreach(var enemy in Enemies)
                if(enemy.Alive&&(!mustFit||Balance.CanCarry(Profile,enemy.Units))&&
                    Vector3.Distance(Player.transform.position,enemy.transform.position)<=radius&&
                    WorldBuilder.ClearLine(Player.transform.position,enemy.transform.position))targets.Add(enemy);
            targets.Sort((a,b)=>(a.transform.position-Player.transform.position).sqrMagnitude.CompareTo(
                (b.transform.position-Player.transform.position).sqrMagnitude));
        }
        public bool Collect(EnemyActor enemy)
        {
            var level=Balance.Level(Profile);
            var success=Transact(p=>Economy.Capture(p,enemy.Units,enemy.Value,enemy.Experience,enemy.Warden));
            if(success)
            {
                if(Balance.Level(Profile)>level)Note("Новий рівень: "+Balance.Level(Profile));
                else if(Profile.cargoUnits==Balance.Capacity(Profile))Note("Вантаж заповнений. Повернися на базу.");
            }
            return success;
        }
        public void Cast(string id)
        {
            if(Paused||Player==null||castLock>0)return;
            if(!Profile.abilities.Contains(id)){OpenShop();return;}
            if(InSafe){Note("База захищена. Здібності доступні за її межами.");return;}
            if(id=="AB-PULL")
            {
                if(Player.PullRemaining>0)return;
                FindTargets(8,true);
                if(targets.Count==0){Note("Немає доступної цілі.");return;}
                targets[0].Pull();Player.PullRemaining=Balance.PullCooldown;
            }
            else if(id=="AB-MOVE")
            {
                if(Player.MoveRemaining>0)return;
                if(!Player.StartDash()){Note("Немає безпечної точки ривка.");return;}
                Player.MoveRemaining=Balance.MoveCooldown;
            }
            else if(id=="AB-DAMAGE")
            {
                if(Player.DamageRemaining>0)return;
                FindTargets(4.5f);
                if(targets.Count==0){Note("Немає доступної цілі.");return;}
                var copy=targets.ToArray();Player.DamageRemaining=Balance.DamageCooldown;
                Ring(Player.transform.position,4.5f,new Color(.8f,.3f,1));
                foreach(var enemy in copy){enemy.Damage(38);if(Paused)break;}
            }
            else return;
            castLock=.2f;Checkpoint();
        }
        public bool Transact(Func<ProfileData,bool> action)
        {
            if(store==null||FatalError!=null)return false;
            try{return store.Apply(p=>{Snapshot(p);return action(p);});}
            catch(Exception ex){Fail(ex);return false;}
        }
        void Snapshot(ProfileData p)
        {
            if(Player==null)return;
            p.x=Player.transform.position.x;p.z=Player.transform.position.z;
            p.health=Mathf.Clamp(Player.Health,0,Balance.MaxHealth(p));
            p.pullRemaining=Player.PullRemaining;p.moveRemaining=Player.MoveRemaining;p.damageRemaining=Player.DamageRemaining;
            p.relayRemaining=relayRemaining;
        }
        public void Checkpoint(){if(store!=null&&Player!=null)Transact(p=>true);}
        public void Die()
        {
            if(dying||Profile==null)return;dying=true;
            var serial=Profile.deathSerial+1;var before=Profile.bank;
            var success=Transact(p=>
            {
                if(!Economy.SettleDeath(p,serial,Balance.DeathPenaltyBps))return false;
                p.health=Balance.MaxHealth(p);p.x=WorldBuilder.SafePoint.x;p.z=WorldBuilder.SafePoint.z;
                return true;
            });
            if(success)
            {
                Player.Health=Profile.health;Player.Teleport(WorldBuilder.SafePoint);
                Note("Повернення на базу. Втрачено геному: "+(before-Profile.bank));
                baseLatch=false;
            }
            dying=false;
        }
        public void Buy(UpgradeKind kind)
        {
            if(!ShopOpen||!InSafe)return;
            var oldMax=Balance.MaxHealth(Profile);
            if(Transact(p=>Economy.Upgrade(p,kind)))
            {Player.Health+=Balance.MaxHealth(Profile)-oldMax;Note("Покращення придбано.");}
            else if(FatalError==null)Note("Недостатньо геному або потрібен вищий рівень.");
        }
        public void BuyRelay()
        {
            if(!ShopOpen||!InSafe)return;
            if(Transact(Economy.BuyRelay))
            {
                relayRemaining=Mathf.Min(relayRemaining,Balance.RelayInterval(Profile));
                Note("Органічне реле посилено.");
            }
            else if(FatalError==null)Note("Реле: потрібен рівень 2, геном або досягнуто максимум.");
        }
        public void Unlock(string id)
        {
            if(!ShopOpen||!InSafe)return;
            if(Transact(p=>Economy.Unlock(p,id)))Note("Здібність відкрита.");
            else if(FatalError==null)Note("Здібність уже відкрита або бракує геному.");
        }
        public void ClaimQuest()
        {
            if(!InSafe){Note("Повернися до термінала на базі.");return;}
            if(Transact(Economy.ClaimQuest))Note("Нагороду отримано.");
            else if(FatalError==null)Note("Завдання ще не виконано.");
        }
        public void OpenShop()
        {
            if(FatalError!=null)return;
            if(!InSafe){Note("Покращення й відкриття здібностей — на базі.");return;}
            Checkpoint();if(FatalError!=null)return;
            ShopOpen=true;SetPaused(true);
        }
        public void ClosePanel()
        {if(FatalError!=null)return;ShopOpen=false;SetPaused(false);}
        public void TogglePause()
        {
            if(FatalError!=null)return;
            if(Paused)ClosePanel();else{Checkpoint();SetPaused(true);}
        }
        void SetPaused(bool value)
        {Paused=value;Time.timeScale=value?0:1;Hud?.ResetInput();}
        public void Note(string message){Toast=message;ToastUntil=Time.unscaledTime+4;}
        void Fail(Exception ex)
        {
            Debug.LogException(ex);FatalError="Не вдалося прочитати або зберегти прогрес. Файли збережено для відновлення.";
            ShopOpen=false;SetPaused(true);
        }
        void OnApplicationPause(bool value)
        {
            if(!value||Player==null)return;
            Checkpoint();ShopOpen=false;SetPaused(true);
        }
        void OnApplicationQuit(){if(FatalError==null)Checkpoint();}
        void OnDestroy(){if(Instance==this){Instance=null;Time.timeScale=1;}}
        public void Ring(Vector3 center,float radius,Color color)
        {
            var material=Visuals.Material(color);var line=Visuals.Line(null,material,.07f);line.positionCount=49;
            for(var i=0;i<=48;i++)
            {var a=i/48f*Mathf.PI*2;line.SetPosition(i,center+new Vector3(Mathf.Cos(a)*radius,.1f,Mathf.Sin(a)*radius));}
            Destroy(line.gameObject,.5f);Destroy(material,.6f);
        }
    }
}
