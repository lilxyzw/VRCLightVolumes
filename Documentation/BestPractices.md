[VRC Light Volumes](../README.md) | [How to Use](../Documentation/HowToUse.md) | **Best Practices** | [Udon Sharp API](../Documentation/UdonSharpAPI.md) | [For Shader Developers](../Documentation/ForShaderDevelopers.md) | [Compatible Shaders](../Documentation/CompatibleShaders.md)

# Best Practices

> - [Regular Light Volumes Use Cases](#Regular-Light-Volumes-Use-Cases)
> - [Point Light Volumes Use Cases](#Point-Light-Volumes-Use-Cases)
> - [Naming Light Volumes](#Quick-Light-Probe-Setup)
> - [Volume Bounds Smoothing](#Volume-Bounds-Smoothing)
> - [Culling Light Volumes](#Culling-Light-Volumes)
> - [Moving Light Volumes](#Moving-Light-Volumes)
> - [Movable Volumes As Light Sources](Movable-Volumes-As-Light-Sources)
> - [Bakery Volume Rotation](#Bakery-Volume-Rotation)
> - [Fixing Bakery Light Probes](#Fixing-Bakery-Light-Probes)
> - [Area Light Volumes Intensity](Area-Light-Volumes-Intensity)

## Regular Light Volumes Use Cases

- Use them with small static props that usually require very high lightmap resolution to avoid visible seams. Light Volumes produce no seams at all, as they are voxel-based.
- Dynamic batching support: if you have tons of low-poly dynamic props across your scene using the same material, and their Mesh Renderers have Light Probes and Reflection Probes disabled, they can be dynamically batched at runtime, potentially improving performance.
- Combine Light Volumes with particles to create stunning volumetric fog effects.
- Switch between two Light Volumes at runtime to create toggleable lighting for rooms or other areas in your scene.
- TV Screens dynamic Global Illumination
- Audio Link Dynamic Lights
- And much more!

## Point Light Volumes Use Cases

- Spot Lights as portable flashlights
- Point Lights as other dynamic light sources
- Area Lights as studio light soft boxes
- Moving blinking lighting for clubs
- Image and cubemaps projectors
- TV Screens dynamic Global Illumination
- Audio Link Dynamic Lights
- And much more!

## Naming Light Volumes

Ensure every Light Volume you bake has a unique game object name. The generated 3D textures inherit these names and can conflict. If you duplicate baked volumes or use prefab instances with the `Bake` flag disabled, you don’t need to rename them.

## Volume Bounds Smoothing

Overlap intersecting volumes slightly (about 0.25 m) to hide seams. The `Smooth Blending` parameter controls edge falloff - keep it smaller than your overlap.

To smooth between a volume and uncovered areas, disable `Sharp Bounds` in Light Volume Setup. This applies smoothing to all edges, so you might need to scale up your volumes to keep the softened edges outside of the intended area.

## Culling Light Volumes

At runtime, you can disable any Light Volume to exclude it from rendering. This works even on non-dynamic volumes. Manually culling unused volumes can significantly boost performance in large scenes.

Disabling **Light Volumes Manager** object disables all the Light Volumes system and fallbacks all the shaders to light probes.

## Moving Light Volumes

To update a volume’s transform at runtime, enable **Dynamic** on its component and check **Auto Update Volumes** in Light Volume Setup. Otherwise, you must manually update positions of Dynamic volumes via an Udon script. If you don’t need runtime updates, leave both options off for better performance.

## Movable Volumes

For dynamic lighting, set the volume to **Additive** so it layers on top of others and also affects lightmapped static meshes with a compatible shader. Minimize overlapping additive volumes to reduce overhead. Use **Max Additive Overdraw** value in Light Volume Setup to limit how many additive volumes render (prioritizing those with higher weight). Setting it to zero disables additive volumes entirely.

## Bakery Volume Rotation

Bakery lightmapper offers high quality with Light Volumes but may not support rotation during baking in some versions. Upgrade to the latest Bakery (via **Bakery → Utilities → Check for Patches**) to bake with Y‑axis rotation. Runtime rotation is still always supported.

## Fixing Bakery Light Probes

Bakery bakes L1 probes to work with "Geometrics SH Evaluation", which can cause overexposure and underexposure issues. Enable **Fix Light Probes L1** in Light Volume Setup to correct the probes after each bake. This may reduce overall contrast slightly but prevents over or underexposure.

## Area Light Volumes Intensity

Note that compared to Point Lights, Area Lights emit less light per unit of intensity. This is to match up with Unity's lightmapper, where emissive lights are Pi times too dark. If you want your area lights to match up with point light intensity, or to match emissive objects, multiply the intensity by Pi (3.14).
