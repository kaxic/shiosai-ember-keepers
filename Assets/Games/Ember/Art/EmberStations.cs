using UnityEngine;
using static EmberMesh;
using static EmberMesh.Finish;

internal static class EmberStations
{
    public static void Altar(EmberMesh m,bool lit)
    {
        string glass=lit?"89DCC8":"667E79";
        m.Ink("777B78",Slate);m.Drum(0,.025f,0,.79f,.76f,.16f,8);
        m.Ink("A38961",Engraved);m.Drum(0,.185f,0,.61f,.48f,.10f,8);
        m.Ink("63877F",Marble);m.Drum(0,.285f,0,.44f,.36f,.39f,8);
        m.Ink("C4A676",Engraved);m.Drum(0,.675f,0,.39f,.57f,.13f,10);
        m.Ink(glass,Marble,lit?.82f:.04f);m.Drum(0,.803f,0,.47f,.46f,.012f,12);
        m.Jewel(At(0,1.09f,0),At(.21f,.29f,.18f),7);
        foreach(float x in new[]{-.34f,.34f})
        {
            m.Ink("DDC7A2");m.Drum(x,.81f,.09f,.055f,.056f,.19f,8);
            if(lit){m.Ink("FFDC94",Plain,.99f);m.Jewel(At(x,1.064f,.09f),At(.039f,.069f,.039f),5);}
        }
        m.Ink(glass,Plain,lit?.64f:.05f);m.Ring(At(0,.046f,0),.89f,.94f,28);
    }
    public static void Coffer(EmberMesh m,int choice)
    {
        bool open=choice==2;
        m.Ink("79583F",Oak);m.Block(0,.31f,0,1.18f,.54f,.73f);
        m.Ink("B18A53",Engraved);m.Block(0,.12f,0,1.23f,.11f,.77f);
        foreach(float x in new[]{-.41f,.41f})m.Block(x,.33f,0,.085f,.47f,.77f);
        m.Ink("453D36");m.Block(0,.587f,0,1.05f,.027f,.65f);
        if(open)
        {
            m.Ink("9B734C",Oak);m.Box(At(0,.83f,.29f),At(1.21f,.54f,.15f),Quaternion.Euler(-18,0,0));
            m.Ink("BD9D62",Engraved);m.Block(0,.64f,-.37f,.18f,.17f,.062f);
        }
        else
        {
            m.Ink("947047",Oak);m.Block(0,.66f,0,1.22f,.19f,.76f);
            m.Ink("C29A5F",Engraved);foreach(float x in new[]{-.41f,.41f})m.Block(x,.665f,0,.095f,.20f,.78f);
            m.Ink(choice==0?"F4C770":"9B8767",Engraved,choice==0?.45f:0);m.Block(0,.52f,-.399f,.17f,.24f,.065f);
        }
        m.Ink(choice==0?"D9B36B":"8C8068",Plain,choice==0?.50f:.06f);m.Ring(At(0,.046f,0),.91f,.955f,28);
    }
    public static void Exit(EmberMesh m)
    {
        m.Ink("818E91",Slate);m.Drum(0,.02f,0,1.03f,.95f,.14f,10);
        m.Ink("89CABC",Plain,.78f);m.Ring(At(0,.171f,0),.65f,.77f,30);
        foreach(float x in new[]{-.86f,.86f})
        {
            m.Ink("55646F",Slate);m.Drum(x,.03f,.20f,.26f,.17f,.24f,8);
            m.Drum(x,.27f,.20f,.16f,.14f,1.02f,8);
            m.Ink("BC9F6C",Engraved);m.Drum(x,1.17f,.20f,.215f,.21f,.11f,8);
        }
        for(int i=0;i<9;i++)
        {
            m.Ink(i==4?"A3DBCA":"A19674",i==4?Plain:Engraved,i==4?.79f:0);
            m.ArchStone(At(0,1.28f,.20f),.79f,.945f,.27f,20*i+.6f,20*i+19.4f);
        }
        for(int i=0;i<3;i++)
        {
            float z=-.49f+i*.31f;m.Ink("BCE6CC",Plain,.88f);
            m.Tri(At(-.17f,.181f,z),At(.17f,.181f,z),At(0,.181f,z+.15f),Vector3.up);
        }
    }
}
