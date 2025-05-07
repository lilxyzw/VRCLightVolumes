[VRC Light Volumes](README.md) | [How to Use](Documentation/HowtoUse.md) | [Best Practices](Documentation/BestPractices.md) | [Optimizations](Documentation/Optimizations.md) | [Udon Sharp API](Documentation/UdonSharpAPI.md) | [For Shader Developers](Documentation/ForShaderDevelopers.md) | [Compatible Shaders](Documentation/CompatibleShaders.md)
# How to use
1. Click RMB in your hierarchy window and choose "Light Volume"
2. Name your Light Volume game object related to the zone you want to apply it to. (Light volumes names must be different)
3. In your Light Volume component press "Edit Bounds" and match the  colume size with the zone in your scene, you want to apply a volume to. Very similar to a Reflection Probe setup.
4. With the "Adaptive Resolution" mode enabled in this component, change the "Voxels Per Unit" value to change the volume's voxel density. You can preview your voxels and even see the approximate file size it will have after baking.
5. Setup all the volumes you need for your scene. You can change the resolution depending on the lighting conditions or the volume size. Note that increasing resolution for a 3D volume, increases the file size cubically, so be aware of a big value!
6. Look for a game object called "Light Volume Manager" on your scene. It automatically appears in your scene if you use any Light Volumes, because volumes depends on it. It will show every Light Volume, that is currently on your scene.
7. Set weight for every volume. Higher the weight - more priority this volume will have.
8. Select "Baking Mode" depending on the Lightmapper you gonna use.
9. Bake your scene.
10. Now it should work! To test it, use any mesh with a shader that supports VRC Light Volumes. There are few shaders provided with this asset in a shader category called "Light Volume Samples".
11. You can always check the example scene in the "Example" folder to look how it works.