# Raymarched Volumetric
This project focuses on rendering **participating media(clouds, fog, etc.)** using **volumetric raymarcher**, while trying to make it **higly customizable** and **as physically accurate as possible**.

## Optimization Techniques Used
* Compute shader based raymarching calculations
* Adjustable itterative downsampling of render texture for compute shader
* Adjustable resolution for 3D noise texture
* Exponent rule for multiplying powers with the same base
* Early termination based on transmittance

## Resources Used
 - [Acerola](https://youtu.be/ryB8hT5TMSg?si=8OAnO4RpsvCn7oA6)
 - [Sebastian Lague](https://youtu.be/4QOcCGI6xOU?si=MbLOhz84CBwUdQNo)
 - [Scratch A Pixel](https://www.scratchapixel.com/lessons/3d-basic-rendering/volume-rendering-for-developers/intro-volume-rendering.html)
