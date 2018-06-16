/*
 * UnityX3D
 *
 * Copyright (c) 2017 Tobias Alexander Franke
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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

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
        public static bool lightmapUnlitFaceSets = false;
        public static bool saveIndividualShapes = false;
        public static bool exportLightmaps = true;

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
            exportLightmaps = EditorGUILayout.Toggle("Export Lightmaps", exportLightmaps);
            savePNGlightmaps = EditorGUILayout.Toggle("Save Lightmaps as PNG", savePNGlightmaps);
            lightmapUnlitFaceSets = EditorGUILayout.Toggle("Set Facesets unlit if lightmapped", lightmapUnlitFaceSets);

            if(GUI.changed)
                Save();
        }

        static void Load()
        {
            useCommonSurfaceShader = EditorPrefs.GetBool("UnityX3D.useCommonSurfaceShader", true);
            bakedLightsAmbient = EditorPrefs.GetBool("UnityX3D.bakedLightsAmbient", true);
            disableHeadlight = EditorPrefs.GetBool("UnityX3D.disableHeadlight", false);
            exportLightmaps = EditorPrefs.GetBool("UnityX3D.exportLightmaps", true);
            savePNGlightmaps = EditorPrefs.GetBool("UnityX3D.savePNGlightmaps", false);
            lightmapUnlitFaceSets = EditorPrefs.GetBool("Unlit Facesets if lightmapped?", false);

            loaded = true;
        }

        static void Save()
        {
            EditorPrefs.SetBool("UnityX3D.useCommonSurfaceShader", useCommonSurfaceShader);
            EditorPrefs.SetBool("UnityX3D.bakedLightsAmbient", bakedLightsAmbient);
            EditorPrefs.SetBool("UnityX3D.disableHeadlight", disableHeadlight);
            EditorPrefs.SetBool("UnityX3D.exportLightmaps", exportLightmaps);
            EditorPrefs.SetBool("UnityX3D.savePNGlightmaps", savePNGlightmaps);
            EditorPrefs.SetBool("UnityX3D.lightmapUnlitFaceSets", lightmapUnlitFaceSets);
        }
    }

    class Tools
    {
        // Clear the console
        public static void ClearConsole()
        {
            Debug.ClearDeveloperConsole();
        }

        // Helper to convert a Vector2 to a string
        public static string ToString(Vector2 v)
        {
            return v.x + " " + v.y;
        }

        // Helper to convert a Vector3 to a string
        public static string ToString(Vector3 v)
        {
            return v.x + " " + v.y + " " + v.z;
        }

        public static string[] TokensFromString(string v)
        {
            if (v != "")
            { 
                char[] delimiters = new char[] { ',', ' ' };
                string[] tokens = v.Split(delimiters);
                return tokens.Where(x => !string.IsNullOrEmpty(x)).ToArray();
            }

            return new string[] { };
        }

        public static int[] IntArrayFromString(string v)
        {
            string[] tokens = TokensFromString(v);
            return tokens.Select(int.Parse).ToArray();
        }

        public static float[] FloatArrayFromString(string v)
        {
            string[] tokens = TokensFromString(v);
            return tokens.Select(float.Parse).ToArray();
        }

        public static Vector3 Vector3FromString(string v, float defaultScalar = 0)
        {
            float[] floatTokens = FloatArrayFromString(v);
            
            if (floatTokens.Length >= 3)
                return new Vector3(floatTokens[0], floatTokens[1], floatTokens[2]);

            if (floatTokens.Length == 1)
                return new Vector3(floatTokens[0], floatTokens[0], floatTokens[0]);

            return new Vector3(defaultScalar, defaultScalar, defaultScalar);
        }

        public static Vector4 Vector4FromString(string v, float defaultScalar = 0)
        {
            float[] floatTokens = FloatArrayFromString(v);

            if (floatTokens.Length >= 4)
                return new Vector4(floatTokens[0], floatTokens[1], floatTokens[2], floatTokens[3]);

            if (floatTokens.Length == 1)
                return new Vector4(floatTokens[0], floatTokens[0], floatTokens[0], floatTokens[0]);

            return new Vector4(defaultScalar, defaultScalar, defaultScalar, defaultScalar);
        }

        // Helper to convert a Color to a string
        public static string ToString(Color c)
        {
            return c.r + " " + c.g + " " + c.b;
        }

        public static double ToRadians(double angle)
        {
            return Mathf.Deg2Rad * angle;
        }

        public static float ToDegrees(float radians)
        {
            return Mathf.Rad2Deg * radians;
        }
    }

    /*
     * This is the main class responsible for importing an X3D document
     */
    class X3DImporter : AssetPostprocessor
    {
        static protected Dictionary<string, GameObject> DefToObjMap = new Dictionary<string, GameObject>();

        public static string GetName(XmlNode node, GameObject obj)
        {
            if (node.Attributes != null)
            {
                var name = node.Attributes["DEF"];
                if (name != null)
                {
                    DefToObjMap[name.Value] = obj;
                    return name.Value;
                }
            }

            if (node.Name != null)
                return node.Name;
            else
                return "Unnamed";
        }

        static string GetAttribute(XmlNode node, string attrib)
        {
            if (node.Attributes != null)
            {
                var a = node.Attributes[attrib];
                if (a != null)
                    return a.Value;
            }

            return "";
        }

        static Vector3 Vector3FromAttribute(XmlNode node, string attrib, float defaultScalar = 0)
        {
            return Tools.Vector3FromString(GetAttribute(node, attrib), defaultScalar);
        }

        static Vector4 Vector4FromAttribute(XmlNode node, string attrib, float defaultScalar = 0)
        {
            return Tools.Vector4FromString(GetAttribute(node, attrib), defaultScalar);
        }

        static float FloatFromAttribute(XmlNode node, string attrib)
        {
            string v = GetAttribute(node, attrib);

            if (v == "")
                return 0;

            return float.Parse(v);
        }

        static void ReadBox(XmlNode boxNode, GameObject obj)
        {
            Vector3 size = Vector3FromAttribute(boxNode, "size", 1);
            obj.transform.localScale = size;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = cube.GetComponent<MeshFilter>().sharedMesh;
            GameObject.DestroyImmediate(cube);
        }

        static void ReadIndexedFaceSet(XmlNode indexedFaceSetNode, GameObject obj)
        {
            int[] coordIndex = Tools.IntArrayFromString(GetAttribute(indexedFaceSetNode, "coordIndex"));
            if (coordIndex.Length == 0)
                return;

            XmlNode coordinateNode = null;
            XmlNode normalNode = null;

            foreach(XmlNode child in indexedFaceSetNode)
            {
                if (child.Name == "Coordinate")
                    coordinateNode = child;

                if (child.Name == "Normal")
                    normalNode = child;
            }

            if (coordinateNode == null)
                return;
            
            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            Mesh mesh = new Mesh();

            // Setup vertices
            {
                float[] points = Tools.FloatArrayFromString(GetAttribute(coordinateNode, "point"));
                List<Vector3> vertices = new List<Vector3>();

                for (int i = 0; i < points.Length; i += 3)
                {
                    vertices.Add(new Vector3(points[i + 0], points[i + 1], points[i + 2]));
                }

                mesh.SetVertices(vertices);
            }

            // Setup normals
            if (normalNode != null)
            {
                float[] vectors = Tools.FloatArrayFromString(GetAttribute(normalNode, "vector"));
                List<Vector3> normals = new List<Vector3>();

                for (int i = 0; i < vectors.Length; i += 3)
                {
                    normals.Add(new Vector3(vectors[i + 0], vectors[i + 1], vectors[i + 2]));
                }

                mesh.SetNormals(normals);
            }

            // Setup indices
            { 
                int size = coordIndex.Length / 4 * 3;

                // Filter out every fourth entry
                int[] triangles = new int[size];
                int j = 0;
                for (int i = 0; i < coordIndex.Length; i += 4, j += 3)
                {
                    triangles[j + 0] = coordIndex[i + 0];
                    triangles[j + 1] = coordIndex[i + 1];
                    triangles[j + 2] = coordIndex[i + 2];
                }

                mesh.SetTriangles(triangles, 0);
            }

            // Insert mesh
            meshFilter.mesh = mesh;
        }

        static void ReadCommonSurfaceShader(XmlNode cssNode, GameObject obj)
        {
            Renderer renderer = obj.GetComponent<MeshRenderer>();

            Vector3 specular = Vector3FromAttribute(cssNode, "specularFactor");
            float metalness = specular.magnitude;
            renderer.sharedMaterial.SetFloat("_Metallic", metalness);

            float shininess = FloatFromAttribute(cssNode, "shininessFactor");
            float smoothness = Mathf.Sqrt(shininess);
            renderer.sharedMaterial.SetFloat("_Glossiness", smoothness);

            Vector3 diffuse = Vector3FromAttribute(cssNode, "diffuseFactor");
            Color diffuseColor = new Color(diffuse[0], diffuse[1], diffuse[2]);
            renderer.sharedMaterial.SetColor("_Color", diffuseColor * 1.0f / (1 - metalness));

            Vector3 emissive = Vector3FromAttribute(cssNode, "emissionColor");
            Color emissiveColor = new Color(emissive[0], emissive[1], emissive[2]);
            renderer.sharedMaterial.SetColor("_EmissionColor", emissiveColor);

            Vector3 ambientFactor = Vector3FromAttribute(cssNode, "ambientFactor");
            RenderSettings.ambientIntensity = ambientFactor.magnitude;
            
            // TODO: Textures
        }

        static void ReadMaterial(XmlNode materialNode, GameObject obj)
        {
            Renderer renderer = obj.GetComponent<MeshRenderer>();

            Vector3 specular = Vector3FromAttribute(materialNode, "specularColor");
            float metalness = specular.magnitude;
            renderer.sharedMaterial.SetFloat("_Metallic", metalness);

            float shininess = FloatFromAttribute(materialNode, "shininess");
            float smoothness = Mathf.Sqrt(shininess);
            renderer.sharedMaterial.SetFloat("_Glossiness", smoothness);

            Vector3 diffuse = Vector3FromAttribute(materialNode, "diffuseColor");
            Color diffuseColor = new Color (diffuse[0], diffuse[1], diffuse[2]);
            renderer.sharedMaterial.SetColor("_Color", diffuseColor * 1.0f / (1 - metalness));
            
            Vector3 emissive = Vector3FromAttribute(materialNode, "emissiveColor");
            Color emissiveColor = new Color(emissive[0], emissive[1], emissive[2]);
            renderer.sharedMaterial.SetColor("_EmissionColor", emissiveColor);

            float ambientIntensity = FloatFromAttribute(materialNode, "ambientIntensity");
            RenderSettings.ambientIntensity = ambientIntensity;

            // TODO: Textures
        }

        static void ReadAppearance(XmlNode appearanceNode, GameObject obj)
        {
            // Add dummy material just in case Shape didn't come with a material
            Renderer renderer = obj.AddComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Standard"));

            // Detect some kind of material
            foreach (XmlNode child in appearanceNode.ChildNodes)
            {
                if (child.Name == "Material")
                {
                    ReadMaterial(child, obj);
                    break;
                }
                else if (child.Name == "CommonSurfaceShader")
                {
                    ReadCommonSurfaceShader(child, obj);
                    break;
                }
            }
        }

        static void ReadShape(XmlNode shapeNode, GameObject obj)
        {
            // Detect some kind of mesh (only one)
            foreach (XmlNode child in shapeNode.ChildNodes)
            {
                if (child.Name == "Box")
                {
                    ReadBox(child, obj);
                    break;
                }
                else if (child.Name == "IndexedFaceSet")
                {
                    ReadIndexedFaceSet(child, obj);
                    break;
                }
            }

            // Detect other relevant attributes
            foreach (XmlNode child in shapeNode.ChildNodes)
            {
                if (child.Name == "Appearance")
                {
                    ReadAppearance(child, obj);
                }
            }
        }

        static void ReadTransform(XmlNode transformNode, GameObject obj)
        {
            Vector3 translate = Vector3FromAttribute(transformNode, "translation");
            obj.transform.localPosition = translate;

            Vector4 rotation = Vector4FromAttribute(transformNode, "rotation");
            Vector3 axis = new Vector3(rotation[0], rotation[1], rotation[2]);
            float angle = Tools.ToDegrees(rotation[3]);
            obj.transform.rotation = Quaternion.AngleAxis(angle, axis);

            Vector3 scale = Vector3FromAttribute(transformNode, "scale", 1);
            obj.transform.localScale = scale;
            
            ReadX3D(transformNode, obj);
        }

        static void ReadX3D(XmlNode node, GameObject parent)
        {
            foreach (XmlNode childNode in node)
            {
                // Filter out comments
                if (childNode.Name == "#comment")
                    continue;

                // Re-use object? TODO: Wait until whole document is actually parsed
                string use = GetAttribute(childNode, "USE");
                if (use != "" && DefToObjMap.Keys.Contains(use))
                {
                    Object.Instantiate(DefToObjMap[use], parent.transform, false);
                    continue;
                }

                GameObject obj = new GameObject();
                obj.name = GetName(childNode, obj);

                // Setup parent node
                if (parent != null)
                    obj.transform.parent = parent.transform;

                if (childNode.Name == "Transform")
                    ReadTransform(childNode, obj);

                if (childNode.Name == "Shape")
                    ReadShape(childNode, obj);
            }
        }

        static void ReadX3D(string path)
        {
            Tools.ClearConsole();

            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(path);

                // Get to Scene node
                XmlNode x3dNode = xml.SelectSingleNode("X3D");
                XmlNode sceneNode = x3dNode.SelectSingleNode("Scene");

                GameObject scene = new GameObject("Scene");
                ReadX3D(sceneNode, scene);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.ToString());
            }

            EditorUtility.ClearProgressBar();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets.Where(x => x.EndsWith(".x3d")))
            {
                ReadX3D(path);
            }
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

        static protected int currentNodeIndex;
        static protected int numNodesToExport;
        
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
                AddXmlAttribute(viewpointNode, "position", Tools.ToString(transform.transform.localPosition));

                float angle = 0F;
                Vector3 axis = Vector3.zero;
                transform.transform.localRotation.ToAngleAxis(out angle, out axis);

                AddXmlAttribute(viewpointNode, "orientation", Tools.ToString(axis) + " " + Tools.ToRadians(angle).ToString());
            }

            return viewpointNode;
        }

        // convert a Mesh and accompanying Renderer to X3D
        static XmlNode ObjectToX3D(string name, Mesh mesh, Renderer renderer)
        {
            if(mesh == null)
            {
                Debug.LogWarning("UnityX3D Warning: SharedMesh reference of " + name + " is null.");
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
                AddXmlAttribute(geometryNode, "radius",(0.5).Tools.ToString());
            }
            else if(mesh.name == "Cylinder Instance")
            {
                geometryNode = CreateNode("Cylinder");
            }
            else if(mesh.name == "Cube")
            {
                geometryNode = CreateNode("Box");
                AddXmlAttribute(geometryNode, "size", Tools.ToString(Vector3.one));
            }
            else
            */
            {
                geometryNode = CreateNode("IndexedFaceSet");

                if (hasLightmap && Preferences.lightmapUnlitFaceSets)
                    AddXmlAttribute(geometryNode, "lit", "FALSE");

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
                        sbVectorValue.Append(Tools.ToString(n) + " ");

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
                        sbPointValue.Append(Tools.ToString(v) + " ");

                    point.Value = sbPointValue.ToString();

                    geometryNode.AppendChild(coordinateNode);
                }

                // process lightmap UV coordinates
                Vector2[] lightmapUVs = mesh.uv2;

                if (lightmapUVs.Length == 0 && hasLightmap)
                {
                    lightmapUVs = mesh.uv;
                }

                if (lightmapUVs.Length > 0 && Preferences.exportLightmaps)
                {
                    XmlNode textureCoordinateNode = CreateNode("TextureCoordinate");
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

                        sbPointValue.Append(Tools.ToString(vv) + " ");
                    }

                    point.Value = sbPointValue.ToString();

                    multiTextureCoordinateNode.AppendChild(textureCoordinateNode);
                }

                // process UV coordinates
                if(mesh.uv.Length > 0 && lightmapUVs != mesh.uv)
                {
                    XmlNode textureCoordinateNode = CreateNode("TextureCoordinate");
                    XmlAttribute point = xml.CreateAttribute("point");
                    textureCoordinateNode.Attributes.Append(point);

                    System.Text.StringBuilder sbPointValue = new System.Text.StringBuilder();

                    foreach (Vector2 v in mesh.uv)
                    {
                        sbPointValue.Append(Tools.ToString(v) + " ");
                    }

                    point.Value = sbPointValue.ToString();

                    multiTextureCoordinateNode.AppendChild(textureCoordinateNode);
                }

                if (multiTextureCoordinateNode.HasChildNodes)
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

            int dotIndex = renderer.gameObject.scene.path.LastIndexOf(".");
            string scenePath = renderer.gameObject.scene.path.Remove(dotIndex);

            int slashIndex = Application.dataPath.LastIndexOf("/");
            string dataPath = Application.dataPath.Remove(slashIndex);

            string lightmapFileName = "Lightmap-" + renderer.lightmapIndex + "_comp_light" + ext;

            string path = dataPath + "/" + scenePath + "/" + lightmapFileName;
            string file = renderer.gameObject.scene.name + "_" + lightmapFileName;

            // TODO maybe prompt for each overwrite?
            if (Preferences.savePNGlightmaps)
            {
                LightmapData lightmapData = LightmapSettings.lightmaps[renderer.lightmapIndex];
                Texture2D lightMap = CreateReadableTexture(lightmapData.lightmapColor, false);

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

            if (material == null || material.shader == null)
            {
                Debug.LogWarning("Material/Shader is null");
                return appearanceNode;
            }

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
                Vector4 uvTiling = material.GetVector("_MainTex_ST");
                
                // if the Renderer has a lightmap
                bool hasLightmap = renderer.lightmapIndex != -1;

                XmlNode textureTransform = CreateNode("TextureTransform");
                AddXmlAttribute(textureTransform, "scale", uvTiling.x + " " + uvTiling.y);

                if (Preferences.useCommonSurfaceShader)
                {
                    XmlNode css = CreateNode("CommonSurfaceShader");

                    // TODO add texture IDs
                    if (hasLightmap && Preferences.exportLightmaps)
                    {
                        XmlNode ambientTextureNode = LightmapToX3D(renderer, "ambientTexture", true);
                        css.AppendChild(ambientTextureNode);
                    }

                    if (albedoMap != null)
                    {
                        XmlNode diffuseTextureNode = TextureToX3D(albedoMap, "diffuseTexture", true);
                        diffuseTextureNode.AppendChild(textureTransform);
                        css.AppendChild(diffuseTextureNode);
                    }

                    if (normalMap != null)
                        css.AppendChild(TextureToX3D(normalMap, "normalTexture", true));

                    if (metallicGlossMap != null)
                        css.AppendChild(TextureToX3D(metallicGlossMap, "specularTexture", true));

                    if (emissionMap != null)
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
                        addAttribute(css, "reflectionTextureId", textureId.Tools.ToString());
                        textureId++;
                    }
                    */

                    AddXmlAttribute(css, "ambientFactor", Tools.ToString(new Vector3(1, 1, 1)));
                    AddXmlAttribute(css, "specularFactor", Tools.ToString(new Vector3(1, 1, 1) * metalness));
                    AddXmlAttribute(css, "diffuseFactor", Tools.ToString(color * (1 - metalness)));
                    AddXmlAttribute(css, "shininessFactor", (smoothness * smoothness).ToString());
                    AddXmlAttribute(css, "emissiveFactor", Tools.ToString(emissionColor));
                    //AddXmlAttribute(css, "reflectionFactor", Tools.ToString(new Vector3(1, 1, 1) * metalness));

                    appearanceNode.AppendChild(css);
                }
                else
                {
                    // write color
                    XmlNode materialNode = CreateNode("Material");
                    AddXmlAttribute(materialNode, "diffuseColor", Tools.ToString(color * (1 - metalness)));
                    AddXmlAttribute(materialNode, "specularColor", Tools.ToString(Vector3.one * metalness));
                    AddXmlAttribute(materialNode, "shininess", (smoothness * smoothness).ToString());
                    AddXmlAttribute(materialNode, "emissiveColor", Tools.ToString(emissionColor));
                    AddXmlAttribute(materialNode, "ambientIntensity", RenderSettings.ambientIntensity.ToString());

                    XmlNode multiTextureNode = CreateNode("MultiTexture");
                    AddXmlAttribute(multiTextureNode, "mode", "ADDSIGNED");
                    AddXmlAttribute(multiTextureNode, "source", "DIFFUSE");

                    XmlNode multiTextureTransformNode = CreateNode("MultiTextureTransform");
                    multiTextureTransformNode.AppendChild(textureTransform);

                    // Write lightmap
                    if (hasLightmap && Preferences.exportLightmaps)
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
            else
                Debug.LogWarning(renderer.name + " has no Standard material");

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
                AddXmlAttribute(lightNode, "ambientIntensity", light.intensity.Tools.ToString());
            else
            */
                AddXmlAttribute(lightNode, "intensity", light.intensity.ToString());

            AddXmlAttribute(lightNode, "color", Tools.ToString(light.color));

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
            XmlNode navigationInfoNode = CreateNode("NavigationInfo");
            AddXmlAttribute(navigationInfoNode, "headlight", Preferences.disableHeadlight ? "false" : "true");
            sceneNode.AppendChild(navigationInfoNode);

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
                XmlNode solidBackground = CreateNode("Background");
                AddXmlAttribute(solidBackground, "color", Tools.ToString(RenderSettings.ambientSkyColor));
                sceneNode.AppendChild(solidBackground);
            }
        }

        static XmlNode TransformToX3D(Transform transform)
        {
            EditorUtility.DisplayProgressBar("UnityX3D", "Exporting scene...", (float)currentNodeIndex/numNodesToExport);
            currentNodeIndex++;
            
            XmlNode transformNode;
            transformNode = CreateNode("Transform");

            AddXmlAttribute(transformNode, "DEF", ToSafeDEF(transform.name));
            AddXmlAttribute(transformNode, "translation", Tools.ToString(transform.transform.localPosition));

            float angle = 0F;
            Vector3 axis = Vector3.zero;
            transform.transform.localRotation.ToAngleAxis(out angle, out axis);
            AddXmlAttribute(transformNode, "rotation",  Tools.ToString(axis) + " " + Tools.ToRadians(angle).ToString());

            AddXmlAttribute(transformNode, "scale", Tools.ToString(transform.transform.localScale));

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

        static int CountNodes(Transform tr)
        {
            int count = 0;

            // Count nodes to display progress
            foreach (Transform c in tr)
                count += CountNodes(c);

            return count + tr.childCount;
        }

        [MenuItem("Assets/X3D/Export X3D...")]
        static void ExportX3D()
        {
            Tools.ClearConsole();

            try
            {
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
                AddXmlAttribute(X3DNode, "profile", "Immersive");
                AddXmlAttribute(X3DNode, "version", "3.3");
                AddXmlAttribute(X3DNode, "xmlns:xsd", "http://www.w3.org/2001/XMLSchema-instance");
                AddXmlAttribute(X3DNode, "xsd:noNamespaceSchemaLocation", "http://www.web3d.org/specifications/x3d-3.3.xsd");
                xml.AppendChild(X3DNode);

                sceneNode = CreateNode("Scene");
                X3DNode.AppendChild(sceneNode);

                ExportRenderSettings();

                XmlNode lhToRh = CreateNode("Transform");
                AddXmlAttribute(lhToRh, "scale", "1 1 -1");
                sceneNode.AppendChild(lhToRh);
                
                sceneNode = lhToRh;

                currentNodeIndex = 0;
                numNodesToExport = 0;

                // Count number of nodes for progress bar
                foreach(Transform tr in trs)
                    numNodesToExport += CountNodes(tr);
                
                foreach(Transform tr in trs)
                    sceneNode.AppendChild(TransformToX3D(tr));

                xml.Save(file);
            }
            catch(System.Exception e)
            {
                Debug.LogError(e.ToString());
            }

            EditorUtility.ClearProgressBar();
        }
    }
}
