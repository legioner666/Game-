using UnityEngine;

namespace ParasiteEvolution
{
    public sealed class EnemyActor : MonoBehaviour
    {
        public GameSession Session;
        public bool Alive { get; private set; } = true;
        public bool Warden { get; private set; }
        public int Zone { get; private set; }
        public int Units => Warden ? 3 : Zone == 2 ? 2 : 1;
        public int Value => Warden ? 30 : 3 + Zone*2;
        public int Experience => Warden ? 20 : 3 + Zone;
        public float Health { get; private set; }
        public bool Pulled => pullUntil > Time.time;
        CharacterController controller;
        Transform model, bar;
        Vector3 home, goal;
        float maximum, attackTimer, wanderTimer, respawnTimer, pullUntil, warning;
        Vector3 stompPoint;
        LineRenderer telegraph;

        public void Initialize(GameSession session, Vector3 point, int zone, bool warden)
        {
            Session=session;home=goal=point;transform.position=point;Zone=zone;Warden=warden;
            maximum=warden?200:24+zone*18;Health=maximum;gameObject.layer=10;
            controller=gameObject.AddComponent<CharacterController>();
            controller.height=warden?2.4f:1.5f;controller.center=Vector3.up*controller.height*.5f;
            controller.radius=warden?.9f:.35f;controller.stepOffset=.15f;
            model=new GameObject("Procedural guard").transform;model.SetParent(transform,false);
            var shell=Visuals.Material(warden?new Color(.42f,.25f,.2f):new Color(.36f,.45f,.48f));
            var core=Visuals.Material(warden?new Color(1,.48f,.12f):new Color(.25f,.85f,.72f));
            Visuals.Shape(PrimitiveType.Capsule,model,new Vector3(0,warden?1.3f:.8f,0),
                warden?new Vector3(1.5f,1.3f,1.5f):new Vector3(.65f,.65f,.65f),shell);
            Visuals.Shape(PrimitiveType.Cube,model,new Vector3(0,1.15f,-.3f),
                new Vector3(.43f,.12f,.1f),core);
            var hp=Visuals.Shape(PrimitiveType.Cube,model,new Vector3(0,warden?2.8f:1.65f,0),
                new Vector3(.9f,.06f,.1f),core);bar=hp.transform;
            if(warden)
            {
                foreach(var side in new[]{-1,1})Visuals.Shape(PrimitiveType.Cube,model,
                    new Vector3(side*.85f,.9f,0),new Vector3(.6f,.8f,.9f),shell);
                telegraph=Visuals.Line(transform,Visuals.Material(new Color(1,.2f,.15f)),.08f);
                telegraph.enabled=false;
            }
            session.Enemies.Add(this);
        }
        void Update()
        {
            if(Session==null||Session.Paused)return;
            var dt=Time.deltaTime;
            if(!Alive)
            {
                respawnTimer-=dt;
                if(respawnTimer<=0&&Vector3.Distance(transform.position,Session.Player.transform.position)>14)
                { Alive=true;Health=maximum;controller.enabled=true;model.gameObject.SetActive(true); }
                return;
            }
            bar.localScale=new Vector3(.9f*Mathf.Clamp01(Health/maximum),.06f,.1f);
            var player=Session.Player.transform.position;
            var distance=Vector3.Distance(transform.position,player);
            if(Pulled)
            {
                if(!WorldBuilder.ClearLine(transform.position,player)){pullUntil=0;return;}
                var away=(transform.position-player).normalized;away.y=0;
                var target=player+away*1.2f;
                controller.Move(Vector3.ClampMagnitude(target-transform.position,12*dt)+Vector3.down*4*dt);
                return;
            }
            if(warning>0)
            {
                warning-=dt;
                if(warning<=0)
                {
                    telegraph.enabled=false;
                    if(Vector3.Distance(player,stompPoint)<3)Session.Player.Hurt(15);
                    Session.Ring(stompPoint,3,new Color(1,.35f,.15f));
                }
                return;
            }
            attackTimer-=dt;wanderTimer-=dt;
            var safe=WorldBuilder.IsSafe(player);
            var leashed=Vector3.Distance(transform.position,home)>15;
            if(leashed||safe)goal=home;
            else if(distance<7)
            {
                if(Zone==0&&!Warden)goal=transform.position+(transform.position-player).normalized*2;
                else goal=player;
                if(Zone>0&&attackTimer<=0&&distance<(Warden?4:1.5f)&&WorldBuilder.ClearLine(transform.position,player))
                {
                    attackTimer=Warden?3:1.5f;
                    if(Warden)
                    { warning=.85f;stompPoint=player;DrawWarning(); }
                    else Session.Player.Hurt(4+Zone*2);
                }
            }
            else if(wanderTimer<=0)
            {
                wanderTimer=2+Random.value*2;
                goal=home+new Vector3(Random.Range(-2.5f,2.5f),0,Random.Range(-2.5f,2.5f));
            }
            if(WorldBuilder.IsSafe(goal))goal=home;
            var motion=goal-transform.position;motion.y=0;
            if(motion.magnitude>.25f)
                controller.Move(motion.normalized*(Warden?1.4f:2.1f)*dt+Vector3.down*4*dt);
            if(leashed&&Vector3.Distance(transform.position,home)<.5f)Health=maximum;
        }
        public void Pull()
        {
            if(!Alive)return;
            warning=0;if(telegraph!=null)telegraph.enabled=false;
            pullUntil=Time.time+.65f;
        }
        public void Damage(float amount)
        {
            if(!Alive||amount<=0)return;
            Health=Mathf.Max(0,Health-amount);
            if(Health>0)return;
            if(!Session.Collect(this)){Health=1;return;}
            Alive=false;pullUntil=0;warning=0;
            if(telegraph!=null)telegraph.enabled=false;
            controller.enabled=false;model.gameObject.SetActive(false);
            respawnTimer=Warden?90:25;
        }
        void DrawWarning()
        {
            telegraph.enabled=true;telegraph.positionCount=49;
            for(var i=0;i<=48;i++)
            {var a=i/48f*Mathf.PI*2;telegraph.SetPosition(i,stompPoint+new Vector3(Mathf.Cos(a)*3,.08f,Mathf.Sin(a)*3));}
        }
        void OnDestroy(){if(Session!=null)Session.Enemies.Remove(this);}
    }
}
