using System;
using System.Windows.Forms;

namespace Demo3D
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            AddButtonFor("Cuboid Demo", () => new Form1().ShowDialog());
            AddButtonFor("RubikCube Demo", () => new Form2().ShowDialog());

        }

        private void AddButtonFor(string text, Action action)
        {
            var button = new Button()
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 50
            };
            button.Click += (s, e) => action();
            this.Controls.Add(button);
        }
    }
}
