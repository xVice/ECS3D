using Assimp.Configs;
using Assimp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using Quaternion = OpenTK.Quaternion;
using PrimitiveType = OpenTK.Graphics.OpenGL.PrimitiveType;

namespace ECS3D
{
    // Base Component class
    public abstract class EntityComponent
    {
        public ECSEngine Engine;
        public GameEntity Entity { get; internal set; }

        public virtual void Awake()
        {

        }

        public virtual void EarlyUpdate()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void LateUpdate()
        {

        }
    }

    // Base Entity class
    public class GameEntity
    {
        public ECSEngine Engine { get; set; }

        public int Id { get; internal set; }
        public string EntityName { get; set; }

        public List<GameEntity> Children = new List<GameEntity>();
        public Dictionary<Type, EntityComponent> Components { get; } = new Dictionary<Type, EntityComponent>();

        public GameEntity CreateChild(string name)
        {
            var child = Engine.CreateEntity(this, name);
            return child;
        }

        public T AddComponent<T>(T component) where T : EntityComponent
        {
            var componentType = component.GetType();
            Components.Add(componentType, component);
            component.Engine = Engine;
            component.Entity = this;
            return (T)component;
        }

        public T GetComponent<T>() where T : EntityComponent
        {
            var type = typeof(T);
            return (T)Components[type];
        }

        public void Awake()
        {
            foreach (var component in Components.Values)
            {
                component.Awake();
            }
        }

        public void Update()
        {
            foreach(var component in Components.Values)
            {
                component.EarlyUpdate();
                component.Update();
                component.LateUpdate();
            }
        }
    }


    // Base ECS Engine class
    public class ECSEngine
    {
        private List<GameEntity> entities = new List<GameEntity>();
        private Dictionary<Type, List<GameEntity>> entitiesByComponentType = new Dictionary<Type, List<GameEntity>>();
        public float DeltaTime { get; set; }

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



    // Mesh component to hold the loaded mesh data
    public class MeshComponent : EntityComponent
    {
        public Scene Scene { get; set; }
        public Color MeshColor { get; set; } = Color.White;

        public MeshComponent(Scene scene)
        {
            Scene = scene;
        }

        private static void CheckGLError(string operation)
        {
            ErrorCode errorCode = GL.GetError();
            if (errorCode != ErrorCode.NoError)
            {
                throw new Exception($"OpenGL error ({errorCode}) occurred during {operation}");
            }
        }

        public void Draw(CameraComponent cam, Matrix4 mvpMatrix, Matrix4 modelMatrix, Vector3 lightDirection)
        {
            int shaderProgram = CreateAndLinkShaderProgram("./vert.shader", "./frag.shader");
            CheckGLError("GL.CompileShader");
            GL.UseProgram(shaderProgram);

            int mvpLocation = GL.GetUniformLocation(shaderProgram, "mvpMatrix");
            GL.UniformMatrix4(mvpLocation, false, ref mvpMatrix);

            int modelMatrixLocation = GL.GetUniformLocation(shaderProgram, "modelMatrix");
            GL.UniformMatrix4(modelMatrixLocation, false, ref modelMatrix);

            // Calculate the normal matrix
            Matrix3 normalMatrix = new Matrix3(Matrix4.Transpose(Matrix4.Invert(modelMatrix)));
            int normalMatrixLocation = GL.GetUniformLocation(shaderProgram, "normalMatrix");
            GL.UniformMatrix3(normalMatrixLocation, false, ref normalMatrix);

            int lightDirectionLocation = GL.GetUniformLocation(shaderProgram, "lightDirection");
            GL.Uniform3(lightDirectionLocation, ref lightDirection);

            int cameraPosLocation = GL.GetUniformLocation(shaderProgram, "cameraPos");
            GL.Uniform3(cameraPosLocation, cam.Position);

            int lightColorLocation = GL.GetUniformLocation(shaderProgram, "lightColor");
            GL.Uniform3(lightColorLocation, new Vector3(1.0f, 1.0f, 1.0f)); // White light color

            int objectColorLocation = GL.GetUniformLocation(shaderProgram, "objectColor");
            GL.Uniform3(objectColorLocation, new Vector3(0.7f, 0.7f, 1.0f)); // Light blue object color

            List<float> vertices, normals;
            List<int> indices;
            LoadModel(Scene, out vertices, out indices, out normals);

            int vao = SetupVAO(vertices, indices, normals);
            GL.BindVertexArray(vao);
            GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
            CheckGLError("GL.DrawElements");
            GL.BindVertexArray(0);

            GL.DeleteProgram(shaderProgram);
        }

        private void LoadModel(Scene scene, out List<float> vertices, out List<int> indices, out List<float> normals)
        {
            var mesh = scene.Meshes[0]; // Assuming there's only one mesh in the scene

            vertices = mesh.Vertices.SelectMany(v => new float[] { v.X, v.Y, v.Z }).ToList();
            indices = mesh.GetIndices().ToList();
            normals = mesh.Normals.SelectMany(n => new float[] { n.X, n.Y, n.Z }).ToList();
        }

        private int SetupVAO(List<float> vertices, List<int> indices, List<float> normals)
        {
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);

            int vboVertices = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboVertices);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Count * sizeof(float), vertices.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            int vboNormals = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboNormals);
            GL.BufferData(BufferTarget.ArrayBuffer, normals.Count * sizeof(float), normals.ToArray(), BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);  // Use index 1 for normals

            int ebo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Count * sizeof(int), indices.ToArray(), BufferUsageHint.StaticDraw);

            GL.BindVertexArray(0);
            return vao;
        }



        private int CreateAndLinkShaderProgram(string vertexShaderPath, string fragmentShaderPath)
        {
            string vertexShaderSource = File.ReadAllText(vertexShaderPath);
            string fragmentShaderSource = File.ReadAllText(fragmentShaderPath);
            Console.WriteLine("Vertex Shader Code:\n" + vertexShaderSource);
            Console.WriteLine("Fragment Shader Code:\n" + fragmentShaderSource);
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

            Console.WriteLine("Vertex Shader Log:\n" + vertexShaderInfoLog);
            Console.WriteLine("Fragment Shader Log:\n" + fragmentShaderInfoLog);
            Console.WriteLine("Program Log:\n" + programInfoLog);
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

            Scene scene = importer.ImportFile(filePath, ppSteps);
            if (scene == null)
                return null;

            MeshComponent model = new MeshComponent(scene);
            return model;
        }
    }


    // Mesh data representation





    public class MeshRenderer : EntityComponent
    {
        public Color MeshColor { get; set; } = Color.White;

        public void Render(CameraComponent cam,Matrix4 viewMatrix ,Matrix4 projectionMatrix ,List<System.Numerics.Vector3> lightDirections)
        {

            var meshComponent = Entity.GetComponent<MeshComponent>();
            var transformComponent = Entity.GetComponent<TransformComponent>();

            Matrix4 worldMatrix = transformComponent.GetModelMatrix();
            Matrix4 mvpMatrix = worldMatrix * viewMatrix * projectionMatrix;
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref mvpMatrix);

            meshComponent.Draw(cam,mvpMatrix,worldMatrix, new Vector3(100,100,0));
        }
    }

    public class CameraComponent : EntityComponent
    {
        // Camera properties
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; }
        public float FieldOfView { get; set; }
        public float AspectRatio { get; set; }
        public float NearPlane { get; set; }
        public float FarPlane { get; set; }
        public float MoveSpeed { get; set; } = 0.1f;

        private float pitch = 0; // Vertical rotation angle
        private float yaw = 0;   // Horizontal rotation angle

        // Camera rotation speed
        public float RotationSpeed { get; set; } = 0.000001f;

        // Constructor

        // Calculate the view matrix
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Target, Up);
        }

        // Calculate the projection matrix
        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlane, FarPlane);
        }

        public void MoveCameraWASD(Keys key)
        {
            var cameraForward = Vector3.Normalize(Target - Position);
            var cameraRight = Vector3.Normalize(Vector3.Cross(Up, cameraForward));

            switch (key)
            {
                case Keys.W:
                case Keys.Up:
                    Position += cameraForward * MoveSpeed;
                    Target += cameraForward * MoveSpeed; // Move target as well
                    break;

                case Keys.A:
                case Keys.Left:
                    Position -= cameraRight * MoveSpeed;
                    Target -= cameraRight * MoveSpeed; // Move target as well
                    break;

                case Keys.D:
                case Keys.Right:
                    Position += cameraRight * MoveSpeed;
                    Target += cameraRight * MoveSpeed; // Move target as well
                    break;

                case Keys.S:
                case Keys.Down:
                    Position -= cameraForward * MoveSpeed;
                    Target -= cameraForward * MoveSpeed; // Move target as well
                    break;
            }
        }

        // Function to rotate the camera using mouse input
        public void RotateCameraMouse(float deltaX, float deltaY)
        {
            yaw += deltaX * RotationSpeed;
            pitch += deltaY * RotationSpeed;

            pitch = MathHelper.Clamp(pitch, -MathHelper.PiOver2, MathHelper.PiOver2);

            // Create rotation quaternions for yaw and pitch
            Quaternion yawRotation = Quaternion.FromAxisAngle(Vector3.UnitY, yaw);
            Quaternion pitchRotation = Quaternion.FromAxisAngle(Vector3.UnitX, pitch);

            // Combine the rotations
            Quaternion combinedRotation = pitchRotation * yawRotation;

            // Rotate the camera position and up vector
            Vector3 rotatedCameraPosition = Vector3.Transform(Position - Target, combinedRotation);
            Position = Target + rotatedCameraPosition;
            Up = Vector3.Transform(Vector3.UnitY, combinedRotation);
        }
    }


    public class TransformComponent : EntityComponent
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;

        // Helper methods to manipulate the transform
        public void Translate(Vector3 translation)
        {
            Position += translation;
        }

        public void Rotate(Quaternion rotation)
        {
            Rotation += rotation;
        }

        public void ScaleBy(Vector3 scale)
        {
            Scale *= scale;
        }

        // Convenience methods to get transformation matrices
        public Matrix4 GetModelMatrix()
        {
            Matrix4 translationMatrix = Matrix4.CreateTranslation(Position);
            Matrix4 rotationMatrix = Matrix4.CreateFromQuaternion(Rotation);
            Matrix4 scaleMatrix = Matrix4.CreateScale(Scale);

            return translationMatrix * rotationMatrix * scaleMatrix;

        }
    }


}
