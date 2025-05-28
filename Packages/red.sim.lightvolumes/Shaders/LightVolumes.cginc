#ifndef VRC_LIGHT_VOLUMES_INCLUDED
#define VRC_LIGHT_VOLUMES_INCLUDED

// Makes it possible to sample Texcure Cube Arrays in surface shaders. Thanks to error.mdl!
#if defined(SHADER_TARGET_SURFACE_ANALYSIS_MOJOSHADER)
    #define UNITY_DECLARE_TEXCUBEARRAY(tex) samplerCUBE tex
    #define UNITY_DECLARE_TEXCUBEARRAY_NOSAMPLER(tex) samplerCUBE tex
    #define UNITY_ARGS_TEXCUBEARRAY(tex) samplerCUBE tex
    #define UNITY_PASS_TEXCUBEARRAY(tex) tex

    #define UNITY_SAMPLE_TEXCUBEARRAY(tex,coord) texCUBE(tex,coord)
    #define UNITY_SAMPLE_TEXCUBEARRAY_LOD(tex,coord,lod) texCUBElod(tex, float4(coord, lod))
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER(tex,samplertex,coord)  texCUBE(tex,coord)
    #define UNITY_SAMPLE_TEXCUBEARRAY_SAMPLER_LOD(tex,samplertex,coord,lod) texCUBElod(tex, float4(coord, lod))
#endif

// Are Light Volumes enabled on scene?
uniform float _UdonLightVolumeEnabled;

// All volumes count in scene
uniform float _UdonLightVolumeCount;

// Additive volumes max overdraw count
uniform float _UdonLightVolumeAdditiveMaxOverdraw;

// Additive volumes count
uniform float _UdonLightVolumeAdditiveCount;

// Should volumes be blended with lightprobes?
uniform float _UdonLightVolumeProbesBlend;

// Should volumes be with sharp edges when not blending with each other
uniform float _UdonLightVolumeSharpBounds;

// Main 3D Texture atlas
uniform sampler3D _UdonLightVolume;

// World to Local (-0.5, 0.5) UVW Matrix 3x4
uniform float4 _UdonLightVolumeInvWorldMatrix3x4[96];

// L1 SH quaternion rotation (relative to baked rotation)
uniform float4 _UdonLightVolumeRotationQaternion[32];

// Value that is needed to smoothly blend volumes ( BoundsScale / edgeSmooth )
uniform float3 _UdonLightVolumeInvLocalEdgeSmooth[32];

// AABB Bounds of islands on the 3D Texture atlas. XYZ: UvwMin, W: Scale per axis
uniform float4 _UdonLightVolumeUvwScale[96];

// Color multiplier (RGB) | If we actually need to rotate L1 components at all (A)
uniform float4 _UdonLightVolumeColor[32];

// Point Lights count
uniform float _UdonPointLightVolumeCount;

// Point light position and inversed squared range 
uniform float4 _UdonPointLightVolumePosition[128];

// Point light color and spot light cos of outer angle
uniform float4 _UdonPointLightVolumeColor[128];

// Spot light direction and cone falloff
uniform float4 _UdonPointLightVolumeDirection[128];

// LUT tex array for spot lights
UNITY_DECLARE_TEX2DARRAY(_UdonPointLightVolumeLUT);

// Cubemap tex array for point lights
UNITY_DECLARE_TEXCUBEARRAY(_UdonPointLightVolumeCubemap);

// Smoothstep to 0, 1 but cheaper
float LV_Smoothstep01(float x) {
    return x * x * (3 - 2 * x);
}

// Rotates vector by Quaternion
float3 LV_MultiplyVectorByQuaternion(float3 v, float4 q) {
    float3 t = 2.0 * cross(q.xyz, v);
    return v + q.w * t + cross(q.xyz, t);
}


// Samples spot light
void LV_PointLight(uint id, float3 worldPos, inout float3 L0, inout float3 L1r, inout float3 L1g, inout float3 L1b, inout uint count) {
    
    // Light position and inversed squared range 
    float4 pos = _UdonPointLightVolumePosition[(uint) id];
    float invSqRange = pos.w;
    
    float3 dir = pos.xyz - worldPos;
    float sqlen = max(dot(dir, dir), 1e-6);
    float invSqLen = rcp(sqlen);
    
    // Culling spotlight by radius
    if (invSqLen < invSqRange ) return;
    
    // Color, angle, direction and cone falloff
    float4 color = _UdonPointLightVolumeColor[(uint) id];
    float cosAngle = color.w;
    float4 ldir = _UdonPointLightVolumeDirection[(uint) id];
    float coneFalloff = ldir.w;

    float3 dirN = dir * rsqrt(sqlen);
    float dirRadius = sqlen * invSqRange;
    
    float3 att = color.rgb; // Light attenuation
    
    if (cosAngle < 1) { // It is a spot light
        
        float spotMask = dot(ldir.xyz, -dirN) - cosAngle;
        if(spotMask < 0) return;
        if (coneFalloff > 0) { // If it uses default attenuation
            att *= saturate((1 - dirRadius) * rcp(dirRadius * 60 + 1.732f)) * LV_Smoothstep01(saturate(spotMask * coneFalloff));
        } else { // If it uses Attenuation LUT
            float spot = 1 - saturate(spotMask * rcp(1 - cosAngle));
            att *= UNITY_SAMPLE_TEX2DARRAY(_UdonPointLightVolumeLUT, float3(sqrt(float2(spot, dirRadius)), -coneFalloff)).xyz;
        }
        
    } else { // It is a point light
        
        att *= saturate((1 - dirRadius) * rcp(dirRadius * 60 + 1.732f));

        if (coneFalloff <= 0) { // If it uses a cubemap
            dirN = LV_MultiplyVectorByQuaternion(dirN, float4(ldir.xyz, cosAngle - 1)); // Rotate cubemap
            att *= UNITY_SAMPLE_TEXCUBEARRAY_LOD(_UdonPointLightVolumeCubemap, float4(dirN, -coneFalloff), 0).xyz;
        }
        
    }

    // Finnally adding SH components and incrementing counter
    count++;
    L0 += att;
    L1r += dirN * att.r;
    L1g += dirN * att.g;
    L1b += dirN * att.b;

}

// Samples spot light but for L0 only
void LV_PointLight_L0(uint id, float3 worldPos, inout float3 L0, inout uint count) {
    
    // Light position and inversed squared range 
    float4 pos = _UdonPointLightVolumePosition[(uint) id];
    float invSqRange = pos.w;
    
    float3 dir = pos.xyz - worldPos;
    float sqlen = max(dot(dir, dir), 1e-6);
    float invSqLen = rcp(sqlen);
    
    // Culling spotlight by radius
    if (invSqLen < invSqRange ) return;
    
    // Color, angle, direction and cone falloff
    float4 color = _UdonPointLightVolumeColor[(uint) id];
    float cosAngle = color.w;
    float4 ldir = _UdonPointLightVolumeDirection[(uint) id];
    float coneFalloff = ldir.w;
    
    float dirRadius = sqlen * invSqRange;
    
    float3 att = color.rgb; // Light attenuation
    
    float3 dirN = dir * rsqrt(sqlen);
    
    if (cosAngle < 1) { // It is a spot light
        float spotMask = dot(ldir.xyz, -dirN) - cosAngle;
        if(spotMask < 0) return;
        if (coneFalloff > 0) { // If it uses default attenuation
            att *= saturate((1 - dirRadius) * rcp(dirRadius * 60 + 1.732f)) * LV_Smoothstep01(saturate(spotMask * coneFalloff));
        } else { // If it uses Attenuation LUT
            float spot = 1 - saturate(spotMask * rcp(1 - cosAngle));
            att *= UNITY_SAMPLE_TEX2DARRAY(_UdonPointLightVolumeLUT, float3(sqrt(spot), dirRadius, -coneFalloff));
        }
        
    } else { // It is a point light
        
        att *= saturate((1 - dirRadius) * rcp(dirRadius * 60 + 1.732f));

        if (coneFalloff <= 0) { // If it uses a cubemap
            dirN = LV_MultiplyVectorByQuaternion(dirN, ldir); // Rotate cubemap
            att *= UNITY_SAMPLE_TEXCUBEARRAY_LOD(_UdonPointLightVolumeCubemap, float4(dirN, -coneFalloff), 0).xyz;
        }
        
    }

    // Finnally adding SH components and incrementing counter
    count++;
    L0 += att;

}

// Gets current rotation matrix by volume ID
float4x4 LV_Matrix(uint id) {
    int id3 = id * 3;
    float4 row0 = _UdonLightVolumeInvWorldMatrix3x4[id3];
    float4 row1 = _UdonLightVolumeInvWorldMatrix3x4[id3 + 1];
    float4 row2 = _UdonLightVolumeInvWorldMatrix3x4[id3 + 2];
    return float4x4(row0, row1, row2, float4(0, 0, 0, 1));
}

// Checks if local UVW point is in bounds from -0.5 to +0.5
bool LV_PointLocalAABB(float3 localUVW){
    return all(abs(localUVW) <= 0.5);
}

// Calculates local UVW using volume ID
float3 LV_LocalFromVolume(uint volumeID, float3 worldPos) {
    return mul(LV_Matrix(volumeID), float4(worldPos, 1.0)).xyz;
}

// Samples 3 SH textures and packing them into L1 channels
void LV_SampleLightVolumeTex(float3 uvw0, float3 uvw1, float3 uvw2, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b) {
    // Sampling 3D Atlas
    float4 tex0 = tex3Dlod(_UdonLightVolume, float4(uvw0, 0));
    float4 tex1 = tex3Dlod(_UdonLightVolume, float4(uvw1, 0));
    float4 tex2 = tex3Dlod(_UdonLightVolume, float4(uvw2, 0));
    // Packing final data
    L0 = tex0.rgb;
    L1r = float3(tex1.r, tex2.r, tex0.a);
    L1g = float3(tex1.g, tex2.g, tex1.a);
    L1b = float3(tex1.b, tex2.b, tex2.a);
}

// Bounds mask for a volume rotated in world space, using local UVW
float LV_BoundsMask(float3 localUVW, float3 invLocalEdgeSmooth) {
    float3 distToMin = (localUVW + 0.5) * invLocalEdgeSmooth;
    float3 distToMax = (0.5 - localUVW) * invLocalEdgeSmooth;
    float3 fade = saturate(min(distToMin, distToMax));
    return fade.x * fade.y * fade.z;
}

// Default light probes SH components
void LV_SampleLightProbe(out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b) {
    L0 = float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
    L1r = unity_SHAr.xyz;
    L1g = unity_SHAg.xyz;
    L1b = unity_SHAb.xyz;
}

// Default light probes L0 only
float3 LV_SampleLightProbe_L0() {
    return float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
}

// Linear single SH L1 channel evaluation
float LV_EvaluateSH(float L0, float3 L1, float3 n) {
    return L0 + dot(L1, n);
}

// Samples a Volume with ID and Local UVW
void LV_SampleVolume(uint id, float3 localUVW, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b) {
    
    // Additive UVW
    uint uvwID = id * 3;
    float4 uvwPos0 = _UdonLightVolumeUvwScale[uvwID];
    float4 uvwPos1 = _UdonLightVolumeUvwScale[uvwID + 1];
    float4 uvwPos2 = _UdonLightVolumeUvwScale[uvwID + 2];
    float3 uvwScale = float3(uvwPos0.w, uvwPos1.w, uvwPos2.w);
    
    float3 uvwScaled = saturate(localUVW + 0.5) * uvwScale;
    float3 uvw0 = uvwPos0.xyz + uvwScaled;
    float3 uvw1 = uvwPos1.xyz + uvwScaled;
    float3 uvw2 = uvwPos2.xyz + uvwScaled;
                
    // Sample additive
    LV_SampleLightVolumeTex(uvw0, uvw1, uvw2, L0, L1r, L1g, L1b);
    
    // Color correction
    float4 color = _UdonLightVolumeColor[id];
    L0 = L0 * color.rgb;
    L1r = L1r * color.r;
    L1g = L1g * color.g;
    L1b = L1b * color.b;
    
    // Rotate if needed
    if (color.a != 0) {
        float4 r = _UdonLightVolumeRotationQaternion[id];
        L1r = LV_MultiplyVectorByQuaternion(L1r, r);
        L1g = LV_MultiplyVectorByQuaternion(L1g, r);
        L1b = LV_MultiplyVectorByQuaternion(L1b, r);
    }
                
}

// Samples a Volume with ID and Local UVW, but L0 component only
float3 LV_SampleVolume_L0(uint id, float3 localUVW) {
    uint uvwID = id * 3;
    float4 uvwPos0 = _UdonLightVolumeUvwScale[uvwID];
    float3 uvwScale = float3(uvwPos0.w, _UdonLightVolumeUvwScale[uvwID + 1].w, _UdonLightVolumeUvwScale[uvwID + 2].w);
    float3 uvwScaled = saturate(localUVW + 0.5) * uvwScale;
    float3 uvw0 = uvwPos0.xyz + uvwScaled;
    return tex3Dlod(_UdonLightVolume, float4(uvw0, 0)).rgb * _UdonLightVolumeColor[id].rgb;
}

// Forms specular based on roughness
float LV_DistributionGGX(float NoH, float roughness) {
    float f = (roughness - 1) * ((roughness + 1) * (NoH * NoH)) + 1;
    return (roughness * roughness) / ((float) 3.141592653589793f * f * f);
}

// Faster normalize
float3 LV_Normalize(float3 v) {
    return rsqrt(dot(v, v)) * v;
}

// Calculates speculars for light volumes or any SH L1 data
float3 LightVolumeSpecular(float3 f0, float smoothness, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b) {
    
    float3 specColor = max(float3(dot(reflect(-L1r, worldNormal), viewDir), dot(reflect(-L1g, worldNormal), viewDir), dot(reflect(-L1b, worldNormal), viewDir)), 0);
    
    float3 rDir = LV_Normalize(LV_Normalize(L1r) + viewDir);
    float3 gDir = LV_Normalize(LV_Normalize(L1g) + viewDir);
    float3 bDir = LV_Normalize(LV_Normalize(L1b) + viewDir);
    
    float rNh = saturate(dot(worldNormal, rDir));
    float gNh = saturate(dot(worldNormal, gDir));
    float bNh = saturate(dot(worldNormal, bDir));
    
    float roughness = 1 - smoothness;
    float roughExp = roughness * roughness;
    
    float rSpec = LV_DistributionGGX(rNh, roughExp);
    float gSpec = LV_DistributionGGX(gNh, roughExp);
    float bSpec = LV_DistributionGGX(bNh, roughExp);
    
    float3 specs = (rSpec + gSpec + bSpec) * f0;
    float3 coloredSpecs = specs * specColor;
    
    float3 a = coloredSpecs + specs * L0;
    float3 b = coloredSpecs * 4;
    
    return max(lerp(a, b, smoothness), 0.0);
    
}

float3 LightVolumeSpecular(float3 albedo, float smoothness, float metallic, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b) {
    float3 specularf0 = lerp(0.04f, albedo, metallic);
    return LightVolumeSpecular(specularf0, smoothness, worldNormal, viewDir, L0, L1r, L1g, L1b);
}

// Calculates speculars for light volumes or any SH L1 data, but simplified, with only one dominant direction
float3 LightVolumeSpecularDominant(float3 f0, float smoothness, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b) {
    
    float3 dominantDir = L1r + L1g + L1b;
    float3 dir = LV_Normalize(LV_Normalize(dominantDir) + viewDir);
    float nh = saturate(dot(worldNormal, dir));
    
    float roughness = 1 - smoothness;
    float roughExp = roughness * roughness;
    
    float spec = LV_DistributionGGX(nh, roughExp);
    
    return max(spec * L0 * f0, 0.0) * 2;
    
}

float3 LightVolumeSpecularDominant(float3 albedo, float smoothness, float metallic, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b) {
    
    float3 specularf0 = lerp(0.04f, albedo, metallic);
    return LightVolumeSpecularDominant(specularf0, smoothness, worldNormal, viewDir, L0, L1r, L1g, L1b);
}

// Calculate Light Volume Color based on all SH components provided and the world normal
float3 LightVolumeEvaluate(float3 worldNormal, float3 L0, float3 L1r, float3 L1g, float3 L1b) {
    return float3(LV_EvaluateSH(L0.r, L1r, worldNormal), LV_EvaluateSH(L0.g, L1g, worldNormal), LV_EvaluateSH(L0.b, L1b, worldNormal));
}

// Calculates SH components based on the world position
void LightVolumeSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b) {

    // Initializing output variables
    L0  = float3(0, 0, 0);
    L1r = float3(0, 0, 0);
    L1g = float3(0, 0, 0);
    L1b = float3(0, 0, 0);
    
    
    // Clamping gloabal iteration counts
    uint pointCount = clamp((uint) _UdonPointLightVolumeCount, 0, 128);
    uint volumesCount = clamp((uint) _UdonLightVolumeCount, 0, 32);
    if (!_UdonLightVolumeEnabled || (volumesCount == 0 && pointCount == 0)) { // Fallback to default light probes if Light Volume are not enabled
        LV_SampleLightProbe(L0, L1r, L1g, L1b);
        return;
    }
    uint maxOverdraw = clamp((uint) _UdonLightVolumeAdditiveMaxOverdraw, 1, 32);
    uint additiveCount = clamp((uint) _UdonLightVolumeAdditiveCount, 0, 32);
    bool lightProbesBlend = _UdonLightVolumeProbesBlend;
    
    // Process Point Lights
    uint pcount = 0;
    [loop]
    for (uint pid = 0; pid < pointCount; pid++) {
        LV_PointLight(pid, worldPos, L0, L1r, L1g, L1b, pcount);
        if (pcount >= maxOverdraw) break;
    }
    
    uint volumeID_A = -1; // Main, dominant volume ID
    uint volumeID_B = -1; // Secondary volume ID to blend main with

    float3 localUVW   = float3(0, 0, 0); // Last local UVW to use in disabled Light Probes mode
    float3 localUVW_A = float3(0, 0, 0); // Main local UVW for Y Axis and Free rotations
    float3 localUVW_B = float3(0, 0, 0); // Secondary local UVW
    
    // Are A and B volumes NOT found?
    bool isNoA = true;
    bool isNoB = true;
    
    // Additive volumes variables
    uint addVolumesCount = 0;
    float3 L0_, L1r_, L1g_, L1b_;
    
    // Iterating through all light volumes with simplified algorithm requiring Light Volumes to be sorted by weight in descending order
    [loop]
    for (uint id = 0; id < volumesCount; id++) {
        localUVW = LV_LocalFromVolume(id, worldPos);
        if (LV_PointLocalAABB(localUVW)) { // Intersection test
            if (id < additiveCount) { // Sampling additive volumes
                if (addVolumesCount < maxOverdraw) {
                    LV_SampleVolume(id, localUVW, L0_, L1r_, L1g_, L1b_);
                    L0 += L0_;
                    L1r += L1r_;
                    L1g += L1g_;
                    L1b += L1b_;
                    addVolumesCount++;
                } 
            } else if (isNoA) { // First, searching for volume A
                volumeID_A = id;
                localUVW_A = localUVW;
                isNoA = false;
            } else { // Next, searching for volume B if A found
                volumeID_B = id;
                localUVW_B = localUVW;
                isNoB = false;
                break;
            }
        }
    }
    
    // Volume A SH components and mask to blend volume sides
    float3 L0_A  = float3(1, 1, 1);
    float3 L1r_A = float3(0, 0, 0);
    float3 L1g_A = float3(0, 0, 0);
    float3 L1b_A = float3(0, 0, 0);

    // If no volumes found, using Light Probes as fallback
    if (isNoA && lightProbesBlend) {
        LV_SampleLightProbe(L0_, L1r_, L1g_, L1b_);
        L0  += L0_;
        L1r += L1r_;
        L1g += L1g_;
        L1b += L1b_;
        return;
    }
        
    // Fallback to lowest weight light volume if outside of every volume
    localUVW_A = isNoA ? localUVW : localUVW_A;
    volumeID_A = isNoA ? volumesCount - 1 : volumeID_A;

    // Sampling Light Volume A
    LV_SampleVolume(volumeID_A, localUVW_A, L0_A, L1r_A, L1g_A, L1b_A);
    
    float mask = LV_BoundsMask(localUVW_A, _UdonLightVolumeInvLocalEdgeSmooth[volumeID_A]);
    if (mask == 1 || isNoA || (_UdonLightVolumeSharpBounds && isNoB)) { // Returning SH A result if it's the center of mask or out of bounds
        L0  += L0_A;
        L1r += L1r_A;
        L1g += L1g_A;
        L1b += L1b_A;
        return;
    }
    
    // Volume B SH components
    float3 L0_B  = float3(1, 1, 1);
    float3 L1r_B = float3(0, 0, 0);
    float3 L1g_B = float3(0, 0, 0);
    float3 L1b_B = float3(0, 0, 0);

    if (isNoB && lightProbesBlend) { // No Volume found and light volumes blending enabled

        // Sample Light Probes B
        LV_SampleLightProbe(L0_B, L1r_B, L1g_B, L1b_B);

    } else { // Blending Volume A and Volume B
            
        // If no volume b found, use last one found to fallback
        localUVW_B = isNoB ? localUVW : localUVW_B;
        volumeID_B = isNoB ? volumesCount - 1 : volumeID_B;
            
        // Sampling Light Volume B
        LV_SampleVolume(volumeID_B, localUVW_B, L0_B, L1r_B, L1g_B, L1b_B);
        
    }
        
    // Lerping SH components
    L0  += lerp(L0_B,  L0_A,  mask);
    L1r += lerp(L1r_B, L1r_A, mask);
    L1g += lerp(L1g_B, L1g_A, mask);
    L1b += lerp(L1b_B, L1b_A, mask);

}

// Calculates SH components based on the world position but for additive volumes only
void LightVolumeAdditiveSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b) {

    // Initializing output variables
    L0  = float3(0, 0, 0);
    L1r = float3(0, 0, 0);
    L1g = float3(0, 0, 0);
    L1b = float3(0, 0, 0);
    
    // Clamping gloabal iteration counts
    uint pointCount = clamp((uint) _UdonPointLightVolumeCount, 0, 128);
    uint additiveCount = clamp((uint) _UdonLightVolumeAdditiveCount, 0, 32);
    if (!_UdonLightVolumeEnabled || (additiveCount == 0 && pointCount == 0)) return;
    uint maxOverdraw = clamp((uint) _UdonLightVolumeAdditiveMaxOverdraw, 1, 32);
    uint count = min(additiveCount, maxOverdraw);
    
    // Process Point Lights
    uint pcount = 0;
    [loop] 
    for (uint pid = 0; pid < pointCount; pid++) {
        LV_PointLight(pid, worldPos, L0, L1r, L1g, L1b, pcount);
        if (pcount >= maxOverdraw) break;
    }
    
    // Additive volumes variables
    float3 localUVW = float3(0, 0, 0);
    float3 L0_, L1r_, L1g_, L1b_;
    
    // Iterating through all light volumes with simplified algorithm requiring Light Volumes to be sorted by weight in descending order
    [loop]
    for (uint id = 0; id < count; id++) {
        localUVW = LV_LocalFromVolume(id, worldPos);
        //Intersection test
        if (LV_PointLocalAABB(localUVW)) {
            LV_SampleVolume(id, localUVW, L0_, L1r_, L1g_, L1b_);
            L0 += L0_;
            L1r += L1r_;
            L1g += L1g_;
            L1b += L1b_;
        }
    }

}

// Calculates L0 components based on the world position
float3 LightVolumeSH_L0(float3 worldPos) {

    // Clamping gloabal iteration counts
    uint pointCount = clamp((uint) _UdonPointLightVolumeCount, 0, 128);
    uint volumesCount = clamp((uint) _UdonLightVolumeCount, 0, 32);
    if (!_UdonLightVolumeEnabled || (volumesCount == 0 && pointCount == 0)) { // Fallback to default light probes if Light Volume are not enabled
        return LV_SampleLightProbe_L0();
    }
    uint maxOverdraw = clamp((uint) _UdonLightVolumeAdditiveMaxOverdraw, 1, 32);
    uint additiveCount = clamp((uint) _UdonLightVolumeAdditiveCount, 0, 32);
    bool lightProbesBlend = _UdonLightVolumeProbesBlend;
    
    float3 L0 = float3(0, 0, 0);
    
    // Process Point Lights
    uint pcount = 0;
    [loop]
    for (uint pid = 0; pid < pointCount; pid++) {
        LV_PointLight_L0(pid, worldPos, L0, pcount);
        if (pcount >= maxOverdraw) break;
    }
    
    uint volumeID_A = -1; // Main, dominant volume ID
    uint volumeID_B = -1; // Secondary volume ID to blend main with

    float3 localUVW   = float3(0, 0, 0); // Last local UVW to use in disabled Light Probes mode
    float3 localUVW_A = float3(0, 0, 0); // Main local UVW for Y Axis and Free rotations
    float3 localUVW_B = float3(0, 0, 0); // Secondary local UVW
    
    // Are A and B volumes NOT found?
    bool isNoA = true;
    bool isNoB = true;
    
    // Additive volumes variables
    uint addVolumesCount = 0;
    
    // Iterating through all light volumes with simplified algorithm requiring Light Volumes to be sorted by weight in descending order
    [loop]
    for (uint id = 0; id < volumesCount; id++) {
        localUVW = LV_LocalFromVolume(id, worldPos);
        if (LV_PointLocalAABB(localUVW)) { // Intersection test
            if (id < additiveCount) { // Sampling additive volumes
                if (addVolumesCount < maxOverdraw) {
                    L0 += LV_SampleVolume_L0(id, localUVW);
                    addVolumesCount++;
                } 
            } else if (isNoA) { // First, searching for volume A
                volumeID_A = id;
                localUVW_A = localUVW;
                isNoA = false;
            } else { // Next, searching for volume B if A found
                volumeID_B = id;
                localUVW_B = localUVW;
                isNoB = false;
                break;
            }
        }
    }

    // If no volumes found, using Light Probes as fallback
    if (isNoA && lightProbesBlend) {
        return L0 + LV_SampleLightProbe_L0();
    }
        
    // Fallback to lowest weight light volume if outside of every volume
    localUVW_A = isNoA ? localUVW : localUVW_A;
    volumeID_A = isNoA ? volumesCount - 1 : volumeID_A;

    // Sampling Light Volume A
    float3 L0_A = LV_SampleVolume_L0(volumeID_A, localUVW_A);
    
    float mask = LV_BoundsMask(localUVW_A, _UdonLightVolumeInvLocalEdgeSmooth[volumeID_A]);
    if (mask == 1 || isNoA || (_UdonLightVolumeSharpBounds && isNoB)) { // Returning SH A result if it's the center of mask or out of bounds
        return L0 + L0_A;
    }
    
    // Volume B L0
    float3 L0_B  = float3(1, 1, 1);

    if (isNoB && lightProbesBlend) { // No Volume found and light volumes blending enabled

        // Sample Light Probes B
        L0_B = LV_SampleLightProbe_L0();

    } else { // Blending Volume A and Volume B
            
        // If no volume b found, use last one found to fallback
        localUVW_B = isNoB ? localUVW : localUVW_B;
        volumeID_B = isNoB ? volumesCount - 1 : volumeID_B;
            
        // Sampling Light Volume B
        L0_B = LV_SampleVolume_L0(volumeID_B, localUVW_B);
        
    }
        
    // Lerping L0
    return L0 + lerp(L0_B,  L0_A,  mask);

}

// Calculates L0 component based on the world position but for additive volumes only
float3 LightVolumeAdditiveSH_L0(float3 worldPos) {

    // Initializing output variables
    float3 L0  = float3(0, 0, 0);
    
    // Clamping gloabal iteration counts
    uint pointCount = clamp((uint) _UdonPointLightVolumeCount, 0, 128);
    uint additiveCount = clamp((uint) _UdonLightVolumeAdditiveCount, 0, 32);
    if (!_UdonLightVolumeEnabled || (additiveCount == 0 && pointCount == 0)) return L0;
    uint maxOverdraw = clamp((uint) _UdonLightVolumeAdditiveMaxOverdraw, 1, 32);
    uint count = min(additiveCount, maxOverdraw);
    
    // Process Point Lights
    uint pcount = 0;
    [loop] 
    for (uint pid = 0; pid < pointCount; pid++) {
        LV_PointLight_L0(pid, worldPos, L0, pcount);
        if (pcount >= maxOverdraw)
            break;
    }
    
    // Additive volumes variables
    float3 localUVW = float3(0, 0, 0);
   
    
    // Iterating through all light volumes with simplified algorithm requiring Light Volumes to be sorted by weight in descending order
    [loop]
    for (uint id = 0; id < count; id++) {
        localUVW = LV_LocalFromVolume(id, worldPos);
        //Intersection test
        if (LV_PointLocalAABB(localUVW)) {
            L0 += LV_SampleVolume_L0(id, localUVW);
        }
    }

    return L0;
    
}

#endif
