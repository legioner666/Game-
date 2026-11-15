using System.Collections.Generic;
using UnityEngine;

namespace ParasiteEvolution
{
    public sealed class SlimeVisual : MonoBehaviour
    {
        sealed class Tendril
        {
            public Mesh mesh;
            public Vector3[] vertices = new Vector3[77];
            public Vector3 anchor;
            public bool planted;
            public float extension;
            public Vector3 root;
        }
        readonly List<Tendril> supports = new List<Tendril>();
        readonly List<Tendril> working = new List<Tendril>();
        public readonly List<Vector3> WorkTargets = new List<Vector3>();
        public Vector3? AbilityAnchor;
        Mesh body;
        Vector3[] baseVertices, bodyVertices;
        Vector3 previous;
        Material material;
        float motion;

        void Awake()
        {
            material = Visuals.Material(new Color(.055f,.037f,.085f));
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.68f);
            var blob = Visuals.Shape(PrimitiveType.Sphere,transform,new Vector3(0,.35f,0),Vector3.one,material);
            body = Instantiate(blob.GetComponent<MeshFilter>().sharedMesh);
            blob.GetComponent<MeshFilter>().sharedMesh = body; body.MarkDynamic();
            baseVertices = body.vertices; bodyVertices = new Vector3[baseVertices.Length];
            for (var i = 0; i < baseVertices.Length; i++)
            {
                var p = baseVertices[i];
                var irregular = 1 + .13f*Mathf.Sin(p.x*9+p.z*3) + .1f*Mathf.Cos(p.z*11-p.y*4);
                baseVertices[i] = new Vector3(p.x*1.65f*irregular,p.y*.95f,p.z*1.35f*irregular);
            }
            // Six support channels are temporary animation tuning. They are not six permanent visible limbs.
            var roots = new[] { new Vector3(-.45f,0,.24f), new Vector3(.38f,0,.4f),
                new Vector3(-.55f,0,-.12f), new Vector3(.47f,0,-.35f),
                new Vector3(-.1f,0,.48f), new Vector3(.08f,0,-.46f) };
            foreach (var root in roots) { var t = CreateTendril(); t.root = root; supports.Add(t); }
            for (var i=0;i<3;i++) working.Add(CreateTendril());
            previous=transform.position;
        }
        Tendril CreateTendril()
        {
            var go = new GameObject("Retractile tendril");go.transform.SetParent(transform,false);
            var t = new Tendril { mesh = new Mesh { name = "Procedural contractile tendril" } };
            t.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh=t.mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            var triangles = new List<int>();
            for(var ring=0;ring<10;ring++) for(var side=0;side<6;side++)
            {
                var a=ring*7+side;var b=a+7;
                triangles.Add(a);triangles.Add(b);triangles.Add(a+1);
                triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);
            }
            t.mesh.vertices=t.vertices;t.mesh.triangles=triangles.ToArray();
            return t;
        }
        void LateUpdate()
        {
            var dt=Time.deltaTime;if(dt<=0)return;
            var velocity=(transform.position-previous)/dt;previous=transform.position;
            var speed=new Vector2(velocity.x,velocity.z).magnitude;
            motion=Mathf.Lerp(motion,Mathf.Min(1,speed/3),dt*10);
            var forward=speed>.1f?new Vector3(velocity.x,0,velocity.z).normalized:transform.forward;
            for(var i=0;i<bodyVertices.Length;i++)
            {
                var p=baseVertices[i];
                var ripple=.025f*Mathf.Sin(Time.time*3+p.x*7+p.z*5);
                bodyVertices[i]=new Vector3(p.x*(1+ripple),p.y*(1+.07f*motion*Mathf.Sin(Time.time*11))+ripple,p.z*(1-ripple));
            }
            body.vertices=bodyVertices;body.RecalculateNormals();body.RecalculateBounds();
            for(var i=0;i<supports.Count;i++)
            {
                var t=supports[i];var phase=Mathf.Repeat(Time.time*(.8f+speed*.14f)+i*.167f,1);
                var plant=motion>.05f&&phase<.30f&&AbilityAnchor==null;
                if(plant&&!t.planted)
                    t.anchor=transform.position+forward*(1.1f+.18f*(i%3))+transform.TransformVector(t.root*1.9f);
                t.planted=plant;
                t.extension=Mathf.MoveTowards(t.extension,plant?1:0,dt*7);
                var root=transform.TransformPoint(t.root+Vector3.up*.3f);
                var end=Vector3.Lerp(root,t.anchor,t.extension);end.y=Mathf.Lerp(root.y,.06f,t.extension);
                Draw(t,root,end,.16f);
            }
            for(var i=0;i<working.Count;i++)
            {
                Vector3? goal=i==0&&AbilityAnchor.HasValue?AbilityAnchor:(i<WorkTargets.Count?(Vector3?)WorkTargets[i]:null);
                var t=working[i];t.extension=Mathf.MoveTowards(t.extension,goal.HasValue?1:0,dt*9);
                var root=transform.position+new Vector3((i-1)*.18f,.38f,.1f);
                if(goal.HasValue)t.anchor=goal.Value;
                Draw(t,root,Vector3.Lerp(root,t.anchor,t.extension),.21f);
            }
        }
        void Draw(Tendril t,Vector3 start,Vector3 end,float radius)
        {
            var delta=end-start;var len=delta.magnitude;
            var axis=len>.001f?delta/len:Vector3.forward;
            var right=Vector3.Cross(axis,Vector3.up).normalized;
            if(right.sqrMagnitude<.1f)right=Vector3.right;
            var up=Vector3.Cross(right,axis).normalized;
            for(var ring=0;ring<=10;ring++)
            {
                var u=ring/10f;
                var center=Vector3.Lerp(start,end,u)+Vector3.up*(Mathf.Sin(u*Mathf.PI)*Mathf.Min(.08f,len*.04f));
                var r=radius*(1-u)*Mathf.Min(1,len/.3f);
                for(var side=0;side<=6;side++)
                {
                    var angle=side/6f*Mathf.PI*2;
                    t.vertices[ring*7+side]=transform.InverseTransformPoint(center+(right*Mathf.Cos(angle)+up*Mathf.Sin(angle))*r);
                }
            }
            t.mesh.vertices=t.vertices;t.mesh.RecalculateNormals();t.mesh.RecalculateBounds();
        }
        public void ResetContacts()
        {
            previous=transform.position;AbilityAnchor=null;WorkTargets.Clear();
            foreach(var t in supports){t.planted=false;t.extension=0;t.anchor=transform.position;}
            foreach(var t in working){t.extension=0;t.anchor=transform.position;}
        }
        void OnDestroy()
        {
            if(body!=null)Destroy(body);if(material!=null)Destroy(material);
            foreach(var t in supports)if(t.mesh!=null)Destroy(t.mesh);
            foreach(var t in working)if(t.mesh!=null)Destroy(t.mesh);
        }
    }
}
