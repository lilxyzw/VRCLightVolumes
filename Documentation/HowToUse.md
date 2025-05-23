[VRC Light Volumes](/README.md) | **How to Use** | [Best Practices](/Documentation/BestPractices.md) | [Udon Sharp API](/Documentation/UdonSharpAPI.md) | [For Shader Developers](/Documentation/ForShaderDevelopers.md) | [Compatible Shaders](/Documentation/CompatibleShaders.md)
# How to Use

## VRC Light Volumes on Avatars

You just need to use a [shader that has VRC Light Volumes support](/Documentation/CompatibleShaders.md) and that's it. Nothing else to do!

Unfortunately, there is no way to make avatars cast light volumes light, it can only be integrated into worlds.

## Quick World Setup

1. Right-click in the **Hierarchy** and select **Light Volume**.
2. Rename the new Light Volume to match the zone you're covering.  
   Each volume must have a **unique name**.
3. In the Light Volume component, click **Edit Bounds** and resize the volume to fit your target area - similar to setting up a Reflection Probe.
4. With **Adaptive Resolution** enabled, adjust **Voxels Per Unit** to control voxel density.  
   A live preview shows voxel placement and the estimated bake size.
5. Repeat this process for each volume in your scene.  
   Keep in mind: doubling the resolution of a 3D volume increases its file size by approximately **8x**.
6. Find the **Light Volume Manager** in your scene.  
   It's automatically added when you create a Light Volume and lists all the volumes in your scene.
7. Assign a **Weight** to each volume.  
   Volumes with higher weight have higher priority in overlapping areas.
8. Select your **Baking Mode** based on the lightmapper you're using.  
   **Bakery** is highly recommended due to faster baking and better quality results.
9. Bake the scene.
10. For reference, check out the **Example** scene in the `Example` folder to see a working setup.

> ⚠️ Note: The number of active Light Volumes is limited to 32 per scene at a time. However, you can dynamically enable and disable volumes at runtime to work with more than 32 total, just not simultaneously.

## Additive Light Volumes

Before diving into additive volumes, here’s how **regular** light volumes work:

They use baked lighting data from the volume that contains a mesh and has the **highest weight**. When multiple volumes overlap, lighting data is **blended smoothly** between them.

### What Are Additive Volumes?

**Additive** light volumes work differently:

They **add their light** on top of the regular volumes. You can control how many additive volumes affect a pixel using the **Additive Max Overdraw** parameter in the **Light Volume Setup** component.

Additive volumes can also affect **lightmapped static geometry**, making them ideal for dynamic lighting like projectors, flashlights, toggleable or interactive lights, etc.

> ### **Performance Notes**
> **Additive Max Overdraw** parameter limits how many **additive volumes** are sampled **per pixel**, not how many can exist in the scene overall.
>
> The more additive volumes **intersect**, the higher the performance cost.

### How to Bake an Additive Light Volume

1. Create a **separate scene** for baking.
2. Add an **additive light volume** and a **light source** inside it.
3. Make sure the volume **fully contains the light's range**.
4. If you're using **Bakery**, add a **static quad** or mesh somewhere in the scene (even far from the light).  
   This is a workaround: Bakery won’t bake anything if there's no static geometry.
5. Bake the scene.

### Bringing It Into Your Main Scene

Once baking is done:

1. **Copy** the baked Light Volume into your main scene.
2. Enable the `Additive` checkbox in the Light Volume component.
3. Enable `Dynamic` if the volume should follow its transform at runtime.
4. Uncheck the `Bake` flag to prevent this volume to be rebaked in future lightmapper bakes
5. In **Light Volume Setup**, enable `Auto Update Volumes`.
6. In **Light Volume Setup** press `Pack Light Volumes` to generate the 3D atlas needed for volumes to work.
   
> If you see some sharp edges, it means that some undesirable light was baked into the volume.
> You can increase the volume size or tweak the *Color Correction* to get rid of undesirable light. Use
> Lowering Dark Lights usually helps. To see changes, you should press **Pack Light Volumes** button in your **Light Volume Setup** component.

Now you should now see your additive volume lighting up the scene.
If it doesn't work, make sure you’re using a [shader that has VRC Light Volumes support](/Documentation/CompatibleShaders.md).

### Attaching to Dynamic Props

To attach an additive volume to a moving object:

- Make the volume a **child** of the dynamic prop.
- It will now **move with the prop** during runtime.
- Disabling the prop with the volume will also **disable its lighting**.

## Audio Link Integration

This package includes a simple [AudioLink](https://github.com/llealloo/audiolink/) integration Udon script.

1. Add the **LightVolumeAudioLink** component to a GameObject in your scene.
2. One **LightVolumeAudioLink** can control Light Volumes and material colors based on a single AudioLink band:  
   Choose one of the following: **Bass**, **LowMid**, **HighMid**, or **Treble**.
3. Assign your **AudioLink** object to the component.
4. Add all Light Volumes you want to control to the `Target Light Volumes` array.
5. Add all Mesh Renderers you want to control to the `Target Mesh Renderers` array.  
   These meshes should use materials with **emission enabled**.  
   The shader must include a property named `_EmissionColor` (the **Standard** shader supports this).
6. Adjust `Volumes Intensity` and `Materials Intensity` to fine-tune the visual effect.
7. In your **AudioLink** component, make sure **Readback** is enabled. Click **Enable Readback** if it’s not already active.
8. Enable `Auto Update Volumes` in your **Light Volume Setup** to let it update the colors automatically in runtime
9. Done! You should now see visual changes reacting to audio.  
   Add more **LightVolumeAudioLink** components to control other AudioLink bands.

## TV Screens Integration

This package includes a simple Udon script for lighting integration from TV screens.

It works visually similar to [LTCGI](https://github.com/PiMaker/ltcgi) in some cases, but it does **not** support real screen reflections. Instead, it works best with **matte** environment materials.

#### Advantages
- Good performance
- Shadowmasks avatars and environment

#### Limitations
- Doesn't make screen reflectios like LTCGI
- Only projects a **single average screen color**

### Quick Setup

1. Create a **separate scene** and bake the area affected by the screen light as an **additive light volume**.  
   See the *Additive Volumes* section for detailed steps.  
   **Important:** Remove all unnecessary lights during baking. Keep only the screen mesh with a **bright emissive material**.
   
2. In your main scene, add the **LightVolumeTVGI** component to a GameObject.

3. Assign the `Target Render Texture` field with the **RenderTexture used by your video player**.  
   Make sure that **Enable Mip Maps** and **Auto Generate Mip Maps** are enabled in the texture’s import settings.

4. Add all Light Volumes you want to control to the `Target Light Volumes` array.

5. Enable `Auto Update Volumes` in your **Light Volume Setup** to let it update the colors automatically in runtime

6. Done! The system will now update the light color at runtime, even affecting avatars.
   If you see unwanted **sharp color transitions** in your additive volume, try adjusting the **Color Correction** settings in the Light Volume component.