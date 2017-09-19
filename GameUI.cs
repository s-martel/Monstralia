﻿/***************************************************************
 * File: ChessBoard.cs
 * Created By: Syed Ghulam Akbar		Date: 29 June, 2005
 * Description: This class implements the actual chess board UI.
 * This class handles drawing and events handling for the board
 ***************************************************************/
using System;
using System.Text;
using System.Collections;
using System.Windows.Forms;
using ChessLibrary;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Speech;
using System.Text.RegularExpressions;

namespace Chess
{
	/// <summary>
	/// Summary description for ChessBoard.
	/// </summary>
	public class GameUI
	{
        SpeechSynthesizer sSynth = new SpeechSynthesizer();
        PromptBuilder style = new PromptBuilder();
        SpeechRecognitionEngine sr = new SpeechRecognitionEngine();
        Choices SpeechMoves = new Choices(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "A", "B", "C", "D", "E", "F", "G", "H","all"});
        GrammarBuilder gb = new GrammarBuilder();


        private ArrayList Squars;	// Picture control array for storing the place holders
		public Images ChessImages;	// Contains reference of chess images
		private string ResourceFolder;		// Contain the location of resource folder
		private int LogCounter;			// Stores the entries in the log
        String LastMove = " ";
        public Game ChessGame;		    // Back end chess game engine
		public Sounds	Sounds;			// Stores the game sounds
		public string	SelectedSquar;	// Contains name of the selected square
        public string LastSelectedSquar;// The last selected square
        String lastSelected = " ";
		public ChessMain ParentForm;	// Reference of the parent form 
		public bool ShowMoveHelp;		// Show possible move by colors
		public bool IsRunning;			// Return true when the game is running
		public bool IsOver;				// Set to true when the game is over
        public bool ShowComputerThinkingProgres = true;    // Set whether to show the progress of the computer thinking
        public bool LastMoveByClick;    // Stores true if the last move was made by mouse click (instead of drag and drop)

		public GameUI(ChessMain form)
		{
            sSynth.Rate = -2;
            
            this.ParentForm = form; // get and store reference of parent form

            // Load all the chess images in a list
            ChessImages = new Images();
            
            #if DEBUG
                ResourceFolder = "..\\..\\Resources\\";
            #else
                ResourceFolder = "Resources\\";
            #endif

            // For Production Release
            ResourceFolder = "Resources\\";
			ChessImages.LoadImages(ResourceFolder);
			Sounds = new Sounds(ResourceFolder);	// create the sounds object for playing sound
			BuildBoard();
            sSynth.SpeakAsync("Welcome to Audio Chess! ");

            ParentForm.ChessCaptureBar.InitializeBar(ChessImages);	// Initialize chess bar

			// Initialize variables
			ShowMoveHelp = true; // 

        }

		// Builds the chess pieces place holder images controls
		public void BuildBoard()
		{
			Squars = new ArrayList();	// Initialize place holder pictures

			// Now dynamically draw all the chess pieces place holder images
           
			for (int row=1; row<=8; row++)  // repeat\at for every column in the chess board row
                for(int col=1; col<=8; col++)
                {
                    
					Squar ChessSquar = new Squar(row, col, this);
					ChessSquar.SetBackgroundSquar(ChessImages);	// Set the chess square background
					Squars.Add(ChessSquar);
					ParentForm.Controls.Add(ChessSquar);
				}
		}

		// returns board square for the given name
		private Squar GetBoardSquar(string strCellName)
		{
            style.AppendTextWithHint(strCellName, SayAs.SpellOut);  // speak out selected square coordinates
            sSynth.SpeakAsync(style);
            style.ClearContent();
            foreach (Squar ChessSquar in Squars)
			{
				if (ChessSquar.Name == strCellName)
					return ChessSquar;
			}
            return null;
		}

		// Redraw the visible board from the internal chess board
		public void RedrawBoard()
		{
			foreach (Squar ChessSquar in Squars)
			{
				if (ChessSquar.BackgroundImage==null) // if background image doesn't exist
				{
					ChessSquar.SetBackgroundSquar(ChessImages);
				}

				if (ChessGame.Board[ChessSquar.Name] != null)	// Valid board square
					ChessSquar.DrawPiece(ChessImages.GetImageForPiece(ChessGame.Board[ChessSquar.Name].piece )); // draw the chess piece image
				
				if (ChessSquar.Name == SelectedSquar && ShowMoveHelp==true) // selected check box
				{
					ChessSquar.BackgroundImage = null;
					ChessSquar.BackColor = System.Drawing.Color.Thistle;
				}
			}

            if (SelectedSquar != null && SelectedSquar != "" && ChessGame.Board[SelectedSquar].piece != null && !ChessGame.Board[SelectedSquar].piece.IsEmpty())
            {
                if (ChessGame.Board[SelectedSquar].piece.Side.isWhite()) sSynth.SpeakAsync("player");
                if (ChessGame.Board[SelectedSquar].piece.Side.isBlack()) sSynth.SpeakAsync("computer");

                if (ChessGame.Board[SelectedSquar].piece.IsPawn()) style.AppendTextWithPronunciation("pawn", "ˈpːɒɳ");
                else style.AppendText(ChessGame.Board[SelectedSquar].piece.ToString(), PromptEmphasis.Strong);  // identify piece at selected square

                sSynth.SpeakAsync(style);
                style.ClearContent();
            }

            // Check if need to show the possible legal moves for the current selected piece
            if (SelectedSquar != null && SelectedSquar != "" && ShowMoveHelp==true && ChessGame.Board[SelectedSquar].piece != null && !ChessGame.Board[SelectedSquar].piece.IsEmpty() &&  ChessGame.Board[SelectedSquar].piece.Side.type == ChessGame.GameTurn )
			{
                ArrayList moves=ChessGame.GetLegalMoves(ChessGame.Board[SelectedSquar]);    // Get all legal moves for the current selected item
                
                // highlight all the possible moves for the current player
                if (moves.Count != 0)sSynth.SpeakAsync("Available moves are ");  // speak coordinates of clicked square
                foreach (Cell cell in moves)
				{                    
                    Squar sqr=GetBoardSquar(cell.ToString());	// get the board by cell position
// 					sqr.BackgroundImage = null;
                    // Show a semi-transparent, saddle color
//                     sqr.BackColor = System.Drawing.Color.FromArgb(200, System.Drawing.Color.SteelBlue);
				}
			}
			SelectedSquar="";	// Reset the selected square position
		}

		// Show current player turn visual clue
		public void ShowPlayerTurn()
		{
			ChessGame.UpdateTime();	// Update the chess thinking times

			if (ChessGame.BlackTurn())
			{
				ParentForm.BlackPlayerTime.Text = ChessGame.BlackPlayer.ThinkTime;
                //ParentForm.WhitePlayerName.Visible = true;
                //ParentForm.BlackPlayerName.Visible = !ParentForm.BlackPlayerName.Visible; // Blink the player name
            }
			else
			{
				ParentForm.WhitePlayerTime.Text = ChessGame.WhitePlayer.ThinkTime;
                //ParentForm.BlackPlayerName.Visible = true;
                //ParentForm.WhitePlayerName.Visible = !ParentForm.WhitePlayerName.Visible; // Blink the player name
            }
		}

		// Called when it's the next player turn to play the move
		// We handle the computer move here
		public void NextPlayerTurn()
		{
            if (ChessGame.ActivePlay.IsComputer()) // If the active player is a computer
			{
                
                sSynth.SpeakAsync("Computer's move.");
                System.Threading.Thread.Sleep(2500); //make the game sound better
                if (ShowComputerThinkingProgres)
                    ParentForm.ChessCaptureBar.Visible = false;
                else
                    ParentForm.ChessCaptureBar.Visible = true;

				Move nextMove = ChessGame.ActivePlay.GetBestMove();	// get the best move for the player

				if (nextMove!=null)	// a valid move is available
					UserMove(nextMove.StartCell.ToString(), nextMove.EndCell.ToString());   
				
				ParentForm.ChessCaptureBar.Visible = true; // show the capture bar
			}
            else
            {
                sSynth.SpeakAsync("Player's move.");
          
                gb.Append(SpeechMoves);
                Grammar g = new Grammar(new GrammarBuilder(gb,0,5));
                sr.SetInputToDefaultAudioDevice();
                sr.LoadGrammar(g);
                sr.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sr_SpeechRecognized);
                sr.RecognizeAsync(RecognizeMode.Multiple);
                    



            }


        }

		// Initialize the Chess player objects
		private void InitPlayers()
		{
            // Show the images depending on the selected player types
            if (ChessGame.BlackPlayer.PlayerType == Player.Type.Human && ChessGame.WhitePlayer.PlayerType == Player.Type.Human)
            {
                ChessGame.BlackPlayer.Image = System.Drawing.Image.FromFile(ResourceFolder + "user.jpg");
                ChessGame.WhitePlayer.Image = System.Drawing.Image.FromFile(ResourceFolder + "user_2.jpg");
            }
            else if (ChessGame.BlackPlayer.PlayerType == Player.Type.Computer && ChessGame.WhitePlayer.PlayerType == Player.Type.Human)
            {
                ChessGame.BlackPlayer.Image = System.Drawing.Image.FromFile(ResourceFolder + "laptop.jpg");
                ChessGame.WhitePlayer.Image = System.Drawing.Image.FromFile(ResourceFolder + "user_2.jpg");
            }
            else if (ChessGame.BlackPlayer.PlayerType == Player.Type.Computer && ChessGame.WhitePlayer.PlayerType == Player.Type.Computer)
            {
                ChessGame.BlackPlayer.Image = System.Drawing.Image.FromFile(ResourceFolder + "laptop.jpg");
                ChessGame.WhitePlayer.Image = System.Drawing.Image.FromFile(ResourceFolder + "laptop_2.png");
            }

			// Initialize other board objects
			ParentForm.WhitePlayerName.Text = ChessGame.WhitePlayer.Name;
			ParentForm.BlackPlayerName.Text = ChessGame.WhitePlayer.Name;

			ParentForm.WhitePlayerImage.Image = ChessGame.WhitePlayer.Image;
			ParentForm.BlackPlayerImage.Image = ChessGame.BlackPlayer.Image;

			ParentForm.WhitePlayerName.Text = ChessGame.WhitePlayer.Name;
			ParentForm.BlackPlayerName.Text = ChessGame.BlackPlayer.Name;

			// Set the time 
			ParentForm.BlackPlayerTime.Text = "00:00:00";
			ParentForm.WhitePlayerTime.Text = "00:00:00";

			ParentForm.lstHistory.Items.Clear();
		}

        void sr_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            
            StringBuilder bar = new StringBuilder();
            StringBuilder bar2 = new StringBuilder();
            String Command = e.Result.Text.ToString();
            Command.Replace(" ", "");
            Console.Write("-"+Command+"-");
            if(Command == "all" && e.Result.Confidence >= .7)
            {
                foreach (Squar ChessSquar in Squars)
                {
                    if (ChessSquar != null && ChessSquar.Name != "" && ChessGame.Board[ChessSquar.Name].piece != null && !ChessGame.Board[ChessSquar.Name].piece.IsEmpty())
                    {
                        
                        if (ChessGame.Board[ChessSquar.Name].piece.Side.isWhite()) sSynth.SpeakAsync("player");
                        if (ChessGame.Board[ChessSquar.Name].piece.Side.isBlack()) sSynth.SpeakAsync("computer");

                        if (ChessGame.Board[ChessSquar.Name].piece.IsPawn()) style.AppendTextWithPronunciation("pawn", "ˈpːɒɳ");
                        else style.AppendText(ChessGame.Board[ChessSquar.Name].piece.ToString(), PromptEmphasis.Strong);  // identify piece at selected square
                        sSynth.SpeakAsync(ChessSquar.Name.ToString());
                        sSynth.SpeakAsync(style);
                        style.ClearContent();
                    }
                }
            }
            if (Command.Length == 3 && Char.IsLetter(Command[0]) && Char.IsDigit(Command[2]) && e.Result.Confidence >= .7)
            {
                bar.Append(Command[0]);
                bar.Append(Command[2]);

                SelectedSquar = bar.ToString();
                if (lastSelected != SelectedSquar)
                {
                    lastSelected = SelectedSquar;
                    RedrawBoard();
                }
            }

            if (Command.Length == 9 && Char.IsLetter(Command[0]) && Char.IsLetter(Command[6]) && Char.IsDigit(Command[2]) && Char.IsDigit(Command[8]) && e.Result.Confidence >= .7)
            {
                
             
                 int val = (int)Char.GetNumericValue(Command[2]);
                 
                 bar.Append(Command[0]);
                 bar.Append(val);

                 val = (int)Char.GetNumericValue(Command[8]);
                 
                 bar2.Append(Command[6]);
                 bar2.Append(val);
                 String CurrentMove = bar.ToString() + bar2.ToString();
                if (ChessGame.Board[bar.ToString()].piece == null)
                {

                }
                else
                {
                    ArrayList moves = ChessGame.GetLegalMoves(ChessGame.Board[bar.ToString()]);
                    foreach (Cell cell in moves)
                    {

                        if (cell.ToString() == bar2.ToString() && CurrentMove != LastMove)
                        {
                            LastMove = CurrentMove;
                            UserMove(bar.ToString(), bar2.ToString());

                        }


                    }
                }
            }
            
        }
        // A move is made by the player
        public bool UserMove(string source, string dest)
		{
            sr.RecognizeAsyncStop();
        
            bool success = true;
			int MoveResult=ChessGame.DoMove(source, dest);
            Console.Write(" --- MoveResult:: " + MoveResult + " --- ");
			RedrawBoard();	// Refresh the board
            
            switch (MoveResult)
			{
				case 0:	// move was successful;
					// check if the last move was promo move
					Move move=ChessGame.GetLastMove();  // get the last move 

                    // Play the sound
                    if (ChessGame.IsUnderCheck()) {
                        Sounds.PlayCheck();	// Player is under check
                        sSynth.SpeakAsync("Check."); // Speech indication of player being under check
                    } else if (move.Type == Move.MoveType.NormalMove || move.Type == Move.MoveType.TowerMove)
                        Sounds.PlayNormalMove();
                    else
                        Sounds.PlayCaptureMove();

					// Add to the capture list
					if ( move.IsCaptureMove() )
						ParentForm.ChessCaptureBar.Add(ChessImages.GetImageForPiece(move.CapturedPiece));

					// If last move was a pawn promotion move, get the new selected piece from user
					if (move.IsPromoMove() && !ChessGame.ActivePlay.IsComputer())
						ChessGame.SetPromoPiece(GetPromoPiece(move.EndCell.piece.Side));	// Set the promo piece as selected by user
					
					// check for the check mate situation
					if (ChessGame.IsCheckMate(ChessGame.GameTurn))
					{
						Sounds.PlayGameOver();
                        sSynth.SpeakAsync("Check mate.");
                        sSynth.SpeakAsync("Game over.");
                        IsOver =true;
						MessageBox.Show(ChessGame.GetPlayerBySide(ChessGame.GameTurn).Name + " is checkmate.", "Game Over",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
					}
					// check for the stalemate situation
					if (ChessGame.IsStaleMate(ChessGame.GameTurn))
					{
						Sounds.PlayGameOver();
                        sSynth.SpeakAsync("Stale mate.");
                        sSynth.SpeakAsync("Game over.");
                        IsOver =true;
						MessageBox.Show(ChessGame.GetPlayerBySide(ChessGame.GameTurn).Name + " is stalemate.", "Game Over",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
					}

                    String output = move.ToString();
                    StringBuilder bar = new StringBuilder();
                    foreach(char c in output)
                    {
                        if (Char.IsDigit(c)){
                            int val = (int)Char.GetNumericValue(c);
                            int newVal = 8 - val + 1 ;
                            bar.Append(newVal);
                        }
                        else
                        {
                            bar.Append(c);
                        }
                    }

					LogUserMove(bar.ToString());
                    Console.Write("---------");
                    Console.Write(move.ToString());                                // Log the user action
                    Console.Write(bar.ToString());
                    Console.Write("---------");

                    sSynth.SpeakAsync(bar.ToString());  // Speech indicates piece taken
                    NextPlayerTurn();
					break;

				default:
                    success = false;
					break;
			}

            return success;
		}

		// Let user give the option of selecting promo piece
		public Piece GetPromoPiece(Side PlayerSide)
		{
			SelectPiece SelectPieceDlg = new SelectPiece();

			// Initialize the images to show on the form
			SelectPieceDlg.Piece1.Image = ChessImages.GetImageForPiece(new Piece(Piece.PieceType.Queen,PlayerSide));
			SelectPieceDlg.Piece2.Image = ChessImages.GetImageForPiece(new Piece(Piece.PieceType.Knight,PlayerSide));
			SelectPieceDlg.Piece3.Image = ChessImages.GetImageForPiece(new Piece(Piece.PieceType.Rook,PlayerSide));
			SelectPieceDlg.Piece4.Image = ChessImages.GetImageForPiece(new Piece(Piece.PieceType.Bishop,PlayerSide));
			
			SelectPieceDlg.ShowDialog(this.ParentForm);	// Show the promo select dialog

			// Now return back corresponding piece 
			switch (SelectPieceDlg.SelectedIndex)
			{
				case 1:
					return new Piece(Piece.PieceType.Queen,PlayerSide);
				case 2:
					return new Piece(Piece.PieceType.Knight,PlayerSide);
				case 3:
					return new Piece(Piece.PieceType.Rook,PlayerSide);
				case 4:
					return new Piece(Piece.PieceType.Bishop,PlayerSide);
			}
			return null;
		}

		// Display the user move in the history log
		public void LogUserMove(string movestring)
		{
			LogCounter++;
			ListViewItem newItem = new ListViewItem(new string[] { LogCounter.ToString(), movestring}, -1);
			
			if (LogCounter % 2 == 0)	// even entry
				newItem.ForeColor = System.Drawing.Color.Blue;

			ParentForm.lstHistory.Items.Add(newItem);
			ParentForm.lstHistory.Items[ParentForm.lstHistory.Items.Count-1].EnsureVisible();	// Scroll down
		
            // If the log has more than 16 items, reduce the width of the detail column to avoid 
            // horizontal scrollbar
            if (ParentForm.lstHistory.Items.Count > 16)
                ParentForm.lstHistory.Columns[1].Width = 90;
        }

		// Undo the last move
		public void UndoMove()
		{
			IsOver=false;				// Reset the is running variable
			Sounds.PlayNormalMove();

            // check if the last move was promo move
            Move move = ChessGame.GetLastMove();	// get the last move 

			if (ChessGame.UnDoMove())
			{
				LogUserMove("Undo Move");	// Log the user action
                sSynth.SpeakAsync("Move undone");    // Speech indicates that move was undone

                // Only remove the item from capture bar, if it was a capture move
                if (move.IsCaptureMove())
                    ParentForm.ChessCaptureBar.RemoveLast();
			}

			// If computer is playing do the double undo
			if (ChessGame.ActivePlay.IsComputer())
			{
                move = ChessGame.GetLastMove();	// get the last move 
				ChessGame.UnDoMove();

                // Only remove the item from capture bar, if it was a capture move
                if (move.IsCaptureMove())
				    ParentForm.ChessCaptureBar.RemoveLast();
			}

			RedrawBoard();	// Refresh the board
		}

		// Handle the computer thinking event
		public void ComputerThinking(int depth, int currentMove, int TotalMoves, int TotalAnalzyed, Move BestMove)
		{
            if (ShowComputerThinkingProgres)
            {
                // Update label and progress bar to display the computer think status
                ParentForm.PrgComputerThinkDepth.Maximum = TotalMoves;
                ParentForm.PrgComputerThinkDepth.Value = currentMove;
                ParentForm.LblComuterThinkLabel.Text = "Computer thinking at depth " + depth.ToString() + ". Total moves analyzed: " + TotalAnalzyed + ". ";

                if (BestMove != null)
                    ParentForm.LblComuterThinkLabel.Text += "Best move found so far is :" + BestMove.ToString();
            }
		}

		// Redo the move from redo history
		public void RedoMove()
		{
			Sounds.PlayNormalMove();
			if (ChessGame.ReDoMove())
			{
				LogUserMove("Redo Move");	// Log the user action
                sSynth.SpeakAsync("Move redone.");   // Speech indicates that move was redone

				// check if the last move was promo move
				Move move=ChessGame.GetLastMove();	// get the last move 

				// Add to the capture list
				if ( move.IsCaptureMove() )
					ParentForm.ChessCaptureBar.Add(ChessImages.GetImageForPiece(move.CapturedPiece));
			}
			RedrawBoard();	// Refresh the board
		}

        /// <summary>
        /// Save the current game state to the given file path
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveGame()
        {
            // Show the File Save as dialog and get the target file path
            SaveFileDialog saveAsDialog = new SaveFileDialog();
            saveAsDialog.Title = "Save file as...";
            saveAsDialog.Filter = "Quick Chess File (*.qcf)|*.qcf";
            saveAsDialog.RestoreDirectory = true;

            if (saveAsDialog.ShowDialog() == DialogResult.OK)
            {
                // Save the file at the given path
                ChessGame.SaveGame(saveAsDialog.FileName);
            }
        }

        /// <summary>
        /// Load the current game state from the given file path
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadGame()
        {
            // Show the File Save as dialog and get the target file path
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Title = "Load Quick Chess file...";
            openDialog.Filter = "Quick Chess File (*.qcf)|*.qcf";
            openDialog.RestoreDirectory = true;

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                ChessGame = new Game();
                ChessGame.Reset();	// Reset the game board
                ParentForm.ChessCaptureBar.Clear();

                IsRunning = true;
                LogCounter = 0;

                // Handle the events fired by the library
                ChessGame.ComputerThinking += new ChessLibrary.Game.ChessComputerThinking(ComputerThinking);

                // Save the file at the given path
                ChessGame.LoadGame(openDialog.FileName);

                // Show the player info
                InitPlayers();
                ParentForm.BlackPlayerTime.Text = ChessGame.BlackPlayer.ThinkTime;
                ParentForm.WhitePlayerTime.Text = ChessGame.WhitePlayer.ThinkTime;

                // Restore the Log and Capture bar items
                object[] moves = ChessGame.MoveHistory.ToArray();
                for (int i = moves.Length - 1; i >= 0; i--)
                {
                    Move move = (Move)moves[i];

                    // Log this user move
                    LogUserMove(move.ToString());

                    // Add to the capture list
				    if ( move.IsCaptureMove() )
					    ParentForm.ChessCaptureBar.Add(ChessImages.GetImageForPiece(move.CapturedPiece));
                }

                // Restore the menu state
                ParentForm.EnableSaveMenu();
                ParentForm.SetGamePrefrencesMenu();

                RedrawBoard();		    // Make the chess board visible on screen
                NextPlayerTurn();		// When the both players are computer this start the game 
            }
        }

		// Initialize a new game and set the pieces on the board
		public void NewGame()
		{
			ParentForm.ChessCaptureBar.Clear();
			NewGame NewGameDlg = new NewGame();
            NewGameDlg.ResourceFolderPath = ResourceFolder;
			NewGameDlg.ShowDialog();

			// Start the new game
			if (NewGameDlg.bStartGame)
			{
				ChessGame = new Game();

				// Handle the events fired by the library
				ChessGame.ComputerThinking += new ChessLibrary.Game.ChessComputerThinking(ComputerThinking);

				ChessGame.Reset();	// Reset the game board
				IsRunning = true;
				LogCounter = 0;

				ChessGame.WhitePlayer.Name = NewGameDlg.WhitePlayerName.Text;
				ChessGame.BlackPlayer.Name = NewGameDlg.BlackPlayerName.Text;

				// Start Human Vs. Computer Game
              //  if (NewGameDlg.PlayersHvC.Checked)
               // {
                    ChessGame.BlackPlayer.PlayerType = Player.Type.Computer;	// Set the black player as computer
                    ChessGame.WhitePlayer.PlayerType = Player.Type.Human;	    // Set the white player as computer (as he has the first move)
               // }

				// Both players are computer
			//	if (NewGameDlg.PlayersCvC.Checked)
			//	{
			//		ChessGame.BlackPlayer.PlayerType = Player.Type.Computer;	// Set the black player as computer
				//	ChessGame.WhitePlayer.PlayerType = Player.Type.Computer;	// Set the black player as computer
				//}

				// Beginner Player
				//if (NewGameDlg.PlayerLevel1.Checked)
				//{
					ChessGame.WhitePlayer.TotalThinkTime = 4;	// set maximum thinking time
					ChessGame.BlackPlayer.TotalThinkTime = 4;	// set maximum thinking time
				//}

				// Intermediate Player
			//	if (NewGameDlg.PlayerLevel2.Checked)
			//	{
				//	ChessGame.WhitePlayer.TotalThinkTime = 8;	// set maximum thinking time
				//	ChessGame.BlackPlayer.TotalThinkTime = 8;	// set maximum thinking time
			//	}

				// Chess Master Player
				//if (NewGameDlg.PlayerLevel3.Checked)
			//	{
				//	ChessGame.WhitePlayer.TotalThinkTime = 20;	// set maximum thinking time
				//	ChessGame.BlackPlayer.TotalThinkTime = 20;	// set maximum thinking time
			//	}

				InitPlayers();
				RedrawBoard();		// Make the chess board visible on screen
                sSynth.SpeakAsync("New game created.");  // Speech indicates that a new game was created
				NextPlayerTurn();		// When the both players are computer this start the game 
			
                // Let user save the game
                ParentForm.EnableSaveMenu();
            }
		}
	}
}
