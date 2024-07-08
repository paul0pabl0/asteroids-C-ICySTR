using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace juego
{
    public partial class MainMenuForm : Form
    {
        public MainMenuForm()
        {
            InitializeComponent();
            this.Text = "Menú Principal";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Size = new Size(300, 250);

            // Botón Iniciar Juego
            Button startButton = new Button();
            startButton.Text = "Iniciar Juego";
            startButton.Location = new Point(100, 50);
            startButton.Click += StartButton_Click;
            this.Controls.Add(startButton);

            // Botón Instrucciones
            Button instructionsButton = new Button();
            instructionsButton.Text = "Instrucciones";
            instructionsButton.Location = new Point(100, 100);
            instructionsButton.Click += InstructionsButton_Click;
            this.Controls.Add(instructionsButton);

            // Botón Salir
            Button exitButton = new Button();
            exitButton.Text = "Salir";
            exitButton.Location = new Point(100, 150);
            exitButton.Click += ExitButton_Click;
            this.Controls.Add(exitButton);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            MainForm gameForm = new MainForm();
            gameForm.Show();
            this.Hide();
        }

        private void InstructionsButton_Click(object sender, EventArgs e)
        {
            // Mostrar un mensaje con las instrucciones
            MessageBox.Show("Instrucciones del juego:\n- Usa las flechas para mover la nave.\n- Presiona Espacio para disparar.\n- Evita los asteroides.", "Instrucciones");
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            // Salir del juego
            Application.Exit();
        }
    }
}
