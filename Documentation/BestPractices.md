[VRC Light Volumes](/README.md) | [How to Use](/Documentation/HowToUse.md) | **Best Practices** | [Udon Sharp API](/Documentation/UdonSharpAPI.md) | [For Shader Developers](/Documentation/ForShaderDevelopers.md) | [Compatible Shaders](/Documentation/CompatibleShaders.md)
# Best Practices

This article covers some non-intuitive features of VRC Light Volumes.

## Quick Volume Setup

If you already have Reflection Probes in your scene, you'll probably want your Light Volumes to match their bounds. To do this, right-click the Reflection Probe in the Hierarchy and create **Light Volume** as a child. Any volume created under any probe will automatically use its bounds.

## Quick Light Probe Setup

If you’ve set up Light Volumes but still need Unity’s default Light Probe Groups for shaders without VRC Light Volumes support, you can generate them from your volumes. Select a Light Volume, then click **Generate Light Probes** in its inspector. In the popup window, adjust probes density. You'll see the probes positions as a preview in your scene. When you’re ready, click **Create Light Probe Group**. A new group will appear as a child object of the selected volume.

## Naming Light Volumes

Ensure every Light Volume you bake has a unique game object name. The generated 3D textures inherit these names and can conflict. If you duplicate baked volumes or use prefab instances with the **Bake** flag disabled, you don’t need to rename them.

## Volume Bounds Smoothing

Overlap intersecting volumes slightly (about 0.25 m) to hide seams. The **Smooth Blending** parameter controls edge falloff - keep it smaller than your overlap.

To smooth between a volume and uncovered areas, disable **Sharp Bounds** in Light Volume Setup. This applies smoothing to all edges, so you might need to scale up your volumes to keep the softened edges outside of the intended area.

## Culling Light Volumes

At runtime, you can disable any Light Volume to exclude it from rendering. This works even on non-dynamic volumes. Manually culling unused volumes can significantly boost performance in large scenes.

## Moving Light Volumes

To update a volume’s transform at runtime, enable **Dynamic** on its component and check **Auto Update Volumes** in Light Volume Setup. Otherwise, you must manually update  positions of Dynamic volumes via an Udon script. If you don’t need runtime updates, leave both options off for better performance.

## Movable Volumes As Light Sources

For dynamic lighting (e.g., flashlights, disco balls), set the volume to **Additive** so it layers on top of others and also affects lightmapped static meshes with a compatible shader. Minimize overlapping additive volumes to reduce overhead. Use **Max Additive Overdraw** value in Light Volume Setup to limit how many additive volumes render (prioritizing those with higher weight). Setting it to zero disables additive volumes entirely.

## Baking Additive Volumes

Bake additive volumes separately from your main scene so they include only their own direct lighting. See the **Additive Baking Example** scene included with this asset. After baking, copy the volume game object into your main scene.

## Bakery Volume Rotation

&#x20;Bakery lightmapper offers high quality with Light Volumes but may not support rotation during baking in some versions. Upgrade to the latest Bakery (via **Bakery → Utilities → Check for Patches**) to bake with Y‑axis rotation. Runtime rotation is still always supported.

## Fixing Bakery Light Probes

Bakery optimizes L1 probes for "Geometrics SH Evaluation", which can cause overexposure and underexposure issues. Enable **Fix Light Probes L1** in Light Volume Setup to correct the probes after each bake. This may reduce overall contrast slightly but prevents over or underexposure.
