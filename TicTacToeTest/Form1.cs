using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TicTacToeTest
{
    public partial class MainWindow : Form
    {
        GameHandler CurrentGame;
        List<Button> GridControls;
    
        public MainWindow()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        public void Mark(Object sender, EventArgs e)
        {
            Button callerButton = (sender as Button);
            callerButton.Text = CurrentGame.getTurn();
            callerButton.Font = new Font("Arial", 32, FontStyle.Bold);
            callerButton.Enabled = false;
            CurrentGame.updateGrid(GridControls.IndexOf(callerButton));
            CurrentGame.switchTurn();
        }

        private void GridPanel_Paint(object sender, PaintEventArgs e)
        {

        }

        private void NewGameButton_Click(object sender, EventArgs e)
        {
            setupGame();
        }

        private void setupGame()
        {
            int gridSize = 3;

            CurrentGame = new GameHandler(gridSize);
            CurrentGame.GameOverEvent += FreezeUI;
            if (GridControls != null)
                GridControls.ForEach(gControl => gControl.Hide());

            GridControls = new List<Button>();

            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    Button nextButton = new Button()
                    {
                        Height = 100,
                        Width = 100,
                        Margin = new Padding(0),
                        Padding = new Padding(0)
                    };
                    nextButton.Location = new Point(nextButton.Width * j, nextButton.Height * i);
                    nextButton.Click += Mark;
                    GridControls.Add(nextButton);
                    GridPanel.Controls.Add(nextButton);
                }
            }
        }

        private void FreezeUI()
        {
            if (GridControls != null)
                GridControls.ForEach(gControl => gControl.Enabled = false);
        }

        private void menuToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setupGame();
        }
    }

    public class GameHandler
    {
        private static string currentTurn;
        private static int turnCount;
        private static int[,] grid;
        private static int gridLength;

        public delegate void GameOver();
        public event GameOver GameOverEvent;

        public GameHandler(int gridSize)
        {
            gridLength = gridSize;
            grid = new int[gridLength, gridLength];
            currentTurn = "X";
        }

        public string getTurn()
        {
            return currentTurn;
        }

        public void switchTurn()
        {
            if (currentTurn == "X")
                currentTurn = "O";
            else
            {
                turnCount++;
                currentTurn = "X";
            }
            evalGame();
        }

        public void updateGrid(int controlIndex) //for internal representation of game
        {
            int markRow = (int) Math.Ceiling((double) (controlIndex + 1) / gridLength);
            int markColumn = (controlIndex + 1) - ((markRow - 1) * gridLength);

            if (getTurn() == "X")
                grid[markRow - 1, markColumn - 1] = 1;
            else
                grid[markRow - 1, markColumn - 1] = -1;       
        }

        public void evalGame() //for determining if any player has a winning combination
        {
            checkRowMatch();
            checkColumnMatch();
            checkDiagonalMatch();
        }

        private void checkRowMatch()
        {
            for (int i = 0; i < gridLength; i++)
            {
                int checkSum = 0;

                for (int j = 0; j < gridLength; j++)
                    checkSum += grid[i, j];

                if (checkSum == gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == gridLength * -1)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();

                    MessageBox.Show("O is a winner!");
                }
            }
        }

        private void checkColumnMatch()
        {
            for (int i = 0; i < gridLength; i++)
            {
                int checkSum = 0;

                for (int j = 0; j < gridLength; j++)
                    checkSum += grid[j, i];

                if (checkSum == gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == gridLength * -1)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("O is a winner!");
                }
            }
        }

        private void checkDiagonalMatch()
        {
            int checkSum = 0;

            for (int i = 0; i < gridLength; i++)
            {
                checkSum += grid[i, i];

                if (checkSum == gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == gridLength * -1)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("O is a winner!");
                }
            }

            checkSum = 0;

            for (int i = 0; i < gridLength; i++)
            {
                checkSum += grid[i, gridLength - (i+1)];

                if (checkSum == gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == gridLength * -1)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("O is a winner!");
                }
            }
        }       
    }
}
