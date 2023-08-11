using Assimp.Configs;
using ECS3D.ECSEngine.Components;
using ECS3D.ECSEngine.Internal;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ECS3D.ECSEngine
{
    public class Engine
    {
        public CameraComponent activeCamera = null;

        private GLControl activeGlControl = null;

        private List<GameEntity> entities = new List<GameEntity>();
        private Dictionary<Type, List<GameEntity>> entitiesByComponentType = new Dictionary<Type, List<GameEntity>>();
        public float DeltaTime { get; set; }

        float previousTime;

        private int skyboxShader = 0;
        private int skyboxTexture = 0;

        private Point lastMousePos;



        public Engine(GLControl control)
        {
            activeGlControl = control;



            //skyboxTexture = LoadSkyboxTexture();
            //skyboxShader = LoadSkyboxShader();

        }

        public List<GameEntity> GetEntities()
        {
            return entities;
        }

        private void CenterMouseCursor()
        {
            Point center = new Point(activeGlControl.Width / 2, activeGlControl.Height / 2);
            Cursor.Position = activeGlControl.PointToScreen(center);
            lastMousePos = center;
        }

        public GameEntity CreateEntity(string name)
        {
            GameEntity entity = new GameEntity { Id = entities.Count + 1, EntityName = name };
            entity.Engine = this;
            entities.Add(entity);

            return entity;
        }

        public GameEntity CreateEntity(GameEntity Parent, string name)
        {
            var ent = CreateEntity(name);
            Parent.Children.Add(ent);
            return ent;
        }

        public void FixAspect()
        {
            if (activeCamera != null)
            {
                activeCamera.AspectRatio = (float)activeGlControl.ClientSize.Width / (float)activeGlControl.ClientSize.Height;
                RenderFrame();

            }
        }

        public void SetActiveCamera(CameraComponent activeCamera)
        {
            this.activeCamera = activeCamera;
            RenderFrame();
        }

        public CameraComponent CreateCamera(string name, Vector3 pos)
        {
            float fieldOfViewDegrees = 60f;
            float fieldOfViewRadians = (float)fieldOfViewDegrees * (float)(Math.PI / 180f);
            var entity = CreateEntity(name);
            var camera = new CameraComponent
            {
                position = pos,
                front = Vector3.UnitZ,
                up = Vector3.UnitY,
                FieldOfView = fieldOfViewRadians,
                AspectRatio = (float)activeGlControl.ClientSize.Width / activeGlControl.ClientSize.Height, // Convert to float before division
                NearPlane = 0.00001f,
                FarPlane = 10000000f
            };

            entity.AddComponent<CameraComponent>(camera);
            return camera;
        }

        private float GetCurrentTimeInSeconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
        }

        public GameEntity Create3DObj(string filepath, Vector3 pos)
        {
            var ent = CreateEntity(Guid.NewGuid().ToString());
            ent.AddComponent<MeshComponent>(MeshComponent.LoadFromFile(filepath, Assimp.PostProcessSteps.Triangulate, new NormalSmoothingAngleConfig(60f)));
            //ent.AddComponent<MeshComponent>(MeshComponent.LoadFromFile(filepath));
            //ent.GetComponent<MeshComponent>().BuildShaderCache();

            var transform = new TransformComponent();
            transform.Position = pos; // Set the initial position
            transform.Rotation = new Quaternion(new Vector3(0, 0, 0), 1f); // Set the initial rotation
            transform.Scale = new Vector3(1f, 1f, 1f); // Set the initial scale
            ent.AddComponent<TransformComponent>(transform);

            var meshRenderer = new MeshRenderer();
            ent.AddComponent<MeshRenderer>(meshRenderer);
            ent.Awake();
            return ent;

        }

        public void MoveCam(Keys key)
        {
            switch (key)
            {
                case Keys.W:
                    activeCamera.Move(CameraComponent.CameraMovement.Forward);
                    break;

                case Keys.A:
                    activeCamera.Move(CameraComponent.CameraMovement.Left);
                    break;

                case Keys.S:
                    activeCamera.Move(CameraComponent.CameraMovement.Backward);
                    break;

                case Keys.D:
                    activeCamera.Move(CameraComponent.CameraMovement.Right);
                    break;
            }
            RenderFrame();
        }

        public void RotateCam(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Calculate the mouse delta
                Vector2 mouseDelta = new Vector2(e.X - lastMousePos.X, e.Y - lastMousePos.Y);

                // Rotate the camera
                activeCamera.Rotate(mouseDelta);

                // Center the mouse cursor again
                CenterMouseCursor();

                // Redraw the scene
                RenderFrame();
            }
        }

        public void RenderFrame()
        {
            if (activeCamera != null)
            {
                activeGlControl.MakeCurrent();

                GL.ClearColor(Color.Gray);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                var viewMatrix = activeCamera.GetViewMatrix();
                var projectionMatrix = activeCamera.GetProjectionMatrix();

                // Set culling mode to back faces
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);

                // Set the view and projection matrices as uniforms in your shaders
                // You should have this setup in your shaders
                GL.MatrixMode(MatrixMode.Modelview);
                GL.LoadMatrix(ref viewMatrix);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadMatrix(ref projectionMatrix);

                // Render the skybox first
                //RenderSkybox();

                // Render the mesh
                foreach (var renderer in GetComponents<MeshRenderer>())
                {
                    int queryId;
                    GL.GenQueries(1, out queryId);
                    GL.BeginQuery(QueryTarget.SamplesPassed, queryId);
                    renderer.Render(activeCamera, viewMatrix, projectionMatrix, new List<System.Numerics.Vector3> { new System.Numerics.Vector3(0, 0, 0) });
                    GL.EndQuery(QueryTarget.SamplesPassed);

                    // Get query result
                    int result;
                    GL.GetQueryObject(queryId, GetQueryObjectParam.QueryResult, out result);

                    // Delete the query object
                    GL.DeleteQueries(1, ref queryId);
                }

                // Swap buffers
                activeGlControl.SwapBuffers();
            }
        }


        private int LoadSkyboxShader()
        {
            // Implement shader loading here and return the shader program ID
            // Example:
            string vertexShaderSource = LoadShaderSource("./shaders/builtin/skyboxes/SkyboxVertexShader.glsl");
            string fragmentShaderSource = LoadShaderSource("./shaders/builtin/skyboxes/SkyboxFragmentShader.glsl");
            int shaderProgram = CompileShaderProgram(vertexShaderSource, fragmentShaderSource);
            // return shaderProgram;
            return shaderProgram; // Placeholder, replace with actual implementation.
        }

        public static string LoadShaderSource(string filePath)
        {
            string shaderSource = string.Empty;

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    shaderSource = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error loading shader source: " + e.Message);
            }

            return shaderSource;
        }

        public static int CompileShaderProgram(string vertexShaderSource, string fragmentShaderSource)
        {
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

            // Check for compilation errors and shader program linking status
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vertexShaderStatus);
            GL.GetShaderInfoLog(vertexShader, out string vertexShaderInfoLog);
            Console.WriteLine("Vertex shader compilation status: " + (vertexShaderStatus == 1 ? "Success" : "Failure"));
            Console.WriteLine("Vertex shader info log:\n" + vertexShaderInfoLog);

            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fragmentShaderStatus);
            GL.GetShaderInfoLog(fragmentShader, out string fragmentShaderInfoLog);
            Console.WriteLine("Fragment shader compilation status: " + (fragmentShaderStatus == 1 ? "Success" : "Failure"));
            Console.WriteLine("Fragment shader info log:\n" + fragmentShaderInfoLog);

            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, out int shaderProgramStatus);
            GL.GetProgramInfoLog(shaderProgram, out string shaderProgramInfoLog);
            Console.WriteLine("Shader program linking status: " + (shaderProgramStatus == 1 ? "Success" : "Failure"));
            Console.WriteLine("Shader program info log:\n" + shaderProgramInfoLog);

            return shaderProgram;
        }

        private void RenderSkybox()
        {
            GL.UseProgram(skyboxShader);

            var viewMatrixNoTranslation = activeCamera.GetViewMatrix();
            GL.UniformMatrix4(0, false, ref viewMatrixNoTranslation);
            var projMatrix = activeCamera.GetProjectionMatrix();
            GL.UniformMatrix4(1, false, ref projMatrix);

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            GL.BindTexture(TextureTarget.TextureCubeMap, skyboxTexture);

            //GL.BindVertexArray(cubeVAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);  // 36 vertices for a cube
            GL.BindVertexArray(0);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);

            GL.BindTexture(TextureTarget.TextureCubeMap, 0);

            GL.UseProgram(0);
        }


        private int LoadSkyboxTexture()
        {
            // Implement texture loading here and return the texture ID
            // Example:
            int textureId = LoadTexture("./textures/builtin/skyboxes/SkyboxTexture.png");
            return textureId;
           
        }

        public static int LoadTexture(string filePath)
        {
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            Bitmap bmp = new Bitmap(filePath);
            BitmapData bmpData = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          bmpData.Width, bmpData.Height, 0,
                          OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte,
                          bmpData.Scan0);

            bmp.UnlockBits(bmpData);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            return textureId;
        }

        public void SetActiveGlControl(GLControl control)
        {
            activeGlControl = control;
        }

        public void AddComponent<T>(GameEntity entity, T component) where T : EntityComponent
        {
            Type componentType = typeof(T);
            if (!entitiesByComponentType.ContainsKey(componentType))
            {
                entitiesByComponentType[componentType] = new List<GameEntity>();
            }
            entitiesByComponentType[componentType].Add(entity);
            component.Entity = entity;
            entity.Components[componentType] = component;
        }

        public void RemoveComponent<T>(GameEntity entity) where T : EntityComponent
        {
            Type componentType = typeof(T);
            if (entity.Components.ContainsKey(componentType))
            {
                entity.Components.Remove(componentType);
                entitiesByComponentType[componentType].Remove(entity);
            }
        }


        public List<T> GetComponents<T>() where T : EntityComponent
        {
            var type = typeof(T);

            var components = entities
                .SelectMany(ent => ent.Components.Values.Where(x => x.GetType() == type))
                .Cast<T>() // Explicitly cast to type T
                .ToList();

            return components;
        }



        public T GetComponent<T>(GameEntity entity) where T : EntityComponent
        {
            Type componentType = typeof(T);
            if (entity.Components.TryGetValue(componentType, out var component))
            {
                return (T)component;
            }

            return null;
        }



        public void Awake()
        {
            foreach (var entity in entities)
            {
                entity.Awake();

            }
        }

        public void Update()
        {
            foreach (var entity in entities)
            {
                entity.Update();
            }
        }
    }

}
