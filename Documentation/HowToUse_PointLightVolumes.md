[VRC Light Volumes](../README.md) | **How to Use** | [Best Practices](../Documentation/BestPractices.md) | [Udon Sharp API](../Documentation/UdonSharpAPI.md) | [For Shader Developers](../Documentation/ForShaderDevelopers.md) | [Compatible Shaders](../Documentation/CompatibleShaders.md)

# How to Use

> [VRC Light Volumes System](../Documentation/HowToUse.md)
>
> [Regular Light Volumes](../Documentation/HowToUse_RegularLightVolumes.md)
>
> **Point Light Volumes**
>
> - [Point Light Volumes Placement](#Point-Light-Volumes-Placement)
> - [Light Shape](#Light-Shape)
> - [Baked Point Light Volume Shadows](#Baked-Point-Light-Volume-Shadows)
> - [Point Light Volume Component Description](#Point-Light-Volume-Component-Description)
>
> [Audio Link Integration](../Documentation/HowToUse_AudioLinkIntegration.md)
>
> [TV Screens Integration](../Documentation/HowToUse_TVScreensIntegration.md)

## Point Light Volumes

**Point Light Volumes** is a fast and optimized custom lighting system that has it's own parametric Point Light, Spot Lights and Area Lights. Point Light Volumes are not voxel based, they forms the light parametrically, or based on special LUT textures (similar to IES). They can project light cookies or cubemaps. It can be up to 128 point lights visible in one scene at the same time. However, this system does not support real-time shadows.

## Point Light Volumes Placement

**Point Light Volumes** are mostly useful in cases when you need independent dynamic lights, that can be individually toggled, moved or changed color in runtime.

If you just have a lot of point light sources that are static and don't change any of their properties in runtime, consider using a regular Light Volume and bake as much lights into it as you want. It is usually much more optimized than placing a lot of individual point lights. However, one point light (Not in Area Light mode!) is usually ~4 times cheaper than a one regular additive light volume.

Area light can be ~8 times less performant than other light types, so use it only if you need a movable and scalable in runtime soft box, or if you really want to save memory. Otherwise, it's more performant to bake a regular Light Volume in a shape of an area light.

Note that more point lights you have active in your scene, the less performance you'll have. So, consider manually turning off unused point lights if you have a lot of them at your scene.

The more point light volumes overlap, the less performance you'll have. Try not to make an insanely huge range for your lights. Use `Debug Range` flag in your Point Light Volume component to preview the region affected by your point light.

**Point light Volume** in `Area Light` mode calculates the **range** automatically based on its size, brightness and the `Area Light Brightness Cutoff` parameter in Light Volume Setup component.

## Light Shape

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

## Baked Point Light Volume Shadows

**Point Light Volumes** do not support real-time shadows. However, you can bake static shadows for them. While this goes against the idea of Point Light Volumes being fully movable, it can still be useful for static lights or lights that move only slightly. That said, this feature is intended for rare and specific use cases.

To bake shadows, simply enable the `Baked Shadows` option on the desired **Point Light Volume**. The `Baked Shadow Radius` parameter controls the softness of the shadows — increasing its value will make them blurrier.

Keep in mind that baked shadows are voxel-based and are stored directly inside each Light Volume at its configured resolution. Make sure the `Point Light Shadows` option is enabled on every Light Volume you want to include in the bake. Enabling `Blur Shadows` is also recommended in most cases to reduce blocky aliasing artifacts, though it will make shadows appear softer.

> ⚠️ **Note:** Each Light Volume can support a maximum of **4 shadow-casting point lights** that intersects with each other.

To bake shadows for Point Light Volumes, simply rebake your scene lighting.

## Point Light Volume Component Description

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