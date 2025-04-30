# VRC Light Volumes
VRC Light Volumes is a nextgen voxel based light probes replacement for VRChat.

This is a free open-source asset, so if you like it, I would be very happy if you **[Support me on Patreon](https://www.patreon.com/red_sim/ "Support me on Patreon")** There is a bunch of other cool assets you will get there!

## Main Features
- Baked per-pixel voxel based lighting
- Affects avatars and dynamic props (shader integration required)
- Really fast and performant
- Can create cheap dynamic light sources that can be moved in runtime
- Can change color in runtime
- Auto bakes light into a single texture atlas
- Works with dynamic batching, which potentially increases performance
- Supports up to 256 Light Volumes per scene
- Light volumes can be culled in runtime with external scripts
- Very easy and fast to setup
- Works with Bakery or the default Unity Lightmapper
- No conflicts with default Unity's light probes
- It just looks beautiful!

## Shaders that are already support Light Volumes

*Yet to be added*

Contact me in Discord if you want your shader to be added in this list.
Discord: @RED_SIM

## Installation through VRChat Creator Companion (Will be available in future)
1. Go to my VPM Listing web page: https://redsim.github.io/vpmlisting/
2. Press "Add to VCC"
3. Confirm adding in the popup dialogue window

## Installation with a unity package
1. Go to my Github releases page: https://github.com/REDSIM/VRCLightVolumes/releases
2. Download the .unitypackage file of the latest build
3. Drag and drop the file into your Unity project

## How to use it, or a quick manual
1. Click RMB in your hierarchy window and choose "Light Volume"
2. Name your Light Volume game object related to the zone you want to apply it to. (Light volumes names must be different)
3. In your Light Volume component press "Edit Bounds" and match the  colume size with the zone in your scene, you want to apply a volume to. Very similar to a Reflection Probe setup.
4. With the "Adaptive Resolution" mode enabled in this component, change the "Voxels Per Unit" value to change the volume's voxel density. You can preview your voxels and even see the approximate file size it will have after baking.
5. Setup all the volumes you need for your scene. You can change the resolution depending on the lighting conditions or the volume size. Note that increasing resolution for a 3D volume, increases the file size cubically, so be aware of a big value!
6. Look for a game object called "Light Volume Manager" on your scene. It automatically appears in your scene if you use any Light Volumes, because volumes depends on it.
7. Drag and drop all the volumes you setuped into the list in Light Volume Manager.
8. Select "Baking Mode" depending on the Lightmapper you gonna use.
9. Bake your scene.
10. Now it should work! To test it, use any mesh with a shader that supports VRC Light Volumes. There are few shaders provided with this asset in a shader category called "Light Volume Samples".
11. You can always check the example scene in the "Example" folder to look how it works.

## For shader developers
If you are a shader developer, it should be easy to integrate Light Volumes support into your shader. First of all, you need to include the "LightVolumes.cginc" file provided with this asset, into your shader:  `#include "LightVolumes.cginc"`. 
Also be sure that you included the "UnityCG.cginc" file **BEFORE** to support the fallback to unity's light probes:  `#include "UnityCG.cginc"`

There are only a few functions that are really required for the integration: 
#### LightVolumeSH
Required to get the Spherical Harmonics components. Using the output values you get from it, you can calculate the speculars for your custom lighting setup. Also this values are required to calculate the final light you get from the light volume.

```
void LightVolumeSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b)
```

`float3 worldPos` - World position of the current fragment
`out float3 L0` - Outputs ambient color of the current fragment.
`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that srores the Red, Green and Blue light directions and power, as a magnitude of these vectors.
#### LightVolumeEvaluate
Calculates the final color you get from the light volume in some kind of a physically realistic way. But alternatively you can implement your own "Evaluate" function to make the result matching your toon shader, for example.

```
float3 LightVolumeEvaluate(float3 worldNormal, float3 L0, float3 L1r, float3 L1g, float3 L1b)
```

Function returns the final color of the current fragment. Your "Albedo" can be multiplied by this value.
`float3 worldNormal` - World normal of the current fragment. Must be normalized to avoid artefacts.
`float3 L0`, `float3 L1r`, `float3 L1g`, `float3 L1b` - Spherical Harmonics components you got from the LightVolumeSH() function.
#### LightVolumeAdditiveSH
Returns Spherical Harmonics components, just as LightVolumeSH() does, but only for volumes that work in additive mode. This function is much lighter than LightVolumeSH(), and useful for shaders that can be used in baked lightmaps mode. Evaluate it and add to your lightmaps color if you want to implement the additive volumes support for the baked lightmaps.

```
void LightVolumeAdditiveSH(float3 worldPos, out float3 L0, out float3 L1r, out float3 L1g, out float3 L1b)
```

`float3 worldPos` - World position of the current fragment
`out float3 L0` - Outputs ambient color of the current fragment.
`out float3 L1r`, `out float3 L1g`, `out float3 L1b` - Outputs vectors that srores the Red, Green and Blue light directions and power, as a magnitude of these vectors.

#### \_UdonLightVolumeEnabled
A global float variable that is not defined and stores 0 if there are no light volumes support on the current scene, or stores 1 if light volumes system is provided. It's not mandatory to check the light volumes support by yourself, because LightVolumeSH() and LightVolumeAdditiveSH() functions are already doed it and fallbacks do Unity Light probes instead of using the light volumes.
