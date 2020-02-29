# Texture Packer & MipMapLevelCalculator

- Open the TestScene
- Both NoiseTextureGenerator and UniformtextureGenerator scripts are here to help you filling the level with many generated textures. Their usage is pretty straightforward : when "Instantiate in Scene" is set to true, they both allow you to instantiate cubes as child of the "Parent Transform" while remaining inside the "Spawning Area".
- I recommend to create 300 to 500 different textures using the UniformTextureGenerator script by setting "Texture Number" to around 400 and the "Maximum Texture Size" to 2048.
- Play the TestScene.
- Click on the "Parse Scene" button will parse the scene.
- The "Show Color Atlas" toggle appears, allowing you to visualize how albedo textures are packed.

Scene Parsing in details :

For each texture type (COLOR, GLOSS_MAP, AO, NORMAL) :

  1- We create a Dictionary VisibleMaterialsByTexture by parsing the scene. Frustrum culled items are ignored during this step.\
  Example: For the Albedo (COLOR) texture type:
  
  [
    AlbedoTextureA -> [ VisibleItemA1_Material1, VisibleItemA1_Material2, VisibleItemA2_Material1 ];\
    AlbedoTextureB -> [ VisibleItemB1_Material1, VisibleItemB1_Material2, VisibleItemB2_Material1 ]
  ]
  
  2- We create an Atlas containing all textures extract from the Dictionary for the current texture type. The atlas tries to pack all textures inside a power-of-two rectangle area.
  
  3- If it fails to pack all the textures (if they are too big to fit inside the packer maximum size) for the current texture type, we start the mipmap level calculator step.
  
  4- The mipmap level calculator first parses the scene again but this time discards the transparent objects to create a brand new Dictionary VisibleMaterialsByTexture (one current limitations of the mipmap level calculator is that it is not capable of calculating the mipmap number for textures plugged at least on one visible transparent material).
  
  5- We extract all textures from this Dictionary for the current texture type and we create unique [0..1] Ids for the textures.
  
  6- We extract all Materials for each textures from the Dictionary and set the "CalculateLOD" shader for them, passing the corresponding texture Ids as well as the texture itself to the shader.
  
  7- For each remaining visible materials (materials without the required type of texture OR materials with transparency OR materials that share the required type of texture with other transparent materials) we also set the "CalculateLOD" shader but Discard their rendering.
  
  8- The "CalculateLOD" shader is using the screen space uv derivatives in the pixel shader to calculate the mipmap number and encode it as a RGBA color. I set the maximum detectable mipmap number to 10 by multiplying the calculated mipmap number by 0.1 before encoding it.
  
  9- We render the scene using a transparent black backgroundColor for the camera.
  
  10- Pass the rendered RenderTexture to the MaxLODComputer compute shader
  
  11 - The MaxLODComputer use the alpha channel to determine if a pixel had been rendered by the "CalculateLOD" shader or it is was previously discard from rendering. 
  
  12- Then, for all rendered pixels, it decodes the RGBA value into texture Ids and mipmap numbers and write to the "Output" buffer only if the currently found mipmap number is lower than the current stored value for the current texture Id (a lower mipmap number for a texture Id indicates a "highest" level of detail for the texture corresponding the Id).
  
  13- We use the mipmap number written in the "Output" buffer for each texture Id to calculate the new decreased sizes for the original textures.
  Example : for the texture id = 1, original size = 2048x2048, the minimum mipmap number read from the "Output" buffer is 3 so each dimensions will be divided by 2^3 : new size = 256x256.
  
  14- We try again to pack all the resized texture inside a power-of-two rectangle area.
