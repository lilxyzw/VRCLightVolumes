[VRC Light Volumes](../README.md) | **How to Use** | [Best Practices](../Documentation/BestPractices.md) | [Udon Sharp API](../Documentation/UdonSharpAPI.md) | [For Shader Developers](../Documentation/ForShaderDevelopers.md) | [Compatible Shaders](../Documentation/CompatibleShaders.md)

# How to Use

> **[VRC Light Volumes System](#VRC-Light-Volumes-System)**
> 	[Light Volumes for Avatars](#Light-Volumes-for-Avatars)
> 	[Light Volumes Quick World Setup](#Light-Volumes-Quick-World-Setup)
> 	[Point Light Volumes Quick World Setup](#Point-Light-Volumes-Quick-World-Setup)
>
> **[Additive Light Volumes](#Additive-Light-Volumes)**
> 	[What Are Additive Volumes?](#What-Are-Additive-Volumes?)
> 	[How to Bake an Additive Light Volume](#How-to-Bake-an-Additive-Light-Volume)
>
> **[Audio Link Integration](#Audio-Link-Integration)**
> 	[Audio Link Quick Setup](#Audio-Link-Quick-Setup)
> 	[Light Volume Audio Link Component Description](#Light-Volume-Audio-Link-Component-Description)
>
> **[TV Screens Integration](#TV-Screens-Integration)**
> 	[TV Screen Quick Setup](#TV-Screen-Quick-Setup)
> 	[Light Volume TVGI Component Description](#Light-Volume-TVGI-Component-Description)



## VRC Light Volumes System

VRC Light Volumes is fast and optimized nextgen lighting solution for VRChat. It can also work without VRChat, but was mostly designed to work with VRChat SDK3 and Udon.

#### VRC Light Volumes system consists of two main parts:

[Regular **Light Volumes**](#Light-Volumes-Quick-World-Setup) is a fast and optimized solution that replaces Unity's light probes with a better per-pixel voxel based lighting. It's similar to [Adaptive Probe Volumes (APV)](https://docs.unity3d.com/6000.1/Documentation/Manual/urp/probevolumes-concept.html) in Unity 6, but with manual ReflectionProbe-like volumes placement and some other extra features.

[**Point Light Volumes**]() is a fast and optimized custom lighting system that has it's own parametric Point Light, Spot Lights and Area Lights. Point Light Volumes are not voxel based, they forms the light parametrically, or based on special LUT textures (similar to IES). They can project light cookies or cubemaps. It can be up to 128 point lights visible in one scene at the same time. However, this system does not support realtime shadows.

### Light Volumes for Avatars

You just need to use a [shader that has VRC Light Volumes support](/Documentation/CompatibleShaders.md) and that's it. Nothing else to do!

> ⚠️ Note: Unfortunately, there is no way to make avatars cast light volumes light, it can only be integrated into worlds.

### Light Volumes Quick World Setup

1. Right-click in the **Hierarchy** and select `Light Volume`.
   You can also do it clicking on a **Reflection Probe** object, and new **Light Volume** will inherit its bounds.
2. Rename the new **Light Volume** to match the zone you're covering.
   Each volume must have a **unique name**.
3. In the **Light Volume** component, click `Edit Bounds` and resize the volume to fit your target area - similar to setting up a **Reflection Probe**. You can also just scale it with a scaling tool.
4. With `Adaptive Resolution` enabled, adjust `Voxels Per Unit` to control voxel density.
   Keep in mind: doubling the resolution of a 3D volume increases its file size by approximately **8x**. 
   Click `Preview Voxels` to show voxel placement grid to check the density.
5. Repeat this process to create as much volumes as you need to cover all of your areas.
6. Find the **Light Volume Manager** in your scene.
   It's automatically added when you create a **Light Volume** and lists all the volumes in your scene.
7. Assign a `Weight` to each volume.
   Volumes with higher weight have higher priority in overlapping areas.
8. Select your `Baking Mode` based on the lightmapper you're using.
   **Bakery** is highly recommended due to faster baking and better quality results.
9. Bake the scene.
10. For reference, check out the `Example` scene in the `Packages/VRC Light Volumes/Example` folder to see a working setup.

> ⚠️ Note: The number of active Light Volumes is limited to 32 per scene at a time. However, you can dynamically enable and disable volumes at runtime to work with more than 32 total, just not simultaneously.

### Point Light Volumes Quick World Setup

1. Right-click in the **Hierarchy** and select `Point Light Volume`.
   It will add a Point Light Volume object on your scene. It's just a simple configurable light source.

2. Select your desired Point Light Volume `Type`. It can be: `Point Light`, `Spot Light`, `Area Light`

   > ⚠️ Note: Point and Spot Lights are the cheapest. Area light can be ~8 times less performant than other light types, so use it only if you need a movable and scalable in runtime soft box, or if you really want to save memory. Otherwise, it's more performant to bake a regular Light Volume in a shape of an area light.

3. `Debug Range` shows the range in which point light volume affects meshes. Try not to overlap a lot of point light volumes. More overlaps means less performance.
4. In most of the cases you need to leave the `Shape` value as `Parametric` - it's the cheapest and the most useful mode. But if you want to project a light **cookie** (point light volume) or a **cubemap** (spot light volume), select `Custom` shape.
5. Choose the `Color`, `Intensity`, `Range` and `Angle` parameters as you wish.
6. Enable `Dynamic` if your light can move in runtime. Otherwise it will be static, which is a tiny bit cheaper.
7. Enabling `Baked Shadows` is useful when you want to bake 3D shadows for your static point light volumes. But it's a little bit more advanced thing, that is useful in some rare cases, so usually just keep it turned off.

> ⚠️ Note: You must use materials with a [shader that has VRC Light Volumes support](/Documentation/CompatibleShaders.md) for your world surfaces and props to see Point Light Volumes! Default Unity's shader will not work!

> ⚠️ Note: The number of active Point Light Volumes is limited to 128 per scene at a time. However, you can dynamically enable and disable point light volumes at runtime to work with more than 32 total, just not simultaneously.



## Additive Light Volumes

Before diving into additive volumes, here’s how **regular** light volumes work:

They use baked lighting data from the volume that contains a mesh and has the **highest weight**. When multiple volumes overlap, lighting data is **blended smoothly** between them.

### What Are Additive Volumes?

**Additive** light volumes work differently:

They **add their light** on top of the regular volumes. Additive volumes can also affect **lightmapped static geometry**, making them ideal for dynamic lighting like toggleable or interactive lights, etc.

### How to Bake an Additive Light Volume

Here will be explained how to bake a togglable light zone for your world. It's useful if you want to make a light switch for an apartment, or a light from a TV screen. If you want to make a point light, flashlight, or a projector, use a Point Light Volume instead! It's much cheaper and easier.

1. Create a **separate scene** for baking. Usually, a copy of your main scene if you want to bake light for a room.
2. **Disable or delete** all the lights that you don't want to bake into your additive light volume.
3. Add a **light volume** and all the lights that you want to bake.
4. Make sure the volume **fully contains the light's range**.
5. **Optionally:** For all of the **lightmap static** meshes, choose `Receive Global Illumination: Light Probes` to exclude them from baking lightmaps. (**Bakery** will still require a mesh that bakes lightmaps to start a bake)
6. Bake the scene.

**Once baking is done:**

1. **Copy** the baked Light Volume into your main scene.
2. **Enable** the `Additive` checkbox in the Light Volume component.
4. **Uncheck** the `Bake` flag to prevent this volume to be rebaked in future lightmapper bakes.
5. In **Light Volume Setup**, enable `Auto Update Volumes`.
6. In **Light Volume Setup** press `Pack Light Volumes` to generate the 3D atlas needed for volumes to work.
   
> ⚠️ Note: If you see some sharp edges, it means that some undesirable light was baked into the volume.
> You can increase the volume size or tweak the **Color Correction** to get rid of undesirable light. Lowering `Shadows` in color correction section usually helps.

Now you should see your additive volume lighting up the scene.
If it doesn't work, make sure you’re using a [shader that has VRC Light Volumes support](/Documentation/CompatibleShaders.md) for all of your world surfaces and props.

You can always change the color and it's intensity in runtime via udon script to animate the light. Turning on and off the light volume game object also toggles the baked light.

You can control how many additive volumes affect a pixel using the `Additive Max Overdraw` parameter in the **Light Volume Setup** component.

> ⚠️ Note: `Additive Max Overdraw` parameter limits how many **additive volumes** are sampled **per pixel**, not how many can exist in the scene overall. The more additive volumes **intersect**, the higher the performance cost.

## Audio Link Integration

This package includes a [AudioLink](https://github.com/llealloo/audiolink/) integration Udon script. 

**LightVolumeAudioLink** component can change Light Volumes, Point light Volumes and Mesh Renderers materials colors in runtime based on AudioLink.

### Audio Link Quick Setup

1. Add the **LightVolumeAudioLink** component to a GameObject in your scene.

2. One **LightVolumeAudioLink** can control Light Volumes, Point Light Volumes and material colors based on an `Audio Band` value you choose: `Bass`, `Low Mid`, `High Mid`, `Treble`

3. Assign your **Audio Link** object in the `Audio Link` field.

4. Add all Light Volumes you want to control to the `Target Light Volumes` list and Point Light Volumes in the `Target Point Light Volumes` list.

5. Add all Mesh Renderers you want to change color by Audio Link to the `Target Mesh Renderers` list.

   > ⚠️ Note: These meshes should use materials with **emission enabled**. The shader must include a property named `_EmissionColor` (the **Standard** shader supports this).

6. Adjust `Materials Intensity` to fine-tune the brightness of your materials. `Intensity` of Light Volumes and Point Light Volumes can be configured in their components.

7. In your **AudioLink** component, make sure that **GPU Readback** is enabled. Click `Enable readback` if it’s not already active.

8. Enable `Auto Update Volumes` in your **Light Volume Setup** to let it update the colors automatically in runtime.

9. Done! You should now see visual changes reacting to audio.
   Add more **LightVolumeAudioLink** components to control other AudioLink bands.

### Light Volume Audio Link Component Description

`Audio Link` - Reference to your Audio Link Manager that should control Light Volumes.

`Audio Band` - Defines which audio band will be used to control Light Volumes. Four bands available: **Bass, Low Mid, High Mid, Treble**.

`Delay` - Defines how many samples back in history we're getting data from. Can be a value from **0** to **127**. Zero means no delay at all.

`Smoothing Enabled` - Enables smoothing algorithm that tries to smooth out flickering that can usually be a problem.

`Smoothing` - Value from 0 to 1 that defines how much smoothing should be applied. **Zero** usually applies just a little bit of smoothing. **One** smoothes out almost all the fast blinks and makes intensity changing very slow.

`Override Color` - Overrides color to a one that is specified, instead of the color chosen by AudioLink. 

`Color` - Color that will be used when **Override Color** is enabled.

`Target Light Volumes` - List of the **Light Volumes** that should be affected by AudioLink.

`Target Point Light Volumes` - List of the **Point Light Volumes** that should be affected by AudioLink.

`Target Mesh Renderers` - List of the **Mesh Renderers** that has materials that should change color based on AudioLink.

`Materials Intensity` - Brightness multiplier of the materials that should change color based on AudioLink.



## TV Screens Integration

This package includes a simple Udon script for lighting integration from TV screens.

It works visually similar to [LTCGI](https://github.com/PiMaker/ltcgi) in some cases, but it does **not** support real screen reflections. Instead, it works best with **matte** environment materials.

#### Advantages

- Good performance
- Shadowmasks avatars and environment

#### Limitations
- Doesn't make screen reflectios like LTCGI
- Only projects a **single average screen color**

### TV Screen Quick Setup

1. Create a **separate scene** and bake the area affected by the screen light as an **additive light volume**.
   See the [Additive Volumes section](#How-to-Bake-an-Additive-Light-Volume) for detailed steps.

   > ⚠️ Note: Remove all unnecessary lights during baking. Keep only the screen mesh with a **bright emissive material**.

2. In your main scene, add the **LightVolumeTVGI** component to a GameObject.

3. Assign the `Target Render Texture` field with the **Render Texture used by your video player**.

   > ⚠️ Note: Make sure that `Enable Mip Maps` and `Auto Generate Mip Maps` are **Enabled** in the texture’s import settings.

4. Add all Light Volumes you want to control to the `Target Light Volumes` list. It's usually a one additive light volume.

5. **Optionally:** Add all Point Light Volumes you want to control to the `Target Point Light Volumes` list. This can be useful to make other lights to inherit your screen's color.

6. Tweak the `Intensity` of your additive light volumes in their own LightVolume components. Because sometimes GI from a screen can look too dim.

7. Enable `Auto Update Volumes` in your **Light Volume Setup** to let it update the colors automatically in runtime.

8. Done! The system will now update the light color at runtime, even affecting avatars.

If you see unwanted **sharp color transitions** in your additive volume, try adjusting the **Color Correction** settings in the Light Volume component. Lowering `Shadows` in color correction section usually helps.

### Light Volume TVGI Component Description

`Target Render Texture` - Render Texture used by your video player. Can be just a static texture if you want it to be. Make sure that **Enable Mip Maps** and **Auto Generate Mip Maps** are **Enabled** in the texture’s import settings.

`Anti Flickering` - Enables smoothing algorithm that tries to smooth out flickering that is usually a problem. Recommended to always be turned on.

`Target Light Volumes` - List of the **Light Volumes** that should be affected by the Light Volume TVGI script

`Target Point Light Volumes` - List of the **Point Light Volumes** that should be affected by the Light Volume TVGI script. Usually you don't need it at all.