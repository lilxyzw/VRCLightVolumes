**VRC Light Volumes** | [How to Use](/Documentation/HowToUse.md) | [Best Practices](/Documentation/BestPractices.md) | [Udon Sharp API](/Documentation/UdonSharpAPI.md) | [For Shader Developers](/Documentation/ForShaderDevelopers.md) | [Compatible Shaders](/Documentation/CompatibleShaders.md)

<p align="center"> <img src="/Documentation/LogoMain.png" alt="My Logo" width="627" /></p>

VRC Light Volumes is a nextgen voxel based light probes replacement for VRChat.

This is a free open-source asset, so if you like it, I would be very happy if you **[Support me on Patreon](https://www.patreon.com/red_sim/ "Support me on Patreon")**.
There is a bunch of other cool assets you will get there!

![](/Documentation/Preview_0.png)

## Use Cases
- Baked partial avatars and dynamic props lighting
- Baked seamless lightmaps for small static objects
- Baked dynamic light sources
- Any Volumetric light effects

## Main Features
- Baked per-pixel voxel based lighting
- Affects avatars and dynamic props (shader integration required)
- Fast and performant
- Can change color in runtime
- Can create cheap dynamic light sources that can be moved in runtime
- Works with dynamic batching, which potentially increases performance
- Works with Bakery or the default Unity Lightmapper
- Works with AudioLink and TV screens
- Very easy and fast to setup
- It just looks beautiful!

## VRChat Worlds to test it
- **[Japanese Alley - VRC Light Volumes Test](https://vrchat.com/home/launch?worldId=wrld_af756ca8-30ee-41a4-b304-2207ebf79db9)**
- **[Light Volumes x AudioLink Test](https://vrchat.com/home/launch?worldId=wrld_ba751467-ca25-4734-91b3-7e503fc171f3)**
- **[2000s Classroom](https://vrchat.com/home/launch?worldId=wrld_f6445b27-037d-4926-b51f-d79ada716b31)**

## Attribution

It would be greatly appreciated if you include a small note in your VRChat world mentioning that VRC Light Volumes are supported there. This helps users know they can use avatars with Light Volumes compatible shaders and also learn more about the system.

For example, you can include a message like this:

```
This world supports VRC Light Volumes. Use avatar shaders with VRC Light Volumes support for an enhanced visual experience.
VRC Light Volumes by RED_SIM — GitHub: https://github.com/REDSIM/VRCLightVolumes/
```

You're not required to include this message — it's entirely optional. But if you do, it helps spread the word and supports the growth of this asset in the VRChat community.

## Installation through VRChat Creator Companion
1. Go to my VPM Listing web page: https://redsim.github.io/vpmlisting/
2. Press "Add to VCC"
3. Confirm adding in the popup dialogue window

## Installation with a unity package
1. Go to my Github releases page: https://github.com/REDSIM/VRCLightVolumes/releases
2. Download the .unitypackage file of the latest build
3. Drag and drop the file into your Unity project
