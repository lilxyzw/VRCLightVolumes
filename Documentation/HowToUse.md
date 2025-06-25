[VRC Light Volumes](../README.md) | **How to Use** | [Best Practices](../Documentation/BestPractices.md) | [Udon Sharp API](../Documentation/UdonSharpAPI.md) | [For Shader Developers](../Documentation/ForShaderDevelopers.md) | [Compatible Shaders](../Documentation/CompatibleShaders.md)

# How to Use

**[VRC Light Volumes System](#VRC-Light-Volumes-System)**

- [Light Volumes for Avatars](#Light-Volumes-for-Avatars)
- [Light Volumes Quick World Setup](#Light-Volumes-Quick-World-Setup)
- [Point Light Volumes Quick World Setup](#Point-Light-Volumes-Quick-World-Setup)

**[Regular Light Volumes](#Regular-Light-Volumes)**

- [Light Volumes Placement](#Light-Volumes-Placement)
- [Auto Light Probes Placement](#Auto-Light-Probes-Placement)
- [Additive Light Volumes](#Additive-Light-Volumes)
- [Light Volumes Color Correction](#Light-Volumes-Color-Correction)
- [Light Volume Component Description](#Light Volume-Component-Description)

[Point Light Volumes](#Point-Light-Volumes)

- [Point Light Volumes Placement](#Point-Light-Volumes-Placement)
- [Light Shape](#Light-Shape)
- [Baked Point Light Volume Shadows](#Baked-Point-Light-Volume-Shadows)
- [Point Light Volume Component Description](#Point-Light-Volume-Component-Description)

**[Audio Link Integration](#Audio-Link-Integration)**

- [Audio Link Quick Setup](#Audio-Link-Quick-Setup)
- [Light Volume Audio Link Component Description](#Light-Volume-Audio-Link-Component-Description)

**[TV Screens Integration](#TV-Screens-Integration)**

- [TV Screen Quick Setup](#TV-Screen-Quick-Setup)
- [Light Volume TVGI Component Description](#Light-Volume-TVGI-Component-Description)



## VRC Light Volumes System

VRC Light Volumes is fast and optimized nextgen lighting solution for VRChat. It can also work without VRChat, but was mostly designed to work with VRChat SDK3 and Udon.

#### VRC Light Volumes system consists of two main parts:

[Regular **Light Volumes**](#Light-Volumes-Quick-World-Setup) is a fast and optimized solution that replaces Unity's light probes with a better per-pixel voxel based lighting. It's similar to Adaptive Probe Volumes (APV) in Unity 6, but with manual ReflectionProbe-like volumes placement and some other extra features.

[**Point Light Volumes**](#Point-Light-Volumes-Quick-World-Setup) is a fast and optimized custom lighting system that has it's own parametric Point Light, Spot Lights and Area Lights. Point Light Volumes are not voxel based, they forms the light parametrically, or based on special LUT textures (similar to IES). They can project light cookies or cubemaps. It can be up to 128 point lights visible in one scene at the same time. However, this system does not support realtime shadows.

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
5. Choose the `Color`, `Intensity`, `Range`, `Angle` and `Falloff` parameters as you wish.
6. Enable `Dynamic` if your light can move in runtime. Otherwise it will be static, which is a tiny bit cheaper.
7. Enabling `Baked Shadows` is useful when you want to bake 3D shadows for your static point light volumes. But it's a little bit more advanced thing, that is useful in some rare cases, so usually just keep it turned off.

> ⚠️ Note: You must use materials with a [shader that has VRC Light Volumes support](/Documentation/CompatibleShaders.md) for your world surfaces and props to see Point Light Volumes! Default Unity's shader will not work!

> ⚠️ Note: The number of active Point Light Volumes is limited to 128 per scene at a time. However, you can dynamically enable and disable point light volumes at runtime to work with more than 128 total, just not simultaneously.



## Regular Light Volumes

Light Volumes is a fast and optimized solution that replaces Unity's light probes with a better per-pixel voxel based lighting. It's similar to Adaptive Probe Volumes (APV) in Unity 6, but with manual ReflectionProbe-like volumes placement and some other extra features.

### Light Volumes Placement

**Light Volumes** should be placed to cover most of the walkable areas in your world. It's perfectly fine to leave some areas uncovered - in those cases, regular Unity Light Probes will be used as a fallback.

If your scene is mostly lit with soft, uniform diffuse lighting, you don’t need to use very high light volumes resolution. In this case `Voxels Per Unit` value can be from `1` to `3` approximately, or even less for big open worlds.

However, if your scene contains sharp shadows or high-contrast lighting, using higher density is strongly recommended! In this case `Voxels Per Unit` value can be from `3` to `15` approximately, depending on the world size itself.

> ⚠️ Note:  Always keep an eye on the *Size Estimation* in your Light Volume component - increasing the density can quickly make the data size extremely large.

A good practice is to place one large, low-resolution Light Volume to cover the entire world, and then add smaller, higher-density volumes in areas with sharp shadows or small, detailed light sources. Just make sure to extend the bounds slightly beyond the target area - some padding is needed to blend the volume edges smoothly. The `Smooth Blending` property in the Light Volume component controls the size of this padding.

If you already have **Reflection Probes** in your scene, you'll probably want your **Light Volumes** to match their bounds. To do this, right-click the Reflection Probe in the Hierarchy and create **Light Volume** as a child. Any volume created under any reflection probe will automatically inherit its bounds.

### Auto Light Probes Placement

Even though **Light Volumes** are designed to replace **Light Probes**, you should still include **Light Probes** in your scene to ensure proper lighting for avatars that do not support **VRC Light Volumes**.

Since Unity does not provide an easy way to automatically place **Light Probes**, doing it manually can be very time-consuming. **VRC Light Volumes** includes a built-in feature to generate **Light Probes** in just a few clicks.

Simply click the `Generate Light Probes` button in the Light Volume component. This will open a small configuration window and display a preview of the probes that will be placed in your scene. The probes will be arranged in a cuboid shape within the bounds of your Light Volume. The **Light Probe** density is usually much lower than the Light Volume density, but you can adjust it in the configuration window as needed.

Once you're happy with the settings, click the `Create Light Probe Group` button. This will create a Light Probe Group as a child of the Light Volume. You can manually edit or remove any unwanted probes, and you can also move the Light Probe Group out of the Light Volume hierarchy if you prefer.

### Additive Light Volumes

Before diving into additive volumes, here’s how **regular** light volumes work:

They use baked lighting data from the volume that contains a mesh and has the **highest weight**. When multiple volumes overlap, lighting data is **blended smoothly** between them.

#### Additive light volumes work differently

They **add their light** on top of the regular volumes. Additive volumes can also affect **lightmapped static geometry**, making them ideal for dynamic lighting like toggleable or interactive lights, etc.

#### How to Bake an Additive Light Volume

Here will be explained how to bake a togglable light zone for your world. It's useful if you want to make a light switch for an apartment, or a light from a TV screen. If you want to make a point light, flashlight, or a projector, use a Point Light Volume instead! It's much cheaper and easier.

1. Create a **separate scene** for baking. Usually, a copy of your main scene if you want to bake light for a room.
2. **Disable or delete** all the lights that you don't want to bake into your additive light volume.
3. Add a **light volume** and all the lights that you want to bake.
4. Make sure the volume **fully contains the light's range**.
5. **Optionally:** For all of the **lightmap static** meshes, choose `Receive Global Illumination: Light Probes` to exclude them from baking lightmaps. (**Bakery** will still require a mesh that bakes lightmaps to start a bake)
6. Bake the scene.

#### Once baking is done

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

### Light Volumes Color Correction

The Light Volume component includes a simple color correction section. It's mainly useful for adjusting the brightness of your baked data, since it sometimes won’t match exactly how it appears in baked lightmaps.

Another common use case is reducing the `Shadows` brightness for baked additive Light Volumes to hide visible undesirable light at the edges.

The `Exposure` property adjusts the overall brightness of the baked data, similar to exposure in photography.

The `Shadows` and `Highlights` properties adjust the brightness of dark and bright regions, respectively. These settings are helpful for correcting underexposed or overexposed areas of the baked data.

Each time you change a value in this section, the **Light Volumes Atlas** will be automatically repacked. This process can take a few seconds, or longer if your atlas is large.

### Light Volume Component Description

`Edit Bounds` button enables a cuboid editing tool to configure the Light Volume bounds. Be sure you have **Gizmos** enabled in your viewport to see the tool handles.

`Preview Voxels` button shows all the voxels to estimate the density of your Light Volume. If you have light volumes baked on your scene, voxels will be shaded in the baked color to preview the baked light.

`Size in VRAM` and `Size in bundle` indicators shows an estimated size of the baked data. Both sizes are estimated, and the final size will be shown in the **Light Volume Setup** component after the data is baked.

#### Volume Setup

`Dynamic` - Defines whether this volume can be moved in runtime. Disabling this option slightly improves performance.

`Additive` - Additive volumes apply their light on top of others as an overlay. Useful for movable and togglable lights. They can also project light onto static lightmapped objects if the surface shader supports it.

`Color` - Multiplies the volume’s color by this value.

`Intensity` - Brightness of the volume.

`Smooth Blending` - Size in meters of this Light Volume's overlapping regions for smooth blending with other volumes.

#### Baked Data

`Texture 0` - Texture3D with baked SH data required for future atlas packing. It won't be uploaded to VRChat. (L0r, L0g, L0b, L1r.z)

`Texture 1` - Texture3D with baked SH data required for future atlas packing. It won't be uploaded to VRChat. (L1r.x, L1g.x, L1b.x, L1g.z)

`Texture 2` - Texture3D with baked SH data required for future atlas packing. It won't be uploaded to VRChat. (L1r.y, L1g.y, L1b.y, L1b.z)

`Shadows Texture` - Optional Texture3D with baked shadows data for future atlas packing. It won't be uploaded to VRChat. Stores occlusion for up to 4 nearby point light volumes.

#### Color Correction

`Exposure` - Makes volume brighter or darker

`Shadows` - Makes dark volume colors brighter or darker.

`Highlights` - Makes bright volume colors brighter or darker.

#### Baking Setup

`Bake` - Uncheck it if you don't want to rebake this volume's textures.

`Point Light Shadows` - Uncheck it if you don't want to rebake occlusion data required for baked point light volumes shadows.

`Blur Shadows` - Post-processes the baked occlusion texture with a softening blur. This can help mitigate 'blocky' shadows caused by aliasing, but also makes shadows less crispy.

`Adaptive Resolution` - Automatically sets the resolution based on the Voxels Per Unit value.

`Voxels Per Unit` - Number of voxels used per meter, linearly. This value increases the Light Volume file size cubically.

`Resolution` - Manual Light Volume resolution in voxel count.



## Point Light Volumes

**Point Light Volumes** is a fast and optimized custom lighting system that has it's own parametric Point Light, Spot Lights and Area Lights. Point Light Volumes are not voxel based, they forms the light parametrically, or based on special LUT textures (similar to IES). They can project light cookies or cubemaps. It can be up to 128 point lights visible in one scene at the same time. However, this system does not support real-time shadows.

### Point Light Volumes Placement

**Point Light Volumes** are mostly useful in cases when you need independent dynamic lights, that can be individually toggled, moved or changed color in runtime.

If you just have a lot of point light sources that are static and don't change any of their properties in runtime, consider using a regular Light Volume and bake as much lights into it as you want. It is usually much more optimized than placing a lot of individual point lights. However, one point light (Not in Area Light mode!) is usually ~4 times cheaper than a one regular additive light volume.

Area light can be ~8 times less performant than other light types, so use it only if you need a movable and scalable in runtime soft box, or if you really want to save memory. Otherwise, it's more performant to bake a regular Light Volume in a shape of an area light.

Note that more point lights you have active in your scene, the less performance you'll have. So, consider manually turning off unused point lights if you have a lot of them at your scene.

The more point light volumes overlap, the less performance you'll have. Try not to make an insanely huge range for your lights. Use `Debug Range` flag in your Point Light Volume component to preview the region affected by your point light.

**Point light Volume** in `Area Light` mode calculates the **range** automatically based on its size, brightness and the `Area Light Brightness Cutoff` parameter in Light Volume Setup component.

### Light Shape

#### Parametric

Point Light Volumes and Spot Light Volumes use `Parametric` light shape by default. They apply an inverse-square attenuation formula, which makes the lighting mostly physically correct. However, to improve performance and enable culling, a `Range` property is used - this is not physically accurate, but it greatly optimizes performance.

In Spot Light mode, several additional parametric shape properties are available. The `Angle` property controls the cone angle of the spotlight in degrees. Unlike Unity’s built-in Spot Light, this angle can exceed 180 degrees to create an inverted cone. The `Falloff` property adjusts the softness of the cone edges.

#### LUT

If you want to create a complex light shape and attenuation, `LUT` light shape is what you need. For the Spot Light mode, LUT works similar to IES light shape format, but easier for people to create their own LUT presets.

**LUT** (Look Up Table) texture data in horizontal direction describes light color change from the center of the spot light cone to the cone edge. Vertical direction of the texture data describes the light attenuation, that is usually should be an inversed square distribution, but you can make it linear or anything else if you want to create some weird light effects.

In Point Light mode, only vertical texture direction is used, as there are no cone. Horizontal data will just be ignored.

#### Custom

If you want just to project a light cookie texture, you can use `Custom` light shape mode. Unlike Unity’s built-in Spot Light, here cookie can project a colored texture, that can work as a projector. Using angle with more than 180 degrees will not create an inversed cone in this case.

Point Light in `Custom` light shape mode can project a cubemap instead of a regular cookie. So it's a perfect solution to make disco balls, lamps that projects stars or anything else you want.

#### Custom Texture Resolution and Format

When you assign a LUT, Cookie texture, or Cubemap, the **Light Volumes** system automatically packs everything into a single **Texture Array**. The `Resolution` and `Format` of this array can be configured in the **Light Volumes Setup** component.

> ⚠️ **Note:** Higher resolutions may increase packing time and cause temporary lag in the editor.

LUTs and Cookie textures share the same resolution, as they are packed into the same texture array. Cubemaps, however, require 6 slices per entry (one for each face), so each cubemap takes up six times more space than a LUT or Cookie. If your input textures have a different resolution, they will be automatically rescaled during packing. 

Duplicated LUTs, Cubemaps, and Cookie textures are only uploaded to VRChat once and are reused by all lights that reference them. So don’t worry about using the same textures across multiple Point Light Volumes - it won’t increase the build size.

#### Available Texture Formats:

- **`RGBA32`** – The lightest format, but it does **not** support HDR. Not recommended for LUTs, as it causes visible banding artifacts.
- **`RGBA Half`** – The recommended format for most cases. Supports HDR and works well with LUTs. It uses half precision, so minimal banding may still be visible, but usually unnoticeable.
- **`RGBA Float`** – The highest quality format with full HDR support and no banding. It’s also the most memory-heavy and is typically overkill for general use.

### Baked Point Light Volume Shadows

**Point Light Volumes** do not support real-time shadows. However, you can bake static shadows for them. While this goes against the idea of Point Light Volumes being fully movable, it can still be useful for static lights or lights that move only slightly. That said, this feature is intended for rare and specific use cases.

To bake shadows, simply enable the `Baked Shadows` option on the desired **Point Light Volume**. The `Baked Shadow Radius` parameter controls the softness of the shadows — increasing its value will make them blurrier.

Keep in mind that baked shadows are voxel-based and are stored directly inside each Light Volume at its configured resolution. Make sure the `Point Light Shadows` option is enabled on every Light Volume you want to include in the bake. Enabling `Blur Shadows` is also recommended in most cases to reduce blocky aliasing artifacts, though it will make shadows appear softer.

> ⚠️ **Note:** Each Light Volume can support a maximum of **4 shadow-casting point lights** that intersects with each other.

To bake shadows for Point Light Volumes, simply rebake your scene lighting.

### Point Light Volume Component Description

`Dynamic` - Defines whether this point light volume can be moved in runtime. Disabling this option slightly improves performance.

`Baked Shadows` - Enables baked shadows for this light. This setting is only available for static lights, which cannot move. You must re-bake your volumes after changing this setting. This incurs some runtime VRAM and performance overhead.

`Baked Shadow Radius` - Shadow radius for the baked shadows. Higher values will produce softer shadows.

`Type` - Changes the light mode between Point Light, Spot Light and Area Light.

`Range` - Radius in meters beyond which point and spot lights are culled. Fewer overlapping lights result in better performance.

`Color` - Multiplies the point light volume’s color by this value.

`Intensity` - Brightness of the point light volume.

`Shape` - Parametric uses settings to compute light falloff. LUT uses a texture: X - cone falloff, Y - attenuation (Y only for point lights). Cookie projects a texture for spot lights. Cubemap projects a cubemap for point lights.

`Angle` - Angle of a spotlight cone in degrees.

`Falloff` - Spotlight cone falloff.

`Falloff LUT` - Texture that defines custom light shape. Similar to IES. X - cone falloff, Y - attenuation. No compression and RGBA Float or RGBA Half format is recommended.

`Cookie` - Projects a square texture for spot lights.

`Cubemap` - Projects a cubemap for point lights.

`Debug Range` - Shows overdrawing range gizmo. Less point light volumes intersections - more performance!



## Audio Link Integration

This package includes a [AudioLink](https://github.com/llealloo/audiolink/) integration Udon script. 

**LightVolumeAudioLink** component can change Light Volumes, Point light Volumes and Mesh Renderers materials colors in runtime based on AudioLink.

### Audio Link Quick Setup

1. Add the **LightVolumeAudioLink** component to a GameObject in your scene.

2. One **LightVolumeAudioLink** can control Light Volumes, Point Light Volumes and material colors based on an `Audio Band` value you choose: `Bass`, `Low Mid`, `High Mid`, `Treble`

3. Assign your **Audio Link** object in the `Audio Link` field.

4. Add all Light Volumes you want to control to the `Target Light Volumes` list and Point Light Volumes in the `Target Point Light Volumes` list.

5. Add all Mesh Renderers you want to change emission color by AudioLink to the `Target Mesh Renderers` list.

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