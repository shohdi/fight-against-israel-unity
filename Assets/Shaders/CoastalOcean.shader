Shader "FightAgainstIsrael/CoastalOcean"
{
    Properties
    {
        _Color ("Ocean tint", Color) = (0.025,0.28,0.34,1)
        _DeepColor ("Deep water", Color) = (0.018,0.13,0.20,1)
        _ShallowColor ("Sunlit water", Color) = (0.045,0.38,0.40,1)
        _Travel ("World travel", Float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.30
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow
        #pragma target 3.0
        #include "UnityCG.cginc"
        fixed4 _Color, _DeepColor, _ShallowColor;
        float _Travel, _Glossiness;
        struct Input { float3 worldPos; };
        void vert(inout appdata_full v)
        {
            float2 p=v.vertex.xz;
            float t=_Time.y;
            v.vertex.y += sin(p.x*.36+p.y*.51+t*.9)*.019 + sin(p.x*.8-p.y*.34+t*1.2)*.011;
        }
        float hash(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
        float noise(float2 p)
        {
            float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
            return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+float2(1,1)),f.x),f.y);
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float2 p=IN.worldPos.xz+float2(0,_Travel);
            float t=_Time.y;
            float broad=noise(p*.065)*.6+noise(p*.17+5.2)*.4;
            float a=sin(p.x*.74+p.y*.42+t*1.25),b=sin(p.x*1.12-p.y*.65+t*.85);
            float ridge=pow(saturate(a*.5+.5),12)*.018+pow(saturate(b*.5+.5),15)*.012;
            o.Albedo=lerp(_DeepColor.rgb,_ShallowColor.rgb,saturate(broad*.86+.09))+ridge*.35;
            o.Normal=normalize(float3((cos(p.x*.74+p.y*.42+t*1.25)*.021+cos(p.x*1.12-p.y*.65+t*.85)*.012),cos(p.x*.74+p.y*.42+t*1.25)*.013-cos(p.x*1.12-p.y*.65+t*.85)*.009,1));
            o.Metallic=.025;
            o.Smoothness=_Glossiness;
            o.Occlusion=1;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
