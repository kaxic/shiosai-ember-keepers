Shader "Ember/Veil" {
 Properties {_Tint("Pigment",Color)=(1,1,1,.5)}
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Pass { Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   half4 _Tint;
   struct vertex {float4 pos:POSITION;fixed4 col:COLOR;};
   struct pixel {float4 pos:SV_POSITION;half4 col:COLOR;};
   pixel vert(vertex i) {pixel o;o.pos=UnityObjectToClipPos(i.pos);o.col=i.col;return o;}
   half4 frag(pixel i):SV_Target {
    half4 c=i.col*_Tint;
    #ifndef UNITY_COLORSPACE_GAMMA
    c.rgb=GammaToLinearSpace(c.rgb);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
