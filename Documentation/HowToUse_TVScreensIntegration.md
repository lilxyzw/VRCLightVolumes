[VRC Light Volumes](../README.md) | **How to Use** | [Best Practices](../Documentation/BestPractices.md) | [Udon Sharp API](../Documentation/UdonSharpAPI.md) | [For Shader Developers](../Documentation/ForShaderDevelopers.md) | [Compatible Shaders](../Documentation/CompatibleShaders.md)

# How to Use

| Menu |
|----|
|[VRC Light Volumes System](../Documentation/HowToUse.md)|
|[Regular Light Volumes](../Documentation/HowToUse_RegularLightVolumes.md)|
| [Point Light Volumes](../Documentation/HowToUse_PointLightVolumes.md)|
| [Audio Link Integration](../Documentation/HowToUse_AudioLinkIntegration.md)|
| **TV Screens Integration**<br />• [TV Screen Quick Setup](#TV-Screen-Quick-Setup)<br />• [Light Volume TVGI Component Description](#Light-Volume-TVGI-Component-Description) |

## TV Screens Integration

This package includes a simple Udon script for making realtime global illumination from TV screens.

![](../Documentation/Preview_13.png)

It works visually similar to [LTCGI](https://github.com/PiMaker/ltcgi) in some cases, but it does **not** support real screen reflections. Instead, it works best with **matte** environment materials.

#### Advantages

- Good performance
- Shadowmasks avatars and environment

#### Limitations
- Doesn't make screen reflections like LTCGI
- Only projects a **single average screen color**

## TV Screen Quick Setup

1. Create a **separate scene** and bake the area affected by the screen light as an **additive light volume**.
   See the [Additive Volumes section](#How-to-Bake-an-Additive-Light-Volume) for detailed steps.

> [!IMPORTANT]
> Remove all unnecessary lights during baking. Keep only the screen mesh with a **bright emissive material**.

2. In your main scene, add the **LightVolumeTVGI** component to a GameObject.

3. Assign the `Target Render Texture` field with the **Render Texture used by your video player**.

> [!WARNING]
> Make sure that `Enable Mip Maps` and `Auto Generate Mip Maps` are **Enabled** in the texture’s import settings.

4. Add all Light Volumes you want to control to the `Target Light Volumes` list. It's usually a one additive light volume.

5. **Optionally:** Add all Point Light Volumes you want to control to the `Target Point Light Volumes` list. This can be useful to make other lights to inherit your screen's color.

6. Tweak the `Intensity` of your additive light volumes in their own LightVolume components. Because sometimes GI from a screen can look too dim.

7. Enable `Auto Update Volumes` in your **Light Volume Setup** to let it update the colors automatically in runtime.

8. Done! The system will now update the light color at runtime, even affecting avatars.

If you see unwanted **sharp color transitions** in your additive volume, try adjusting the **Color Correction** settings in the Light Volume component. Lowering `Shadows` in color correction section usually helps.

## Light Volume TVGI Component Description

| Parameter | Description |
| --- | --- |
|`Target Render Texture` | Render Texture used by your video player. Can be just a static texture if you want it to be. Make sure that **Enable Mip Maps** and **Auto Generate Mip Maps** are **Enabled** in the texture’s import settings.|
|`Anti Flickering` | Enables smoothing algorithm that tries to smooth out flickering that is usually a problem. Recommended to always be turned on.|
|`Target Light Volumes` | List of the **Light Volumes** that should be affected by the Light Volume TVGI script.|
|`Target Point Light Volumes` | List of the **Point Light Volumes** that should be affected by the Light Volume TVGI script. Usually you don't need it at all.|
