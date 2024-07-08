using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Timer = System.Windows.Forms.Timer;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Drawing2D;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Collections;

namespace juego
{
    public enum AsteroidSize
    {   //Enum que define los tres posibles tamaños de un asteroide
        Small,
        Medium,
        Large
    }
    public partial class MainForm : Form
    {
        // Timers para controlar el movimiento, colisiones e invulnerabilidad
        private Timer asteroidTimer;
        private Timer playerShipTimer;
        private Timer collisionTimer;
        private Timer invulnerabilityTimer;

        // Listas y variables de juego
        private List<Asteroid> asteroids;
        private List<ScoreRecord> scoreRecords;
        private Random random;
        private PlayerShip playerShip;
        private int score;
        private int lives;

        // Variables de control de teclado y estado del juego
        private bool decelerar = false;
        private bool leftKeyPressed = false;
        private bool rightKeyPressed = false;
        private bool upKeyPressed = false;
        private bool downKeyPressed = false;
        private bool spaceKeyPressed = false;
        private bool gameRunning = true;
        private bool isInvulnerable = false;
        private bool shipVisible = true;
        private bool gamePaused = false;
        private bool canShoot = true;

        // Constantes de juego
        private const int asteroidSpeed = 7;
        private const int initialLives = 5;

        // Nombre del archivo para los records
        private const string scoreRecordsFile = "score_records.txt"; 

        public MainForm()
        {
            InitializeComponent();
            InitializeGame();
            LoadScoreRecords(); // Cargar los records guardados (si los hay)
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;

            // Configurar pantalla completa
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
        }

        // Inicialización del juego al cargar el formulario
        private void InitializeGame()
        {
            asteroids = new List<Asteroid>();
            scoreRecords = new List<ScoreRecord>();
            random = new Random();
  
            InitializePlayerShip();
            InitializeTimers();

            this.KeyDown += MainForm_KeyDown;
            this.KeyUp += MainForm_KeyUp;

            score = 0;
            lives = initialLives;
        }

        // Configuración de los timers del juego
        private void InitializeTimers()
        {
            asteroidTimer = new Timer();
            asteroidTimer.Interval = 16;
            asteroidTimer.Tick += AsteroidTimer_Tick;
            asteroidTimer.Start();

            playerShipTimer = new Timer();
            playerShipTimer.Interval = 16;
            playerShipTimer.Tick += PlayerShipTimer_Tick;
            playerShipTimer.Start();

            collisionTimer = new Timer();
            collisionTimer.Interval = 16;
            collisionTimer.Tick += CollisionTimer_Tick;
            collisionTimer.Start();

            invulnerabilityTimer = new Timer();
            invulnerabilityTimer.Interval = 2000; // Intervalo de invulnerabilidad en milisegundos
            invulnerabilityTimer.Tick += InvulnerabilityTimer_Tick;
        }

        // Timer para mover asteroides y generar nuevos si es necesario
        private void AsteroidTimer_Tick(object sender, EventArgs e)
        {
            if (gamePaused || !gameRunning) return;
            MoveAsteroids();
            GenerateAsteroidIfRequired();
            Invalidate();
        }

        // Timer para mover la nave del jugador y sus proyectiles
        private void PlayerShipTimer_Tick(object sender, EventArgs e)
        {
            if (gamePaused || !gameRunning) return;
            if (decelerar && playerShip.Speed > 0) playerShip.Decelerate();
            MovePlayerShip();
            playerShip.MoveProjectiles();
            Invalidate();
        }

        // Timer para controlar las colisiones
        private void CollisionTimer_Tick(object sender, EventArgs e)
        {
            if (gamePaused || !gameRunning) return;
            // Colisiones entre nave y asteroides
            if (!isInvulnerable && CollisionDetector.CheckCollision(playerShip, asteroids))
            {
                HandleShipCollision();
            }

            // Colisiones entre proyectiles y asteroides
            HandleProjectileCollisions();
            Invalidate();
        }

        // Timer para controlar la invulnerabilidad después de una colisión
        private void InvulnerabilityTimer_Tick(object sender, EventArgs e)
        {
            isInvulnerable = false;
            shipVisible = true; // Mostrar la nave nuevamente
            invulnerabilityTimer.Stop();
            Invalidate(); // Redibujar para mostrar la nave
        }

        // Manejar la colisión de la nave con un asteroide
        private void HandleShipCollision()
        {
            lives--;
            isInvulnerable = true;
            shipVisible = false; // Ocultar la nave durante la invulnerabilidad
            invulnerabilityTimer.Start();
            if (lives <= 0)
            {
                gameRunning = false;
                GameOver();
            }
            else
            {
                PauseTimers();
                gamePaused = true;
                MessageBox.Show($"¡La nave ha colisionado con un asteroide! Vidas restantes: {lives}. Cierra el diálogo para continuar.");
                gamePaused = false;
                ResumeTimers();
                RestartGame(false);
            }
        }

        // Manejar las colisiones de los proyectiles con los asteroides
        private void HandleProjectileCollisions()
        {
            List<Asteroid> hitAsteroids = CollisionDetector.CheckProjectileCollisions(playerShip, asteroids);
            foreach (Asteroid asteroid in hitAsteroids)
            {
                asteroids.Remove(asteroid);
                SplitAsteroid(asteroid);
            }
        }

        // Mostrar la pantalla de Game Over y reiniciar el juego
        private void GameOver()
        {
            gamePaused = true;
            MessageBox.Show($"¡Juego terminado! Puntuación final: {score}. Cierra el diálogo y pulsa ENTER para volver a empezar.");
            gamePaused = false;
            AddScoreRecord("Jugador", score); // Añadir el record al finalizar el juego
            RestartGame(true);
            lives = initialLives;
            asteroids.Clear(); // Limpiar la lista de asteroides al finalizar el juego
        }

        // Reiniciar el juego después de Game Over o al inicio
        private void RestartGame(bool resetScore = false)
        {
            if (asteroidTimer != null) asteroidTimer.Stop();
            if (playerShipTimer != null) playerShipTimer.Stop();
            if (collisionTimer != null) collisionTimer.Stop();

            gameRunning = true;
            InitializePlayerShip();
            InitializeTimers();
            ResetKeyStates();
            if (resetScore) score = 0; // Reiniciar la puntuación solo si se especifica
                                       // No limpiar la lista de asteroides aquí
        }

        // Dividir un asteroide en varios más pequeños al ser destruido por un proyectil
        private void SplitAsteroid(Asteroid asteroid)
        {
            int newAsteroidCount = 2;
            int points = 0;

            switch (asteroid.AsteroidSize)
            {
                case AsteroidSize.Large:
                    Size newSizeLarge = new Size(asteroid.Size.Width / 2, asteroid.Size.Height / 2);
                    AsteroidSize newAsteroidSizeLarge = AsteroidSize.Medium;
                    points = 25; // Puntos para asteroides grandes

                    score += points; 

                    for (int i = 0; i < newAsteroidCount; i++)
                    {
                        double newDirection = random.NextDouble() * Math.PI * 2;
                        asteroids.Add(new Asteroid(asteroid.Position, newSizeLarge, newDirection, newAsteroidSizeLarge));
                    }
                    break;

                case AsteroidSize.Medium:
                    Size newSizeMedium = new Size(asteroid.Size.Width / 2, asteroid.Size.Height / 2);
                    AsteroidSize newAsteroidSizeMedium = AsteroidSize.Small;
                    points = 50; // Puntos para asteroides medianos

                    score += points; 

                    for (int i = 0; i < newAsteroidCount; i++)
                    {
                        double newDirection = random.NextDouble() * Math.PI * 2;
                        asteroids.Add(new Asteroid(asteroid.Position, newSizeMedium, newDirection, newAsteroidSizeMedium));
                    }
                    break;

                case AsteroidSize.Small:
                    points = 100; // Puntos para asteroides pequeños
                    score += points; 

                    // Eliminar el asteroide pequeño de la lista
                    asteroids.Remove(asteroid);
                    break;

                default:
                    return;
            }
        }

        // Inicializar la nave del jugador al inicio del juego
        private void InitializePlayerShip()
        {
            System.Drawing.Image playerShipImage = Properties.Resources.playerShipImage;

            // Obtencion de dimensiones de la pantalla
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // Calcular posición inicial en el centro de la pantalla
            int initialX = screenWidth / 2;
            int initialY = screenHeight / 2;

            playerShip = new PlayerShip(
                initialX,
                initialY,
                playerShipImage,
                0,
                8,
                1,
                new Size(screenWidth, screenHeight), 
                7
            );
        }

        // Evento al presionar una tecla
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) leftKeyPressed = true;
            else if (e.KeyCode == Keys.Right) rightKeyPressed = true;
            else if (e.KeyCode == Keys.Up)
            {
                decelerar = false;
                upKeyPressed = true;
            }
            else if (e.KeyCode == Keys.Down) downKeyPressed = true;
            else if (e.KeyCode == Keys.Space && canShoot)
            {
                canShoot = false;
                playerShip.Shoot();
            }
            else if (e.KeyCode == Keys.Escape) // Salir de pantalla completa
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.WindowState = FormWindowState.Normal;
                this.Size = new Size(1920, 1080); // Tamaño predeterminado
            }
            HandlePlayerControls();
        }

        // Evento al soltar una tecla
        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) leftKeyPressed = false;
            else if (e.KeyCode == Keys.Right) rightKeyPressed = false;
            else if (e.KeyCode == Keys.Up)
            {
                decelerar = true;
                upKeyPressed = false;
            }
            else if (e.KeyCode == Keys.Down) downKeyPressed = false;
            else if (e.KeyCode == Keys.Space) canShoot = true;
            HandlePlayerControls();
        }

        // Manejar los controles del jugador basados en las teclas presionadas
        private void HandlePlayerControls()
        {
            if (leftKeyPressed) playerShip.RotateLeft();
            if (rightKeyPressed) playerShip.RotateRight();
            if (upKeyPressed) playerShip.Accelerate();
            if (downKeyPressed) playerShip.ActivateHyperspace(this.ClientSize);
            if (spaceKeyPressed) playerShip.Shoot();
        }

        // Mover la nave del jugador por la pantalla
        private void MovePlayerShip()
        {
            if (playerShip != null)
            {
                playerShip.Move();
                if (playerShip.X < 0) playerShip.X = ClientSize.Width;
                else if (playerShip.X > ClientSize.Width) playerShip.X = 0;
                if (playerShip.Y < 0) playerShip.Y = ClientSize.Height;
                else if (playerShip.Y > ClientSize.Height) playerShip.Y = 0;
            }
        }

        // Mover todos los asteroides en juego
        private void MoveAsteroids()
        {
            foreach (Asteroid asteroid in asteroids)
            {
                asteroid.Move(asteroidSpeed);
                if (asteroid.Position.X < -asteroid.Size.Width) asteroid.Position = new Point(ClientSize.Width, asteroid.Position.Y);
                else if (asteroid.Position.X > ClientSize.Width) asteroid.Position = new Point(0, asteroid.Position.Y);
                if (asteroid.Position.Y < -asteroid.Size.Height) asteroid.Position = new Point(asteroid.Position.X, ClientSize.Height);
                else if (asteroid.Position.Y > ClientSize.Height) asteroid.Position = new Point(asteroid.Position.X, 0);
            }
        }

        // Generar un nuevo asteroide aleatoriamente si es necesario
        private void GenerateAsteroidIfRequired()
        {
            if (asteroids.Count >= 5 || (random.NextDouble() < 0.1)) // Probabilidad adicional del 10%
            {
                return; // Si ya hay suficientes asteroides o no se cumple la probabilidad adicional, no generar más
            }

            int x = 0, y = 0;
            int position = random.Next(4);

            switch (position)
            {
                case 0:
                    x = random.Next(ClientSize.Width);
                    y = -50;
                    break;
                case 1:
                    x = random.Next(ClientSize.Width);
                    y = ClientSize.Height;
                    break;
                case 2:
                    x = -50;
                    y = random.Next(ClientSize.Height);
                    break;
                case 3:
                    x = ClientSize.Width;
                    y = random.Next(ClientSize.Height);
                    break;
            }

            Size size;
            AsteroidSize asteroidSize = (AsteroidSize)random.Next(3);

            switch (asteroidSize)
            {
                case AsteroidSize.Small:
                    size = new Size(random.Next(20, 40), random.Next(20, 40));
                    break;
                case AsteroidSize.Medium:
                    size = new Size(random.Next(40, 60), random.Next(40, 60));
                    break;
                case AsteroidSize.Large:
                    size = new Size(random.Next(60, 80), random.Next(60, 80));
                    break;
                default:
                    size = new Size(50, 50);
                    break;
            }

            double direction = random.NextDouble() * Math.PI * 2;
            asteroids.Add(new Asteroid(new Point(x, y), size, direction, asteroidSize));
        }

        // Reiniciar el estado de las teclas presionadas
        private void ResetKeyStates()
        {
            leftKeyPressed = false;
            rightKeyPressed = false;
            upKeyPressed = false;
            downKeyPressed = false;
            spaceKeyPressed = false;
        }

        // Pausar todos los timers del juego
        private void PauseTimers()
        {
            asteroidTimer.Stop();
            playerShipTimer.Stop();
            collisionTimer.Stop();
        }

        // Reanudar todos los timers del juego
        private void ResumeTimers()
        {
            asteroidTimer.Start();
            playerShipTimer.Start();
            collisionTimer.Start();
        }

        // Redibujar la pantalla de juego con la nave, asteroides y puntuación
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (gameRunning)
            {
                if (shipVisible) playerShip.Draw(e.Graphics);

                foreach (Asteroid asteroid in asteroids)
                {
                    asteroid.Draw(e.Graphics);
                }
                DrawScoreRecords(e.Graphics);
                e.Graphics.DrawString($"Puntuación: {score}", new System.Drawing.Font("Arial", 18), Brushes.White, new PointF(10, 15));
                e.Graphics.DrawString($"Vidas: {lives}", new System.Drawing.Font("Arial", 18), Brushes.White, new PointF(10, 50));
            }
        }

        // Método para dibujar los registros de puntuaciones en la interfaz gráfica
        private void DrawScoreRecords(Graphics graphics)
        {
            int recordHeight = 30;
            int recordCount = Math.Min(scoreRecords.Count, 5); // Mostrar máximo 5 records
            int startY = 30; // Posición Y inicial

            // Calcular la posición X para que esté en la esquina superior derecha
            int startX = ClientSize.Width - 130; 

            for (int i = 0; i < recordCount; i++)
            {
                ScoreRecord record = scoreRecords[i];
                string text = $"{record.PlayerName}: {record.Score}";

                // Calcular la posición Y para cada récord
                int y = startY + i * recordHeight;

                // Dibujar el texto en la posición calculada
                graphics.DrawString(text, Font, Brushes.White, new PointF(startX, y));
            }
        }

        // Método para añadir un nuevo registro de puntuación
        private void LoadScoreRecords()
        {
            if (File.Exists(scoreRecordsFile))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(scoreRecordsFile))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] parts = line.Split(',');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int score))
                            {
                                scoreRecords.Add(new ScoreRecord(parts[0], score));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar los records: {ex.Message}");
                }
            }
        }

        // Método para guardar los registros de puntuaciones en un archivo
        private void SaveScoreRecords()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(scoreRecordsFile))
                {
                    foreach (ScoreRecord record in scoreRecords)
                    {
                        writer.WriteLine($"{record.PlayerName},{record.Score}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar los records: {ex.Message}");
            }
        }

        // Método para añadir un nuevo registro de puntuación
        public void AddScoreRecord(string playerName, int score)
        {
            scoreRecords.Add(new ScoreRecord(playerName, score));
            scoreRecords.Sort((r1, r2) => r2.Score.CompareTo(r1.Score)); // Ordenar de mayor a menor puntuación

            // Limitar la lista a un máximo de 10 records
            if (scoreRecords.Count > 10)
            {
                scoreRecords.RemoveAt(scoreRecords.Count - 1);
            }

            SaveScoreRecords(); // Guardar los records actualizados en el archivo
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Método para acciones al cargar el formulario
        }
    }

    public class Asteroid
    {
        public Point Position { get; set; }  // Posición del asteroide en la pantalla
        public Size Size { get; set; }        // Tamaño del asteroide
        public double Direction { get; set; } // Dirección de movimiento del asteroide en radianes
        public AsteroidSize AsteroidSize { get; set; } // Tamaño del asteroide (pequeño, mediano, grande)

        public Asteroid(Point position, Size size, double direction, AsteroidSize asteroidSize)
        {
            Position = position;
            Size = size;
            Direction = direction;
            AsteroidSize = asteroidSize;
        }

        // Método para mover el asteroide en función de su dirección y velocidad
        public void Move(int asteroidSpeed)
        {
            double deltaX = Math.Cos(Direction) * asteroidSpeed;
            double deltaY = Math.Sin(Direction) * asteroidSpeed;
            Position = new Point((int)(Position.X + deltaX), (int)(Position.Y + deltaY));
        }

        // Método para dibujar el asteroide en el contexto gráfico especificado
        public void Draw(Graphics g)
        {
            // Definición de la forma del asteroide como un polígono
            Point[] asteroidShape = {
            new Point(Position.X, Position.Y + Size.Height / 2),
            new Point(Position.X + Size.Width / 3, Position.Y),
            new Point(Position.X + Size.Width, Position.Y + Size.Height / 3),
            new Point(Position.X + Size.Width, Position.Y + 2 * Size.Height / 3),
            new Point(Position.X + 2 * Size.Width / 3, Position.Y + Size.Height),
            new Point(Position.X + Size.Width / 2, Position.Y + Size.Height),
            new Point(Position.X, Position.Y + Size.Height)
        };

            // Dibujar el asteroide como un polígono relleno de color negro
            g.FillPolygon(Brushes.Black, asteroidShape);

            // Dibujar el contorno del asteroide en blanco
            g.DrawPolygon(Pens.White, asteroidShape);
        }
    }

    public class PlayerShip
    {
        private Size clientSize;
        public double X { get; set; }
        public double Y { get; set; }
        public System.Drawing.Image ShipImage { get; set; }
        public double Speed { get; set; }
        public double RotationAngle { get; set; }
        public double MaxSpeed { get; set; }
        public double AccelerationRate { get; set; }
        public double RotationSpeed { get; set; }
        public List<Projectile> Projectiles { get; set; }
        public double ProjectileSpeed { get; set; }

        // Constructor para inicializar las propiedades de la nave
        public PlayerShip(double x, double y, System.Drawing.Image shipImage, double speed, double maxSpeed, double accelerationRate, Size clientSize, double projectileSpeed)
        {
            X = x;
            Y = y;
            ShipImage = shipImage;
            Speed = speed;
            MaxSpeed = maxSpeed;
            AccelerationRate = accelerationRate;
            RotationSpeed = 6.0;
            this.clientSize = clientSize;
            ProjectileSpeed = projectileSpeed;
            Projectiles = new List<Projectile>();
        }

        // Método para acelerar la nave
        public void Accelerate()
        {
            if (Speed < MaxSpeed)
            {
                Speed += AccelerationRate;
            }
        }

        // Método para desacelerar la nave
        public void Decelerate()
        {
            if (Speed > 0)
            {
                Speed -= AccelerationRate / 10;
                if (Speed < 0)
                {
                    Speed = 0;
                }
            }
        }

        // Método para rotar la nave a la izquierda
        public void RotateLeft()
        {
            RotationAngle -= RotationSpeed;
            if (RotationAngle < 0)
            {
                RotationAngle += 360;
            }
        }

        // Método para rotar la nave a la derecha
        public void RotateRight()
        {
            RotationAngle += RotationSpeed;
            if (RotationAngle >= 360)
            {
                RotationAngle -= 360;
            }
        }

        // Método para activar el salto de hiperespacio
        public void ActivateHyperspace(Size clientSize)
        {
            Random random = new Random();
            X = random.NextDouble() * clientSize.Width;
            Y = random.NextDouble() * clientSize.Height;
        }

        // Método para mover la nave
        public void Move()
        {
            double radians = Math.PI * RotationAngle / 180;
            double deltaX = Math.Sin(radians) * Speed;
            double deltaY = -Math.Cos(radians) * Speed;

            X += deltaX;
            Y += deltaY;

            if (X < 0) X = clientSize.Width;
            if (X > clientSize.Width) X = 0;
            if (Y < 0) Y = clientSize.Height;
            if (Y > clientSize.Height) Y = 0;

            Speed *= 0.99; // Reducción gradual de la velocidad
        }

        // Método para disparar proyectiles
        public void Shoot()
        {

            double radians = Math.PI * RotationAngle / 180;

            double projectileX = X;
            double projectileY = Y;
            double projectileSpeed = 20;

            Projectiles.Add(new Projectile(projectileX, projectileY, projectileSpeed, RotationAngle));

        }

        // Método para mover los proyectiles
        public void MoveProjectiles()
        {
            foreach (var projectile in Projectiles.ToList())
            {
                projectile.Move();

                // Eliminar proyectiles que salen de los límites de la pantalla
                if (projectile.X < 0 || projectile.X > clientSize.Width || projectile.Y < 0 || projectile.Y > clientSize.Height)
                {
                    Projectiles.Remove(projectile);
                }
            }
        }

        // Método para dibujar la nave y sus proyectiles
        public void Draw(Graphics g)
        {
            int newWidth = ShipImage.Width / 15; // Ajustar el tamaño de la nave
            int newHeight = ShipImage.Height / 13; // Ajustar el tamaño de la nave

            int x = (int)(X - newWidth / 2);
            int y = (int)(Y - newHeight / 2);

            Matrix originalMatrix = g.Transform;

            // Aplicar transformación para rotar la imagen de la nave
            g.TranslateTransform((float)X, (float)Y);
            g.RotateTransform((float)RotationAngle);
            g.TranslateTransform(-(float)X, -(float)Y);

            g.DrawImage(ShipImage, new Rectangle(x, y, newWidth, newHeight));

            // Restaurar la transformación original
            g.Transform = originalMatrix;

            // Dibujar cada proyectil
            foreach (var projectile in Projectiles)
            {
                g.FillEllipse(Brushes.Red, (float)projectile.X, (float)projectile.Y, 5, 5);
            }
        }

        // Método para obtener la ruta gráfica de la nave(usado para detección de colisiones)
        public GraphicsPath GetPath()
        {
            GraphicsPath path = new GraphicsPath();
            int[] xPoints = { (int)X, (int)(X - ShipImage.Width / 24), (int)(X + ShipImage.Width / 24) };
            int[] yPoints = { (int)(Y - ShipImage.Height / 20), (int)(Y + ShipImage.Height / 20), (int)(Y + ShipImage.Height / 20) };
            path.AddPolygon(new Point[] { new Point(xPoints[0], yPoints[0]), new Point(xPoints[1], yPoints[1]), new Point(xPoints[2], yPoints[2]) });
            return path;
        }

        // Método para verificar si un punto está dentro de la nave (usado para detección de colisiones)
        public bool IsPointInShip(Point point)
        {
            using (GraphicsPath path = GetPath())
            {
                return path.IsVisible(point);
            }
        }
    }

    public class CollisionDetector
    {
        // Verifica si la nave del jugador ha colisionado con cualquier asteroide en la lista
        public static bool CheckCollision(PlayerShip playerShip, List<Asteroid> asteroids)
        {
            foreach (Asteroid asteroid in asteroids)
            {
                if (CheckCollision(playerShip.GetPath(), GetAsteroidPath(asteroid)))
                {
                    return true;
                }
            }
            return false;
        }

        // Verifica si los proyectiles disparados por la nave han colisionado con algún asteroide
        public static List<Asteroid> CheckProjectileCollisions(PlayerShip playerShip, List<Asteroid> asteroids)
        {
            List<Asteroid> hitAsteroids = new List<Asteroid>();

            // Itera a través de los proyectiles disparados por la nave
            foreach (var projectile in playerShip.Projectiles.ToList())
            {
                foreach (Asteroid asteroid in asteroids)
                {
                    if (CheckCollision(GetProjectilePath(projectile), GetAsteroidPath(asteroid)))
                    {
                        hitAsteroids.Add(asteroid);
                        playerShip.Projectiles.Remove(projectile);
                        break;
                    }
                }
            }

            return hitAsteroids;
        }

        // Método para verificar la colisión entre dos rutas gráficas
        private static bool CheckCollision(GraphicsPath path1, GraphicsPath path2)
        {
            using (Region region1 = new Region(path1))
            using (Region region2 = new Region(path2))
            {
                region1.Intersect(region2);
                return !region1.IsEmpty(Graphics.FromImage(new Bitmap(1, 1)));
            }
        }

        // Método para obtener la ruta gráfica de un asteroide
        private static GraphicsPath GetAsteroidPath(Asteroid asteroid)
        {
            GraphicsPath asteroidPath = new GraphicsPath();
            asteroidPath.AddPolygon(GetAsteroidPoints(asteroid));
            return asteroidPath;
        }

        // Método para obtener la ruta gráfica de un proyectil
        private static GraphicsPath GetProjectilePath(Projectile projectile)
        {
            GraphicsPath projectilePath = new GraphicsPath();
            projectilePath.AddEllipse((float)projectile.X, (float)projectile.Y, 5, 5);
            return projectilePath;
        }

        // Método para obtener los puntos de un asteroide
        private static Point[] GetAsteroidPoints(Asteroid asteroid)
        {
            return new Point[]
            {
            new Point(asteroid.Position.X, asteroid.Position.Y + asteroid.Size.Height / 2),
            new Point(asteroid.Position.X + asteroid.Size.Width / 3, asteroid.Position.Y),
            new Point(asteroid.Position.X + asteroid.Size.Width, asteroid.Position.Y + asteroid.Size.Height / 3),
            new Point(asteroid.Position.X + asteroid.Size.Width, asteroid.Position.Y + 2 * asteroid.Size.Height / 3),
            new Point(asteroid.Position.X + 2 * asteroid.Size.Width / 3, asteroid.Position.Y + asteroid.Size.Height),
            new Point(asteroid.Position.X + asteroid.Size.Width / 2, asteroid.Position.Y + asteroid.Size.Height),
            new Point(asteroid.Position.X, asteroid.Position.Y + asteroid.Size.Height)
            };
        }
    }

    public class Projectile
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Direction { get; set; }

        // Constructor que inicializa las propiedades del proyectil
        public Projectile(double x, double y, double speed, double direction)
        {
            X = x;
            Y = y;
            Speed = speed;
            Direction = direction;
        }

        // Método para mover el proyectil basado en su velocidad y dirección
        public void Move()
        {
            double radians = Math.PI * Direction / 180;
            double deltaX = Math.Sin(radians) * Speed;
            double deltaY = -Math.Cos(radians) * Speed;

            X += deltaX;
            Y += deltaY;
        }
    }
    public class ScoreRecord
    {
        public string PlayerName { get; set; }
        public int Score { get; set; }

        // Constructor que inicializa las propiedades con los valores proporcionados
        public ScoreRecord(string playerName, int score)
        {
            PlayerName = playerName;
            Score = score;
        }
    }
}