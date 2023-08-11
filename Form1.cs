using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ECS3D
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ecsControl1.Engine.Create3DObj("./teapot.obj", new OpenTK.Vector3(0, 0, 0));
            ecsControl1.Engine.SetActiveCamera(ecsControl1.Engine.CreateCamera("Cam1", new OpenTK.Vector3(0, 0, -15)));

            BuildDemoShit();

        }

        private void BuildDemoShit()
        {
            // Create a DataTable to store the data
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Entity");
            dataTable.Columns.Add("Component");
            dataTable.Columns.Add("Property");
            dataTable.Columns.Add("Value");

            foreach (var entity in ecsControl1.Engine.GetEntities())
            {
                foreach (var component in entity.Components)
                {
                    foreach (var property in component.Value.GetType().GetProperties())
                    {
                        string propertyName = property.Name;
                        string propertyValue = property.GetValue(component.Value)?.ToString();
                        if (propertyValue != null)
                        {
                            dataTable.Rows.Add(entity.EntityName, component.Key, propertyName, propertyValue);
                        }
                    }
                }
            }

            // Assign the DataTable as the DataGridView's data source
            dataGridView1.DataSource = dataTable;

            // Add a button column for editing
            DataGridViewButtonColumn editColumn = new DataGridViewButtonColumn();
            editColumn.HeaderText = "Edit";
            editColumn.Text = "Edit";
            editColumn.UseColumnTextForButtonValue = true;
            dataGridView1.Columns.Add(editColumn);

            // Handle the cell click event for editing
            dataGridView1.CellClick += DataGridView1_CellClick;

            foreach (var entity in ecsControl1.Engine.GetEntities())
            {
                TreeNode entityNode = new TreeNode($"{entity.EntityName}");

                foreach (var component in entity.Components)
                {
                    TreeNode componentNode = new TreeNode(component.Key.ToString());

                    // Node for properties of the component
                    TreeNode propertiesNode = new TreeNode("Properties");

                    foreach (var property in component.Value.GetType().GetProperties())
                    {
                        string propertyName = property.Name;
                        string propertyValue = property.GetValue(component.Value)?.ToString();
                        if (propertyValue != null)
                        {
                            TreeNode propertyNode = new TreeNode($"{propertyName} | {propertyValue}");
                            propertiesNode.Nodes.Add(propertyNode);
                        }
                    }

                    // Node for methods (functions) of the component
                    TreeNode functionsNode = new TreeNode("Functions");

                    foreach (var method in component.Value.GetType().GetMethods())
                    {
                        TreeNode methodNode = new TreeNode(method.Name);
                        functionsNode.Nodes.Add(methodNode);
                    }

                    componentNode.Nodes.Add(propertiesNode);
                    componentNode.Nodes.Add(functionsNode);

                    entityNode.Nodes.Add(componentNode);
                }

                treeView1.Nodes.Add(entityNode);
            }
        }

        private void DataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == dataGridView1.Columns.Count - 1) // Edit column clicked
            {
                if (e.RowIndex >= 0 && e.RowIndex < dataGridView1.RowCount)
                {
                    string entityName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                    string componentName = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
                    string propertyName = dataGridView1.Rows[e.RowIndex].Cells[2].Value.ToString();
                    string propertyValue = dataGridView1.Rows[e.RowIndex].Cells[3].Value.ToString();

 
                }
            }
        }





    }
}
