using UnityEngine;
using static EmberMesh;
using static EmberMesh.Finish;

internal static class EmberFigures
{
    // -1 lamp keeper, 0 hollow guard, 1 cinder hound, 2 lantern wisp, 3 bronze sentinel, 4 bell warden.
    internal static float Height(int kind)
    {
        switch(kind){case 1:return .93f;case 2:return 1.50f;case 3:return 2.18f;case 4:return 2.73f;default:return 1.66f;}
    }
    internal static float Footprint(int kind)
    {return kind==4?1.17f:kind==3?.85f:kind==1?.65f:kind==2?.42f:.51f;}
    internal static Vector3 Joint(int kind,int part)
    {
        float sign=part==1||part==3?1:-1;
        if(part==0)return Vector3.zero;
        if(kind==1)return At(sign*.20f,.46f,part<=2?.31f:-.32f);
        if(kind==2)return At(sign*.20f,1.05f,0);
        float scale=kind==4?1.52f:kind==3?1.27f:1f;
        return (part<=2?At(sign*(kind>=3?.38f:.286f),1.08f,.01f):At(sign*.154f,.57f,0))*scale;
    }
    internal static void Build(EmberMesh m,int kind,int part)
    {
        if(kind==1) { Hound(m,part);return; }
        if(kind==2) { Wisp(m,part);return; }
        if(part==0){Body(m,kind);return;}
        float sign=part==1||part==3?1:-1;
        bool hero=kind==-1, heavy=kind>=3;
        float scale=kind==4?1.52f:kind==3?1.27f:1;
        Vector3 p(float x,float y,float z)=>At(x,y,z)*scale;
        void block(float x,float y,float z,float w,float h,float d)=>m.Box(p(x,y,z),p(w,h,d),Quaternion.identity);
        if(part>=3)
        {
            m.Ink(heavy?"555C67":"3F4B58",Slate);m.Tube(p(0,0,0),p(sign*.025f,-.31f,.02f),.125f*scale,.10f*scale,7);
            m.Ink(hero?"949591":heavy?"867667":"8D8080",Engraved);
            m.Jewel(p(sign*.025f,-.31f,.045f),p(.128f,.162f,.13f),7);
            m.Ink(heavy?"674E46":"564645",Oak);
            block(sign*.024f,-.485f,.06f,.212f,.16f,.31f);
            m.Ink(hero?"B79866":"997D59",Engraved);block(sign*.024f,-.447f,.10f,.213f,.031f,.25f);return;
        }
        m.Ink(hero?"4D757B":kind==0?"784C4C":"645C62",hero?Marble:Slate);
        m.Tube(p(0,0,0),p(sign*.075f,-.25f,.045f),.121f*scale,.093f*scale,8);
        m.Ink(hero?"CBB994":kind==4?"AA8B5F":"8D8A7E",Engraved);
        m.Jewel(p(sign*.038f,-.024f,0),p(heavy?.27f:.18f,heavy?.225f:.153f,heavy?.225f:.165f),7);
        m.Ink("755949",Oak);m.Tube(p(sign*.07f,-.22f,.032f),p(sign*.055f,-.39f,.113f),.083f*scale,.073f*scale,7);
        m.Ink(hero?"CBBBA2":"9F9285",Slate);m.Jewel(p(sign*.055f,-.39f,.112f),p(.095f,.113f,.102f),7);
        if(part==2)
        {
            if(hero)
            {
                m.Ink("8D744A",Engraved);m.Beam(p(-.05f,-.39f,.11f),p(-.05f,-.52f,.11f),.034f,.037f);
                Lantern(m,p(-.05f,-.62f,.12f),.85f);
            }
            else if(kind==0)
            {
                m.Ink("8F7257",Engraved);m.Jewel(p(-.025f,-.26f,.222f),p(.245f,.397f,.084f),6,0);
                m.Ink("855456",Basalt);m.Jewel(p(-.025f,-.26f,.266f),p(.19f,.327f,.064f),6);
                m.Ink("C5A57B",Engraved);block(-.025f,-.25f,.322f,.043f,.46f,.037f);
            }
            return;
        }
        if(heavy)
        {
            m.Ink("795B49",Oak);m.Tube(p(.055f,-.55f,-.10f),p(.055f,-.12f,.57f),.050f*scale,.053f*scale,8);
            m.Ink(kind==4?"826550":"7C7772",Engraved);
            block(.055f,-.09f,.63f,kind==4?.55f:.44f,.34f,.32f);
            m.Ink(kind==4?"F2AC63":"95D4BE",Plain,.9f);
            block(.055f,-.08f,.80f,.098f,.21f,.016f);return;
        }
        // A broad silver silhouette stays readable from above, with a luminous keeper's edge.
        m.Ink("8C6B4D",Oak);m.Tube(p(.05f,-.43f,.01f),p(.05f,-.34f,.32f),.044f,.044f,7);
        m.Ink("CBA46D",Engraved);block(.05f,-.335f,.29f,.31f,.055f,.065f);
        Vector3 h=p(.05f,-.32f,.34f), e=p(.05f,-.15f,1.04f);
        m.Ink(hero?"BAD4D5":"B5AAA2",Slate);
        m.Tri(h-p(.095f,0,0),h+p(.095f,0,0),e,Vector3.up);
        m.Tri(h-p(.095f,0,0),e,h+p(0,-.057f,.09f),Vector3.down);
        m.Tri(e,h+p(.095f,0,0),h+p(0,-.057f,.09f),Vector3.down);
        m.Ink(hero?"A0F1DD":"DEA77B",Plain,.83f);
        m.Tri(h+p(.061f,.005f,0),h+p(.098f,.005f,0),e+p(0,.005f,0),Vector3.up);
    }
    static void Body(EmberMesh m,int kind)
    {
        bool hero=kind==-1,heavy=kind>=3;float scale=kind==4?1.52f:kind==3?1.27f:1;
        Vector3 p(float x,float y,float z)=>At(x,y,z)*scale;
        void block(float x,float y,float z,float w,float h,float d)=>m.Box(p(x,y,z),p(w,h,d),Quaternion.identity);
        void gem(float x,float y,float z,float w,float h,float d,int n=8)=>m.Jewel(p(x,y,z),p(w,h,d),n,.25f);
        m.Ink(hero?"517D82":kind==0?"845253":kind==3?"697A73":"594555",hero?Marble:Basalt);
        m.Tube(p(0,.55f,0),p(0,.85f,0),.25f*scale,.19f*scale,8);
        m.Ink(hero?"637F83":kind==0?"807578":kind==3?"8B8377":"796151",Engraved);
        gem(0,.99f,0,heavy?.44f:.292f,.33f,heavy?.294f:.21f);
        m.Ink("A58D69",Engraved);m.Tube(p(0,.70f,0),p(0,.77f,0),.239f*scale,.231f*scale,8);
        m.Ink(hero?"E2C693":"CC9163",Engraved);block(0,.736f,.216f,.111f,.065f,.044f);
        if(hero)
        {
            m.Ink("35747A",Marble);m.Quad(p(-.24f,1.15f,-.13f),p(.24f,1.15f,-.13f),p(.30f,.52f,-.23f),p(-.26f,.46f,-.24f),Vector3.back);
            m.Ink("D6BB7E",Engraved);m.Beam(p(-.125f,.89f,-.195f),p(0,.77f,-.210f),.024f,.030f);
            m.Beam(p(0,.77f,-.211f),p(.125f,.89f,-.195f),.024f,.030f);
            m.Ink("D4C2A1",Slate);gem(0,1.37f,.03f,.163f,.179f,.171f);
            m.Ink("366D78",Marble);gem(0,1.41f,-.043f,.224f,.239f,.178f);
            m.Ink("E2C091",Plain);block(0,1.36f,.164f,.22f,.096f,.041f);
            m.Ink("37484D");block(0,1.391f,.191f,.186f,.035f,.02f);
            m.Ink("E5CA8F",Engraved);gem(-.15f,1.14f,.152f,.075f,.089f,.048f,5);
            m.Ink("CE8161");m.Beam(p(.08f,1.17f,-.15f),p(.254f,.94f,-.25f),.103f,.045f);
        }
        else
        {
            string core=kind==4?"FFAE60":kind==3?"92DFC6":"FFC27C";
            m.Ink(heavy?"5F5B60":"8A8280",Slate);gem(0,1.38f,0,heavy?.242f:.22f,.24f,.212f);
            m.Ink("3C3D47");block(0,1.36f,.18f,.305f,.127f,.080f);
            m.Ink(core,Plain,.95f);block(0,1.38f,.226f,heavy?.201f:.232f,.047f,.018f);
            m.Ink("B29365",Engraved);gem(0,1.52f,.02f,heavy?.28f:.218f,.16f,.21f);
            m.Ink(kind==0?"A65C50":"BCA06A",Engraved);
            for(int i=0;i<(kind==4?5:2);i++)
            {
                float a=kind==4?i*1.256f:Mathf.PI*i;Vector3 d=At(Mathf.Cos(a),0,Mathf.Sin(a));
                m.Tube(p(d.x*.17f,1.53f,d.z*.16f),p(d.x*.34f,1.79f+.054f*(i%2),d.z*.28f),.078f*scale,.006f,6);
            }
            if(heavy)
            {
                m.Ink(core,Plain,1);gem(0,.98f,.255f,.112f,.139f,.063f,6);
                gem(0,1.00f,-.263f,.10f,.130f,.042f,6);
                m.Ink(kind==4?"954D4F":"476D6C",Marble);
                foreach(float side in new[]{-1f,1f})m.Quad(p(side*.13f,.75f,-.20f),p(side*.29f,.75f,-.12f),p(side*.36f,.27f,-.19f),p(side*.19f,.31f,-.27f),Vector3.back);
            }
        }
    }
    static void Lantern(EmberMesh m,Vector3 p,float s)
    {
        m.Ink("C5A16A",Engraved);m.Drum(p.x,p.y-s*.16f,p.z,s*.147f,s*.16f,s*.052f,6);
        m.Ink("F6CD72",Plain,1);m.Drum(p.x,p.y-s*.105f,p.z,s*.13f,s*.14f,s*.207f,6);
        m.Ink("846B51",Engraved);m.Drum(p.x,p.y+s*.10f,p.z,s*.16f,s*.061f,s*.085f,6);
        for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f+.25f;var q=p+At(Mathf.Cos(a)*.124f*s,0,Mathf.Sin(a)*.124f*s);m.Tube(q-Vector3.up*.11f*s,q+Vector3.up*.11f*s,.013f,.013f,5);}
    }
    static void Hound(EmberMesh m,int part)
    {
        if(part!=0)
        {
            float sign=part==1||part==3?1:-1;
            m.Ink("665856",Basalt);m.Tube(Vector3.zero,At(sign*.08f,-.22f,-.04f),.090f,.068f,7);
            m.Ink("9D7D61",Engraved);m.Tube(At(sign*.08f,-.22f,-.04f),At(sign*.03f,-.40f,.028f),.074f,.044f,7);
            m.Ink("4D4751",Slate);m.Jewel(At(sign*.035f,-.40f,.068f),At(.092f,.060f,.13f));return;
        }
        m.Ink("62525B",Basalt);m.Jewel(At(0,.51f,-.05f),At(.282f,.244f,.57f),8);
        m.Ink("8B6557",Engraved);m.Jewel(At(0,.64f,.34f),At(.253f,.229f,.24f),7);
        m.Ink("635960",Slate);m.Jewel(At(0,.67f,.56f),At(.193f,.157f,.22f),7);
        m.Ink("F3A962",Plain,.92f);
        foreach(float s in new[]{-1f,1f})
        {
            m.Jewel(At(s*.153f,.711f,.663f),At(.043f,.041f,.056f),5);
            m.Ink("9F856F",Engraved);m.Tube(At(s*.13f,.75f,.43f),At(s*.20f,.95f,.365f),.086f,.004f,5);m.Ink("F3A962",Plain,1);
        }
        for(int i=0;i<5;i++){float z=-.41f+i*.143f;m.Ink(i%2==0?"BA7C57":"E09E64",Engraved,.36f);m.Tube(At(0,.68f,z),At(0,.91f,z-.08f),.079f,.006f,5);}
        m.Ink("67575B",Basalt);m.Tube(At(0,.55f,-.45f),At(.10f,.67f,-.95f),.104f,.025f,7);
    }
    static void Wisp(EmberMesh m,int part)
    {
        if(part!=0)return;
        m.Ink("546D8B",Marble);m.Jewel(At(0,.92f,0),At(.299f,.51f,.260f),7);
        m.Ink("B2BDFF",Plain,.98f);m.Jewel(At(0,1.00f,.16f),At(.173f,.252f,.150f),7);
        m.Ink("A89883",Engraved);m.Ring(At(0,.95f,0),.34f,.406f,24);m.Ring(At(0,1.19f,0),.248f,.274f,24);
        for(int i=0;i<5;i++)
        {
            float a=i*1.257f;float x=Mathf.Cos(a),z=Mathf.Sin(a);
            m.Ink("8388AF",Marble,.32f);m.Jewel(At(x*.36f,.79f,z*.34f),At(.079f,.268f,.078f),5);
            m.Ink("E9CDFF",Plain,1);m.Jewel(At(x*.36f,.53f,z*.34f),At(.038f,.065f,.038f),5);
        }
    }
}
