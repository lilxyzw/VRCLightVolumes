[VRC Light Volumes](/README.md) | **How to Use** | [Best Practices](/Documentation/BestPractices.md) | [Udon Sharp API](/Documentation/UdonSharpAPI.md) | [For Shader Developers](/Documentation/ForShaderDevelopers.md) | [Compatible Shaders](/Documentation/CompatibleShaders.md)
# How to Use

1. Right-click in the Hierarchy and select **Light Volume**.  
2. Rename the new Light Volume to match the zone you’re covering (each volume must have a unique name).  
3. In the Light Volume component, click **Edit Bounds** and resize the box to fit your target area—much like setting up a Reflection Probe.  
4. Enable **Adaptive Resolution**, then adjust **Voxels Per Unit** to control voxel density. A live preview shows voxel placement and estimated bake size.  
5. Repeat for each volume in your scene. Remember that doubling the resolution of a 3D volume roughly octuples its file size, so balance detail with memory cost.  
6. Locate the **Light Volume Manager** in your scene (it’s added automatically when you use Light Volumes). It lists every active volume.  
7. Assign a **Weight** to each volume—the higher the weight, the greater its priority when volumes overlap.  
8. Choose your **Baking Mode** based on the lightmapper you’re using.  
9. Bake the scene.  
10. Add a mesh with any shader that supports VRC Light Volumes.  
11. For reference, open the **Example** scene in the **Example** folder to see a working setup.  