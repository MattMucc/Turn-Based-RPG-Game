Shader "Game/ControllerDirection"
{
 Properties {
  [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
  _Stick ("Left stick", Float) = 0
  _Direction ("Direction", Float) = 0
  _FaceColor ("Face", Color) = (0,0,0,1)
  _SymbolColor ("Symbol", Color) = (1,1,1,1)
  _SpriteUV ("Sprite UV", Vector) = (0,0,1,1)
  _StencilComp ("Stencil Comparison", Float) = 8
  _Stencil ("Stencil ID", Float) = 0
  _StencilOp ("Stencil Operation", Float) = 0
  _StencilWriteMask ("Stencil Write Mask", Float) = 255
  _StencilReadMask ("Stencil Read Mask", Float) = 255
  _ColorMask ("Color Mask", Float) = 15
 }
 SubShader {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
   struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; };
   sampler2D _MainTex; fixed4 _FaceColor, _SymbolColor; float4 _SpriteUV, _ClipRect; float _Stick, _Direction;
   v2f vert(appdata v) { v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
   fixed4 frag(v2f i):SV_Target {
    float2 p=(i.uv-_SpriteUV.xy)/(_SpriteUV.zw-_SpriteUV.xy)-0.5;
    float2 q=p;
    if (_Direction>2.5) q=float2(p.y,-p.x);
    else if (_Direction>1.5) q=-p;
    else if (_Direction>0.5) q=float2(-p.y,p.x);
    float alpha; fixed3 rgb;
    if (_Stick>0.5) {
     float disc=step(length(p),0.28);
     float arrow=step(0.29,q.y)*step(q.y,0.49)*step(abs(q.x),(0.49-q.y));
     float letter=max(step(abs(p.x+0.045),0.023)*step(abs(p.y),0.10),step(abs(p.y+0.08),0.023)*step(abs(p.x-0.01),0.08));
     alpha=max(disc,arrow); rgb=lerp(fixed3(0.015,0.015,0.02),fixed3(1,1,1),max(letter*disc,arrow));
    } else {
     alpha=max(step(abs(p.x),0.14)*step(abs(p.y),0.46),step(abs(p.y),0.14)*step(abs(p.x),0.46));
     rgb=lerp(fixed3(0.27,0.29,0.33),fixed3(1,1,1),step(0.15,q.y));
    }
    fixed4 c=fixed4(rgb,alpha*i.color.a);
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-0.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
