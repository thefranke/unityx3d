# UnityX3D

This script is a handler for X3D, an open standard to represent 3D scenes/applications in an XML dialect. By simply dropping **unityx3d.cs** into your Unity assets, a new menu entry **Assets/X3D** should appear.

Simply select parts of the scene or everything  with CTRL+A and click **Assets/X3D/Export to X3D**. Currently, there is an option to use the CommonSurfaceShader instead of the standard X3D material to enhance some of the material visuals.

# Open issues

* **Material**: The material mapping from the physically based standard shader in Unity 5 cannot be fully done with the currently available X3D nodes. The standard needs better material representation. Other material representations apart from the standard shader are currently ignored.

* **Environment**: Currently, there is no working code to export environment maps or skydomes. Procedurally generated backgrounds are ignored.

* **Textures**: Generated textures are ignored.

* **Camera**: Camera placement is currently wrong due to coordinate system issues.

* **Shaders**: Custom shaders are currently ignored. Need to find code to trigger GLSL conversion first, then need to find a way to map shader inputs (textures, shader constants etc.) from Unity to X3D and vice versa.

* **Mesh**: Animated meshes with rigs aren't exported. HAnim would make sense here.

* **Import**: The X3D importer is incomplete.
