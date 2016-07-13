/*
 * UnityX3D
 *
 * Copyright (c) 2015 Tobias Alexander Franke
 * http://www.tobias-franke.eu
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using UnityEditor;

using System.IO;
using System.Xml;
using System.Collections.Generic;

namespace UnityX3D
{
	[InitializeOnLoad]
	public class Preferences
	{
		protected static bool loaded = false;

        public static bool useCommonSurfaceShader = true;
        public static bool bakedLightsAmbient = true;
        public static bool disableHeadlight = false;
        public static bool savePNGlightmaps = false;

		static Preferences()
		{
			Load();
		}

		[PreferenceItem("UnityX3D")]
		static void PreferenceGUI()
		{
			if(!loaded)
				Load();

            useCommonSurfaceShader = EditorGUILayout.Toggle("Use CommonSurfaceShader", useCommonSurfaceShader);
            bakedLightsAmbient = EditorGUILayout.Toggle("Export static lights as ambient", bakedLightsAmbient);
            disableHeadlight = EditorGUILayout.Toggle("Disable X3D headlight", disableHeadlight);
            savePNGlightmaps = EditorGUILayout.Toggle("Save Lightmaps as PNG", savePNGlightmaps);

			if(GUI.changed)
				Save();
		}

		static void Load()
		{
            useCommonSurfaceShader = EditorPrefs.GetBool("UnityX3D.useCommonSurfaceShader", true);
            bakedLightsAmbient = EditorPrefs.GetBool("UnityX3D.bakedLightsAmbient", true);
            disableHeadlight = EditorPrefs.GetBool("UnityX3D.disableHeadlight", false);
            savePNGlightmaps = EditorPrefs.GetBool("UnityX3D.savePNGlightmaps", false);

			loaded = true;
		}

		static void Save()
		{
            EditorPrefs.SetBool("UnityX3D.useCommonSurfaceShader", useCommonSurfaceShader);
            EditorPrefs.SetBool("UnityX3D.bakedLightsAmbient", bakedLightsAmbient);
            EditorPrefs.SetBool("UnityX3D.disableHeadlight", disableHeadlight);
            EditorPrefs.SetBool("UnityX3D.savePNGlightmaps", savePNGlightmaps);
		}
	}
	
	/*
	 * This is a stub main class responsible for importing an X3D document
	 */
	class X3DImporter : AssetPostprocessor
	{
		void OnPostprocessModel(GameObject g)
		{
		}
	}

	/*
	 * This is the main class responsible for exporting the selected scene to an X3D document
	 */
	public class X3DExporter : MonoBehaviour
	{
		static protected XmlDocument xml;
        static protected string outputPath;

        static protected XmlNode sceneNode;
        static protected XmlNode X3DNode;

        static protected List<string> defNamesInUse;

        // Clear the console
        static void ClearConsole () 
        {
            var logEntries = System.Type.GetType("UnityEditorInternal.LogEntries,UnityEditor.dll");
            var clearMethod = logEntries.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            clearMethod.Invoke(null,null);
        }

		// helper to convert a Vector2 to a string
		static string ToString(Vector2 v)
		{
			return v.x + " " + v.y;
		}

		// helper to convert a Vector3 to a string
		static string ToString(Vector3 v)
		{
			return v.x + " " + v.y + " " + v.z;
		}

		// helper to convert a Color to a string
		static string ToString(Color c)
		{
			return c.r + " " + c.g + " " + c.b;
		}

		// create a safe-to-use ID
        static string ToSafeDEF(string def)
		{
			string uDef = def.Replace(" ", "_");

			string safeDef = uDef;
			int i = 1;

			while(defNamesInUse.Contains(safeDef))
			{
				safeDef = uDef + "_" + i;
				i++;
			}

			defNamesInUse.Add(safeDef);

			return safeDef;
		}

		static double ToRadians(double angle)
		{
			return(Mathf.PI / 180) * angle;
		}

		// helper function to add an attribute with a name and value to a node
		static XmlAttribute AddXmlAttribute(XmlNode node, string name, string value)
		{
			XmlAttribute attr = xml.CreateAttribute(name);
			attr.Value = value;
			node.Attributes.Append(attr);
			return attr;
		}

        static XmlNode CreateNode(string name)
        {
            return xml.CreateElement(name);
        }

		// convert a Unity Camera to X3D
        static XmlNode CameraToX3D(Camera camera, Transform transform = null)
		{
			XmlNode viewpointNode;
			viewpointNode = CreateNode("Viewpoint");

			AddXmlAttribute(viewpointNode, "DEF", ToSafeDEF(camera.name));

			double fov = camera.fieldOfView * Mathf.PI / 180;
			AddXmlAttribute(viewpointNode, "fieldOfView", fov.ToString());

			if(transform != null)
			{
				AddXmlAttribute(viewpointNode, "position", ToString(transform.transform.localPosition));

				float angle = 0F;
				Vector3 axis = Vector3.zero;
				transform.transform.localRotation.ToAngleAxis(out angle, out axis);

				AddXmlAttribute(viewpointNode, "orientation", ToString(axis) + " " + ToRadians(angle).ToString());
			}

			return viewpointNode;
		}

		// convert a Mesh and accompanying Renderer to X3D
        static XmlNode ObjectToX3D(string name, Mesh mesh, Renderer renderer)
		{
			if(mesh == null)
			{
				Debug.Log("UnityX3D Warning: SharedMesh reference of " + name + " is null.");
				return null;
			}

			XmlNode shapeNode = ObjectToX3D(mesh, renderer);
			return shapeNode;
		}

		// convert a Mesh and accompanying Renderer with its Materials to X3D
        static XmlNode ObjectToX3D(Mesh mesh, Renderer renderer)
		{
			XmlNode shapeNode;
			shapeNode = CreateNode("Shape");

			AddXmlAttribute(shapeNode, "DEF", ToSafeDEF(mesh.name));

			shapeNode.AppendChild(RendererToX3D(renderer));
            shapeNode.AppendChild(MeshToX3D(mesh, renderer.lightmapScaleOffset, renderer.lightmapIndex != -1));

			return shapeNode;
		}

		// convert a Unity Mesh to X3D
        static XmlNode MeshToX3D(Mesh mesh, Vector4 lightmapScaleOffset, bool hasLightmap)
		{
            XmlNode geometryNode;

            // TODO: Predefined geometries and MultiTextureCoordiantes for lightmaps
            /*
			if(mesh.name == "Sphere Instance")
			{
				geometryNode = CreateNode("Sphere");
				AddXmlAttribute(geometryNode, "radius",(0.5).ToString());
			}
			else if(mesh.name == "Cylinder Instance")
			{
				geometryNode = CreateNode("Cylinder");
			}
			else if(mesh.name == "Cube")
			{
				geometryNode = CreateNode("Box");
				AddXmlAttribute(geometryNode, "size", ToString(Vector3.one));
			}
			else
            */
			{
				geometryNode = CreateNode("IndexedFaceSet");

				// process indices
				XmlAttribute coordIndex = xml.CreateAttribute("coordIndex");
				geometryNode.Attributes.Append(coordIndex);

				System.Text.StringBuilder sbCoordIndexValue = new System.Text.StringBuilder();

				for(int i = 0; i < mesh.triangles.Length; i += 3)
				{
					sbCoordIndexValue.Append(mesh.triangles[i + 0].ToString() + " ");
					sbCoordIndexValue.Append(mesh.triangles[i + 1].ToString() + " ");
					sbCoordIndexValue.Append(mesh.triangles[i + 2].ToString() + " ");
					sbCoordIndexValue.Append(" -1 ");
				}

				coordIndex.Value = sbCoordIndexValue.ToString();

				// process normals
				if(mesh.normals.Length > 0)
				{
					XmlNode normalNode = CreateNode("Normal");
					XmlAttribute vector = xml.CreateAttribute("vector");
					normalNode.Attributes.Append(vector);

					System.Text.StringBuilder sbVectorValue = new System.Text.StringBuilder();

					foreach(Vector3 n in mesh.normals)
						sbVectorValue.Append(ToString(n) + " ");

					vector.Value = sbVectorValue.ToString();

					geometryNode.AppendChild(normalNode);
				}

                XmlNode multiTextureCoordinateNode = CreateNode("MultiTextureCoordinate");

				// process vertices
				if(mesh.vertices.Length > 0)
				{
					XmlNode coordinateNode = CreateNode("Coordinate");
					XmlAttribute point = xml.CreateAttribute("point");
					coordinateNode.Attributes.Append(point);

					System.Text.StringBuilder sbPointValue = new System.Text.StringBuilder();

					foreach(Vector3 v in mesh.vertices)
						sbPointValue.Append(ToString(v) + " ");

					point.Value = sbPointValue.ToString();

					geometryNode.AppendChild(coordinateNode);
				}

                // process lightmap UV coordinates
                Vector2[] lightmapUVs = mesh.uv2;

                if (lightmapUVs.Length == 0 && hasLightmap)
                {
                    lightmapUVs = mesh.uv;
                }

                if (lightmapUVs.Length > 0)
                {
                    XmlNode textureCoordinateNode = CreateNode("TextureCoordinate2D");
                    XmlAttribute point = xml.CreateAttribute("point");
                    textureCoordinateNode.Attributes.Append(point);

                    System.Text.StringBuilder sbPointValue = new System.Text.StringBuilder();

                    foreach (Vector2 v in lightmapUVs)
                    {
                        Vector2 vv = v;

                        vv.x *= lightmapScaleOffset.x;
                        vv.y *= lightmapScaleOffset.y;

                        vv.x += lightmapScaleOffset.z;
                        vv.y += lightmapScaleOffset.w;

                        sbPointValue.Append(ToString(vv) + " ");
                    }

                    point.Value = sbPointValue.ToString();

                    multiTextureCoordinateNode.AppendChild(textureCoordinateNode);
                }

				// process UV coordinates
				if(mesh.uv.Length > 0)
				{
					XmlNode textureCoordinateNode = CreateNode("TextureCoordinate2D");
					XmlAttribute point = xml.CreateAttribute("point");
					textureCoordinateNode.Attributes.Append(point);

					System.Text.StringBuilder sbPointValue = new System.Text.StringBuilder();

                    foreach (Vector2 v in mesh.uv)
                    {
                        sbPointValue.Append(ToString(v) + " ");
                    }

					point.Value = sbPointValue.ToString();

                    multiTextureCoordinateNode.AppendChild(textureCoordinateNode);
				}

                geometryNode.AppendChild(multiTextureCoordinateNode);
			}

			return geometryNode;
		}

		// Find a texture asset and copy it to the output path of the X3D file
        static string WriteTextureFile(Texture texture)
		{
			string path = AssetDatabase.GetAssetPath(texture);
			string file = Path.GetFileName(path);
			
			if (file != "")
				// TODO maybe prompt for each overwrite?
				File.Copy(path, outputPath + "/" + file, true);
			else
			{
				Texture2D tex2d = texture as Texture2D;
				
				if (tex2d)
				{
					file = System.Guid.NewGuid().ToString();
					byte[] bytes = tex2d.EncodeToPNG();
					File.WriteAllBytes(outputPath + "/" + file, bytes);
				}
			}

			return file;
		}

        static Texture2D CreateReadableTexture(Texture2D texture, bool isLinear)
        {
            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.ARGB32,
                isLinear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(texture, tmp);

            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;

            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;

            // Create a new readable Texture2D to copy the pixels to it
            Texture2D readable = new Texture2D(texture.width, texture.height);

            // Copy the pixels from the RenderTexture to the new Texture
            readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readable.Apply();

            // Reset the active RenderTexture
            RenderTexture.active = previous;

            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            return readable;
        }

        static string WriteLightmapTextureFile(Renderer renderer)
        {
            if (renderer.lightmapIndex == -1)
                return "";

            string ext = ".exr";

            if (Preferences.savePNGlightmaps)
                ext = ".png";
                
            
            string path = Application.dataPath + "/" + renderer.gameObject.scene.name + "/Lightmap-0_comp_light" + ext;
            string file = renderer.gameObject.scene.name + "_" + Path.GetFileName(path);

            // TODO maybe prompt for each overwrite?
            if (Preferences.savePNGlightmaps)
            {
                LightmapData lightmapData = LightmapSettings.lightmaps[renderer.lightmapIndex];
                Texture2D lightMap = CreateReadableTexture(lightmapData.lightmapFar, false);

                byte[] bytes = lightMap.EncodeToPNG();
                File.WriteAllBytes(outputPath + "/" + file, bytes);
            }
            else
            {
                File.Copy(path, outputPath + "/" + file, true);
            }
        
            return file;
        }

		// convert a Unity Texture to X3D
        static XmlNode TextureToX3D(string filename, string containerFieldName = "", bool css = false)
		{
			XmlNode textureNode = null;

			XmlNode imageTexture2DNode = CreateNode("ImageTexture2D");
			XmlAttribute url = xml.CreateAttribute("url");
			imageTexture2DNode.Attributes.Append(url);

			url.Value = filename;

            if (filename.EndsWith(".exr"))
            {
                XmlNode texturePropertiesNode = CreateNode("TextureProperties");
                AddXmlAttribute(texturePropertiesNode, "internalFormat", "rgba16f");

                imageTexture2DNode.AppendChild(texturePropertiesNode);
            }

			if (css)
			{
				XmlNode surfaceShaderTextureNode = CreateNode ("SurfaceShaderTexture");
				textureNode = surfaceShaderTextureNode;
				textureNode.AppendChild(imageTexture2DNode);
			}
			else
				textureNode = imageTexture2DNode;

			if(containerFieldName.Length != 0)
			{
				XmlAttribute containerField = xml.CreateAttribute("containerField");
				containerField.Value = containerFieldName;
				textureNode.Attributes.Append(containerField);
			}

			return textureNode;
		}

        static XmlNode TextureToX3D(Texture2D texture, string containerFieldName = "", bool css = false)
        {
            string filename = WriteTextureFile(texture);
            
            return TextureToX3D(filename, containerFieldName, css);
        }

        static XmlNode LightmapToX3D(Renderer renderer, string containerFieldName = "", bool css = false)
        {
            string filename = WriteLightmapTextureFile(renderer);

            return TextureToX3D(filename, containerFieldName, css);;
        }

		// convert a Unity Renderer to X3D
        static XmlNode RendererToX3D(Renderer renderer)
		{
			Material material = renderer.sharedMaterial;
			
			XmlNode appearanceNode = CreateNode("Appearance");

			// handle the Standard PBR shader
			if(material.shader.name == "Standard")
			{
				Color color = material.GetColor("_Color");
				Texture2D albedoMap = material.GetTexture("_MainTex") as Texture2D;
				Texture2D metallicGlossMap = material.GetTexture("_MetallicGlossMap") as Texture2D;
				float smoothness = material.GetFloat("_Glossiness");
				float metalness = material.GetFloat("_Metallic");
				Texture2D normalMap = material.GetTexture("_BumpMap") as Texture2D;
				Color emissionColor = material.GetColor("_EmissionColor");
				Texture2D emissionMap = material.GetTexture("_EmissionMap") as Texture2D;
				Vector4 uvTiling = material.GetVector("_MainTex_ST");
				
				// if the Renderer has a lightmap
                bool hasLightmap = renderer.lightmapIndex != -1;

				XmlNode textureTransform = CreateNode("TextureTransform");
				AddXmlAttribute(textureTransform, "scale", uvTiling.x + " " + uvTiling.y);

				if(Preferences.useCommonSurfaceShader)
				{
					XmlNode css = CreateNode("CommonSurfaceShader");

					// TODO add texture IDs
                    if(hasLightmap)
					{
						XmlNode ambientTextureNode = LightmapToX3D(renderer, "ambientTexture", true);
						css.AppendChild(ambientTextureNode);
					}

					if(albedoMap != null)
					{
						XmlNode diffuseTextureNode = TextureToX3D(albedoMap, "diffuseTexture", true);
						diffuseTextureNode.AppendChild(textureTransform);
						css.AppendChild(diffuseTextureNode);
					}

					if(normalMap != null)
                        css.AppendChild(TextureToX3D(normalMap, "normalTexture", true));

					if(metallicGlossMap != null)
                        css.AppendChild(TextureToX3D(metallicGlossMap, "specularTexture", true));

					if(emissionMap != null)
                        css.AppendChild(TextureToX3D(emissionMap, "emissiveTexture", true));

					// TODO: find environment map for glossy reflection
					/*
					if(RenderSettings.skybox.name != "Default-Skybox")
					{
						// find cubemap texture
						string id = ...;
						string path = AssetDatabase.GUIDToAssetPath(id);
						Texture2D cubemap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

						css.AppendChild(toX3D(cubemap, "reflectionTexture"));
						addAttribute(css, "reflectionTextureId", textureId.ToString());
						textureId++;
					}
					*/

                    AddXmlAttribute(css, "ambientFactor", ToString(new Vector3(1, 1, 1)));
					AddXmlAttribute(css, "specularFactor", ToString(new Vector3(1, 1, 1) * metalness));
					AddXmlAttribute(css, "diffuseFactor", ToString(color *(1 - metalness)));
					AddXmlAttribute(css, "shininessFactor", (smoothness*smoothness).ToString());
					AddXmlAttribute(css, "emissiveFactor", ToString(emissionColor));
                    //AddXmlAttribute(css, "reflectionFactor", ToString(new Vector3(1, 1, 1) * metalness));

					appearanceNode.AppendChild(css);
				}
				else
				{
					// write color
					XmlNode materialNode = CreateNode("Material");

					AddXmlAttribute(materialNode, "diffuseColor", ToString(color *(1 - metalness)));
					AddXmlAttribute(materialNode, "specularColor", ToString(Vector3.one * metalness));
					AddXmlAttribute(materialNode, "shininess", (smoothness*smoothness).ToString());
					AddXmlAttribute(materialNode, "emissiveColor", ToString(emissionColor));
					AddXmlAttribute(materialNode, "ambientIntensity", RenderSettings.ambientIntensity.ToString());

                    XmlNode multiTextureNode = CreateNode("MultiTexture");
                    AddXmlAttribute(multiTextureNode, "mode", "ADD ADD");

                    XmlNode multiTextureTransformNode = CreateNode("MultiTextureTransform");
                    multiTextureTransformNode.AppendChild(textureTransform);

                    // Write lightmap
                    if(hasLightmap)
                    {
                        multiTextureNode.AppendChild(LightmapToX3D(renderer));

                        XmlNode lightmapTextureTransform = CreateNode("TextureTransform");
                        AddXmlAttribute(lightmapTextureTransform, "scale", "1 1");

                        multiTextureTransformNode.AppendChild(lightmapTextureTransform);
                    }

                    // write albedo texture
                    if (albedoMap != null)
                    {
                        multiTextureNode.AppendChild(TextureToX3D(albedoMap));
                        multiTextureTransformNode.AppendChild(textureTransform);
                    }

                    appearanceNode.AppendChild(multiTextureNode);
                    appearanceNode.AppendChild(multiTextureTransformNode);

					appearanceNode.AppendChild(materialNode);
				}
			}

			return appearanceNode;
		}

		// convert a Unity Light to X3D
        static XmlNode LightToX3D(Light light)
		{
			XmlNode lightNode;

			switch(light.type)
			{
			case LightType.Spot:
				lightNode = CreateNode("SpotLight");
				break;

			case LightType.Directional:
				lightNode = CreateNode("DirectionalLight");
				break;

			default:
				lightNode = CreateNode("PointLight");
				break;
			}

			AddXmlAttribute(lightNode, "DEF", ToSafeDEF(light.name));
		
            /*
            if (Preferences.bakedLightsAmbient && light)
                AddXmlAttribute(lightNode, "ambientIntensity", light.intensity.ToString());
            else
            */
                AddXmlAttribute(lightNode, "intensity", light.intensity.ToString());

            AddXmlAttribute(lightNode, "color", ToString(light.color));

			switch(light.type)
			{
			case LightType.Spot:
				AddXmlAttribute(lightNode, "cutOffAngle",(light.spotAngle * Mathf.PI / 180).ToString());
				AddXmlAttribute(lightNode, "radius", light.range.ToString());
				break;

			case LightType.Directional:
				break;

			default:
				AddXmlAttribute(lightNode, "radius", light.range.ToString());
				break;
			}

			return lightNode;
		}

        static void ExportRenderSettings()
		{
			XmlNode navigationInfo = CreateNode("NavigationInfo");
			AddXmlAttribute(navigationInfo, "headlight", "FALSE");

			/*
			if(RenderSettings.skybox.name != "Default-Skybox")
			{
				// find cubemap texture

				string id = ... // find Asset in DB
				string path = AssetDatabase.GUIDToAssetPath(id);
				Texture2D cubemap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

				XmlNode skydomeBackground = CreateNode("SkydomeBackground");
				XmlNode appearance = CreateNode("Appearance");

				skydomeBackground.AppendChild(appearance);
				appearance.AppendChild(toX3D(cubemap));

				addAttribute(skydomeBackground, "sphereRes", "32");

				sceneNode.AppendChild(skydomeBackground);

			}
			else
			*/
			{
				XmlNode solidBackground = CreateNode("SolidBackground");
				AddXmlAttribute(solidBackground, "color", ToString(RenderSettings.ambientSkyColor));
				sceneNode.AppendChild(solidBackground);
			}
		}

        static XmlNode TransformToX3D(Transform transform)
		{
			XmlNode transformNode;
			transformNode = CreateNode("Transform");

			AddXmlAttribute(transformNode, "DEF", ToSafeDEF(transform.name));
			AddXmlAttribute(transformNode, "translation", ToString(transform.transform.localPosition));

			float angle = 0F;
			Vector3 axis = Vector3.zero;
			transform.transform.localRotation.ToAngleAxis(out angle, out axis);
			AddXmlAttribute(transformNode, "rotation",  ToString(axis) + " " + ToRadians(angle).ToString());

			AddXmlAttribute(transformNode, "scale", ToString(transform.transform.localScale));

			if(transform.GetComponent<Camera>())
			{
				transformNode.AppendChild
				(
					CameraToX3D(transform.GetComponent<Camera>())
				);
			}

			if(transform.GetComponent<Light>())
			{
				transformNode.AppendChild
				(
					LightToX3D(transform.GetComponent<Light>())
				);
			}

			if(transform.GetComponent<MeshFilter>())
			{
				MeshFilter m = transform.GetComponent<MeshFilter>();
				
                XmlNode meshNode = ObjectToX3D(m.name, m.sharedMesh, transform.GetComponent<Renderer>());

                if (meshNode != null)
    				transformNode.AppendChild
    				(
    					meshNode
    				);
			}

			if(transform.GetComponent<SkinnedMeshRenderer>())
			{
				SkinnedMeshRenderer m = transform.GetComponent<SkinnedMeshRenderer>();
				
				transformNode.AppendChild
				(
					ObjectToX3D(m.name, m.sharedMesh, transform.GetComponent<Renderer>())
				);
			}

			// recurse through children
			foreach(Transform child in transform)
				transformNode.AppendChild(TransformToX3D(child));

			return transformNode;
		}

        [MenuItem("File/Export X3D")]
        static void ExportX3D()
        {
            ClearConsole();

            // grab the selected objects
            Transform[] trs = Selection.GetTransforms(SelectionMode.TopLevel);

            // get a path to save the file
            string file = EditorUtility.SaveFilePanel("Save X3D file as", "${HOME}/Desktop", "", "x3d");
            outputPath = Path.GetDirectoryName(file);

            if(file.Length == 0)
            {
                // TODO output error
                return;
            }

            xml = new XmlDocument();

            defNamesInUse = new List<string>();

            XmlNode xmlHeader = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            xml.AppendChild(xmlHeader);

            XmlDocumentType docType = xml.CreateDocumentType("X3D", "http://www.web3d.org/specifications/x3d-3.3.dtd", null, null);
            xml.AppendChild(docType);

            X3DNode = CreateNode("X3D");
            xml.AppendChild(X3DNode);

            sceneNode = CreateNode("Scene");
            X3DNode.AppendChild(sceneNode);

            XmlNode lhToRh = CreateNode("Transform");
            AddXmlAttribute(lhToRh, "scale", "1 1 -1");
            sceneNode.AppendChild(lhToRh);
            
			sceneNode = lhToRh;

            ExportRenderSettings();

            foreach(Transform tr in trs)
                sceneNode.AppendChild(TransformToX3D(tr));

            xml.Save(file);
        }
	}
}
