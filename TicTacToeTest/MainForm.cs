using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace TicTacToeTest
{
    public partial class MainWindow : Form
    {
        public delegate void SafePropDelegate(Control ctrl, string PropName, object PropValue);
        public GameHandler CurrentGame;
        List<Button> GridControls;
    
        public MainWindow()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Text = "Tic Tac Toe";
        }

        private void SetControlPropSafe(Control ctrl, string PropName, object PropValue)
        {
            if (ctrl.InvokeRequired)
            {
                var d = new SafePropDelegate(SetControlPropSafe);

                Invoke(d, new object[] { ctrl, PropName, PropValue });
            }
            else
                ctrl.GetType().GetProperty(PropName).SetValue(ctrl, PropValue);
        }

        public void Mark(Object sender, EventArgs e)
        {
            Button callerButton = (sender as Button);
            callerButton.Text = CurrentGame.getMove();
            callerButton.Enabled = false;
            CurrentGame.updateGrid(GridControls.IndexOf(callerButton));
            CurrentGame.switchTurn();
        }

        private void setupGame(int gSize, bool bMult, bool bServer)
        {
            CurrentGame = new GameHandler(gSize, bMult, bServer);
            if (bMult)
            {
                CurrentGame.OnDataReceivedEvent += RedrawField;
                CurrentGame.OnDataReceivedEvent += UnfreezeUI;
                CurrentGame.OnDataSentEvent += FreezeUI;

                if (!CurrentGame.getIdentity().Equals("X")) //player does not get the first move
                    CurrentGame.GameStartEvent += FreezeUI;
            }
            CurrentGame.GameOverEvent += FreezeUI;

            if (GridControls != null)
                GridControls.ForEach(gControl => gControl.Hide());

            GridControls = new List<Button>();

            for (int i = 0; i < gSize; i++)
            {
                for (int j = 0; j < gSize; j++)
                {
                    Button nextButton = new Button()
                    {
                        Height = 100,
                        Width = 100,
                        Margin = new Padding(0),
                        Padding = new Padding(0)
                    };
                    nextButton.Location = new Point(nextButton.Width * j, nextButton.Height * i);
                    nextButton.Font = new Font("Arial", 32, FontStyle.Bold);
                    nextButton.Click += Mark;
                    GridControls.Add(nextButton);
                    GridPanel.Controls.Add(nextButton);
                }
            }

            if (bMult)
            {
                FreezeUI();
                if (!bServer)
                {
                    ConnectToGame connectionMenu = new ConnectToGame(this);
                    connectionMenu.ShowDialog(this);
                }
                else
                {
                    toolStripStatusLabel1.Text = "Waiting for players to join...";
                    Task.Run(() => { //THIS WORKS
                        CurrentGame.client = CurrentGame.listener.AcceptTcpClient();
                    }).ContinueWith(delegate {
                                                UnfreezeUI();
                                                toolStripStatusLabel1.Text = "Game in progress"; },
                    TaskScheduler.FromCurrentSynchronizationContext());

                    //THIS DOESN'T
                    //FIND OUT WHY
                    /*Task t = new Task(() => CurrentGame.client = CurrentGame.listener.AcceptTcpClient());
                    t.ContinueWith(delegate { UnfreezeUI(); } , TaskScheduler.FromCurrentSynchronizationContext()); */
                }
            }
        }

        private void RedrawField()
        {
            //MessageBox.Show("Redrawing field");
            if (GridControls != null)
            {
                foreach (Button gControl in GridControls)
                {
                    //fetch i and j indices corresponding to index of this Button
                    int gcIndex = GridControls.IndexOf(gControl);
                    int RemarkRow = (int)Math.Ceiling((double)(gcIndex + 1) / CurrentGame.CurrentGameInfo.gridLength);
                    int RemarkColumn = (gcIndex + 1) - ((RemarkRow - 1) * CurrentGame.CurrentGameInfo.gridLength);

                    //decide the text to put into field
                    if (CurrentGame.CurrentGameInfo.grid[RemarkRow - 1, RemarkColumn - 1].Equals(1))
                        SetControlPropSafe(gControl, "Text", "X"); //SetControlTextSafe(gControl, "X");//gControl.Text = "X";
                    else if (CurrentGame.CurrentGameInfo.grid[RemarkRow - 1, RemarkColumn - 1].Equals(-1))
                        SetControlPropSafe(gControl, "Text", "O"); //SetControlTextSafe(gControl, "O");//gControl.Text = "O";
                    else
                        SetControlPropSafe(gControl, "Text", String.Empty); //SetControlTextSafe(gControl, String.Empty);// gControl.Text = String.Empty;
                }
            }
        }

        private void UnfreezeUI()
        {
            if (GridControls != null)
            {
                var unmarked = GridControls.Where(gControl => gControl.Text == string.Empty);
                foreach (Button gControl in unmarked)
                {                   
                    SetControlPropSafe(gControl, "Enabled", true);//gControl.Enabled = true;
                }
            }  
        }

        private void FreezeUI()
        {
            if (GridControls != null)
                GridControls.ForEach(gControl => gControl.Enabled = false);
        }

        private void newGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setupGame(3, false, false);
        }

        private void hostGameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setupGame(3, true, true);
        }

        private void connectToServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setupGame(3, true, false);
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {
        }
    }

    //Game data container that can be serialized and sent across network
    [Serializable()]
    public class GameInfo : ISerializable
    {
        public string currentMove { get; set; }
        public int turnNumber { get; set; }
        public int gridLength { get; set; }
        public int[,] grid { get; set; }

        public GameInfo(string startMove, int turnNo, int gridLen)
        {
            currentMove = startMove;
            turnNumber = turnNo;
            grid = new int[gridLen, gridLen];
            gridLength = gridLen;
        }

        //sync game state with another GameInfo object, e.g. imported from network stream
        public void ImportData(GameInfo GI)
        {
            currentMove = GI.currentMove;
            turnNumber = GI.turnNumber;
            
            for (int i = 0; i < gridLength; i++)
            {
                for (int j = 0; j < gridLength; j++)
                    grid[i,j] = GI.grid[i,j];
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("CurrentMove", currentMove);
            info.AddValue("TurnNumber", turnNumber);
            info.AddValue("Grid", grid);
            info.AddValue("GridLength", gridLength);
        }

        public GameInfo(SerializationInfo info, StreamingContext context)
        {
            currentMove = (string)info.GetValue("CurrentMove", typeof(string));
            turnNumber = (int)info.GetValue("TurnNumber", typeof(int));
            grid = (int[,])info.GetValue("Grid", typeof(int[,]));
            gridLength = (int)info.GetValue("GridLength", typeof(int));
        }
    }

    //Game logic container
    public class GameHandler
    {
        //general stuff
        public GameInfo CurrentGameInfo;
        private bool bMultiplayer;

        public delegate void GameStart();
        public delegate void GameOver();
        public delegate void OnDataSent();
        public delegate void OnDataReceived();

        public event GameStart GameStartEvent;
        public event GameOver GameOverEvent;
        public event OnDataSent OnDataSentEvent;
        public event OnDataReceived OnDataReceivedEvent;

        //Multiplayer stuff
        public TcpClient client;
        public TcpListener listener;
        public BinaryFormatter bf;
        public string Identity; //who we are playing as (X/O)

        public GameHandler(int gridSize, bool bMulti, bool bServer)
        {
            CurrentGameInfo = new GameInfo("X", 0, gridSize);
            bMultiplayer = bMulti;

            if (bMultiplayer)
            {
                bf = new BinaryFormatter();
                client = new TcpClient();

                if (bServer)
                {
                    Identity = "X";
                    listener = new TcpListener(IPAddress.Any, 5555);
                    listener.Start();
                }
                else
                {
                    Identity = "O"; //by default, assume all host players as X and all connecting players as O
                }
            }
        }

        public void StartGame()
        {
            if (GameStartEvent != null)
                GameStartEvent();

            if (getIdentity().Equals("O")) //Player 2 immediately goes to wait
                WaitForOtherPlayer();
        }

        //TODO clean up encapsulation of properties and stuff
        public string getIdentity()
        {
            return Identity;
        }

        public string getMove()
        {
            return CurrentGameInfo.currentMove;
        }

        public void setTurn(string turn)
        {
            CurrentGameInfo.currentMove = turn;
        }

        public void switchTurn()
        {
            if (getMove().Equals("X"))
                setTurn("O");
            else
            {
                CurrentGameInfo.turnNumber++;
                setTurn("X");
            }

            if (bMultiplayer)
            {
                bf.Serialize(client.GetStream(), CurrentGameInfo); //send data to other party
                if (OnDataSentEvent != null)
                    OnDataSentEvent();

                Task.Run(() => WaitForOtherPlayer());
                //WaitForOtherPlayer();
            }

            evalGame();
        }

        public void WaitForOtherPlayer()
        {
            //MessageBox.Show($" {getIdentity()} just deserialized {Obj.ToString()}!");
            CurrentGameInfo.ImportData((GameInfo)bf.Deserialize(client.GetStream()));
            if (OnDataReceivedEvent != null)
                OnDataReceivedEvent();
        }

        public void updateGrid(int controlIndex) //for internal representation of game
        {
            int markRow = (int) Math.Ceiling((double) (controlIndex + 1) / CurrentGameInfo.gridLength);
            int markColumn = (controlIndex + 1) - ((markRow - 1) * CurrentGameInfo.gridLength);

            if (getMove().Equals("X"))
                CurrentGameInfo.grid[markRow - 1, markColumn - 1] = 1;
            else
                CurrentGameInfo.grid[markRow - 1, markColumn - 1] = -1;       
        }

        public void evalGame() //for determining if any player has a winning combination
        {
            checkRowMatch();
            checkColumnMatch();
            checkDiagonalMatch();
        }

        private void checkRowMatch()
        {
            for (int i = 0; i < CurrentGameInfo.gridLength; i++)
            {
                int checkSum = 0;

                for (int j = 0; j < CurrentGameInfo.gridLength; j++)
                    checkSum += CurrentGameInfo.grid[i, j];

                if (checkSum == CurrentGameInfo.gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();

                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == CurrentGameInfo.gridLength * -1)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();

                    MessageBox.Show("O is a winner!");
                }
            }
        }

        private void checkColumnMatch()
        {
            for (int i = 0; i < CurrentGameInfo.gridLength; i++)
            {
                int checkSum = 0;

                for (int j = 0; j < CurrentGameInfo.gridLength; j++)
                    checkSum += CurrentGameInfo.grid[j, i];

                if (checkSum == CurrentGameInfo.gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == CurrentGameInfo.gridLength * -1)
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

            for (int i = 0; i < CurrentGameInfo.gridLength; i++)
            {
                checkSum += CurrentGameInfo.grid[i, i];

                if (checkSum == CurrentGameInfo.gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == CurrentGameInfo.gridLength * -1)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("O is a winner!");
                }
            }

            checkSum = 0;

            for (int i = 0; i < CurrentGameInfo.gridLength; i++)
            {
                checkSum += CurrentGameInfo.grid[i, CurrentGameInfo.gridLength - (i+1)];

                if (checkSum == CurrentGameInfo.gridLength)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("X is a winner!");
                }
                else if (checkSum == CurrentGameInfo.gridLength * -1)
                {
                    if (GameOverEvent != null)
                        GameOverEvent();
                    MessageBox.Show("O is a winner!");
                }
            }
        }       
    }
}
