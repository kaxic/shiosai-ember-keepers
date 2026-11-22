using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static EmberMesh;

/// <summary>World, actors and telegraphs consume the combat snapshot. No gameplay uses render objects.</summary>
public sealed class EmberView : MonoBehaviour
{
    sealed class Figure
    {
        public Transform root, pivot, health, remaining;
        public Transform[] limbs=new Transform[5];
        public MeshRenderer[] paint=new MeshRenderer[5];
        public int kind;
        public float stride, motion;
        public Vector2 previous;
        public bool seen;
    }
    sealed class Warning
    {
        public Transform root;
        public Mesh mesh;
        public MeshRenderer renderer;
        public bool seen;
    }
    readonly Dictionary<int,Figure> foes=new Dictionary<int,Figure>();
    readonly Dictionary<int,Warning> warnings=new Dictionary<int,Warning>();
    readonly Dictionary<string,Mesh> library=new Dictionary<string,Mesh>();
    readonly List<int> expired=new List<int>();
    readonly List<Vector3> firePoints=new List<Vector3>();
    readonly List<Transform> sparks=new List<Transform>();
    MaterialPropertyBlock mpb;
    Material carved, veil;
    Transform world, decor, playerSlash, pulse, dash, restSites;
    int restState=-2;
    Mesh stageMesh;
    Figure hero;
    Camera lens;
    Light key;
    string current;
    float clock;

    public void Render(EmberGame game)
    {
        if(!world) Setup();
        string stage=game.IsHub?"keep":game.District+"/"+game.Room+"/"+game.Cycle;
        if(stage!=current)
        {
            current=stage;
            Rebuild(game.IsHub,game.District,game.Room);
        }
        Sanctum(game.Mode==EmberMode.Sanctum?game.RestChoice:-1);
        clock+=Mathf.Min(Time.unscaledDeltaTime,.055f);
        FitCamera();
        Animate(hero,game.Hero,game.Facing,game.Motion,game.Swing,game.Dodge,game.Hurt,0);
        foreach(var item in foes.Values)item.seen=false;
        foreach(var data in game.Enemies)
        {
            if(!foes.TryGetValue(data.id,out var actor))
            {
                actor=NewFigure(data.kind,game.District);actor.previous=data.pos;foes.Add(data.id,actor);
            }
            bool moving=(data.pos-actor.previous).sqrMagnitude>.000001f;
            actor.motion=Mathf.MoveTowards(actor.motion,moving?1:0,Time.unscaledDeltaTime*8);
            actor.seen=true;
            float wind=data.wind>0?Mathf.Clamp01(1-data.wind):.0f;
            Animate(actor,data.pos,data.face,actor.motion,0,0,data.hurt,wind);
            float hp=data.maxHp>0?(float)data.hp/data.maxHp:0;
            actor.health.gameObject.SetActive((hp<.999f || data.kind==4)&&hp>0);
            actor.health.rotation=lens.transform.rotation;
            actor.remaining.localScale=new Vector3(Mathf.Clamp01(hp),1,1);
        }
        expired.Clear();foreach(var pair in foes)if(!pair.Value.seen)expired.Add(pair.Key);
        foreach(int id in expired){Discard(foes[id].root.gameObject);foes.Remove(id);}
        UpdateWarnings(game.Dangers);
        UpdateHeroEffects(game);
        for(int i=0;i<sparks.Count;i++)
        {
            float age=Mathf.Repeat(clock*(.53f+.065f*(i%4))+i*.376f,1);
            Vector3 hearth=firePoints[i/3];
            sparks[i].localPosition=hearth+At(Mathf.Sin(i*6.38f+age*4)*.15f,.14f+age*.77f,Mathf.Cos(age*4+i)*.085f);
            sparks[i].localScale=Vector3.one*(.025f+.025f*Mathf.Sin(age*Mathf.PI));
        }
    }
    static void Discard(UnityEngine.Object item)
    {
        if(!item)return;
        if(Application.isPlaying)Destroy(item);else DestroyImmediate(item);
    }
    Transform Group(string label,Transform parent)
    {
        var go=new GameObject(label);go.transform.SetParent(parent,false);return go.transform;
    }
    Mesh Cached(string label,Action<EmberMesh> draw)
    {
        if(library.TryGetValue(label,out var existing))return existing;
        var pencil=new EmberMesh();draw(pencil);
        var mesh=pencil.Build(label);library.Add(label,mesh);return mesh;
    }
    MeshRenderer Draw(string label,Transform parent,Mesh mesh,Material surface)
    {
        var item=Group(label,parent);
        item.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=item.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=surface;
        renderer.lightProbeUsage=LightProbeUsage.Off;
        renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        if(surface==veil)
        {
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        return renderer;
    }
    void Setup()
    {
        mpb=new MaterialPropertyBlock();
        world=Group("The lantern keeper's halls",transform);
        var artShader=Resources.Load<Shader>("Ember/CarvedLit");
        var fxShader=Resources.Load<Shader>("Ember/Veil");
        carved=new Material(artShader){name="Carved stone, bronze and timber"};veil=new Material(fxShader){name="Light and shadow wash"};
        string[] names={"Flagstones","Slate","Verdigris","Furnace","Damask","Oak"};
        string[] slots={"_Stone","_Slate","_Jade","_Lava","_Gold","_Wood"};
        carved.SetTexture("_TileDetail",Resources.Load<Texture2D>("Ember/Tesserae"));
        for(int i=0;i<names.Length;i++)carved.SetTexture(slots[i],Resources.Load<Texture2D>("Ember/"+names[i]));
        lens=Camera.main;
        if(!lens)
        {
            var cameraObject=Group("Hall camera",world).gameObject;cameraObject.tag="MainCamera";lens=cameraObject.AddComponent<Camera>();
        }
        if(!FindAnyObjectByType<AudioListener>())lens.gameObject.AddComponent<AudioListener>();
        lens.orthographic=true;lens.clearFlags=CameraClearFlags.SolidColor;
        lens.nearClipPlane=.15f;lens.farClipPlane=110;lens.allowHDR=false;
        key=Group("Light through the vault",world).gameObject.AddComponent<Light>();
        key.type=LightType.Directional;key.color=new Color(1,.87f,.73f);key.intensity=1.1f;
        key.transform.rotation=Quaternion.LookRotation(new Vector3(.53f,-.80f,.36f));
        key.shadows=LightShadows.Soft;key.shadowStrength=.77f;key.shadowNormalBias=.16f;
        hero=NewFigure(-1);
        playerSlash=Effect("sword sweep",m=>{m.Ink("F8DAA0",Finish.Plain,1);m.Ring(Vector3.zero,1.00f,1.10f,22,-67,67);m.Ring(Vector3.up*.014f,.94f,.966f,16,-36,49);});
        pulse=Effect("hearth pulse",m=>{m.Ink("FFCB72",Finish.Plain,1);m.Ring(Vector3.zero,.93f,1,52);m.Ring(Vector3.up*.007f,.82f,.84f,45);});
        dash=Effect("swift step",m=>{m.Ink("A9E1E1",Finish.Plain,.95f);for(int j=0;j<3;j++){float x=(j-1)*.18f;m.Tri(At(x-.025f,0,-.17f),At(x+.025f,0,-.17f),At(x*.8f,0,-.74f-j*.11f),Vector3.up);}});
    }
    Transform Effect(string label,Action<EmberMesh> shape)
    {
        var fx=Draw(label,world,Cached(label,shape),carved);fx.shadowCastingMode=ShadowCastingMode.Off;return fx.transform;
    }
    void FitCamera()
    {
        float aspect=Mathf.Max(.65f,lens.aspect);
        lens.orthographicSize=Mathf.Max(6.43f,11.60f/aspect);
        var focus=At(0,.52f,.39f);
        lens.transform.SetPositionAndRotation(focus+At(0,17.25f,-18.9f),Quaternion.LookRotation(At(0,-17.25f,18.9f)));
    }
    void Rebuild(bool hub,int district,int room)
    {
        if(decor){decor.gameObject.SetActive(false);Discard(decor.gameObject);}
        if(stageMesh)Discard(stageMesh);
        foreach(var entry in foes.Values){entry.root.gameObject.SetActive(false);Discard(entry.root.gameObject);}foes.Clear();
        foreach(var item in warnings.Values){Discard(item.mesh);Discard(item.root.gameObject);}warnings.Clear();
        decor=Group(hub?"Lamplighter keep":"Beneath the keep",world);
        restSites=null;restState=-2;
        sparks.Clear();firePoints.Clear();
        var layout=new EmberMesh();new EmberStage(layout,firePoints,hub,district,room).Build();
        stageMesh=layout.Build(hub?"Workshop and gate":"District "+(district+1));
        Draw("Carved hall",decor,stageMesh,carved);
        Mesh spark=Cached("floating coal",m=>{m.Ink("FFD189",Finish.Plain,1);m.Jewel(Vector3.zero,At(.82f,1.4f,.82f),5);});
        for(int i=0;i<firePoints.Count*3;i++)
        {
            var mote=Draw("Ember",decor,spark,carved);mote.shadowCastingMode=ShadowCastingMode.Off;sparks.Add(mote.transform);
        }
        ColorUtility.TryParseHtmlString("#"+(hub?"26363D":new[]{"29323C","233932","1D3449","39292E","25484F","533B21","3B2F4A","233143"}[Mathf.Clamp(district,0,7)]),out var backdrop);
        lens.backgroundColor=backdrop;
    }
    void Sanctum(int choice)
    {
        if(choice==restState)return;
        restState=choice;
        if(restSites){restSites.gameObject.SetActive(false);Discard(restSites.gameObject);restSites=null;}
        if(choice<0)return;
        restSites=Group("Rest chamber",decor);
        void station(string label,Vector3 p,Action<EmberMesh> form,float radius)
        {
            var shape=Draw(label,restSites,Cached(label,form),carved);shape.transform.localPosition=p;
            var shadow=Draw("Grounding",restSites,Cached("soft contact",Shadow),veil);shadow.sortingOrder=-4;
            shadow.transform.localPosition=p+Vector3.up*.051f;shadow.transform.localScale=At(radius,1,radius*.81f);
        }
        station("Healing font "+(choice==0),At(-4,0,1.1f),m=>EmberStations.Altar(m,choice==0),1.13f);
        station("Relic coffer "+choice,At(4,0,1.1f),m=>EmberStations.Coffer(m,choice),1.07f);
        station("Processional gate",At(0,0,4.25f),EmberStations.Exit,1.38f);
    }
    Figure NewFigure(int kind,int district=0)
    {
        int design=kind;
        kind=EmberFigures.Skeleton(design);
        var f=new Figure {kind=kind};f.root=Group(kind<0?"灯守":"回廊の守り手",world);f.pivot=Group("Pose",f.root);
        float r=EmberFigures.Footprint(kind);
        Mesh shade=Cached("soft contact",Shadow);
        var ground=Draw("Contact shadow",f.root,shade,veil);
        ground.transform.localPosition=At(-.035f,.050f,.035f);
        ground.transform.localScale=At(r*1.34f,1,r*(kind==1?1.60f:1.10f));ground.sortingOrder=-4;
        for(int part=0;part<5;part++)
        {
            if(kind==2&&part>0)continue;
            int piece=part;
            Mesh shape=Cached("actor "+design+"/"+(design==4?district:0)+"/"+part,m=>{EmberFigures.Build(m,kind,piece);if(piece==0)EmberFigures.Insignia(m,design,district);});
            var renderer=Draw("Articulated piece "+part,f.pivot,shape,carved);
            f.paint[part]=renderer;f.limbs[part]=renderer.transform;
            renderer.transform.localPosition=EmberFigures.Joint(kind,part);
        }
        if(kind>=0)
        {
            f.health=Group("Vitality",f.root);f.health.localPosition=Vector3.up*(EmberFigures.Height(kind)+.17f);
            float width=kind==4?1.75f:.86f;
            var back=Draw("Charcoal",f.health,Cached("health backing",m=>{m.Ink("272831",Finish.Plain,1);m.Block(0,0,0,1.03f,.083f,.01f);}),carved);
            back.transform.localScale=new Vector3(width,1,1);back.shadowCastingMode=ShadowCastingMode.Off;
            f.remaining=Group("Amount",f.health);f.remaining.localPosition=Vector3.left*width*.5f;
            var bar=Draw("Fire",f.remaining,Cached("health pigment",m=>{m.Ink("E89A75",Finish.Plain,1);m.Block(.5f,0,-.012f,1,.040f,.009f);}),carved);
            bar.transform.localScale=At(width,1,1);bar.shadowCastingMode=ShadowCastingMode.Off;
        }
        return f;
    }
    // Concentric alpha bands, interpolated in surface space, preserve the stone grain below feet.
    static void Shadow(EmberMesh m)
    {
        int sectors=28;float[] ring={0,.32f,.62f,1};string[] tint={"15202ABF","15202A90","15202A4A","15202A00"};
        for(int band=0;band<ring.Length-1;band++)
        for(int i=0;i<sectors;i++)
        {
            float a=i*Mathf.PI*2/sectors,b=(i+1)*Mathf.PI*2/sectors;
            Vector3 x=At(Mathf.Cos(a),0,Mathf.Sin(a)),y=At(Mathf.Cos(b),0,Mathf.Sin(b));
            m.GradientTri(x*ring[band],x*ring[band+1],y*ring[band+1],tint[band],tint[band+1],tint[band+1]);
            m.GradientTri(x*ring[band],y*ring[band+1],y*ring[band],tint[band],tint[band+1],tint[band]);
        }
    }
    void Animate(Figure f,Vector2 position,Vector2 facing,float movement,float swing,float dodge,float hurt,float wind)
    {
        float stride=(f.previous-position).magnitude;
        f.stride+=Mathf.Min(.33f,stride)*6.9f;f.previous=position;
        f.root.position=At(position.x,0,position.y);
        if(facing.sqrMagnitude>.001f)f.pivot.localRotation=Quaternion.LookRotation(At(facing.x,0,facing.y));
        float gait=Mathf.Sin(f.stride)*movement;
        float strike=swing>0?Mathf.Sin((1-Mathf.Clamp01(swing))*Mathf.PI):0;
        float y=f.kind==2?.13f*Mathf.Sin(clock*2.6f+position.x):Mathf.Abs(gait)*.065f;
        f.pivot.localPosition=At(0,y-wind*.10f-dodge*.14f,0);
        if(f.kind==2)f.limbs[0].localRotation=Quaternion.Euler(6*Mathf.Sin(clock*2.4f),clock*22,4*Mathf.Cos(clock*2.1f));
        else
        {
            f.limbs[0].localRotation=Quaternion.Euler(dodge*26-wind*9,strike*17,Mathf.Sin(clock*17)*hurt*6);
            for(int p=1;p<5;p++)
            {
                float sign=(p==1||p==4)?1:-1;
                float angle=gait*(p<=2?23:30)*sign-wind*(p==1?48:7);
                float yaw=0;
                if(p==1&&swing>0){angle-=strike*43;yaw=(Mathf.Clamp01(1-swing)*2-1)*67;}
                f.limbs[p].localRotation=Quaternion.Euler(angle+dodge*14,yaw,sign*dodge*9);
            }
        }
        mpb.Clear();mpb.SetFloat("_Hit",Mathf.Clamp01(hurt)*.92f);
        foreach(var surface in f.paint)if(surface)surface.SetPropertyBlock(mpb);
    }
    void UpdateHeroEffects(EmberGame g)
    {
        playerSlash.gameObject.SetActive(g.Swing>0);
        pulse.gameObject.SetActive(g.Pulse>0);
        dash.gameObject.SetActive(g.Dodge>0);
        var spot=At(g.Hero.x,.087f,g.Hero.y);
        float face=Mathf.Atan2(g.Facing.x,g.Facing.y)*Mathf.Rad2Deg;
        if(g.Swing>0)
        {
            playerSlash.SetPositionAndRotation(spot+Vector3.up*.48f,Quaternion.Euler(-8,face+(1-g.Swing)*55-27,0));
            playerSlash.localScale=Vector3.one*(.75f+.57f*(1-g.Swing));
        }
        if(g.Pulse>0){pulse.position=spot;pulse.localScale=Vector3.one*Mathf.Lerp(.61f,3.25f,1-g.Pulse);}
        if(g.Dodge>0){dash.SetPositionAndRotation(spot,Quaternion.Euler(0,face,0));dash.localScale=Vector3.one*(.5f+g.Dodge);}
    }
    void UpdateWarnings(List<EmberDanger> dangers)
    {
        foreach(var w in warnings.Values)w.seen=false;
        foreach(var hit in dangers)
        {
            if(!warnings.TryGetValue(hit.id,out var shape))
            {
                var outline=new EmberMesh();
                var delta=hit.to-hit.from;
                Telegraph(outline,Mathf.Max(.1f,hit.radius),delta.magnitude);
                var mesh=outline.Build("Warning footprint");
                var renderer=Draw("Incoming attack",world,mesh,veil);renderer.sortingOrder=4;
                shape=new Warning{root=renderer.transform,mesh=mesh,renderer=renderer};warnings.Add(hit.id,shape);
            }
            shape.seen=true;Vector2 direction=hit.to-hit.from;
            shape.root.SetPositionAndRotation(At(hit.from.x,.095f,hit.from.y),
                Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg,0));
            float strength=.74f+.25f*Mathf.Clamp01(hit.progress);
            mpb.Clear();mpb.SetColor("_Tint",new Color(1,.76f+.20f*hit.progress,.73f,strength));shape.renderer.SetPropertyBlock(mpb);
        }
        expired.Clear();foreach(var pair in warnings)if(!pair.Value.seen)expired.Add(pair.Key);
        foreach(int id in expired){var w=warnings[id];w.root.gameObject.SetActive(false);Discard(w.root.gameObject);Discard(w.mesh);warnings.Remove(id);}
    }
    static void Telegraph(EmberMesh m,float radius,float length)
    {
        const int half=24;
        var border=new Vector3[(half+1)*2];
        for(int i=0;i<=half;i++)
        {
            float angle=i*Mathf.PI/half;
            border[i]=At(Mathf.Cos(angle)*radius,0,length+Mathf.Sin(angle)*radius);
            border[i+half+1]=At(-Mathf.Cos(angle)*radius,0,-Mathf.Sin(angle)*radius);
        }
        var center=At(0,0,length*.5f);
        for(int i=0;i<border.Length;i++)
        {
            var a=border[i];var b=border[(i+1)%border.Length];
            m.Ink("FA5E4A53");m.Tri(center,a,b,Vector3.up);
            Vector3 inward(Vector3 v){var pivot=At(0,0,Mathf.Clamp(v.z,0,length));return v+(pivot-v).normalized*Mathf.Min(.095f,radius*.15f);}
            m.Ink("FFB069F5");m.Quad(a+Vector3.up*.003f,b+Vector3.up*.003f,inward(b)+Vector3.up*.003f,inward(a)+Vector3.up*.003f,Vector3.up);
        }
        if(length>.55f)
        {
            m.Ink("FFD9AFF0");
            for(float z=.05f;z<length;z+=1.0f)m.Tri(At(-.14f,.008f,z),At(.14f,.008f,z),At(0,.008f,z+.24f),Vector3.up);
        }
    }
    void OnDestroy()
    {
        if(stageMesh)Discard(stageMesh);
        foreach(var pair in warnings)Discard(pair.Value.mesh);
        foreach(var mesh in library.Values)Discard(mesh);
        Discard(carved);Discard(veil);
    }
}
