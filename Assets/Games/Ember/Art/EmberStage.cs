using System.Collections.Generic;
using UnityEngine;
using static EmberMesh;
using static EmberMesh.Finish;

internal sealed class EmberStage
{
    readonly EmberMesh m;
    readonly List<Vector3> fires;
    readonly int district, room;
    readonly bool hub;
    readonly string stone, highlight, inlay, signal;
    readonly Finish masonry;
    const string Gold="BA9559", Brass="927449", Iron="384653", OakBrown="755540";
    internal EmberStage(EmberMesh m,List<Vector3> fires,bool hub,int district,int room)
    {
        this.m=m;this.fires=fires;this.hub=hub;this.district=Mathf.Clamp(district,0,7);this.room=room;
        stone=hub?"707270":new[]{"686971","52776A","536F84","694F52","4C8586","AB7846","6F547C","596781"}[this.district];
        highlight=hub?"A8A089":new[]{"A6A098","A4B395","88ABBE","AF8B71","D5CDB3","E5BD70","CEB6D7","DBC797"}[this.district];
        inlay=hub?"AB8D5F":new[]{"A58E67","A1A57A","8BA6B4","BA8759","89CBBE","E9B351","B998DB","D7B875"}[this.district];
        signal=hub?"83DAD4":new[]{"F2B05C","9DDBA1","80D5EF","FFAB62","AEF2DC","FFD475","E8ABFE","FFE6AA"}[this.district];
        masonry=hub?Flagstone:new[]{Slate,Marble,Slate,Basalt,Marble,Oak,Marble,Slate}[this.district];
    }
    void Ink(string c,Finish f=Plain,float glow=0)=>m.Ink(c,f,glow);
    void Block(float x,float y,float z,float w,float h,float d,float rot=0)=>m.Block(x,y,z,w,h,d,rot);
    void Drum(float x,float y,float z,float r,float s,float h,int n=10)=>m.Drum(x,y,z,r,s,h,n);
    internal void Build()
    {
        Ink("25303A",Slate);Block(0,-.79f,0,20.2f,1.30f,12.6f);
        Ink(stone,masonry);Block(0,-.12f,0,19.72f,.24f,12.20f);
        Ink(hub?"887966":new[]{"74808A","6C8A77","697F94","896D66","82ABAB","BF956B","9686A5","7D87A1"}[district],hub?Flagstone:masonry);
        Block(0,-.047f,0,18.26f,.112f,11.18f);
        // Narrow border just outside the playable rectangle; maintain a visually quiet arena centre.
        Ink(inlay,Engraved);
        foreach(float x in new[]{-9.24f,9.24f})Block(x,.008f,0,.14f,.030f,11.37f);
        foreach(float z in new[]{-5.67f,5.67f})Block(0,.008f,z,18.63f,.030f,.12f);
        for(int i=0;i<17;i++){float x=(i-8)*1.15f;Ink(i%4==0?inlay:stone,Slate);Block(x,-.23f,-6.03f,1.13f,.42f,.31f);}
        for(int i=0;i<11;i++)
        {
            float z=(i-5)*1.1f;
            foreach(float x in new[]{-9.72f,9.72f})
            {
                Ink(i%2==0?stone:highlight,masonry);Block(x,.10f,z,.43f,.27f,1.07f);
                if(i%4==0){Ink(inlay,Engraved);Block(x,.26f,z,.45f,.065f,.27f);}
            }
        }
        FloorEmblem();
        if(!hub && district>=4)for(int j=0;j<3;j++)
        {
            var point=EmberJourney.VentSite(district,room,j);
            Vector3 p=At(point.x,.042f,point.y);
            Ink(inlay,Engraved);m.Ring(p,.42f,.52f,12);m.Ring(p,.73f,.75f,24);
            Ink(signal,Marble,.38f);
            for(int k=0;k<6;k++){float a=k*Mathf.PI/3;m.Jewel(p+At(Mathf.Cos(a)*.34f,0,Mathf.Sin(a)*.34f),At(.063f,.022f,.063f),4);}
        }
        // Lower retaining wall, broken upper silhouettes and masonry joints.
        for(int i=0;i<13;i++)
        {
            float x=(i-6)*1.65f;
            Ink(i%3==1?stone:highlight,masonry);Block(x,.34f,6.05f,1.62f,.68f,.72f);
            Ink(stone,masonry);Block(x+.18f,.86f,6.13f,1.60f,.35f,.59f);
            Ink(inlay,Engraved);Block(x,1.085f,6.09f,1.68f,.098f,.72f);
        }
        if(hub) Sanctuary();
        else
        {
            m.originOffset=Vector3.forward*1.04f;
            switch(district){case 0: Mine();break;case 1: Grove();break;case 2: Canal();break;case 3: Foundry();break;default: OuterCourt();break;}
            m.originOffset=Vector3.zero;
        }
        // Wall braziers mark arena dimensions with two unambiguous pools of fire.
        foreach(float x in new[]{-9.91f,9.91f})
        {
            Brazier(x,-2.2f,.68f);
            if(!hub && district!=2) Brazier(x,3.8f,.82f);
        }
    }
    void OuterCourt()
    {
        for(int i=-2;i<=2;i++)
        {
            float x=i*3.7f, z=5.5f;
            if(district==4)
            {
                Arch(x,z,1.36f,1.8f,.31f,.69f);
                Ink("8AE4D5",Water,.55f);Block(x,.12f,z-.30f,2.4f,.12f,1.1f);
                Ink("E6E6C7",Marble);m.Jewel(At(x,1.2f,z),At(.31f,.32f,.31f),11);
            }
            else if(district==5)
            {
                for(int row=0;row<3;row++)for(int col=0;col<2;col++)
                {
                    Vector3 center=At(x+(col-.5f)*1.25f+(row%2)*.52f,1f+row*.99f,z);
                    for(int edge=0;edge<6;edge++)
                    {
                        float a=edge*Mathf.PI/3,b=a+Mathf.PI/3;
                        Ink("CF9B51",Engraved);m.Tube(center+At(Mathf.Cos(a)*.64f,Mathf.Sin(a)*.64f,0),center+At(Mathf.Cos(b)*.64f,Mathf.Sin(b)*.64f,0),.078f,.078f,6);
                    }
                    Ink(signal,Marble,.37f);m.Jewel(center-Vector3.forward*.15f,At(.22f,.33f,.15f),6);
                }
            }
            else if(district==6)
            {
                for(int j=-1;j<=1;j++){Ink(j==0?highlight:stone,Marble);m.Jewel(At(x+j*.60f,1.2f+(.3f*(j+1)),z),At(.48f,1.52f+.33f*((i+3)%3),.64f),7,j);}
                Ink(signal,Engraved,.8f);m.Ring(At(x,.035f,z-.75f),1.05f,1.12f,7);
            }
            else
            {
                float radius=i==0?2.12f:1.06f;
                Pillar(x,z,1.65f,.75f);
                var c=At(x,2.45f,z);Ink(inlay,Engraved);
                for(int k=0;k<3;k++)for(int j=0;j<28;j++)
                {
                    var turn=Quaternion.Euler(k*37f,k*46f,17f);
                    float a=j*2*Mathf.PI/28,b=(j+1)*2*Mathf.PI/28;
                    m.Tube(c+turn*At(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),c+turn*At(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0),.049f,.049f,5);
                }
                Ink(signal,Plain,.85f);m.Jewel(c,Vector3.one*radius*.31f,11);
            }
        }
    }
    void FloorEmblem()
    {
        float radius=hub?1.81f:district==2?2.33f:2.11f;
        var center=At(0,.025f,hub?-.32f:.10f);
        Ink(inlay,Engraved);m.Ring(center,radius-.080f,radius,48);
        m.Ring(center,radius+.123f,radius+.150f,48);
        int points=hub?8:4+district*2;
        for(int j=0;j<points;j++)
        {
            float a=(j*360f/points+(hub?0:room*6f))*Mathf.Deg2Rad;
            var d=At(Mathf.Cos(a),0,Mathf.Sin(a));var tangent=Vector3.Cross(Vector3.up,d);
            float length=j%2==0?1:.79f;
            Vector3 top=center+d*radius*.85f*length, heel=center+d*radius*.18f;
            m.Tri(heel,top,center+d*radius*.45f+tangent*.093f,Vector3.up);
        }
        m.Ring(center,.20f,.25f,12);
        for(int s=-1;s<=1;s+=2)
        {
            for(int j=0;j<4;j++)
            {
                float x=s*(3.22f+j*1.44f);Ink(inlay,Engraved);
                Block(x,.016f,-4.93f,.70f,.016f,.052f);Block(x,.016f,4.93f,.70f,.016f,.052f);
            }
        }
    }
    void Pillar(float x,float z,float h,float width)
    {
        Ink(stone,masonry);Drum(x,0,z,width*.77f,width*.75f,.25f,8);
        Ink(highlight,masonry);Drum(x,.25f,z,width*.70f,width*.46f,.29f,8);
        Ink(stone,masonry);Drum(x,.54f,z,width*.46f,width*.39f,h-.98f,8);
        Ink(inlay,Engraved);Drum(x,.61f,z,width*.50f,width*.49f,.105f,8);
        Drum(x,h-.52f,z,width*.46f,width*.49f,.13f,8);
        Ink(highlight,masonry);Drum(x,h-.4f,z,width*.46f,width*.70f,.30f,8);
        Ink(stone,masonry);Block(x,h-.05f,z,width*1.44f,.21f,width*1.44f);
    }
    void Arch(float x,float z,float radius,float spring,float thick,float depth)
    {
        Pillar(x-radius-thick*.5f,z,spring,thick*1.56f);Pillar(x+radius+thick*.5f,z,spring,thick*1.56f);
        int count=13;
        for(int i=0;i<count;i++)
        {
            Ink(i==6?inlay:i%3==0?highlight:stone,i==6?Engraved:masonry);
            m.ArchStone(At(x,spring,z),radius,radius+thick+(i==6?.13f:0),depth+(i==6?.15f:0),i*180f/count+.28f,(i+1)*180f/count-.28f);
        }
    }
    void HangingCloth(float x,float y,float z,string color)
    {
        Ink(Brass);m.Tube(At(x-.38f,y,z),At(x+.38f,y,z),.041f,.041f,8);
        Ink(color);Block(x,y-.56f,z,.61f,1.16f,.055f);
        m.Tri(At(x-.31f,y-1.12f,z-.028f),At(x+.31f,y-1.12f,z-.028f),At(x,y-1.53f,z-.028f),Vector3.back);
        Ink(Gold);Block(x,y-.57f,z-.04f,.041f,.65f,.025f);
        m.Box(At(x,y-.51f,z-.06f),At(.19f,.19f,.03f),Quaternion.Euler(0,0,45));
    }
    void Brazier(float x,float z,float y)
    {
        Ink(Iron,Slate);Drum(x,0,z,.42f,.32f,.20f,8);Drum(x,.20f,z,.22f,.17f,y-.16f,8);
        Ink(Brass,Engraved);Drum(x,y,z,.19f,.43f,.16f,9);
        Ink("E27B4F",Plain,.74f);Drum(x,y+.155f,z,.355f,.32f,.028f,9);
        Flame(At(x,y+.18f,z),.54f);
    }
    void Flame(Vector3 p,float height)
    {
        for(int j=0;j<5;j++)
        {
            float a=j*2.399f,x=Mathf.Cos(a)*height*.15f,z=Mathf.Sin(a)*height*.15f;
            Ink(j%2==0?"F9A74E":"ED7544",Plain,.96f);
            m.Jewel(p+At(x,height*(.30f+.085f*(j%3)),z),At(height*.14f,height*(.30f+.08f*(j%2)),height*.14f),5,j);
        }
        Ink("FFDD87",Plain,1);m.Jewel(p+At(0,height*.30f,0),At(height*.17f,height*.33f,height*.16f),7);
        fires.Add(p+m.originOffset+Vector3.up*height*.42f);
    }
    void Sanctuary()
    {
        foreach(float x in new[]{-7.3f,-3.65f,0,3.65f,7.3f})Arch(x,6.31f,1.30f,2.16f,.38f,.77f);
        HangingCloth(-3.72f,3.04f,5.84f,"7E3F44");HangingCloth(3.72f,3.04f,5.84f,"7E3F44");
        // Hearth with a glowing open mouth, an oversailing copper hood and a horned anvil.
        float fx=-5.5f,fz=1.90f;
        Ink("5A514A",Flagstone);Block(fx,.30f,fz,2.34f,.60f,1.54f);
        foreach(float sign in new[]{-1f,1f})
        {
            Ink("786957",Slate);Block(fx+sign*.91f,1.0f,fz,.40f,.79f,1.46f);
            Ink(Brass,Engraved);Block(fx+sign*.91f,1.41f,fz,.53f,.11f,1.56f);
        }
        Ink("392F2F",Basalt);Block(fx,1.03f,fz+.54f,1.65f,1f,.26f);
        Ink("AD5D3F",Basalt);Block(fx,1.56f,fz,2.37f,.25f,1.64f);
        Ink("786759",Engraved);m.Drum(fx,1.67f,fz+.12f,1.0f,.50f,.60f,4);
        Ink(stone,Slate);Block(fx,2.68f,fz+.12f,.72f,1.03f,.75f);
        Ink(Brass);Block(fx,3.24f,fz+.12f,.91f,.14f,.93f);
        Flame(At(fx,.62f,fz-.34f),.76f);
        Ink(OakBrown,Oak);Drum(fx+1.35f,0,fz-1.22f,.43f,.37f,.60f,9);
        Ink("515D63",Engraved);Block(fx+1.35f,.70f,fz-1.22f,.58f,.25f,.30f);
        Block(fx+1.35f,.86f,fz-1.22f,.90f,.10f,.34f);
        m.Tube(At(fx+1.76f,.84f,fz-1.22f),At(fx+2.13f,.86f,fz-1.22f),.16f,.015f,6);
        Ink("B18350",Oak);Block(-7.56f,.40f,2.85f,.83f,.80f,.73f);
        Block(-7.47f,1.11f,2.9f,.64f,.60f,.62f,12);
        Gate(5.5f,1.87f,true);
        foreach(float x in new[]{-2.6f,2.6f})Brazier(x,4.9f,.69f);
    }
    void Gate(float x,float z,bool portal)
    {
        Ink(stone,masonry);Drum(x,0,z,1.66f,1.54f,.20f,12);
        Ink(inlay,Engraved);m.Ring(At(x,.213f,z),1.22f,1.34f,36);
        Arch(x,z+.12f,1.14f,1.24f,.34f,.60f);
        if(!portal)return;
        Ink("294858",Marble,.22f);m.Block(x,1.14f,z+.31f,1.96f,2.08f,.08f);
        Ink("4E8388",Marble,.32f);m.Jewel(At(x,1.32f,z+.22f),At(.88f,1.03f,.106f),10);
        Ink(signal,Plain,.94f);m.ArchStone(At(x,1.31f,z+.04f),.66f,.72f,.055f,0,180);
        m.ArchStone(At(x,1.31f,z+.04f),.66f,.72f,.055f,180,360);
        m.Jewel(At(x,1.31f,z+.02f),At(.20f,.47f,.075f),6);
    }
    void Mine()
    {
        for(int i=0;i<3;i++)
        {
            float x=(i-1)*6.13f;Ink("715743",Oak);
            foreach(float s in new[]{-1f,1f})
            {
                Block(x+s*2.28f,1.48f,6.03f,.39f,3.07f,.49f);
                m.Beam(At(x+s*2.25f,1.91f,5.97f),At(x+s*1.12f,3.14f,5.98f),.26f,.34f);
                Ink("6A6E68",Engraved);Block(x+s*2.28f,.64f,6.02f,.45f,.20f,.55f);Ink("715743",Oak);
            }
            Block(x,3.11f,6.04f,5.17f,.40f,.52f);
        }
        for(int i=0;i<15;i++)
        {
            float x=-10+i*1.43f,z=7.4f+.45f*Mathf.Sin(i*1.73f);
            Ink(i%3==0?"595860":"4D5357",Slate);
            m.Jewel(At(x,.90f,z),At(1.65f,1.5f+.67f*Mathf.Sin(i*2.2f),1.12f),7,i);
        }
        for(int i=0;i<13;i++){Ink("775D47",Oak);Block(-8.5f+i*1.41f,.10f,5.30f,.20f,.14f,.98f);}
        Ink("8E9290",Engraved);Block(0,.19f,4.97f,19.1f,.09f,.069f);Block(0,.19f,5.64f,19.1f,.09f,.069f);
        float cx=room%2==0?6.6f:-6.6f;
        Ink("47484B",Slate);Block(cx,.63f,5.40f,1.38f,.66f,.68f);Ink("A38355",Engraved);Block(cx,.97f,5.4f,1.51f,.10f,.78f);
        for(int j=0;j<6;j++){Ink(j%3==0?"D89855":"5B5460",j%3==0?Engraved:Slate);m.Jewel(At(cx+(j-2.5f)*.18f,1.02f,5.40f+.12f*Mathf.Sin(j)),At(.20f,.25f,.17f),6,j);}
        Brazier(0,5.19f,1.13f);
    }
    void Grove()
    {
        for(int j=0;j<3;j++)Arch((j-1)*6.16f,6.3f,2.32f,2.24f,.48f,.83f);
        for(int j=0;j<6;j++)
        {
            float x=-9.4f+j*3.81f;float h=2.2f+.5f*Mathf.Sin(j);
            Vector3 basePoint=At(x,.10f,6.17f),tip=At(x+.43f,h+1.2f,6.03f);
            Ink("605B46",Oak);m.Tube(basePoint,tip,.35f,.20f,7);
            for(int k=0;k<4;k++)
            {
                float sign=k%2==0?-1:1;var end=tip+At(sign*(.62f+.27f*k),.03f+.19f*k,(k-1.5f)*.29f);
                Ink("655B40",Oak);m.Tube(tip-Vector3.up*.74f,end,.15f,.042f,6);
                Ink(k%2==0?"668B62":"436E5A",Marble);
                m.Jewel(end,At(.78f+.09f*k,.44f,.57f),8,k);
            }
        }
        Ink("BAB38A",Marble);Drum(0,.12f,5.43f,1.24f,.94f,.45f,8);
        Ink("598A72",Marble);Drum(0,.57f,5.43f,.76f,.96f,.22f,8);
        Ink(signal,Plain,.75f);m.Jewel(At(0,1.44f,5.43f),At(.40f,.74f,.34f),7);
        HangingCloth(-4.02f,3.13f,5.94f,"497663");HangingCloth(4.02f,3.13f,5.94f,"497663");
    }
    void Canal()
    {
        Ink("31596B",Water,.24f);Block(0,-.57f,6.05f,27.3f,.055f,7.5f);
        foreach(float x in new[]{-10.58f,10.58f})Block(x,-.58f,0,1.45f,.06f,13.4f);
        for(int i=0;i<5;i++)Arch((i-2)*3.86f,7.42f,1.38f,1.57f,.36f,.83f);
        Ink("758D9C",Slate);Block(0,3.37f,7.42f,20.20f,.31f,1.28f);
        Ink(inlay,Engraved);Block(0,3.57f,6.83f,20.4f,.09f,.13f);
        foreach(float x in new[]{-7.04f,0,7.04f})
        {
            Pillar(x,5.47f,1.17f,1.0f);Ink("558AA4",Marble,.31f);
            m.Jewel(At(x,2.08f,5.47f),At(.49f,.88f,.45f),6);
            Ink(signal,Plain,.86f);m.Jewel(At(x-.16f,2.10f,5.14f),At(.105f,.54f,.073f),5);
            Ink(Brass,Engraved);
            for(int j=0;j<4;j++){float a=j*1.57f+.78f;var d=At(Mathf.Cos(a),0,Mathf.Sin(a));m.Tube(At(x,1.15f,5.47f)+d*.42f,At(x,2.48f,5.47f)+d*.49f,.05f,.025f,6);}
        }
    }
    void Foundry()
    {
        foreach(float x in new[]{-8f,8f})
        {
            Ink("4D4449",Basalt);Drum(x,0,6.07f,1.64f,1.46f,2.24f,10);
            Ink("9B684B",Engraved);Drum(x,.46f,6.07f,1.64f,1.61f,.18f,10);Drum(x,2.26f,6.07f,1.60f,1.61f,.25f,10);
            Ink("6C4948",Basalt);Drum(x,2.51f,6.07f,1.41f,.81f,1.08f,10);
            Ink("D37D45",Plain,.96f);Block(x,1.26f,4.68f,.84f,.88f,.065f);
            for(int k=0;k<4;k++){Ink(Iron);Block(x-.37f+k*.25f,1.26f,4.62f,.074f,.98f,.09f);}
            Flame(At(x,.83f,4.54f),.47f);
        }
        Gate(0,5.76f,false);
        Ink("77583E",Engraved);m.Drum(0,1.61f,5.78f,.78f,.56f,.32f,14);
        m.Drum(0,1.93f,5.78f,.54f,.35f,.76f,14);Ink(Gold,Engraved);m.Drum(0,1.56f,5.78f,.83f,.81f,.075f,14);
        Ink("CAB286",Plain,.43f);m.Jewel(At(0,1.41f,5.78f),At(.13f,.23f,.13f),8);
        for(int j=0;j<12;j++)
        {
            float x=-6+j*1.1f;float h=2.83f-.62f*Mathf.Sin(j*Mathf.PI/11);
            Ink("987B59",Engraved);m.Tube(At(x-.6f,h+.08f,6.20f),At(x+.64f,h-.04f,6.20f),.049f,.05f,6);
        }
        HangingCloth(-3.25f,3.13f,5.88f,"8D4145"); HangingCloth(3.25f,3.13f,5.88f,"8D4145");
    }
}
