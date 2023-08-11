using ECS3D.ECSEngine.Internal;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Drawing;

namespace ECS3D.ECSEngine.Components
{
    public class MeshRenderer : EntityComponent
    {
        public Color MeshColor { get; set; } = Color.White;

        private MeshComponent meshComponent;
        private TransformComponent transformComponent;

        public Dictionary<Assimp.Mesh, int> AssimpShaderCache = new Dictionary<Assimp.Mesh, int>();
        public Dictionary<glTFLoader.Schema.Material, int> GltfShaderCache = new Dictionary<glTFLoader.Schema.Material, int>();


        public override void Awake()
        {

            meshComponent = Entity.GetComponent<MeshComponent>();
            transformComponent = Entity.GetComponent<TransformComponent>();

        }

        public void Render(CameraComponent cam, Matrix4 viewMatrix, Matrix4 projectionMatrix, List<System.Numerics.Vector3> lightDirections)
        {


            Matrix4 worldMatrix = transformComponent.GetModelMatrix();
            Matrix4 mvpMatrix = worldMatrix * viewMatrix * projectionMatrix;
            GL.MatrixMode(OpenTK.Graphics.OpenGL.MatrixMode.Modelview);
            GL.LoadMatrix(ref mvpMatrix);
            if (meshComponent.Enabled)
            {
                meshComponent.Draw(cam, mvpMatrix, worldMatrix, cam.position);
            }

        }
    }
}
