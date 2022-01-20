# BrickBuildAR

The greatest 3D Brick instructions you'll ever meet.

![Mockup screen of the AR application on an iPad](https://github.com/jay-k98/HSKL_BrickBuildAR/blob/main/Mockup/BrickBuildAR_mockup.png)

## Required Packages

### Unity Registry
- ARKit XR Plugin 4.2.1
- AR Foundation 4.2.1
- Input System 1.1.1
- Post Processing 3.1.1

### Unity Asset Store
- [Lean Touch 2.3.5](https://assetstore.unity.com/packages/tools/input-management/lean-touch-30111)

## Project Settings

### Resolution and Presentation
- Portrait: **false**
- Portrait Upside Down: **false**
- Landscape Right: **true**
- Landscape Left: **true**

### Player
- Target Device: **iPhone + iPad**
- Target minimum iOS version: **12.0**
- Requires ARKit support: **true**
- Active Input Handling: **Both**

### XR Plug-in Management
- Initialize XR on Startup: **true**
- Plug-in Providers > ARKit: **true**

## Parts and Colors
We used parts from the community run part library [LDraw Parts List](https://www.ldraw.org/cgi-bin/ptlist.cgi). The meshes were optimized in Blender and exported as FBX.
Official colors can be obtained from [Brickipedia Color Palette](https://brickipedia.fandom.com/wiki/Colour_Palette).

## Instructions YAML

The data structure is based on the YAML format. Each build step has a unique ID.
Related build steps can be referenced by the ID's.
There are different types of build steps:

### Assembling single parts

Single parts are joined together here. Each build step contains the ID of the component, the local position, the rotation of the component, as well as the color of the component.

![YAML of a single part step](https://github.com/jay-k98/HSKL_BrickBuildAR/blob/main/Screens/yaml_part_step.png)

### Assembling components

For components, only the component ID and the position are specified. Specifying the rotation would be possible like for single parts, but is not required at the moment.

![YAML of a component step](https://github.com/jay-k98/HSKL_BrickBuildAR/blob/main/Screens/yaml_component_step.png)

### Rotate view

These steps only contain a rotation vector, to rotate the view and a so called "Smoothing-Factor", which should indicate if the rotation should be animated (value = 1) or not (value = 0).

Furthermore, each building step contains a specification for the position on the y-axis, as well as a rotation vector for the correct alignment in space. The y-axis represents the height of the object in space. Sometimes this is desirable, for example if the object has to be turned upside down.
