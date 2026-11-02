Shader "Game/ControllerFace"
{
 Properties {
  [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
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
   sampler2D _MainTex; fixed4 _FaceColor, _SymbolColor; float4 _SpriteUV, _ClipRect;
   v2f vert(appdata v) { v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
   fixed4 frag(v2f i):SV_Target {
    float2 p=(i.uv-_SpriteUV.xy)/(_SpriteUV.zw-_SpriteUV.xy)-0.5;
    float d=length(p); float aa=max(fwidth(d),0.001);
    float disc=1-smoothstep(0.5-aa,0.5,d);
    fixed mask=tex2D(_MainTex,i.uv).a;
    fixed4 c=lerp(_SymbolColor,_FaceColor,mask); c.a=disc*i.color.a;
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
