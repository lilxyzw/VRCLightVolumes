uniform float _UdonLightVolumeEnabled;
uniform float _UdonLightVolumeCount;
uniform sampler3D _UdonLightVolume;
uniform float3 _UdonLightVolumeTexelSize;

uniform float4 _UdonLightVolumeWorldMin[256];
uniform float4 _UdonLightVolumeWorldMax[256];
uniform float4 _UdonLightVolumeUvwMin[768];
uniform float4 _UdonLightVolumeUvwMax[768];
uniform float _UdonLightVolumeWeight[768];

#pragma shader_feature_local BICUBIC_LIGHT_VOLUME_SAMPLING_ENABLED

// Calculate single SH L1 channel
float EvaluateSHL1(float L0, float3 L1, float3 n) {
	float R0 = L0;
	float3 R1 = L1 * 0.5;
	float lenR1 = length(R1);
	float q = dot(normalize(R1), n) * 0.5 + 0.5;
	float p = 1 + 2 * lenR1 / R0;
	float a = (1 - lenR1 / R0) / (1 + lenR1 / R0);
	return R0 * (a + (1 - a) * (p + 1) * pow(q, p));
}

// Calculate Light Volume Color
float3 EvaluateLightVolume(float4 tex0, float4 tex1, float4 tex2, float3 worldNormal){
    
    float L0R = tex0.r;
    float L0G = tex0.g;
    float L0B = tex0.b;
    float3 L1R = float3(tex1.r, tex2.r, tex0.a);
    float3 L1G = float3(tex1.g, tex2.g, tex1.a);
    float3 L1B = float3(tex1.b, tex2.b, tex2.a);
    return float3(EvaluateSHL1(L0R, L1R, worldNormal), EvaluateSHL1(L0G, L1G, worldNormal), EvaluateSHL1(L0B, L1B, worldNormal));
	
}

// Calculate World Normal
float3 CalculateWorldNormal(float3 normal, float3 tangent, float3 bitangent, float3 tanNormal) {
	float3 tanToWorld0 = float3(tangent.x, bitangent.x, normal.x);
	float3 tanToWorld1 = float3(tangent.y, bitangent.y, normal.y);
	float3 tanToWorld2 = float3(tangent.z, bitangent.z, normal.z);
	return normalize(float3(dot(tanToWorld0, tanNormal), dot(tanToWorld1, tanNormal), dot(tanToWorld2, tanNormal)));
}

// AABB intersection check
bool PointAABB(float3 pos, float3 min, float3 max) {
	return all(pos >= min && pos <= max);
}

// Remaps value
float3 Remap(float3 value, float3 minOld, float3 maxOld, float3 minNew, float3 maxNew) {
	return minNew + (value - minOld) * (maxNew - minNew) / (maxOld - minOld);
}

// Default light probes
float3 EvaluateLightProbe(float3 worldNormal) {
    float3 color;
    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    color.r = EvaluateSHL1(L0.r, unity_SHAr.xyz, worldNormal);
    color.g = EvaluateSHL1(L0.g, unity_SHAg.xyz, worldNormal);
    color.b = EvaluateSHL1(L0.b, unity_SHAb.xyz, worldNormal);
    
    return color;
}

// B-spline for bicubic sampling
float BSplineWeight(float x) {
    x = abs(x);
    float x2 = x * x;
    float x3 = x2 * x;
    if (x < 1.0)
        return (0.5 * x3 - x2 + 2.0 / 3.0);
    if (x < 2.0)
        return (-1.0 / 6.0 * x3 + x2 - 2.0 * x + 4.0 / 3.0);
    return 0.0;
}

// Bicubic 3D texture sampling
float4 SampleBicubic3D(sampler3D tex, float3 texelSize, float3 uvw) {
    
    float3 texCoord = uvw / texelSize;
    float3 floorCoord = floor(texCoord - 0.5);
    float3 fractCoord = texCoord - (floorCoord + 0.5);
    float4 result = float4(0.0, 0.0, 0.0, 0.0);

    for (int k = -1; k <= 2; ++k) {
        for (int j = -1; j <= 2; ++j) {
            for (int i = -1; i <= 2; ++i) {
                
                float3 currentTexelIntCoord = floorCoord + float3(i, j, k);
                float3 sampleUVW = (currentTexelIntCoord + 0.5) * texelSize;
                float4 sampleValue = tex3D(tex, sampleUVW);

                float weightU = BSplineWeight(fractCoord.x - i);
                float weightV = BSplineWeight(fractCoord.y - j);
                float weightW = BSplineWeight(fractCoord.z - k);

                float totalWeight = weightU * weightV * weightW;

                result += sampleValue * totalWeight;
                
            }
        }
    }
    
    return result;
    
}

float3 CalculateLightVolume(float3 worldNormal, float3 worldPos) {

    // Return white and skip all the calculations if light volumes are not enabled
    if (!_UdonLightVolumeEnabled) {
        return float4(1,1,1,1);
    }
    
	float maxWeight = -9999;
	int volumeID = 0;

	// Iterating through all light volumes
    for (int id = 0; id < _UdonLightVolumeCount; id++)
    {

        float weight = _UdonLightVolumeWeight[id];
        float3 min = _UdonLightVolumeWorldMin[id].xyz;
        float3 max = _UdonLightVolumeWorldMax[id].xyz;

		if(weight > maxWeight && PointAABB(worldPos, min, max)) {
			volumeID = id;
			maxWeight = weight;
		}

	}

    float3 worldMin = _UdonLightVolumeWorldMin[volumeID].xyz;
    float3 worldMax = _UdonLightVolumeWorldMax[volumeID].xyz;
	
    float3 uvMin0 = _UdonLightVolumeUvwMin[volumeID * 3].xyz;
    float3 uvMax0 = _UdonLightVolumeUvwMax[volumeID * 3].xyz;
    float3 uvMin1 = _UdonLightVolumeUvwMin[volumeID * 3 + 1].xyz;
    float3 uvMax1 = _UdonLightVolumeUvwMax[volumeID * 3 + 1].xyz;
    float3 uvMin2 = _UdonLightVolumeUvwMin[volumeID * 3 + 2].xyz;
    float3 uvMax2 = _UdonLightVolumeUvwMax[volumeID * 3 + 2].xyz;
				
    float3 volumeUV0 = clamp(Remap(worldPos, worldMin, worldMax, uvMin0, uvMax0), uvMin0, uvMax0);
    float3 volumeUV1 = clamp(Remap(worldPos, worldMin, worldMax, uvMin1, uvMax1), uvMin1, uvMax1);
    float3 volumeUV2 = clamp(Remap(worldPos, worldMin, worldMax, uvMin2, uvMax2), uvMin2, uvMax2);

    #ifndef BICUBIC_LIGHT_VOLUME_SAMPLING_ENABLED
    float4 tex0 = tex3D(_UdonLightVolume, volumeUV0);
    float4 tex1 = tex3D(_UdonLightVolume, volumeUV1);
    float4 tex2 = tex3D(_UdonLightVolume, volumeUV2);
    #else
    float4 tex0 = SampleBicubic3D(_UdonLightVolume, _UdonLightVolumeTexelSize, volumeUV0);
    float4 tex1 = SampleBicubic3D(_UdonLightVolume, _UdonLightVolumeTexelSize, volumeUV1);
    float4 tex2 = SampleBicubic3D(_UdonLightVolume, _UdonLightVolumeTexelSize, volumeUV2);
    #endif
    
    return EvaluateLightVolume(tex0, tex1, tex2, worldNormal);

}