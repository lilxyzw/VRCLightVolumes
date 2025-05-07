[VRC Light Volumes](/README.md) | [How to Use](/Documentation/HowToUse.md) | [Best Practices](/Documentation/BestPractices.md) | [Optimizations](/Documentation/Optimizations.md) | [Udon Sharp API](/Documentation/UdonSharpAPI.md) | [For Shader Developers](/Documentation/ForShaderDevelopers.md) | [Compatible Shaders](/Documentation/CompatibleShaders.md)
# For shader developers
If you are a shader developer, it should be easy to integrate Light Volumes support into your shader. First of all, you need to include the "LightVolumes.cginc" file provided with this asset, into your shader:  `#include "LightVolumes.cginc"`. 
Also be sure that you included the "UnityCG.cginc" file **BEFORE** to support the fallback to unity's light probes:  `#include "UnityCG.cginc"`

There are only a few functions that are really required for the integration: 
### LightVolumeSH
Required to get the Spherical Harmonics components. Using the output values you get from it, you can calculate the speculars for your custom lighting setup. Also this values are required to calculate the final light you get from the light volume.

```
void LightVolumeSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b)
```

`float3 worldPos` - World position of the current fragment
`out float3 L0` - Outputs ambient color of the current fragment.
`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that srores the Red, Green and Blue light directions and power, as a magnitude of these vectors.
### LightVolumeEvaluate
Calculates the final color you get from the light volume in some kind of a physically realistic way. But alternatively you can implement your own "Evaluate" function to make the result matching your toon shader, for example.

```
float3 LightVolumeEvaluate(float3 worldNormal, float3 L0, float3 L1r, float3 L1g, float3 L1b)
```

Function returns the final color of the current fragment. Your "Albedo" can be multiplied by this value.
`float3 worldNormal` - World normal of the current fragment. Must be normalized to avoid artefacts.
`float3 L0`, `float3 L1r`, `float3 L1g`, `float3 L1b` - Spherical Harmonics components you got from the LightVolumeSH() function.
### LightVolumeAdditiveSH
Returns Spherical Harmonics components, just as LightVolumeSH() does, but only for volumes that work in additive mode. This function is much lighter than LightVolumeSH(), and useful for shaders that can be used in baked lightmaps mode. Evaluate it and add to your lightmaps color if you want to implement the additive volumes support for the baked lightmaps.

```
void LightVolumeAdditiveSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b)
```

`float3 worldPos` - World position of the current fragment
`out float3 L0` - Outputs ambient color of the current fragment.
`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that srores the Red, Green and Blue light directions and power, as a magnitude of these vectors.

### \_UdonLightVolumeEnabled
A global float variable that is not defined and stores 0 if there are no light volumes support on the current scene, or stores 1 if light volumes system is provided. It's not mandatory to check the light volumes support by yourself, because LightVolumeSH() and LightVolumeAdditiveSH() functions are already doed it and fallbacks do Unity Light probes instead of using the light volumes.
