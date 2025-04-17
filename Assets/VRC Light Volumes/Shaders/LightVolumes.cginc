uniform float _UdonLightVolumeEnabled;
uniform float _UdonLightVolumeCount;
uniform float _UdonLightVolumeBlend;
uniform sampler3D _UdonLightVolume;

uniform float4 _UdonLightVolumeColorCorrection[256];

uniform float4 _UdonLightVolumeWorldMin[256];
uniform float4 _UdonLightVolumeWorldMax[256];
uniform float4 _UdonLightVolumeUvwMin[768];
uniform float4 _UdonLightVolumeUvwMax[768];

//
// All the internal functions here uses LV_ (LightVolumes) prefix to avoid name conflicts in other shaders
//

// LV_Remaps value
float3 LV_Remap(float3 value, float3 minOld, float3 maxOld, float3 minNew, float3 maxNew) {
	return minNew + (value - minOld) * (maxNew - minNew) / (maxOld - minOld);
}

// LV_Remaps value and clamps the result
float3 LV_RemapClamped(float3 value, float3 minOld, float3 maxOld, float3 minNew, float3 maxNew) {
    return clamp(LV_Remap(value, minOld, maxOld, minNew, maxNew), minNew, maxNew);
}

float3 ClampMagnitude(float3 v, float maxMagnitude)
{
    float mag = length(v); // находим длину вектора
    return mag > maxMagnitude ? v * (maxMagnitude / mag) : v; // если длина больше максимальной, то нормализуем и умножаем на максимальную длину
}

// Calculate single SH L1 channel
float LV_EvaluateSHL1(float L0, float3 L1, float3 n)
{
	float3 R1 = L1 * 0.5;
	float lenR1 = length(R1);
	float q = dot(normalize(R1), n) * 0.5 + 0.5;
    float p = 1 + 2 * lenR1 / L0;
    float a = (1 - lenR1 / L0) / (1 + lenR1 / L0);
    return L0 * (a + (1 - a) * (p + 1) * pow(q, p));
}

float EvaluateSHL1(float L0, float3 L1, float3 n) {
    
    L1 = L1 / 2;
    float L1length = length(L1);
    if (L1length > 0.0 && L0 > 0.0) {
        float k = min(L0 / L1length, 1.13);
        L1 *= k;
    }
    
    return L0 + dot(L1, n);
    
}


// Calculate Light Volume Color based on 3 textures provided
float3 LV_EvaluateLightVolume(float4 tex0, float4 tex1, float4 tex2, float3 worldNormal){
    float L0R = tex0.r;
    float L0G = tex0.g;
    float L0B = tex0.b;
    float3 L1R = float3(tex1.r, tex2.r, tex0.a);
    float3 L1G = float3(tex1.g, tex2.g, tex1.a);
    float3 L1B = float3(tex1.b, tex2.b, tex2.a);
    return float3(EvaluateSHL1(L0R, L1R, worldNormal), EvaluateSHL1(L0G, L1G, worldNormal), EvaluateSHL1(L0B, L1B, worldNormal));
}

// Samples 3 SH textures required for evaluating light volumes
void LV_SampleLightVolumeTexID(int volumeID, float3 worldPos, out float4 tex0, out float4 tex1, out float4 tex2) {
    
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
				
    float3 volumeUVW0 = LV_RemapClamped(worldPos, worldMin, worldMax, uvwMin0, uvwMax0);
    float3 volumeUVW1 = LV_RemapClamped(worldPos, worldMin, worldMax, uvwMin1, uvwMax1);
    float3 volumeUVW2 = LV_RemapClamped(worldPos, worldMin, worldMax, uvwMin2, uvwMax2);
    
    tex0 = tex3D(_UdonLightVolume, volumeUVW0);
    tex1 = tex3D(_UdonLightVolume, volumeUVW1);
    tex2 = tex3D(_UdonLightVolume, volumeUVW2);
    
}

// Only samples tex0 for getting L0 ambient color
void LV_SampleLightVolumeTexID(int volumeID, float3 worldPos, out float4 tex0) {
    
    // World bounds
    float3 worldMin = _UdonLightVolumeWorldMin[volumeID].xyz;
    float3 worldMax = _UdonLightVolumeWorldMax[volumeID].xyz;
	
    // UVW bounds
    int3 uvwID = int3(volumeID * 3, volumeID * 3 + 1, volumeID * 3 + 2);
    float3 uvwMin0 = _UdonLightVolumeUvwMin[uvwID.x].xyz;
    float3 uvwMax0 = _UdonLightVolumeUvwMax[uvwID.x].xyz;
    
    float3 volumeUVW0 = LV_RemapClamped(worldPos, worldMin, worldMax, uvwMin0, uvwMax0);

    tex0 = tex3D(_UdonLightVolume, volumeUVW0);
    
}

// Default light probes
float3 LV_EvaluateLightProbe(float3 worldNormal) {
    float3 color;
    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    color.r = EvaluateSHL1(L0.r, unity_SHAr.xyz, worldNormal);
    color.g = EvaluateSHL1(L0.g, unity_SHAg.xyz, worldNormal);
    color.b = EvaluateSHL1(L0.b, unity_SHAb.xyz, worldNormal);
    //return ShadeSH9(float4(worldNormal, 1.0));
    return color;
    
}
// Default light probes but only ambient color
float3 LV_EvaluateLightProbe() {
    return ShadeSH9(float4(0, 0, 0, 1.0));
}
// Default light probes and also return ambient color
float3 LV_EvaluateLightProbe(float3 worldNormal, out float3 ambientColor) {
    ambientColor = LV_EvaluateLightProbe();
    return LV_EvaluateLightProbe(worldNormal);
}

// Faster than smoothstep
float LV_FastSmooth(float x) {
	return x * x * (3.0 - 2.0 * x);
}

// Corrects exposure, shadows, mids and highlights
float3 LV_SimpleColorCorrection(float3 color, float exposure, float shadowGain, float midGain, float highlightGain) {
	color *= exposure;
	float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
    float shadowMask = LV_FastSmooth(saturate((0.4 - luma) * 2.5)); // 0.4 and 0.6 are for smoother tones overlap
    float highlightMask = LV_FastSmooth(saturate((luma - 0.6) * 2.5)); // 2.5f is actually 1/0.4
	float midMask = 1.0 - shadowMask - highlightMask;
	float gain = shadowMask * shadowGain + midMask * midGain + highlightMask * highlightGain;
	return color * gain;
}

// AABB intersection check
bool LV_PointAABB(float3 pos, float3 min, float3 max) {
	return all(pos >= min && pos <= max);
}

// Bounds mask
float LV_BoundsMask(float3 pos, float3 minBounds, float3 maxBounds, float edgeSmooth) {
    float3 distToMin = (pos - minBounds) / edgeSmooth;
    float3 distToMax = (maxBounds - pos) / edgeSmooth;
    float3 fade = saturate(min(distToMin, distToMax));
    return fade.x * fade.y * fade.z;
}

// Calculates final Light Volumes color based on world position and world normal
float3 LightVolume(float3 worldNormal, float3 worldPos) {

    // Fallback to default light probes if Light Volume are not enabled
    if (!_UdonLightVolumeEnabled) return LV_EvaluateLightProbe(worldNormal);
    
    int volumeID_A = -1; // Main, dominant volume ID
    int volumeID_B = -1; // Secondary volume ID to blend main with
    
    // Iterating through all light volumes with simplified algorithm requiring Light Volumes to be sorted by weight in descending order
    for (int id = 0; id < _UdonLightVolumeCount; id++) {
        if (LV_PointAABB(worldPos, _UdonLightVolumeWorldMin[id].xyz, _UdonLightVolumeWorldMax[id].xyz)) {
            if (volumeID_A != -1) { volumeID_B = id; break; }
            else volumeID_A = id;
        }
    }
    
    // If no volumes found, using fallback to light probes
    if (volumeID_A == -1) return LV_EvaluateLightProbe(worldNormal); 
    // If at least, main, dominant volume found
    
    float mask = LV_BoundsMask(worldPos, _UdonLightVolumeWorldMin[volumeID_A].xyz, _UdonLightVolumeWorldMax[volumeID_A].xyz, _UdonLightVolumeBlend); // Mask to blend volume
        
    if (mask < 1) { // Only blend mask for pixels in the smoothed edges region
        if (volumeID_B != -1) { // Blending volume A and Volume B
            
            float4 texA[3]; // Main textures bundle
            float4 texB[3]; // Secondary textures bundle
            
            LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA[0], texA[1], texA[2]); // Sampling main volume
            LV_SampleLightVolumeTexID(volumeID_B, worldPos, texB[0], texB[1], texB[2]); // Sampling secondary volume
            
            float4 tex[3]; // Blended textures color
            
            // Blending textures
            tex[0] = lerp(texB[0], texA[0], mask);
            tex[1] = lerp(texB[1], texA[1], mask);
            tex[2] = lerp(texB[2], texA[2], mask);
            
            // Evaluating result
            return LV_EvaluateLightVolume(tex[0], tex[1], tex[2], worldNormal); 
            
        } else { // Blending volume A and light probes
            
            float4 texA[3]; // Main textures bundle
            LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA[0], texA[1], texA[2]); // Sampling main volume
            float3 colorA = LV_EvaluateLightVolume(texA[0], texA[1], texA[2], worldNormal); // Evaluating main volume
            float3 colorB = LV_EvaluateLightProbe(worldNormal); // Evaluating light probes
            
            // Blending result
            return lerp(colorB, colorA, mask);
            
        }
    } else { // If no need to blend
        
        float4 texA[3]; // Main textures bundle
        LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA[0], texA[1], texA[2]); // Sampling main volume
        return LV_EvaluateLightVolume(texA[0], texA[1], texA[2], worldNormal); // Evaluating main volume
        
    }

}

// Calculates final Light Volumes color based on world position and world normal, also returns ambient color
float3 LightVolume(float3 worldNormal, float3 worldPos, out float3 ambientColor) {

    // Fallback to default light probes if Light Volume are not enabled
    if (!_UdonLightVolumeEnabled) return LV_EvaluateLightProbe(worldNormal);
    
    int volumeID_A = -1; // Main, dominant volume ID
    int volumeID_B = -1; // Secondary volume ID to blend main with
    
    // Iterating through all light volumes with simplified algorithm requiring Light Volumes to be sorted by weight in descending order
    for (int id = 0; id < _UdonLightVolumeCount; id++) {
        if (LV_PointAABB(worldPos, _UdonLightVolumeWorldMin[id].xyz, _UdonLightVolumeWorldMax[id].xyz)) {
            if (volumeID_A != -1) { volumeID_B = id; break; }
            else volumeID_A = id;
        }
    }
    
    // If no volumes found, using fallback to light probes
    if (volumeID_A == -1) return LV_EvaluateLightProbe(worldNormal); 
    // If at least, main, dominant volume found
    
    float mask = LV_BoundsMask(worldPos, _UdonLightVolumeWorldMin[volumeID_A].xyz, _UdonLightVolumeWorldMax[volumeID_A].xyz, _UdonLightVolumeBlend); // Mask to blend volume
        
    if (mask < 1) { // Only blend mask for pixels in the smoothed edges region
        if (volumeID_B != -1) { // Blending volume A and Volume B
            
            float4 texA[3]; // Main textures bundle
            float4 texB[3]; // Secondary textures bundle
            
            LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA[0], texA[1], texA[2]); // Sampling main volume
            LV_SampleLightVolumeTexID(volumeID_B, worldPos, texB[0], texB[1], texB[2]); // Sampling secondary volume
            
            float4 tex[3]; // Blended textures color
            
            // Blending textures
            tex[0] = lerp(texB[0], texA[0], mask);
            tex[1] = lerp(texB[1], texA[1], mask);
            tex[2] = lerp(texB[2], texA[2], mask);
            
            ambientColor = tex[0].rgb;
            
            // Evaluating result
            return LV_EvaluateLightVolume(tex[0], tex[1], tex[2], worldNormal); 
            
        } else { // Blending volume A and light probes
            
            float3 lightProbesAmbient;
            float4 texA[3]; // Main textures bundle
            LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA[0], texA[1], texA[2]); // Sampling main volume
            float3 colorA = LV_EvaluateLightVolume(texA[0], texA[1], texA[2], worldNormal); // Evaluating main volume
            float3 colorB = LV_EvaluateLightProbe(worldNormal, lightProbesAmbient); // Evaluating light probes
            
            // Blending result
            ambientColor = lerp(lightProbesAmbient, texA[0].rgb, mask);
            return lerp(colorB, colorA, mask);
            
        }
    } else { // If no need to blend
        
        float4 texA[3]; // Main textures bundle
        LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA[0], texA[1], texA[2]); // Sampling main volume
        ambientColor = texA[0].rgb;
        return LV_EvaluateLightVolume(texA[0], texA[1], texA[2], worldNormal); // Evaluating main volume
        
    }

}

// Calculates final Light Volumes ambient color based on world position
float3 LightVolumeAmbient(float3 worldPos) {

    // Fallback to default light probes if Light Volume are not enabled
    if (!_UdonLightVolumeEnabled) return LV_EvaluateLightProbe();
    
    int volumeID_A = -1; // Main, dominant volume ID
    int volumeID_B = -1; // Secondary volume ID to blend main with
    
    // Iterating through all light volumes with simplified algorithm requiring Light Volumes to be sorted by weight in descending order
    for (int id = 0; id < _UdonLightVolumeCount; id++) {
        if (LV_PointAABB(worldPos, _UdonLightVolumeWorldMin[id].xyz, _UdonLightVolumeWorldMax[id].xyz)) {
            if (volumeID_A != -1) { volumeID_B = id; break; }
            else volumeID_A = id;
        }
    }
    
    // If no volumes found, using fallback to light probes
    if (volumeID_A == -1) return LV_EvaluateLightProbe(); 
    // If at least, main, dominant volume found
    
    float mask = LV_BoundsMask(worldPos, _UdonLightVolumeWorldMin[volumeID_A].xyz, _UdonLightVolumeWorldMax[volumeID_A].xyz, _UdonLightVolumeBlend); // Mask to blend volume
        
    if (mask < 1) { // Only blend mask for pixels in the smoothed edges region
        if (volumeID_B != -1) { // Blending volume A and Volume B
            
            float4 texA; // Main texture
            float4 texB; // Secondary texture
            
            LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA); // Sampling main volume
            LV_SampleLightVolumeTexID(volumeID_B, worldPos, texB); // Sampling secondary volume
            
            // Blending result
            return lerp(texB, texA, mask);
            
        } else { // Blending volume A and light probes
            
            float4 texA; // Main texture
            LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA); // Sampling main volume
            float3 colorA = texA.rgb;
            float3 colorB = LV_EvaluateLightProbe(); // Evaluating light probes
            
            // Blending result
            return lerp(colorB, colorA, mask);
            
        }
    } else { // If no need to blend
        
        float4 texA; // Main texture
        LV_SampleLightVolumeTexID(volumeID_A, worldPos, texA); // Sampling main volume
        return texA.rgb;
        
    }

}

// Bakery's non-linear light probes

//// Non-linear light probes and also return ambient color
//float3 LV_EvaluateLightProbeNonLinear(float3 worldNormal, out float3 ambientColor) {
//    float3 color;
//    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
//    color.r = LV_EvaluateSHL1(L0.r, unity_SHAr.xyz, worldNormal);
//    color.g = LV_EvaluateSHL1(L0.g, unity_SHAg.xyz, worldNormal);
//    color.b = LV_EvaluateSHL1(L0.b, unity_SHAb.xyz, worldNormal);
//    ambientColor = L0;
//    return color;
//}
//// Non-linear light probes
//float3 LV_EvaluateLightProbeNonLinear(float3 worldNormal) {
//    float3 dummy;
//    return LV_EvaluateLightProbeNonLinear(worldNormal, dummy);
//}
//// Non-linear light probes but only ambient color
//float3 LV_EvaluateLightProbeNonLinearAmbient() {
//    return float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
//}