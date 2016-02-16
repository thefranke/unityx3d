/*
 * UnityX3D - http://www.tobias-franke.eu/projects/unityx3d/
 *
 * Copyright (c) 2015 Tobias Alexander Franke (tobias.franke@siggraph.org)
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

/*
 * This is a stub main class responsible for importing an X3D document
 */ 
class X3DImporter : AssetPostprocessor
{
	void OnPostprocessModel (GameObject g)
	{
	}
}

/*
 * This is the main class responsible for exporting the selected scene to an X3D document
 */ 
public class X3DExporter : ScriptableWizard
{
	protected XmlDocument xml;
	protected string outputPath;

	protected XmlNode sceneNode;
	protected XmlNode X3DNode;
	
	public bool useCommonSurfaceShader = true;

	[MenuItem ("File/X3D/Export")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard("Export selection to X3D", typeof(X3DExporter), "Export");
	}
	
	void OnWizardCreate()
	{
		// grab the selected objects
		Transform[] trs = Selection.GetTransforms(SelectionMode.TopLevel);
	
		// get a path to save the file
		string file = EditorUtility.SaveFilePanel("Save X3D file as", "${HOME}/Desktop", "", "x3d");
		outputPath = Path.GetDirectoryName(file);

		if (file.Length == 0) {
			// TODO output error
			return;
		}

		xml = new XmlDocument();

		XmlNode xmlHeader = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
		xml.AppendChild(xmlHeader);

		XmlDocumentType docType = xml.CreateDocumentType("X3D", "http://www.web3d.org/specifications/x3d-3.3.dtd", null, null);
		xml.AppendChild(docType);

		X3DNode = xml.CreateElement("X3D");
		xml.AppendChild(X3DNode);

		sceneNode = xml.CreateElement("Scene");
		X3DNode.AppendChild(sceneNode);

		XmlNode lhToRh = xml.CreateElement("Transform");
		addAttribute(lhToRh, "scale", "1 1 -1");
		sceneNode.AppendChild (lhToRh);
		sceneNode = lhToRh;

		exportRenderSettings();

		foreach (Transform tr in trs)
			sceneNode.AppendChild(toX3D(tr));

		xml.Save(file);
	}

	// helper to convert a Vector2 to a string
	static string toString(Vector2 v)
	{
		return v.x + " " + v.y;
	}

	// helper to convert a Vector3 to a string
	static string toString(Vector3 v)
	{
		return v.x + " " + v.y + " " + v.z;
	}

	// helper to convert a Color to a string
	static string toString(Color c)
	{
		return c.r + " " + c.g + " " + c.b;
	}

	// create a safe-to-use ID
	static string toSafeDEF(string def)
	{
		return def.Replace(" ", "_"); 
	}
	
	// helper function to add an attribute with a name and value to a node
	XmlAttribute addAttribute(XmlNode node, string name, string value)
	{
		XmlAttribute attr = xml.CreateAttribute(name);
		attr.Value = value;
		node.Attributes.Append(attr);
		return attr;
	}

	// convert a Unity Camera to X3D
	XmlNode toX3D(Camera camera, Transform transform = null)
	{
		XmlNode viewpointNode;
		viewpointNode = xml.CreateElement("Viewpoint");
		
		addAttribute(viewpointNode, "DEF", toSafeDEF(camera.name));
		
		double fov = camera.fieldOfView * Mathf.PI / 180;
		addAttribute (viewpointNode, "fieldOfView", fov.ToString ());
		
		if (transform != null) 
		{
			addAttribute(viewpointNode, "position", toString (transform.transform.localPosition));
						
			float angle = 0F;
			Vector3 axis = Vector3.zero;
			transform.transform.localRotation.ToAngleAxis (out angle, out axis);

			addAttribute(viewpointNode, "orientation", toString (axis) + " " + angle.ToString ());
		}

		return viewpointNode;
	}

	// convert a MeshFilter and accompanying Material with its Materials to X3D
	XmlNode toX3D(MeshFilter meshFilter, Material material)
	{
		XmlNode shapeNode = toX3D(meshFilter.sharedMesh, material);
		addAttribute (shapeNode, "DEF", toSafeDEF (meshFilter.name));
		return shapeNode;
	}

	// convert a MeshFilter and accompanying Material with its Materials to X3D
	XmlNode toX3D(SkinnedMeshRenderer skinnedMeshRenderer, Material material)
	{
		XmlNode shapeNode = toX3D(skinnedMeshRenderer.sharedMesh, material);
		addAttribute(shapeNode, "DEF", toSafeDEF(skinnedMeshRenderer.name));
		return shapeNode;
	}

	// convert a MeshFilter and accompanying Material with its Materials to X3D
	XmlNode toX3D(Mesh mesh, Material material)
	{
		XmlNode shapeNode;
		shapeNode = xml.CreateElement("Shape");

		shapeNode.AppendChild(toX3D(material));
		shapeNode.AppendChild(toX3D(mesh));
		
		return shapeNode;
	}

	// convert a Unity Mesh to X3D
	XmlNode toX3D(Mesh mesh)
	{
		XmlNode geometryNode;

		if (mesh.name == "Sphere Instance")
		{
			geometryNode = xml.CreateElement("Sphere");
			addAttribute(geometryNode, "radius", (0.5).ToString());
		}
		else if (mesh.name == "Cylinder Instance")
		{
			geometryNode = xml.CreateElement("Cylinder");
		}
		else if (mesh.name == "Cube")
		{
			geometryNode = xml.CreateElement("Box");
			addAttribute(geometryNode, "size", toString(Vector3.one));
		}
		else
		{
			geometryNode = xml.CreateElement("IndexedFaceSet");

			// process indices
			XmlAttribute coordIndex = xml.CreateAttribute("coordIndex");
			geometryNode.Attributes.Append(coordIndex);

			System.Text.StringBuilder sbCoordIndexValue = new System.Text.StringBuilder();

			for (int i = 0; i < mesh.triangles.Length; i += 3)
			{
				sbCoordIndexValue.Append(mesh.triangles[i + 0].ToString() + " ");
				sbCoordIndexValue.Append(mesh.triangles[i + 1].ToString() + " ");
				sbCoordIndexValue.Append(mesh.triangles[i + 2].ToString() + " ");
				sbCoordIndexValue.Append(" -1 ");
			}

			coordIndex.Value = sbCoordIndexValue.ToString();

			// process normals
			if (mesh.normals.Length > 0)
			{
				XmlNode normalNode = xml.CreateElement("Normal");
				XmlAttribute vector = xml.CreateAttribute("vector");
				normalNode.Attributes.Append(vector);

				System.Text.StringBuilder sbVectorValue = new System.Text.StringBuilder();

				foreach (Vector3 n in mesh.normals)
					sbVectorValue.Append(toString(n) + " ");

				vector.Value = sbVectorValue.ToString();

				geometryNode.AppendChild(normalNode);
			}

			// process vertices
			if (mesh.vertices.Length > 0)
			{
				XmlNode coordinateNode = xml.CreateElement("Coordinate");
				XmlAttribute point = xml.CreateAttribute("point");
				coordinateNode.Attributes.Append(point);

				System.Text.StringBuilder sbPointValue = new System.Text.StringBuilder();

				foreach(Vector3 v in mesh.vertices)
					sbPointValue.Append(toString(v) + " ");

				point.Value = sbPointValue.ToString();

				geometryNode.AppendChild(coordinateNode);
			}

			// process UV coordinates
			if (mesh.uv.Length > 0)
			{
				XmlNode textureCoordinateNode = xml.CreateElement("TextureCoordinate2D");
				XmlAttribute point = xml.CreateAttribute("point");
				textureCoordinateNode.Attributes.Append(point);

				System.Text.StringBuilder sbPointValue = new System.Text.StringBuilder();

				foreach (Vector2 v in mesh.uv)
					sbPointValue.Append(toString(v) + " ");

				point.Value = sbPointValue.ToString();

				geometryNode.AppendChild(textureCoordinateNode);
			}
		}

		return geometryNode;
	}

	// Find a texture asset and copy it to the output path of the X3D file
	string writeTexture(Texture texture)
	{
		string path = AssetDatabase.GetAssetPath(texture);
		string file = Path.GetFileName(path);

		// TODO maybe prompt for each overwrite?
		File.Copy(path, outputPath + "/" + file, true);

		return file;
	}

	// convert a Unity Texture to X3D
	XmlNode toX3D(Texture2D texture, string containerFieldName = "")
	{
		XmlNode imageTexture2DNode = xml.CreateElement ("ImageTexture2D");

		XmlAttribute url = xml.CreateAttribute ("url");
		imageTexture2DNode.Attributes.Append (url);
		
		string filename = writeTexture(texture);
		url.Value = filename;

		if (containerFieldName.Length != 0) 
		{
			XmlAttribute containerField = xml.CreateAttribute("containerField");
			containerField.Value = containerFieldName;
			imageTexture2DNode.Attributes.Append (containerField);
		}

		return imageTexture2DNode;
	}

	// convert a Unity Material to X3D
	XmlNode toX3D(Material material)
	{
		XmlNode appearanceNode = xml.CreateElement("Appearance");

		// handle the Standard PBR shader
		if (material.shader.name == "Standard") 
		{
			Color color = material.GetColor("_Color");
			Texture2D albedoMap = material.GetTexture("_MainTex") as Texture2D;
			Texture2D metallicGlossMap = material.GetTexture("_MetallicGlossMap") as Texture2D;
			float smoothness = material.GetFloat("_Glossiness");
			float metalness = material.GetFloat("_Metallic");
			Texture2D normalMap = material.GetTexture("_BumpMap") as Texture2D;
			Color emissionColor = material.GetColor("_EmissionColor");
			Texture2D emissionMap = material.GetTexture("_EmissionMap") as Texture2D;

			if (useCommonSurfaceShader)
			{
				XmlNode css = xml.CreateElement("CommonSurfaceShader");

				// TODO add texture IDs
				if (albedoMap != null)
				{
					css.AppendChild(toX3D(albedoMap, "ambientTexture"));
					css.AppendChild(toX3D(albedoMap, "diffuseTexture"));
				}

				if (normalMap != null)
					css.AppendChild(toX3D(normalMap, "normalTexture"));

				if (metallicGlossMap != null)
					css.AppendChild(toX3D(metallicGlossMap, "specularTexture"));

				if (emissionMap != null)
					css.AppendChild(toX3D(emissionMap, "emissiveTexture"));

				// TODO: find environment map for glossy reflection
				/* 
				if (RenderSettings.skybox.name != "Default-Skybox")
				{
					// find cubemap texture
					string id = ...;
					string path = AssetDatabase.GUIDToAssetPath(id);
					Texture2D cubemap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

					css.AppendChild(toX3D(cubemap, "reflectionTexture"));
					addAttribute(css, "reflectionTextureId", textureId.ToString ());
					textureId++;
				}
				*/

				//addAttribute(css, "ambientFactor", toString(color * (1 - metalness)));
				//addAttribute(css, "reflectionFactor", toString(new Vector3(1, 1, 1) * metalness));
				addAttribute(css, "specularFactor", toString(new Vector3(1, 1, 1) * metalness));
				addAttribute(css, "diffuseFactor", toString(color * (1 - metalness)));
				addAttribute(css, "shininessFactor", ((smoothness*smoothness)).ToString());
				addAttribute(css, "emissiveFactor", toString(emissionColor));

				appearanceNode.AppendChild(css);
			}
			else
			{
				// write albedo texture
				if (albedoMap != null)
					appearanceNode.AppendChild(toX3D(albedoMap));

				// write color
				XmlNode materialNode = xml.CreateElement("Material");

				addAttribute(materialNode, "diffuseColor", toString(color * (1 - metalness)));
				addAttribute(materialNode, "specularColor", toString(Vector3.one * metalness));
				addAttribute(materialNode, "shininess", (smoothness*smoothness).ToString());
				addAttribute(materialNode, "emissiveColor", toString(emissionColor));
				addAttribute(materialNode, "ambientIntensity", RenderSettings.ambientIntensity.ToString());

				appearanceNode.AppendChild(materialNode);
			}
		}

		return appearanceNode;
	}

	// convert a Unity Light to X3D
	XmlNode toX3D(Light light)
	{
		XmlNode lightNode;
		
		switch (light.type)
		{
		case LightType.Spot:
			lightNode = xml.CreateElement("SpotLight");
			break;

		case LightType.Directional:
			lightNode = xml.CreateElement("DirectionalLight");
			break;

		default:
			lightNode = xml.CreateElement("PointLight");
			break;
		}

		addAttribute(lightNode, "DEF", toSafeDEF(light.name));
		addAttribute(lightNode, "intensity", light.intensity.ToString ());

		switch (light.type)
		{
		case LightType.Spot:
			addAttribute(lightNode, "cutOffAngle", (light.spotAngle * Mathf.PI / 180).ToString());
			addAttribute(lightNode, "radius", light.range.ToString());
			break;

		case LightType.Directional:
			break;

		default:
			addAttribute(lightNode, "radius", light.range.ToString());
			break;
		}

		return lightNode;
	}

	void exportRenderSettings()
	{
		XmlNode navigationInfo = xml.CreateElement("NavigationInfo");
		addAttribute(navigationInfo, "headlight", "FALSE");

		/*
		if (RenderSettings.skybox.name != "Default-Skybox") 
		{
			// find cubemap texture

			string id = ... // find Asset in DB
			string path = AssetDatabase.GUIDToAssetPath (id);
			Texture2D cubemap = AssetDatabase.LoadAssetAtPath<Texture2D> (path);

			XmlNode skydomeBackground = xml.CreateElement ("SkydomeBackground");
			XmlNode appearance = xml.CreateElement ("Appearance");

			skydomeBackground.AppendChild (appearance);
			appearance.AppendChild (toX3D (cubemap));

			addAttribute (skydomeBackground, "sphereRes", "32");

			sceneNode.AppendChild (skydomeBackground);

		} 
		else 
		*/
		{
			XmlNode solidBackground = xml.CreateElement("SolidBackground");
			addAttribute(solidBackground, "color", toString(RenderSettings.ambientSkyColor));
			sceneNode.AppendChild(solidBackground);
		}
	}

	XmlNode toX3D(Transform transform)
	{
		XmlNode transformNode;
		transformNode = xml.CreateElement("Transform");

		addAttribute(transformNode, "translation", toString (transform.transform.localPosition));

		float angle = 0F;
		Vector3 axis = Vector3.zero;
		transform.transform.localRotation.ToAngleAxis(out angle, out axis);
		addAttribute(transformNode, "rotation",  toString(axis) + " " + angle.ToString());

		addAttribute(transformNode, "scale", toString (transform.transform.localScale));

		if (transform.GetComponent<Camera>())
		{
			transformNode.AppendChild
			(
				toX3D(transform.GetComponent<Camera>())
			);
		}
		
		if (transform.GetComponent<Light>())
		{
			transformNode.AppendChild
			(
				toX3D(transform.GetComponent<Light>())
			);
		}

		if (transform.GetComponent<MeshFilter>()) 
		{
			Material material = null;

			if (transform.GetComponent<Renderer>()) 
				material = transform.GetComponent<Renderer>().sharedMaterial;
			
			transformNode.AppendChild
			(
				toX3D(transform.GetComponent<MeshFilter>(), material)
			);
		} 

		if (transform.GetComponent<SkinnedMeshRenderer>()) 
		{
			Material material = null;

			if (transform.GetComponent<Renderer>())
				material = transform.GetComponent<Renderer>().sharedMaterial;

			transformNode.AppendChild
			(
				toX3D(transform.GetComponent<SkinnedMeshRenderer>(), material)
			);
		}
		
		// recurse through children
		foreach (Transform child in transform)
			transformNode.AppendChild(toX3D(child));

		return transformNode;
	}
}