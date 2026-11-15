using UnityEngine;
using UnityEngine.InputSystem;

namespace ParasiteEvolution
{
    public sealed class SlimeActor : MonoBehaviour
    {
        public GameSession Session;
        public SlimeVisual Visual { get; private set; }
        public float Health;
        public float PullRemaining, MoveRemaining, DamageRemaining;
        public Vector3 Aim { get; private set; } = Vector3.forward;
        public bool Dashing { get; private set; }
        CharacterController controller;
        Vector3 dashGoal;
        float dashTimeout;
        public void Initialize(GameSession session)
        {
            Session=session;gameObject.layer=9;
            controller=gameObject.AddComponent<CharacterController>();
            controller.height=.85f;controller.center=new Vector3(0,.45f,0);controller.radius=.55f;
            controller.skinWidth=.04f;controller.stepOffset=.15f;
            Visual=gameObject.AddComponent<SlimeVisual>();
            var p=session.Profile;Health=p.health;
            PullRemaining=p.pullRemaining;MoveRemaining=p.moveRemaining;DamageRemaining=p.damageRemaining;
            Teleport(new Vector3(p.x,0,p.z));
        }
        void Update()
        {
            if(Session==null||Session.Paused)return;
            var dt=Time.deltaTime;
            PullRemaining=Mathf.Max(0,PullRemaining-dt);MoveRemaining=Mathf.Max(0,MoveRemaining-dt);
            DamageRemaining=Mathf.Max(0,DamageRemaining-dt);
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.digit1Key.wasPressedThisFrame)Session.Cast("AB-PULL");
                if(keyboard.digit2Key.wasPressedThisFrame)Session.Cast("AB-MOVE");
                if(keyboard.digit3Key.wasPressedThisFrame)Session.Cast("AB-DAMAGE");
                if(keyboard.escapeKey.wasPressedThisFrame)Session.TogglePause();
            }
            if(Session.Paused)return;
            Vector3 delta;
            if(Dashing)
            {
                dashTimeout-=dt;
                delta=Vector3.MoveTowards(transform.position,dashGoal,18*dt)-transform.position;
                Visual.AbilityAnchor=dashGoal+Vector3.up*.2f;
                if(dashTimeout<=0||Vector3.Distance(transform.position,dashGoal)<.2f)EndDash();
            }
            else
            {
                var input=Session.Hud.Move;
                if(keyboard!=null)input+=new Vector2((keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0),
                    (keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0));
                input=Vector2.ClampMagnitude(input,1);
                var direction=new Vector3(input.x,0,input.y);
                if(direction.sqrMagnitude>.01f)Aim=direction.normalized;
                var stride=1-.12f*Mathf.Cos(Time.time*11);
                delta=direction*Balance.Speed(Session.Profile)*stride*dt;
            }
            var prior=transform.position;
            controller.Move(delta+Vector3.down*4*dt);
            if(Dashing&&delta.sqrMagnitude>.0001f&&(transform.position-prior).sqrMagnitude<.00001f)EndDash();
            Health=Mathf.Min(Health,Balance.MaxHealth(Session.Profile));
        }
        public bool StartDash()
        {
            var distance=9f;
            if(Physics.SphereCast(transform.position+Vector3.up*.55f,.57f,Aim,out var hit,distance,
                WorldBuilder.WallMask,QueryTriggerInteraction.Ignore))distance=Mathf.Max(0,hit.distance-.15f);
            if(distance<1)return false;
            var goal=transform.position+Aim*distance;
            if(!Physics.Raycast(goal+Vector3.up*3,Vector3.down,5,WorldBuilder.EnvironmentMask))return false;
            dashGoal=new Vector3(goal.x,transform.position.y,goal.z);dashTimeout=.8f;Dashing=true;
            return true;
        }
        void EndDash(){Dashing=false;Visual.AbilityAnchor=null;}
        public void Teleport(Vector3 point)
        {
            if(controller!=null)controller.enabled=false;
            transform.position=point;
            if(controller!=null)controller.enabled=true;
            Dashing=false;if(Visual!=null)Visual.ResetContacts();
        }
        public void Hurt(float damage)
        {
            if(Session.Paused||WorldBuilder.IsSafe(transform.position)||damage<=0)return;
            Health=Mathf.Max(0,Health-damage);
            if(Health<=0)Session.Die();
        }
    }
}
