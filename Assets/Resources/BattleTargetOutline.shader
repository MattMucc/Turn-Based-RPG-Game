Shader "Tenebrasia/BattleTargetOutline"
{
    Properties { _Color ("Color", Color) = (1,0.05,0.03,1) _Width ("Width", Float) = 0.045 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 vertex : SV_POSITION; };
            float4 _Color; float _Width;
            v2f vert(appdata v)
            {
                v2f o;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                world += UnityObjectToWorldNormal(v.normal) * _Width;
                o.vertex = mul(UNITY_MATRIX_VP, float4(world, 1)); return o;
            }
            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
