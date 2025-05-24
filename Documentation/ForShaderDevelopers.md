[VRC Light Volumes](/README.md) | [How to Use](/Documentation/HowToUse.md) | [Best Practices](/Documentation/BestPractices.md) | [Udon Sharp API](/Documentation/UdonSharpAPI.md) | **For Shader Developers** | [Compatible Shaders](/Documentation/CompatibleShaders.md)
# For shader developers

If you are a shader developer, it should be easy to integrate Light Volumes support into your shader.

Both shader code way with a .cginc file and Amplify Shader Editor way with special nodes are available!

## Integrating Light Volumes with Amplify Shader Editor (ASE)

There are few ASE nodes available for you for an easy integration. Look into `Packages/VRC Light Volumes/Shaders/ASE Shaders` folder to check the integration examples.

### LightVolume

Required to get the Spherical Harmonics components. Using the output values you get from it, you can calculate the speculars for your custom lighting setup.

`AdditiveOnly` flag specifies if you need to only sample additive volumes. Useful for static lightmapped meshes. 

### LightVolumeEvaluate

Calculates the final color you get from the light volume in some kind of a physically realistic way. But alternatively you can implement your own "Evaluate" function to make the result matching your toon shader, for example.

You should usually multiply it by your "Albedo" and add to the final color, as an emission.

### LightVolumeSpecular

Calculates approximated speculars based on SH components. Can be used with Light Volumes or even with any other SH L1 values, like Unity default light probes. The result should be added to the final color, just like emission. You should NOT multiply this by albedo color!

`Dominant Direction` flag specifies if you want to use a simpler and lighter way of generating speculars. Generates one color specular for the dominant light direction instead of three color speculars in a regular method.

## Light Volume integration through shader code

First of all, you need to include the "LightVolumes.cginc" file provided with this asset, into your shader:  `#include "LightVolumes.cginc"`. 
Also be sure that you included the "UnityCG.cginc" file **BEFORE** to support the fallback to unity's light probes:  `#include "UnityCG.cginc"`

All the functions are recommended to use in the fragment shader. All the calculations are cheap enough.

### 1. Basic Light Volumes Integration

Start by replacing your light probe logic (usually where `ShadeSH9()` or `unity_SHAr` is used) with `LightVolumeSH()`

Evaluate the returned SH data using `LightVolumeEvaluate()` But you can use your own method to get the lighting color.  

Typically, the result color should be multiplied by the albedo and added to the final fragment color. You may also apply AO or other adjustments before combining it.

> `LightVolumeSH()` automatically falls back to Unity’s built-in light probes if Light Volumes are not available. No need for a manual check.

### 2. Additive Light Volumes for Lightmapped Geometry

Additive light volumes are can cast light on your static lightmapped geometry. To make it work, you need to integrate a function into your lightmapped lighting section of the shader. It's probably somewhere where you use `unity_Lightmap` variable.

Call a `LightVolumeAdditiveSH()` function there to get SH components. This function is even lighter because only samples additive light volumes if they are provided. It returns zeroes if no additive light volumes found or Light Volumes are available in scene.

Then evaluate the color with `LightVolumeEvaluate()` and **add** the resulting color to your lightmap output.

> You can also check `_UdonLightVolumeEnabled > 0` to skip evaluation entirely when not LightVolumes are not represented in the scene.

### 3. Custom SH Evaluation Notes

If you use a custom evaluation method instead of `LightVolumeEvaluate()`, make sure you use L1 components too.

Using L0 only (ambient term) results in unrealistic shading and can make objects look translucent.
You must consider L1 directions—or at least the dominant direction and its magnitude for proper shading.

> Test your method with strong directional lighting baked into a volume. Incorrect evaluation may cause color artifacts or exposure issues.

### 4. Specular Lighting (Optional but Recommended)

You can enhance gloss and metal surfaces with speculars from SH data:

Use `LightVolumeSpecular()` function for colored speculars. Ideal for avatars.
Use `LightVolumeSpecularDominant()` for a single specular using dominant light direction. Better for hard surface PBR shaders.

Add the result straight to your final fragment color.

These functions already apply albedo internally **do not multiply again**. You can still apply your own specular occlusion/masking if needed.

> For more advanced shading (e.g. anisotropic specular), implement your own model based on SH data.

## Shader Functions

There are only a few functions that are really required for the integration: 

### void LightVolumeSH()
Required to get the Spherical Harmonics components. Using the output values you get from it, you can calculate the speculars for your custom lighting setup.

Also this values are required to calculate the final light you get from the light volume.

```
void LightVolumeSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b)
```

`float3 worldPos` - World position of the current fragment

`out float3 L0` - Outputs ambient color of the current fragment.

`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that stores the Red, Green and Blue light directions and power, as a magnitude of these vectors.

### float3 LightVolumeEvaluate()
Calculates the final color you get from the light volume in some kind of a physically realistic way. But alternatively you can implement your own "Evaluate" function to make the result matching your toon shader, for example.

You should usually multiply it by your "Albedo" and add to the final color, as an emission.

```
float3 LightVolumeEvaluate(float3 worldNormal, float3 L0, float3 L1r, float3 L1g, float3 L1b)
```

`float3 worldNormal` - World normal of the current fragment. Must be normalized to avoid artefacts.

`float3 L0`, `float3 L1r`, `float3 L1g`, `float3 L1b` - Spherical Harmonics components you got from the LightVolumeSH() function.

### void LightVolumeAdditiveSH()
Returns Spherical Harmonics components, just as LightVolumeSH() does, but only for volumes that work in additive mode. This function is much lighter than LightVolumeSH(), and useful for shaders that can be used in baked lightmaps mode.

Evaluate it and add to your lightmaps color if you want to implement the additive volumes support for the baked lightmaps.

```
void LightVolumeAdditiveSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b)
```

`float3 worldPos` - World position of the current fragment

`out float3 L0` - Outputs ambient color of the current fragment.

`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that stores the Red, Green and Blue light directions and power, as a magnitude of these vectors.

### float3 LightVolumeSpecular()
Calculates approximated speculars based on SH components. Can be used with Light Volumes or even with any other SH L1 values, like Unity default light probes. The result should be added to the final color, just like emission. You should NOT multiply this by albedo color!

Usually works much better for avatars, because can show several color speculars at the same time for each of R, G, B light directions. Slightly less performant than LightVolumeSpecularDominant()

```
float3 LightVolumeSpecular(float3 albedo, float smoothness, float metallic, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b)
```

`float3 albedo` - Final albedo color

`float smoothness` - Final surface smoothness

`float metallic` - Final surface metalness

`float3 worldNormal` - World normal of the current fragment. Must be normalized to avoid artefacts.

`float3 viewDir` - World space camera view direction. Must be normalized.

`out float3 L0` - Outputs ambient color of the current fragment.

`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that stores the Red, Green and Blue light directions and power, as a magnitude of these vectors.

You can also provide the surface's specular color directly.

```
float3 LightVolumeSpecular(float3 specColor, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b)
```

`float3 specColor` - Final surface specular color

### float3 LightVolumeSpecularDominant()
Calculates approximated speculars based on SH components. Can be used with Light Volumes or even with any other SH L1 values, like Unity default light probes. The result should be added to the final color, just like emission. You should NOT multiply this by albedo color!

Usually works better for static PBR surfaces, because can show a one color specular for the dominant light direction. Slightly more performant than LightVolumeSpecular()

```
float3 LightVolumeSpecularDominant(float3 albedo, float smoothness, float metallic, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b)
```

`float3 albedo` - Final albedo color

`float smoothness` - Final surface smoothness

`float metallic` - Final surface metalness

`float3 worldNormal` - World normal of the current fragment. Must be normalized to avoid artefacts.

`float3 viewDir` - World space camera view direction. Must be normalized.

`out float3 L0` - Outputs ambient color of the current fragment.

`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that stores the Red, Green and Blue light directions and power, as a magnitude of these vectors.

You can also provide the surface's specular color directly.

```
float3 LightVolumeSpecularDominant(float3 specColor, float3 worldNormal, float3 viewDir, float3 L0, float3 L1r, float3 L1g, float3 L1b)
```

`float3 specColor` - Final surface specular color

### float \_UdonLightVolumeEnabled
A global float variable that is not defined and stores `0` if there are no light volumes support on the current scene, or stores `1` if light volumes system is provided.

It's not mandatory to check the light volumes support by yourself, because **LightVolumeSH()** and **LightVolumeAdditiveSH()** functions already do it and fallback to Unity Light probes instead of using the light volumes.
