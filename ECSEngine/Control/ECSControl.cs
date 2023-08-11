using OpenTK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ECS3D.ECSEngine.Control
{
    public partial class ECSControl : UserControl
    {
        public Engine Engine { get; set; }

        public ECSControl()
        {
            InitializeComponent();
        }

        private void ECSControl_Load(object sender, EventArgs e)
        {
            Engine = new Engine(glContext);
            Engine.Awake();
        }

        private void glContext_Paint(object sender, PaintEventArgs e)
        {
            Engine.RenderFrame();
        }

        private void glContext_KeyDown(object sender, KeyEventArgs e)
        {
            Engine.MoveCam(e.KeyCode);
        }

        private void glContext_MouseMove(object sender, MouseEventArgs e)
        {
            Engine.RotateCam(e);
        }

        private void ECSControl_Resize(object sender, EventArgs e)
        {
            if(Engine != null)
            {
                Engine.FixAspect();
            }
        }


    }
}
