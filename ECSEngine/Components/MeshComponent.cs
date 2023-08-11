using Assimp.Configs;
using Assimp;
using glTFLoader.Schema;
using glTFLoader;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;
using System.IO;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;
using ECS3D.ECSEngine.Internal;

namespace ECS3D.ECSEngine.Components
{
    public class MeshComponent : EntityComponent
    {
        public Assimp.Scene Scene { get; set; } = null;

        private MeshRenderer renderer;
        public Gltf Gltf { get; set; } = null;
        public Color MeshColor { get; set; } = Color.White;

        bool buildingCache = false;

        public MeshComponent(Assimp.Scene scene)
        {
            Scene = scene;
        }

        public MeshComponent(Gltf gltf)
        {
            Gltf = gltf;
        }

        private static void CheckGLError(string operation)
        {
            ErrorCode errorCode = GL.GetError();
            if (errorCode != ErrorCode.NoError)
            {
                throw new Exception($"OpenGL error ({errorCode}) occurred during {operation}");
            }
        }


        public override void Awake()
        {
            renderer = Entity.GetComponent<MeshRenderer>();
            BuildShaderCache();
        }


        public override void OnEnable()
        {
            BuildShaderCache();

        }

        public void BuildShaderCache()
        {
            if (!buildingCache)
            {
                buildingCache = true;
                Console.WriteLine("[SHCH] - Building Shader Cache for: " + Entity.EntityName);
                if (Scene != null)
                {
                    foreach (var mesh in Scene.Meshes)
                    {
                        renderer.AssimpShaderCache.Add(mesh, BuildAssimpShader(mesh));
                    }
                }
                else if (Gltf != null)
                {
                    foreach (var material in Gltf.Materials)
                    {
                        renderer.GltfShaderCache.Add(material, BuildGltfShader(material));
                    }
                }

            }

        }

        public override void OnDisable()
        {
            Console.WriteLine("[SHCH] - Clearing cached shaders for: " + Entity.EntityName);

            if(Scene != null)
            {
                foreach (var mesh in Scene.Meshes)
                {

                    if (renderer.AssimpShaderCache.ContainsKey(mesh))
                    {
                        int shaderIndex = renderer.AssimpShaderCache[mesh];

                        renderer.AssimpShaderCache.Remove(mesh);
                        GL.DeleteProgram(shaderIndex);
                    }


                }

            }
            else if(Gltf != null)
            {
            
            }


        }


        public void Draw(CameraComponent cam, Matrix4 mvpMatrix, Matrix4 modelMatrix, Vector3 lightDirection)
        {

            if (Scene != null)
            {
                for (int i = 0; i < Scene.RootNode.ChildCount; i++)
                {
                    var node = Scene.RootNode.Children[i];

                    if (node.HasMeshes)
                    {
                        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                        GL.DepthFunc(DepthFunction.Less);

                        foreach (var meshIndex in node.MeshIndices)
                        {
                            var mesh = Scene.Meshes[meshIndex];
                            var material = Scene.Materials[mesh.MaterialIndex];

                            int shaderProgram = renderer.AssimpShaderCache[mesh];
                            Console.WriteLine("[SHCH] - Using cached shader at index: " + shaderProgram);
                            GL.UseProgram(shaderProgram);


                            //Expose even more stuff to the shader:
                            int mvpLocation = GL.GetUniformLocation(shaderProgram, "MvpMatrix");
                            GL.UniformMatrix4(mvpLocation, false, ref mvpMatrix);

                            int modelMatrixLocation = GL.GetUniformLocation(shaderProgram, "ModelMatrix");
                            GL.UniformMatrix4(modelMatrixLocation, false, ref modelMatrix);

                            // Calculate the normal matrix
                            Matrix3 normalMatrix = new Matrix3(Matrix4.Transpose(Matrix4.Invert(modelMatrix)));
                            int normalMatrixLocation = GL.GetUniformLocation(shaderProgram, "NormalMatrix");
                            GL.UniformMatrix3(normalMatrixLocation, false, ref normalMatrix);

                            int lightDirectionLocation = GL.GetUniformLocation(shaderProgram, "LightDirection");
                            GL.Uniform3(lightDirectionLocation, ref lightDirection);

                            int cameraPosLocation = GL.GetUniformLocation(shaderProgram, "CameraPos");
                            GL.Uniform3(cameraPosLocation, cam.position);

                            int vao = SetupVAO(mesh);
                            GL.BindVertexArray(vao);

                            if (shaderProgram != 0 && vao != 0)
                            {
                                GL.DrawElements(PrimitiveType.Triangles, mesh.GetIndices().Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
                            }

                            GL.BindVertexArray(0);
                            GL.DeleteProgram(shaderProgram);
                        }



                    }

                }
            }
            else if (Gltf != null)
            {
                foreach (var nodeIndex in Gltf.Scenes[0].Nodes)
                {
                    var node = Gltf.Nodes[nodeIndex];
                    var mesh = Gltf.Meshes[(int)node.Mesh];
                    foreach (var primitive in mesh.Primitives)
                    {
                        var materialIndex = primitive.Material;  // Index of the material used by this primitive
                        var material = Gltf.Materials[(int)materialIndex];  // Access the material using the index


                    }
                }
            }

        }

        public int BuildGltfShader(glTFLoader.Schema.Material material)
        {
            Console.WriteLine("[GLSL] - Building a pbr shader..");
            Console.WriteLine("[GLSL] - Linking shader..");
            int shaderProg = CreateAndLinkShaderProgram("./shaders/builtin/pbr.vert", "./shaders/builtin/pbr.frag");
            CheckGLError("GL.CompileShader");
            GL.UseProgram(shaderProg);

            int AlphaCutoff = GL.GetUniformLocation(shaderProg, "AlphaCutoff");
            GL.Uniform1(AlphaCutoff, material.AlphaCutoff);
            Console.WriteLine("[GLSL] - AlphaCutoff set!");
            CheckGLError("GL.Uniform1");

            int baseColorFactorLocation = GL.GetUniformLocation(shaderProg, "BaseColorFactor");
            GL.Uniform4(baseColorFactorLocation, material.PbrMetallicRoughness.BaseColorFactor[0], material.PbrMetallicRoughness.BaseColorFactor[1], material.PbrMetallicRoughness.BaseColorFactor[2], material.PbrMetallicRoughness.BaseColorFactor[3]);
            Console.WriteLine("[GLSL] - Base Color Factor set!");
            CheckGLError("GL.Uniform4");

            int metallicFactorLocation = GL.GetUniformLocation(shaderProg, "MetallicFactor");
            GL.Uniform1(metallicFactorLocation, material.PbrMetallicRoughness.MetallicFactor);
            Console.WriteLine("[GLSL] - Metallic Factor set!");
            CheckGLError("GL.Uniform1");

            int roughnessFactorLocation = GL.GetUniformLocation(shaderProg, "RoughnessFactor");
            GL.Uniform1(roughnessFactorLocation, material.PbrMetallicRoughness.RoughnessFactor);
            Console.WriteLine("[GLSL] - Roughness Factor set!");
            CheckGLError("GL.Uniform1");

            int EmissiveFactorLocation = GL.GetUniformLocation(shaderProg, "EmissiveFactor");
            for (int i = 0; i < material.EmissiveFactor.Length; i++)
            {
                GL.Uniform1(EmissiveFactorLocation + i, material.EmissiveFactor[i]);

                // Check for errors after each Uniform call
                CheckGLError("GL.Uniform1");

                // You can also check the uniform value to ensure it's set correctly
                float uniformValue;
                GL.GetUniform(shaderProg, EmissiveFactorLocation + i, out uniformValue);
                Console.WriteLine($"Uniform value for index {i}: {uniformValue}");
            }
            Console.WriteLine("[GLSL] - Emissive Factor set!");

            int baseColorTextureLocation = GL.GetUniformLocation(shaderProg, "BaseColorTexture");
            GL.Uniform1(baseColorTextureLocation, 0); // Texture unit 0
            if (material.PbrMetallicRoughness.BaseColorTexture != null)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, material.PbrMetallicRoughness.BaseColorTexture.Index);
                CheckGLError("GL.BindTexture");
            }
            Console.WriteLine("[GLSL] - Base Color Texture set!");

            int metallicRoughnessTextureLocation = GL.GetUniformLocation(shaderProg, "MetallicRoughnessTexture");
            GL.Uniform1(metallicRoughnessTextureLocation, 1); // Texture unit 1
            if (material.PbrMetallicRoughness.MetallicRoughnessTexture != null)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, material.PbrMetallicRoughness.MetallicRoughnessTexture.Index);
                CheckGLError("GL.BindTexture");
            }
            Console.WriteLine("[GLSL] - Metallic Roughness Texture set!");

            int OcclussionTexture = GL.GetUniformLocation(shaderProg, "OcclussionTexture");
            GL.Uniform1(OcclussionTexture, 2); // Texture unit 1
            if (material.PbrMetallicRoughness.MetallicRoughnessTexture != null)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, material.OcclusionTexture.Index);
                CheckGLError("GL.BindTexture");
            }
            Console.WriteLine("[GLSL] - Occlussion Texture set!");



            GL.UseProgram(0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            CheckGLError("GL.BindTexture");
            Console.WriteLine("[GLSL] - Shader build! index: " + shaderProg);

            return shaderProg;
        }


        private int SetupVAO(Assimp.Mesh mesh)
        {
            var vertices = mesh.Vertices.SelectMany(v => new float[] { v.X, v.Y, v.Z }).ToList();
            var indices = mesh.GetIndices().ToList();
            var normals = mesh.Normals.SelectMany(n => new float[] { n.X, n.Y, n.Z }).ToList();

            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            int vboVertices = SetupVBO(vertices, 0, 3);
            int vboNormals = SetupVBO(normals, 1, 3);

            int ebo = SetupEBO(indices);

            GL.BindVertexArray(0);

            return vao;
        }

        private int SetupVBO(List<float> data, int attributeIndex, int attributeSize)
        {
            int vbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Count * sizeof(float), data.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(attributeIndex, attributeSize, VertexAttribPointerType.Float, false, attributeSize * sizeof(float), 0);
            GL.EnableVertexAttribArray(attributeIndex);

            return vbo;
        }

        private int SetupEBO(List<int> indices)
        {
            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(int), indices.ToArray(), BufferUsageHint.StaticDraw);

            return ebo;
        }

        //Can compile shaders at runtime if needed
        public int BuildAssimpShader(Assimp.Mesh mesh)
        {
            var material = Scene.Materials[mesh.MaterialIndex];

            Console.WriteLine("[GLSL] - Building a pbr shader..");
            Console.WriteLine("[GLSL] - Linking shader..");
            int shaderProg = CreateAndLinkShaderProgram("./shaders/builtin/pbr.vert", "./shaders/builtin/pbr.frag");
            CheckGLError("GL.CompileShader");
            GL.UseProgram(shaderProg);

            Console.WriteLine("[GLSL] - Color Ambient set!");
            int ColorAmbient = GL.GetUniformLocation(shaderProg, "ColorAmbient");
            GL.Uniform4(ColorAmbient, ToCol(material.ColorAmbient));
            Console.WriteLine("[GLSL] - Color Diffuse set!");
            int ColorDiffuse = GL.GetUniformLocation(shaderProg, "ColorDiffuse");
            GL.Uniform4(ColorDiffuse, ToCol(material.ColorDiffuse));
            Console.WriteLine("[GLSL] - Color Emissive set!");
            int ColorEmissive = GL.GetUniformLocation(shaderProg, "ColorEmissive");
            GL.Uniform4(ColorEmissive, ToCol(material.ColorEmissive));
            Console.WriteLine("[GLSL] - Color Reflective set!");
            int ColorReflective = GL.GetUniformLocation(shaderProg, "ColorReflective");
            GL.Uniform4(ColorReflective, ToCol(material.ColorReflective));
            Console.WriteLine("[GLSL] - Color Specular set!");
            int ColorSpecular = GL.GetUniformLocation(shaderProg, "ColorSpecular");
            GL.Uniform4(ColorSpecular, ToCol(material.ColorSpecular));
            Console.WriteLine("[GLSL] - Color Transparent set!");
            int ColorTransparent = GL.GetUniformLocation(shaderProg, "ColorTransparent");
            GL.Uniform4(ColorTransparent, ToCol(material.ColorTransparent));
            Console.WriteLine("[GLSL] - Adding materials..");
            int textureUnitCounter = 0;
            foreach (var texture in material.GetAllMaterialTextures())
            {
                string materialName = texture.TextureType.ToString() + "Texture";

                int textureId;
                GL.GenTextures(1, out textureId);
                GL.BindTexture(TextureTarget.Texture2D, textureId);

                var image = System.Drawing.Image.FromFile(texture.FilePath);

                // Convert the image to a byte array (assuming it's a Bitmap)
                Bitmap bitmap = new Bitmap(image);
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // Load the image data into OpenGL
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                              OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                bitmap.UnlockBits(data);
                bitmap.Dispose();

                // Set texture parameters (e.g., filtering modes)
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

                // Set wrapping modes if needed
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

                // Bind the texture to the current texture unit
                GL.ActiveTexture(TextureUnit.Texture0 + textureUnitCounter);
                GL.BindTexture(TextureTarget.Texture2D, textureId);

                int textureUniformLocation = GL.GetUniformLocation(shaderProg, materialName);
                GL.UseProgram(shaderProg);
                GL.Uniform1(textureUniformLocation, textureUnitCounter); // Use the current texture unit

                // Increment the texture unit counter
                Console.WriteLine($"[GLSL] - Exposed {materialName} to glsl!");
                textureUnitCounter++;
            }
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            Console.WriteLine("[GLSL] - Shader build! index: " + shaderProg);

            return shaderProg;
        }

        private Vector4 ToCol(Color4D col)
        {
            return new Vector4(col.R, col.G, col.B, col.A);
        }



        private int CreateAndLinkShaderProgram(string vertexShaderPath, string fragmentShaderPath)
        {
            string vertexShaderSource = File.ReadAllText(vertexShaderPath);
            string fragmentShaderSource = File.ReadAllText(fragmentShaderPath);
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);

            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            string vertexShaderInfoLog = GL.GetShaderInfoLog(vertexShader);
            string fragmentShaderInfoLog = GL.GetShaderInfoLog(fragmentShader);
            string programInfoLog = GL.GetProgramInfoLog(shaderProgram);

            GL.DetachShader(shaderProgram, vertexShader);
            GL.DetachShader(shaderProgram, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);



            return shaderProgram;
        }

        public static MeshComponent LoadFromFile(string filePath, PostProcessSteps ppSteps, params PropertyConfig[] configs)
        {
            if (!File.Exists(filePath))
                return null;

            AssimpContext importer = new AssimpContext();
            if (configs != null)
            {
                foreach (PropertyConfig config in configs)
                    importer.SetConfig(config);
            }

            Assimp.Scene scene = importer.ImportFile(filePath, ppSteps);
            if (scene == null)
                return null;

            importer.Dispose();

            MeshComponent model = new MeshComponent(scene);
            return model;
        }

        public static MeshComponent LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var gltf = Interface.LoadModel(filePath);
            if (gltf == null)
                return null;



            MeshComponent model = new MeshComponent(gltf);
            return model;
        }

    }


}
