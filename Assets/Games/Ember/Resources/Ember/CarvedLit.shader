Shader "Ember/Carved Hall"
{
 Properties {
  [NoScaleOffset] _Stone ("Court stone", 2D)="gray"{}
  [NoScaleOffset] _Slate ("Slate", 2D)="gray"{}
  [NoScaleOffset] _Jade ("Veined jade", 2D)="gray"{}
  [NoScaleOffset] _Lava ("Red basalt", 2D)="gray"{}
  [NoScaleOffset] _Gold ("Engraved bronze", 2D)="gray"{}
  [NoScaleOffset] _Wood ("Workshop oak", 2D)="gray"{}
  _Hit ("Hit flash", Range(0,1))=0
 }
 SubShader {
  Tags {"RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Tags {"LightMode"="ForwardBase"}
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fwdbase
   #include "UnityCG.cginc"
   #include "AutoLight.cginc"
   sampler2D _Stone,_Slate,_Jade,_Lava,_Gold,_Wood;
   half _Hit;
   struct data {float4 vertex:POSITION; float3 normal:NORMAL; float4 uv:TEXCOORD0; fixed4 color:COLOR;};
   struct output {float4 pos:SV_POSITION;float4 paint:TEXCOORD0;half3 normal:TEXCOORD1;half3 color:COLOR;float3 world:TEXCOORD2; SHADOW_COORDS(3)};
   output vert(data v){output o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color.rgb;o.paint=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;TRANSFER_SHADOW(o);return o;}
   half3 pigment(sampler2D s,float2 p)
   {
    half3 c=tex2D(s,p).rgb;
    #ifndef UNITY_COLORSPACE_GAMMA
    c=LinearToGammaSpace(c);
    #endif
    return c;
   }
   half4 frag(output i):SV_Target
   {
    float2 uv=i.paint.xy;float type=i.paint.z;float glow=i.paint.w;half3 c=i.color;
    if(type>.5 && type<1.5)c+=(pigment(_Stone,uv/3.3)-half3(.42,.29,.18))*.68;
    else if(type<2.5 && type>1.5)c+=(pigment(_Slate,uv/3.5)-half3(.38,.38,.36))*.58;
    else if(type<3.5 && type>2.5)c+=(pigment(_Jade,uv/3.7)-half3(.13,.19,.17))*.70;
    else if(type<4.5 && type>3.5)c+=(pigment(_Lava,uv/2.8)-half3(.27,.16,.11))*.63;
    else if(type<5.5 && type>4.5)c+=(pigment(_Gold,uv/1.7)-half3(.38,.24,.11))*.45;
    else if(type<6.5 && type>5.5)c+=(pigment(_Wood,uv/1.9)-half3(.32,.18,.11))*.60;
    else if(type>6.5){
     float ripple=sin(uv.y*4.3+sin(uv.x*1.4+_Time.y*.4)*1.12+_Time.y*.85);
     c+=half3(.13,.24,.23)*smoothstep(.85,1,ripple)*(.5+.5*cos(uv.x*.95-_Time.y*.38));
    }
    half3 n=normalize(i.normal);
    half direct=saturate(dot(n,normalize(half3(-.53,.80,-.36))));
    half fill=saturate(dot(n,normalize(half3(.50,.27,.67))));
    half3 shade=half3(.49,.54,.63)+half3(.47,.42,.33)*direct*lerp(.57,1,SHADOW_ATTENUATION(i))+half3(.07,.10,.13)*fill;
    half3 lit=saturate(c*shade);
    float flicker=sin(_Time.y*4.2+i.world.x*3.7+i.world.z*.64)*.025;
    lit=lerp(lit,saturate(c*(1.12+flicker)),saturate(glow));
    lit=lerp(lit,half3(1,.77,.48),saturate(_Hit)*.79);
    #ifndef UNITY_COLORSPACE_GAMMA
    lit=GammaToLinearSpace(lit);
    #endif
    return half4(lit,1);
   }
   ENDCG
  }
  UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
 }
 Fallback "Diffuse"
}
