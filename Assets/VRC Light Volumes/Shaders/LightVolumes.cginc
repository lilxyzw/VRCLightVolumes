uniform float _UdonLightVolumeEnabled;
uniform float _UdonLightVolumeCount;
uniform float _UdonLightVolumeBlend;
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

// Calculate Light Volume Color based on 3 textures provided
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

// Remaps value and clamps the result
float3 RemapClamped(float3 value, float3 minOld, float3 maxOld, float3 minNew, float3 maxNew) {
    return clamp(Remap(value, minOld, maxOld, minNew, maxNew), minNew, maxNew);
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
float4 SampleTexture3DBicubic(sampler3D tex, float3 uvw, float3 texelSize) {
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

// Sample light Volume by ID
float3 SampleLightVolumeID(int volumeID, float3 worldPos, float3 worldNormal) {
    
    // World bounds
    float3 worldMin = _UdonLightVolumeWorldMin[volumeID].xyz;
    float3 worldMax = _UdonLightVolumeWorldMax[volumeID].xyz;
	
    // UVW bounds
    int3 uvwID = int3(volumeID * 3, volumeID * 3 + 1, volumeID * 3 + 2);
    float3 uvwMin0 = _UdonLightVolumeUvwMin[uvwID.x].xyz;
    float3 uvwMax0 = _UdonLightVolumeUvwMax[uvwID.x].xyz;
    float3 uvwMin1 = _UdonLightVolumeUvwMin[uvwID.y].xyz;
    float3 uvwMax1 = _UdonLightVolumeUvwMax[uvwID.y].xyz;
    float3 uvwMin2 = _UdonLightVolumeUvwMin[uvwID.z].xyz;
    float3 uvwMax2 = _UdonLightVolumeUvwMax[uvwID.z].xyz;
				
    float3 volumeUV0 = RemapClamped(worldPos, worldMin, worldMax, uvwMin0, uvwMax0);
    float3 volumeUV1 = RemapClamped(worldPos, worldMin, worldMax, uvwMin1, uvwMax1);
    float3 volumeUV2 = RemapClamped(worldPos, worldMin, worldMax, uvwMin2, uvwMax2);

    #ifndef BICUBIC_LIGHT_VOLUME_SAMPLING_ENABLED
    float4 tex0 = tex3D(_UdonLightVolume, volumeUV0);
    float4 tex1 = tex3D(_UdonLightVolume, volumeUV1);
    float4 tex2 = tex3D(_UdonLightVolume, volumeUV2);
    #else
    float4 tex0 = SampleTexture3DBicubic(_UdonLightVolume, volumeUV0, _UdonLightVolumeTexelSize);
    float4 tex1 = SampleTexture3DBicubic(_UdonLightVolume, volumeUV1, _UdonLightVolumeTexelSize);
    float4 tex2 = SampleTexture3DBicubic(_UdonLightVolume, volumeUV2, _UdonLightVolumeTexelSize);
    #endif
    
    return EvaluateLightVolume(tex0, tex1, tex2, worldNormal);
}

// Bounds mask
float BoundsMask(float3 pos, float3 minBounds, float3 maxBounds, float edgeSmooth) {
    float3 distToMin = (pos - minBounds) / edgeSmooth;
    float3 distToMax = (maxBounds - pos) / edgeSmooth;
    float3 fade = saturate(min(distToMin, distToMax));
    return fade.x * fade.y * fade.z;
}

float3 LightVolume(float3 worldNormal, float3 worldPos) {

    // Return white and skip all the calculations if light volumes are not enabled
    if (!_UdonLightVolumeEnabled) {
        return float4(1,1,1,1);
    }
    
    float maxWeightA = 1.175494e-38f; // Min float value. Use 6.10352e-05 for half
    float maxWeightB = 1.175494e-38f; // Min float value. Use 6.10352e-05 for half
    
    int volumeID_A = 0; // Main volume ID
    int volumeID_B = 0; // Secondary volume ID to blend fragments with on overlap
    
	// Iterating through all light volumes and checking bounds
    for (int id = 0; id < _UdonLightVolumeCount; id++) {

        float3 min = _UdonLightVolumeWorldMin[id].xyz;
        float3 max = _UdonLightVolumeWorldMax[id].xyz;

        if (PointAABB(worldPos, min, max)) {
            
            float weight = _UdonLightVolumeWeight[id];

            if (weight > maxWeightA) {
                
                maxWeightB = maxWeightA;
                volumeID_B = volumeID_A;

                maxWeightA = weight;
                volumeID_A = id;
                
            } else if (weight > maxWeightB && id != volumeID_A) {
                
                maxWeightB = weight;
                volumeID_B = id;
                
            }
            
        }
        
    }
    
    volumeID_A = max(0, volumeID_A);
    
    // Sampling main volume
    float3 color_A = SampleLightVolumeID(volumeID_A, worldPos, worldNormal);
    
    if (volumeID_A != volumeID_B) { // Need to blend when inside of two intersecting light volumes
        
        // Main volume world bounds
        float3 worldMin = _UdonLightVolumeWorldMin[volumeID_A].xyz;
        float3 worldMax = _UdonLightVolumeWorldMax[volumeID_A].xyz;
        
        // Mask to blend volumes
        float mask = BoundsMask(worldPos, worldMin, worldMax, _UdonLightVolumeBlend);
        
        if (mask < 1) { // Only blend mask for pixels in the smoothed edges region
            
            // Sampling secondary volume
            float3 color_B = SampleLightVolumeID(volumeID_B, worldPos, worldNormal);
            
            // Blending
            return lerp(color_B, color_A, mask);
            
        } else {
            
            return color_A;
            
        }
        
    } else { // No need to blend, when inside of a single light volume or not on it's side
        
        return color_A;
        
    }

}