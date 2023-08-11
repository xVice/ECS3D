using System;
using System.Windows.Forms;
using Vector3 = OpenTK.Vector3;
using ECS3D.ECSEngine;
using ECS3D.ECSEngine.Components;
using System.Collections.Generic;
using System.Linq;

namespace ECS3D
{
    public partial class OpenGLForm : Form
    {
        private Engine ecsEngine;

        public OpenGLForm()
        {
            InitializeComponent();
        }
        private void OpenGLForm_Load(object sender, EventArgs e)
        {
            ecsEngine = new Engine(glControl1);
            ecsEngine.Create3DObj("./teapot.obj", new Vector3(0, 0f, 0f));
            ecsEngine.SetActiveCamera(ecsEngine.CreateCamera("Camera1", new Vector3(0, 0, -15)));
            ecsEngine.Awake();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            ecsEngine.RenderFrame();
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            if(ecsEngine != null)
            {
                ecsEngine.FixAspect();
            }
        }


        private void glControl1_KeyDown(object sender, KeyEventArgs e)
        {
            ecsEngine.MoveCam(e.KeyCode);
        }

        private void glControl1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            ecsEngine.RotateCam(e);
        }

    }
}
