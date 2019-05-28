/*
 * UnityX3D
 *
 * Copyright (c) 2017 Tobias Alexander Franke
 * http://www.tobias-franke.eu
 *
 * Copyright (c) 2019 John Grime, ETL, University of Oklahoma
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
using System.Text;
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
        public static void ClearConsole()
        {
            Debug.ClearDeveloperConsole();
        }

        //
        // String helper routines
        //

        public static string ToString(Vector2 v)
        {
            return v.x + " " + v.y;
        }

        public static string ToString(Vector3 v)
        {
            return v.x + " " + v.y + " " + v.z;
        }

        public static string[] TokensFromString(string v)
        {
            if (!string.IsNullOrEmpty(v))
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

        //
        // XML node attribute helper routines
        //

        public static string GetAttribute(XmlNode node, string attrib)
        {
            if (node.Attributes != null)
            {
                var a = node.Attributes[attrib];
                if (a != null)
                    return a.Value;
            }

            return "";
        }

        public static Vector3 Vector3FromAttribute(XmlNode node, string attrib, float defaultScalar = 0)
        {
            return Vector3FromString(GetAttribute(node, attrib), defaultScalar);
        }

        public static Vector4 Vector4FromAttribute(XmlNode node, string attrib, float defaultScalar = 0)
        {
            return Vector4FromString(GetAttribute(node, attrib), defaultScalar);
        }

        public static float FloatFromAttribute(XmlNode node, string attrib)
        {
            string v = GetAttribute(node, attrib);

            if (v == "")
                return 0;

            return float.Parse(v);
        }

        //
        // Misc. helper routines
        //

        public static string ToString(Color c)
        {
            return c.r + " " + c.g + " " + c.b;
        }

        public static double ToRadians(double degrees)
        {
            return Mathf.Deg2Rad * degrees;
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
        //
        // Data containers, output of processing specific X3D nodes
        //

        class MaterialInfo
        {
            // Per-material
            public float smoothness, metalness;
            public Color diffuseColor, emissiveColor;

            // This is actually a global render setting
            public float ambientIntensity;
        }

        class AppearanceInfo
        {
            public MaterialInfo material = null;
            public Texture2D texture = null;
        }

        class TransformInfo
        {
            public Vector3 translation, scale;
            public Quaternion rotation;
        }

        //
        // Map to help implement DEF/USE in X3D's "Shape" nodes
        //

        Dictionary<string, GameObject> DefToObjMap = new Dictionary<string, GameObject>();

        //
        // Extract node name (if present), populating DefToObj map as we go.
        //

        string GetName(XmlNode node, GameObject obj)
        {
            if (node.Attributes != null)
            {
                XmlAttribute name = node.Attributes["DEF"];
                if (name != null)
                {
                    DefToObjMap[name.Value] = obj;
                    return name.Value;
                }
            }

            return node.Name ?? "Unnamed";
        }

        //
        // Mesh generation 
        //

        GameObject ReadBox(XmlNode boxNode, GameObject parent)
        {
            Vector3 size = Tools.Vector3FromAttribute(boxNode, "size", 1);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            GameObject obj = new GameObject("Box");

            obj.transform.SetParent(parent.transform);
            obj.transform.localScale = size;

            MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
            meshFilter.mesh = cube.GetComponent<MeshFilter>().sharedMesh;

            GameObject.DestroyImmediate(cube);

            return obj;
        }

        List<GameObject> ReadIndexedFaceSet(XmlNode indexedFaceSetNode, GameObject parent)
        {
            List<GameObject> components = new List<GameObject>();
            float[] _vtx = null, _uv = null, _nrm = null, _rgba = null;

            //
            // Both of the following contain per-triangle indices into flat coord / tex coord arrays;
            // each set of triangle info is stored as a 4-tuple, with the 4th element ignored (it's a
            // sentinel with value -1 denoting the end of polygonal faces; we obviously assume triangles).
            //
            int[] _tri_vtx_idx = Tools.IntArrayFromString(Tools.GetAttribute(indexedFaceSetNode, "coordIndex"));
            int[] _tri_uv_idx = Tools.IntArrayFromString(Tools.GetAttribute(indexedFaceSetNode, "texCoordIndex"));

            // If we have no triangle vertices, we can't really do anything; return empty list.
            if (_tri_vtx_idx.Length == 0) return components;

            // Extract raw data arrays
            foreach (XmlNode child in indexedFaceSetNode)
            {
                if (child.Name == "Coordinate")
                    _vtx = Tools.FloatArrayFromString(Tools.GetAttribute(child, "point"));

                if (child.Name == "TextureCoordinate")
                    _uv = Tools.FloatArrayFromString(Tools.GetAttribute(child, "point"));

                if (child.Name == "Normal")
                    _nrm = Tools.FloatArrayFromString(Tools.GetAttribute(child, "vector"));

                if (child.Name == "ColorRGBA")
                    _rgba = Tools.FloatArrayFromString(Tools.GetAttribute(child, "color"));
            }

            // This should probably be treated as an error: We expect at least one vertex!
            if (_vtx == null) return components;

            //
            // Each triangle has per-vertex indices into the arrays containing the coordinate,
            // uv, normal, and rgba data. Walk the triangles, creating UNIQUE vertex data for
            // each triangle; Unity meshes assume separate per-vertex data for eg texture
            // coords etc (this is not guaranteed in X3D files).
            // 
            // Unity also typically limits vertex count in a mesh due to using 16 bit indices
            // (x < 2^16, so x < 65,536); if this limit is reached, add remaining data to a new,
            // different mesh. Unity GameObjects can only have one mesh, so meshes are assigned
            // to child objects (with new child objects created as and when needed).
            //

            // 65535 % 3 == 0, so (x >= max_mesh_vtx) OK for adding sets of 3 vertices
            const int max_mesh_vtx = 65535;

            // Current mesh to which we're adding vertex and triangle information
            Mesh mesh = null;

            // Current mesh data is accumulated into these Lists
            List<int> tri = new List<int>();
            List<Vector3> vtx = new List<Vector3>();
            List<Vector3> nrm = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<Color> rgba = new List<Color>();

            // Raw index arrays are sequential integer 4-tuples, so n_tri = length/4
            int n_tri = _tri_vtx_idx.Length / 4;

            // Set up new triangle data, creating unique vertex data as we go
            for (int tri_i = 0; tri_i < n_tri; tri_i++)
            {
                int tri_offset = tri_i * 4; // as indices are per-triangle 4-tuples (4th element ignored) 

                // Create new mesh object if needed; we'll only see this in the first loop iteration,
                // or immediately after adding a full mesh and clearing the previous accumulators.
                if (vtx.Count() < 1)
                {
                    GameObject obj = new GameObject("Mesh");
                    obj.transform.SetParent(parent.transform);

                    mesh = new Mesh();

                    MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
                    meshFilter.mesh = mesh;

                    components.Add(obj); // <- ensure new object reference is returned to the caller
                }

                // Generate new per-vertex data for each of the 3 vertices in the triangle
                for (int vtx_offset=0; vtx_offset < 3; vtx_offset++)
                {
                    int i = _tri_vtx_idx[tri_offset + vtx_offset];

                    // Create new per-vertex coordinate
                    {
                        int j = (i * 3);
                        vtx.Add(new Vector3(_vtx[j + 0], _vtx[j + 1], _vtx[j + 2]));
                    }

                    // Create new per-vertex normal
                    if (_nrm != null)
                    {
                        int j = (i * 3);
                        nrm.Add(new Vector3(_nrm[j + 0], _nrm[j + 1], _nrm[j + 2]));
                    }

                    // Create new per-vertex rgba color value
                    if (_rgba != null)
                    {
                        int j = (i * 4);
                        rgba.Add(new Vector4(_rgba[j + 0], _rgba[j + 1], _rgba[j + 2], _rgba[j + 3]));
                    }

                    // Create new per-vertex texture coordinate
                    if (_uv != null)
                    {
                        i = _tri_uv_idx[tri_offset + vtx_offset];
                        int j = (i * 2);
                        uv.Add(new Vector2(_uv[j + 0], _uv[j + 1]));
                    }
                }

                // Set new triangle vertex indices; simply sequential.
                tri.Add(tri.Count());
                tri.Add(tri.Count());
                tri.Add(tri.Count());

                // Have we reached the limit of what a single mesh can contain?
                if (vtx.Count() >= max_mesh_vtx)
                {
                    // Set mesh data
                    mesh.SetVertices(vtx);

                    if (_nrm  != null) mesh.SetNormals(nrm);
                    if (_rgba != null) mesh.SetColors(rgba);
                    if (_uv   != null) mesh.SetUVs(0, uv);

                    mesh.SetTriangles(tri, 0);

                    // Clear accumulators; triggers new mesh creation if loop continues.
                    tri.Clear();
                    vtx.Clear();
                    nrm.Clear();
                    uv.Clear();
                    rgba.Clear();
                }
            }

            // Populate the final mesh, if needed
            if (vtx.Count() > 0)
            {
                mesh.SetVertices(vtx);

                if (_nrm  != null) mesh.SetNormals(nrm);
                if (_rgba != null) mesh.SetColors(rgba);
                if (_uv   != null) mesh.SetUVs(0, uv);

                mesh.SetTriangles(tri, 0);
            }

            return components;
        }

        //
        // Material / texture generation
        //

        MaterialInfo ReadMaterial(XmlNode node, bool isCommonSurfaceShader = false)
        {
            float shininess;
            Vector3 specular, diffuse, emissive;
            float ambientIntensity;

            if (isCommonSurfaceShader)
            {
                shininess = Tools.FloatFromAttribute(node, "shininessFactor");

                specular = Tools.Vector3FromAttribute(node, "specularFactor");
                diffuse = Tools.Vector3FromAttribute(node, "diffuseFactor");
                emissive = Tools.Vector3FromAttribute(node, "emissionColor");

                Vector3 ambientFactor = Tools.Vector3FromAttribute(node, "ambientFactor");
                ambientIntensity = ambientFactor.magnitude;
            }
            else
            {
                shininess = Tools.FloatFromAttribute(node, "shininess");

                specular = Tools.Vector3FromAttribute(node, "specularColor");
                diffuse = Tools.Vector3FromAttribute(node, "diffuseColor");
                emissive = Tools.Vector3FromAttribute(node, "emissiveColor");

                ambientIntensity = Tools.FloatFromAttribute(node, "ambientIntensity");
            }

            return new MaterialInfo {
                smoothness = Mathf.Sqrt(shininess),
                metalness = specular.magnitude,
                diffuseColor = new Color(diffuse[0], diffuse[1], diffuse[2]),
                emissiveColor = new Color(emissive[0], emissive[1], emissive[2]),
                ambientIntensity = ambientIntensity
            };

            // TODO: textures
        }

        Texture2D ReadImageTexture(XmlNode imageTextureNode, string filepathPrefix)
        {
            string url = Tools.GetAttribute(imageTextureNode, "url");
            string localPath;

            // 
            // Use ImageTexture URL to create object texture. Here, I assume:
            // 
            // 1. URL refers to a local file (remote data fetch via http/ftp/etc currently unsupported)
            //
            // 2. There is only one texture path defined; in principle, the "url" attribute is an
            //    "MFString" - which can be a comma-delineated array of (quoted) strings:
            // 
            // https://doc.x3dom.org/author/Texturing/ImageTexture.html
            // http://www.web3d.org/specifications/X3dSchemaDocumentation3.3/x3d-3.3_MFString.html
            // 

            try
            {
                System.Uri uri = new System.Uri(Tools.GetAttribute(imageTextureNode, "url"));
                if (!uri.IsFile)
                {
                    Debug.LogWarning($"ImageTexture URI scheme '{uri.Scheme}' is not currently supported.");
                    return null;
                }
                localPath = uri.LocalPath;
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"ImageTexture parse failure for '{url}' as URI; assuming local file path.");
                if (string.IsNullOrEmpty(url))
                {
                    Debug.LogWarning($"ImageTexture url is null or empty; texture will not be applied.");
                    return null;
                }
                localPath = url;
            }

            string path = System.IO.Path.Combine(filepathPrefix, localPath);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"ImageTexture file path '{path}' not found; texture will not be applied.");
                return null;
            }

            // Read specified image data from file into a Unity texture
            byte[] imageBytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);

            return texture;
        }

        AppearanceInfo ReadAppearance(XmlNode appearanceNode, string filepathPrefix)
        {
            AppearanceInfo appearance = new AppearanceInfo();

            // Detect some kind of material & texture (if present)
            foreach (XmlNode child in appearanceNode.ChildNodes)
            {
                bool isCSS = (child.Name == "CommonSurfaceShader");

                if (isCSS || child.Name == "Material")
                {
                    appearance.material = ReadMaterial(child, isCommonSurfaceShader: isCSS);
                }
                else if (child.Name == "ImageTexture")
                {
                    appearance.texture = ReadImageTexture(child, filepathPrefix);
                }
            }

            return appearance;
        }

        //
        // Shapes are a combination of mesh data and the material properties applied to the mesh
        //

        void ReadShape(XmlNode shapeNode, GameObject parent, string filepathPrefix)
        {
            List<GameObject> components = new List<GameObject>();
            AppearanceInfo appearance = null;

            // Extract mesh components and appearance data
            foreach (XmlNode child in shapeNode.ChildNodes)
            {
                if (child.Name == "Box")
                {
                    GameObject box = ReadBox(child, parent);
                    components.Add(box);
                }
                else if (child.Name == "IndexedFaceSet")
                {
                    components.AddRange(ReadIndexedFaceSet(child, parent));
                }
                else if (child.Name == "Appearance")
                {
                    appearance = ReadAppearance(child, filepathPrefix);
                }
            }

            // Apply appearance data to all mesh components of the shape.
            foreach (GameObject obj in components)
            {
                Renderer renderer = obj.GetComponent<MeshRenderer>();

                if (renderer == null)
                    renderer = obj.AddComponent<MeshRenderer>();

                // Add a default material, will be modified below if needed.
                renderer.material = new Material(Shader.Find("Standard"));

                if (appearance == null) continue;

                // Apply material properties
                if (appearance.material != null)
                {
                    MaterialInfo m = appearance.material;
                    renderer.sharedMaterial.SetFloat("_Metallic", m.metalness);
                    renderer.sharedMaterial.SetFloat("_Glossiness", m.smoothness);
                    renderer.sharedMaterial.SetColor("_Color", m.diffuseColor * 1.0f / (1 - m.metalness));
                    renderer.sharedMaterial.SetColor("_EmissionColor", m.emissiveColor);
                    RenderSettings.ambientIntensity = m.ambientIntensity;
                }

                // Apply texture
                if (appearance.texture != null)
                    renderer.material.mainTexture = appearance.texture;
            }
        }

        TransformInfo ReadTransform(XmlNode transformNode)
        {
            TransformInfo ti = new TransformInfo();

            // Translation
            ti.translation = Tools.Vector3FromAttribute(transformNode, "translation");

            // Convert axis/angle rotation into quaternion
            {
                Vector4 values = Tools.Vector4FromAttribute(transformNode, "rotation");
                Vector3 axis = new Vector3(values[0], values[1], values[2]);
                float angle = Tools.ToDegrees(values[3]);
                ti.rotation = Quaternion.AngleAxis(angle, axis);
            }

            // scaling
            ti.scale = Tools.Vector3FromAttribute(transformNode, "scale", 1);

            return ti;
        }

        void ReadX3D(XmlNode node, GameObject parent, string filepathPrefix)
        {
            foreach (XmlNode childNode in node)
            {
                // Filter out comments
                if (childNode.Name == "#comment")
                    continue;

                // Re-use object? TODO: Wait until whole document is actually parsed
                string use = Tools.GetAttribute(childNode, "USE");
                if (use != "" && DefToObjMap.Keys.Contains(use))
                {
                    Object.Instantiate(DefToObjMap[use], parent.transform, false);
                    continue;
                }

                GameObject new_obj = new GameObject();
                new_obj.name = GetName(childNode, new_obj);

                // Set parent of new object, if specified
                if (parent != null)
                    new_obj.transform.parent = parent.transform;

                // What does the current node suggest we do?
                if (childNode.Name == "Transform")
                {
                    TransformInfo ti = ReadTransform(childNode);

                    new_obj.transform.localPosition = ti.translation;
                    new_obj.transform.rotation = ti.rotation;
                    new_obj.transform.localScale = ti.scale;

                    ReadX3D(childNode, new_obj, filepathPrefix); // Recurse into any child nodes under this transform
                }
                else if (childNode.Name == "Shape")
                {
                    ReadShape(childNode, new_obj, filepathPrefix);
                }
            }
        }

        //
        // Public entry point into X3D parsing; returns root GameObject for scene, populates XmlDocument.
        //

        public GameObject ReadX3D(string path, out XmlDocument xml, string filepathPrefix = null)
        {
            xml = null;

            Tools.ClearConsole();

            // We can't do much with a bad path.
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning( $"ReadX3D(): path is null or empty." );
                return null;
            }

            // No user-defined filepath prefix? Attempt to infer from path.
            if (filepathPrefix == null)
            {
                var fileInfo = new System.IO.FileInfo(path);
                filepathPrefix = fileInfo.Directory.FullName;
            }

            xml = new XmlDocument();
            xml.Load(path);

            // Get to Scene node
            XmlNode x3dNode = xml.SelectSingleNode("X3D");
            XmlNode sceneNode = x3dNode.SelectSingleNode("Scene");

            // Process scene node
            GameObject scene = new GameObject("Scene");
            ReadX3D(sceneNode, scene, filepathPrefix);

            EditorUtility.ClearProgressBar();

            return scene;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets.Where(x => x.EndsWith(".x3d")))
            {
                XmlDocument xml;
                X3DImporter imp = new X3DImporter();

                imp.ReadX3D(path, out xml);
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

                XmlTextWriter xmlTextWriter = new XmlTextWriter(file, new UTF8Encoding(false));
                xmlTextWriter.Formatting = Formatting.Indented; 
                xml.Save(xmlTextWriter);
                xmlTextWriter.Close();
            }
            catch(System.Exception e)
            {
                Debug.LogError(e.ToString());
            }

            EditorUtility.ClearProgressBar();
        }
    }
}
