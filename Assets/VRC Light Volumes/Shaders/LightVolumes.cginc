
// Are Light Volumes enabled on scene?
uniform float _UdonLightVolumeEnabled;

// All volumes count in scene
uniform float _UdonLightVolumeCount;

// How volumes edge blending
uniform float _UdonLightVolumeBlend;

// Main 3D Texture atlas
uniform sampler3D _UdonLightVolume;

// Rotation types: 0 - Fixed, 1 - Y Axis, 2 - Free
uniform float _UdonLightVolumeRotationType[256];

// Fixed rotation:   A - WorldMin             B - WorldMax
// Y Axis rotation:  A - WorldCenter | SinY   B - InvBoundsSize | CosY
// Free rotation:    A - 0                    B - 0
uniform float4 _UdonLightVolumeDataA[256];
uniform float4 _UdonLightVolumeDataB[256];

// Used with free rotation, World to Symmetric (-0.5, 0.5) UVW Matrix
uniform float4x4 _UdonLightVolumeInvWorldMatrix[256];

// AABB Bounds of islands on the 3D Texture atlas
uniform float4 _UdonLightVolumeUvwMin[768];
uniform float4 _UdonLightVolumeUvwMax[768];


// Linear single SH L1 channel evaluation
float LV_EvaluateSH(float L0, float3 L1, float3 n) {
    return L0 + dot(L1, n);
}

// Calculate Light Volume Color based on all SH components provided
float3 LV_EvaluateLightVolume(float3 L0, float3 L1r, float3 L1g, float3 L1b, float3 worldNormal){
    return float3(LV_EvaluateSH(L0.r, L1r, worldNormal), LV_EvaluateSH(L0.g, L1g, worldNormal), LV_EvaluateSH(L0.b, L1b, worldNormal));
}

// Default light probes
float3 LV_EvaluateLightProbe(float3 worldNormal) {
    float3 color;
    float3 L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    color.r = LV_EvaluateSH(L0.r, unity_SHAr.xyz, worldNormal);
    color.g = LV_EvaluateSH(L0.g, unity_SHAg.xyz, worldNormal);
    color.b = LV_EvaluateSH(L0.b, unity_SHAb.xyz, worldNormal);
    return color;
}

// AABB intersection check
bool LV_PointAABB(float3 pos, float3 min, float3 max) {
	return all(pos >= min && pos <= max);
}

// Checks if local UVW point is in bounds from -0.5 to +0.5
bool LV_PointLocalAABB(float3 localUVW){
    return all(abs(localUVW) <= 1);
}

// Calculates Island UVW for Fixed Rotation Mode
float3 LV_IslandFromFixedVolume(int volumeID, int texID, float3 worldPos){
    // World bounds
    float3 worldMin = _UdonLightVolumeDataA[volumeID].xyz;
    float3 worldMax = _UdonLightVolumeDataB[volumeID].xyz;
    // UVW bounds
    int uvwID = volumeID * 3 + texID;
    float3 uvwMin = _UdonLightVolumeUvwMin[uvwID].xyz;
    float3 uvwMax = _UdonLightVolumeUvwMax[uvwID].xyz;
    // Ramapping world bounds to UVW bounds
    return clamp(uvwMin + (worldPos - worldMin) * (uvwMax - uvwMin) / (worldMax - worldMin), uvwMin, uvwMax);
}

// Calculates local UVW for Y Axis rotation mode
float3 LV_LocalFromYAxisVolume(int volumeID, float3 worldPos){
    // Bounds and rotation data
    float3 invBoundsSize = _UdonLightVolumeDataB[volumeID].xyz;
    float3 boundsCenter = _UdonLightVolumeDataA[volumeID].xyz;
    float sinY = _UdonLightVolumeDataA[volumeID].w;
    float cosY = _UdonLightVolumeDataB[volumeID].w;
    // Ramapping world bounds to UVW bounds
    float3 p = worldPos - boundsCenter;
    float localX = p.x * cosY - p.z * sinY;
    float localZ = p.x * sinY + p.z * cosY;
    float localY = p.y;
    return float3(localX, localY, localZ) * invBoundsSize;
}

// Calculates local UVW for Free rotation mode
float3 LV_LocalFromFreeVolume(int volumeID, float3 worldPos) {
    return mul(_UdonLightVolumeInvWorldMatrix[volumeID], float4(worldPos, 1.0)).xyz;
}

// Calculates Island UVW from local UVW
float3 LV_LocalToIsland(int volumeID, int texID, float3 localUVW){
    // UVW bounds
    int uvwID = volumeID * 3 + texID;
    float3 uvwMin = _UdonLightVolumeUvwMin[uvwID].xyz;
    float3 uvwMax = _UdonLightVolumeUvwMax[uvwID].xyz;
    // Ramapping world bounds to UVW bounds
    return clamp(lerp(uvwMin, uvwMax, localUVW + 0.5), uvwMin, uvwMax);
}

// Samples 3 SH textures and packing them into L1 channels
void LV_SampleLightVolumeTex(float3 uvw0, float3 uvw1, float3 uvw2, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b) {
    // Sampling 3D Atlas
    float4 tex0 = tex3D(_UdonLightVolume, uvw0);
    float4 tex1 = tex3D(_UdonLightVolume, uvw1);
    float4 tex2 = tex3D(_UdonLightVolume, uvw2);
    // Packing final data
    L0 = tex0.rgb;
    L1r = float3(tex1.r, tex2.r, tex0.a);
    L1g = float3(tex1.g, tex2.g, tex1.a);
    L1b = float3(tex1.b, tex2.b, tex2.a);
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

// Bounds mask
float LV_BoundsMask(float3 pos, float3 minBounds, float3 maxBounds, float edgeSmooth) {
    float3 distToMin = (pos - minBounds) / edgeSmooth;
    float3 distToMax = (maxBounds - pos) / edgeSmooth;
    float3 fade = saturate(min(distToMin, distToMax));
    return fade.x * fade.y * fade.z;
}

// Bounds mask, but for rotated in world space, using symmetric UVW
float LV_BoundsMaskOBB(float3 symmetricUVW, float3 edgeSmooth, float3 invBoundsScale) {
    float3 edgeSmoothLocal = edgeSmooth * invBoundsScale;
    float3 distToMin = (symmetricUVW + 0.5) / edgeSmoothLocal;
    float3 distToMax = (0.5 - symmetricUVW) / edgeSmoothLocal;
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
        if (LV_PointAABB(worldPos, _UdonLightVolumeDataA[id].xyz, _UdonLightVolumeDataB[id].xyz)) {
            if (volumeID_A != -1) { volumeID_B = id; break; }
            else volumeID_A = id;
        }
    }
    
    // If no volumes found, using fallback to light probes
    if (volumeID_A == -1) return LV_EvaluateLightProbe(worldNormal); 
    // If at least, main, dominant volume found
    
    float mask = LV_BoundsMask(worldPos, _UdonLightVolumeDataA[volumeID_A].xyz, _UdonLightVolumeDataB[volumeID_A].xyz, _UdonLightVolumeBlend); // Mask to blend volume
        
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