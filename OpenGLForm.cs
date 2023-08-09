using Assimp.Configs;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Quaternion = OpenTK.Quaternion;
using Vector3 = OpenTK.Vector3;

namespace ECS3D
{
    public partial class OpenGLForm : Form
    {
        private ECSEngine ecsEngine;

        private GameEntity cameraEntity; //Cameraentity
        private GameEntity ent1; //"teapot" obj

        private CameraComponent camera; //camera component attached to the cameraentity above, for ease use, could just do -> cameraEntity.GetComponent<CameraComponent>() but that makes lookup times slower!


        float currentTime;
        float previousTime;

        public OpenGLForm()
        {
            InitializeComponent();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            glControl1.MakeCurrent();


            var viewMatrix = camera.GetViewMatrix();
            var projectionMatrix = camera.GetProjectionMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Viewport(0, 0, glControl1.ClientSize.Width, glControl1.ClientSize.Height);
            GL.LoadMatrix(ref viewMatrix);
            GL.LoadMatrix(ref projectionMatrix);

            float currentTime = GetCurrentTimeInSeconds();
            float deltaTime = currentTime - previousTime;

            // Ensure previousTime is valid before using it to calculate delta time
            if (previousTime != 0) // You can also add a check for deltaTime > 0 if needed
            {
                ecsEngine.DeltaTime = deltaTime;
            }

            // Clear OpenGL framebuffer



            // Render the mesh
            foreach (var renderer in ecsEngine.GetComponents<MeshRenderer>())
            {
  
                renderer.Render(camera, viewMatrix, projectionMatrix, new List<System.Numerics.Vector3> { new System.Numerics.Vector3(0, 0, 0) });
            }
            ecsEngine.Update();
            // Finally, swap the back and front buffers to display the rendered image
            glControl1.SwapBuffers();

            previousTime = currentTime;
            //glControl1.Refresh();
        }

        private float GetCurrentTimeInSeconds()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
        }

        public GameEntity SetupCam()
        {
            float fieldOfViewDegrees = 60f;
            float fieldOfViewRadians = (float)fieldOfViewDegrees * (float)(Math.PI / 180f);
            cameraEntity = ecsEngine.CreateEntity("Camera1");
            camera = new CameraComponent
            {
                Position = new Vector3(0f, 0f, -200f),
                Target = Vector3.Zero,
                Up = Vector3.UnitY,
                FieldOfView = fieldOfViewRadians,
                AspectRatio = ClientSize.Width / ClientSize.Height,
                NearPlane = 1f,
                FarPlane = 1000f
            };
            cameraEntity.AddComponent<CameraComponent>(camera);
            return cameraEntity;
        }

        public GameEntity CreateObj(string filepath, Vector3 pos)
        {
            var ent = ecsEngine.CreateEntity(Guid.NewGuid().ToString());
            ent.AddComponent<MeshComponent>(MeshComponent.LoadFromFile(filepath, Assimp.PostProcessSteps.Triangulate, new NormalSmoothingAngleConfig(60f)));

            var transform = new TransformComponent();
            transform.Position = pos; // Set the initial position
            transform.Rotation = new Quaternion(new Vector3(0,0,0), 1f); // Set the initial rotation
            transform.Scale = new Vector3(0.1f, 0.1f, 0.1f); // Set the initial scale
            ent.AddComponent<TransformComponent>(transform);

            var meshRenderer = new MeshRenderer();
            meshRenderer.Engine = ecsEngine;
            meshRenderer.Entity = ent;
            ent.AddComponent<MeshRenderer>(meshRenderer);
            return ent;

        }

        private void OpenGLForm_Load(object sender, EventArgs e)
        {
            ecsEngine = new ECSEngine();

            ent1 = CreateObj("./teapot.obj", new Vector3(0, 0, 15f));


            camera = SetupCam().GetComponent<CameraComponent>();

            ecsEngine.Awake();
        }
    }
}
