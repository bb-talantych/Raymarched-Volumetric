# Raymarched Volumetric
This project focuses on rendering **participating media(clouds, fog, etc.)** using **volumetric raymarcher**, while trying to make it as **customizable** and **physically accurate** as possible.

![](./Examples/volumetric-simple.gif)

## Options Overview
### Raymarching Parameters
![](./Examples/options/options-raymarching-parameters.png)
* **Step Size** - lenght of raymarching step inside volume
* **Light Step Size** - same as **step size**, but for light
* **Downsampling itterations** - each itteration divides the resolution by 2, but stops when height isn't fully divisible

### Noise Parameters
![](./Examples/options/options-noise-parameters.png)
* **Texture Resolution** - resolution of 3D noise texture
* **Worley Tiling** - Tiling of **tilable worley**
* **Noise Offset Tiling** - Tiling of secondary **offset noise**
* **Noise Mix** - lerps between **offset noise(0)** and **worley(1)**
* **Min Dist Multiplier** - Makes worley bubbles more pronounced and round

### Volumetric Parameters
![](./Examples/options/options-volumetric-parameters.png)
* **Animate** and **Animation Dir** - control animation
* **Absoption Multiplier** and **Absoption Coef** - control **absoption coefficient**
* **Scattering Multiplier** and **Scattering Coef** - control **scattering coefficient**
* **Volume Density** - multiplier on **density**(sampled from 3D noise texture)
* **Phase** - choise **of phase function**
  * Isotropic - no phase function
  * Henyey Greenstein
  * Rayleigh
  * Schlick
* **Asymmetry Factor** - **asymmetry factor** needed for some **phase functions**
* **Density Falloff** - smoothsteps density between **sphere center(0)** and **sphere edge(1)**
* **Multi Scattered Light Multiplier** - multiplier for **multi scattered light approximation**
* **Powder Power** - lerps **powder** value between **1(0)** and **powder(1)**
* **Powder Exponent** - multiplier inside **exp() function** that is used to calculate **powder**
* **Ambient Setting** - options for ambient light(sampled from skybox cubemap)
  * Regular - doesn't apply **powder** to **ambient light**
  * Apply Powder - applies **powder** to **ambient light**
* **Ambient Power** - lerps **ambient light** value between **0(0)** and * **Phase** - choise **of phase function**
### Final Output
![](./Examples/volumetric-custom.gif)

## Optimization Techniques Used
* Compute shader based raymarching calculations
* Adjustable, itterative downsampling of render texture for compute shader
* Adjustable resolution for 3D noise texture
* Reduction of "exp()" function calls using exponent power rule
* Early termination based on transmittance
* Max steps limits for volume and light raymarching

## Resources Used
 - [Acerola](https://youtu.be/ryB8hT5TMSg?si=8OAnO4RpsvCn7oA6)
 - [Sebastian Lague](https://youtu.be/4QOcCGI6xOU?si=MbLOhz84CBwUdQNo)
 - [Scratch A Pixel](https://www.scratchapixel.com/lessons/3d-basic-rendering/volume-rendering-for-developers/intro-volume-rendering.html)
 - [The Real-time Volumetric Cloudscapes of Horizon: Zero Dawn](https://advances.realtimerendering.com/s2015/The%20Real-time%20Volumetric%20Cloudscapes%20of%20Horizon%20-%20Zero%20Dawn%20-%20ARTR.pdf)
 - "Chapter 14 Volumetric and Translucency Rendering" from "Real-time Rendering 4"
