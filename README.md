# Raymarched Volumetric
This project focuses on creating a **higly customizable renderer** for **volumetric** participating media(clouds, fog, etc.) using **raymarching**. 

## Optimization Techniques Used
* Compute shader based raymarching calculations
* Adjustable itterative downsampling of render texture for compute shader
* Adjustable resolution for 3D noise texture
* Exponent rule for multiplying powers with the same base
* Early termination based on transmittance
