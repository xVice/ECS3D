using Assimp.Configs;
using ECS3D.ECSEngine.Components;
using ECS3D.ECSEngine.Internal;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        private Point lastMousePos;

        private void CenterMouseCursor()
        {
            Point center = new Point(activeGlControl.Width / 2, activeGlControl.Height / 2);
            Cursor.Position = activeGlControl.PointToScreen(center);
            lastMousePos = center;
        }

        public Engine(GLControl control)
        {
            activeGlControl = control;
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
                activeCamera.AspectRatio = (float)activeGlControl.ClientSize.Width / activeGlControl.ClientSize.Height;
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
            if(activeCamera != null)
            {
                activeGlControl.MakeCurrent();


                var viewMatrix = activeCamera.GetViewMatrix();
                var projectionMatrix = activeCamera.GetProjectionMatrix();
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.CullFace);

                // Set culling mode to back faces
                GL.CullFace(CullFaceMode.Back);
                GL.MatrixMode(MatrixMode.Projection);
                GL.LoadIdentity();
                GL.Viewport(0, 0, activeGlControl.ClientSize.Width, activeGlControl.ClientSize.Height);
                GL.LoadMatrix(ref viewMatrix);
                GL.LoadMatrix(ref projectionMatrix);

                float currentTime = GetCurrentTimeInSeconds();
                float deltaTime = currentTime - previousTime;

                // Ensure previousTime is valid before using it to calculate delta time
                if (previousTime != 0) // You can also add a check for deltaTime > 0 if needed
                {
                    DeltaTime = deltaTime;
                }



                // Clear OpenGL framebuffer
                GL.ClearColor(Color.Gray);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                // Begin occlusion query

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
                // End occlusion query

                Update();
                // Finally, swap the back and front buffers to display the rendered image
                activeGlControl.SwapBuffers();

                previousTime = currentTime;
                //glControl1.Refresh();
            }

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
