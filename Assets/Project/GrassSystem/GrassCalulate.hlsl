
//#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float3 CalulatePosition(float3 _Position)
{
	return float3(_Position.x + cos(_Position.z * 4.75f) * 0.1f, 0, _Position.z + cos(_Position.x * 1.82f) * 0.1f);
}

float3 CalulateRightWeight(float3 _RightVector, float _PosX, float _PosZ)
{
	return _RightVector * smoothstep(-4.0f, 4.0f, tan(_PosX * 85.4f + _PosZ * 142.7f));
}


float CalulateHeight(float4 _HeightSource)
{
	return (_HeightSource.r * 255.0f) + (_HeightSource.g * 2.55f) + (_HeightSource.b * 0.0255f) + (_HeightSource.a * 0.000255f);
}