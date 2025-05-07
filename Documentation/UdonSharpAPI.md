[VRC Light Volumes](/README.md) | [How to Use](/Documentation/HowToUse.md) | [Best Practices](/Documentation/BestPractices.md) | **Udon Sharp API** | [For Shader Developers](/Documentation/ForShaderDevelopers.md) | [Compatible Shaders](/Documentation/CompatibleShaders.md)
# Udon Sharp API

There are only a two udon scritps you can control:

### LightVolumeManager
Stores the light volumes 3D atlas and references to all of the Light Volume Instances. Controls and updates all the Light Volumes in your scene

`Texture3D LightVolumeAtlas` - 3D Texture atlas that is an atlas that contains all the Light Volumes SH baked data.

`bool LightProbesBlending` - When enabled, areas outside Light Volumes fall back to light probes. Otherwise, the Light Volume with the smallest weight is used as fallback. It also improves performance.

`bool SharpBounds` - Disables smooth blending with areas outside Light Volumes. Use it if your entire scene's play area is covered by Light Volumes. It also improves performance.

`bool AutoUpdateVolumes` - Automatically updates a volume's position, rotation, and scale in Play mode using an Udon script. Use only if you have movable volumes in your scene.

`bool AdditiveMaxOverdraw` - Limits the maximum number of additive volumes that can affect a single pixel. If you have many dynamic additive volumes that may overlap, it's good practice to limit overdraw to maintain performance.

`LightVolumeInstance[] LightVolumeInstances` - All Light Volume instances sorted in decreasing order by weight. You can enable or disable volumes game objects at runtime. Manually disabling unnecessary volumes improves performance.

`void UpdateVolumes()` - Method that updates all the volumes gloabal shader parameters. Useful if you want manually update volumes instead of having **AutoUpdateVolumes** enabled.


### LightVolumeInstance
Stores all the volume configuration including 3D UVs, world transform, color, etc.

`Color Color` - Multiplies volumes color by this value. Changing the color is useful for animating Additive volumes. You can even control the R, G, B channels separately this way.

`bool IsDynamic` - Defines whether this volume can be moved in runtime. Disabling this option slightly improves performance. You can even change it in runtime.

`bool IsAdditive` - Additive volumes apply their light on top of others as an overlay. Useful for movable lights like flashlights, projectors, disco balls, etc. They can also project light onto static lightmapped objects if the surface shader supports it.

`Quaternion InvBakedRotation` - Inverse rotation of the pose the volume was baked in. Automatically recalculated for dynamic volumes with auto-update, or manually via the **UpdateRotation()** method.

`Vector4 BoundsUvwMin0` - Min bounds of Texture0 in 3D atlas space.

`Vector4 BoundsUvwMin1` - Min bounds of Texture1 in 3D atlas space.

`Vector4 BoundsUvwMin2` - Min bounds of Texture2 in 3D atlas space.

`Vector4 BoundsUvwMax0` - Max bounds of Texture0 in 3D atlas space.

`Vector4 BoundsUvwMax1` - Max bounds of Texture1 in 3D atlas space.

`Vector4 BoundsUvwMax2` - Max bounds of Texture2 in 3D atlas space.

`Vector4 InvLocalEdgeSmoothing` - Inversed edge smoothing in 3D atlas space. Recalculates via **SetSmoothBlending(float radius)** method.

`Matrix4x4 InvWorldMatrix` - Inversed TRS matrix of this volume that transforms it into the 1x1x1 cube. Recalculates via the **UpdateRotation()** method.

`Vector4 RelativeRotation` - Current volume's rotation relative to the rotation it was baked with. Mandatory for dynamic volumes. Recalculates via the **UpdateRotation()** method.

`bool IsRotated` - True if there is any relative rotation. No relative rotation improves performance. Recalculated via the **UpdateRotation()** method.

`void SetSmoothBlending(float radius)` - Calculates **InvLocalEdgeSmoothing** value. Execute it if you want to control edge smoothing in runtime. You can even control every direction independent if it's needed.

`void UpdateRotation()` - Recalculates **InvWorldMatrix** and **RelativeRotation**. Executes automatically from **LightVolumeManager.UpdateDynamicVolumes()** or while **LightVolumeManager.AutoUpdateVolumes** enabled. Usually don't need to be executed manually.
