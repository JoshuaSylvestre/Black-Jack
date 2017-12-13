using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using XnaCards;

namespace BlackJack
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class BlackJack : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        const int WindowWidth = 800;
        const int WindowHeight = 600;

        // max valid blockjuck score for a hand
        const int MaxHandValue = 21;

        // deck and hands
        Deck deck;
        List<Card> dealerHand = new List<Card>();
        List<Card> playerHand = new List<Card>();

        // hand placement
        const int TopCardOffset = 100;
        const int HorizontalCardOffset = 150;
        const int VerticalCardSpacing = 125;

        // messages
        SpriteFont messageFont;
        const string ScoreMessagePrefix = "Score: ";
        Message playerScoreMessage;
        Message dealerScoreMessage;
        Message winnerMessage;
        List<Message> messages = new List<Message>();

        // message placement
        const int ScoreMessageTopOffset = 25;
        const int HorizontalMessageOffset = HorizontalCardOffset;
        Vector2 winnerMessageLocation = new Vector2(WindowWidth / 2,
            WindowHeight / 2);

        // menu buttons
        Texture2D quitButtonSprite;
        List<MenuButton> menuButtons = new List<MenuButton>();

        // menu button placement
        const int TopMenuButtonOffset = TopCardOffset;
        const int QuitMenuButtonOffset = WindowHeight - TopCardOffset;
        const int HorizontalMenuButtonOffset = WindowWidth / 2;
        const int VeryicalMenuButtonSpacing = 125;

        // use to detect hand over when player and dealer didn't hit
        bool playerHit = false;
        bool dealerHit = false;

        // game state tracking
        static GameState currentState = GameState.WaitingForPlayer;

        public BlackJack()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution and show mouse
            graphics.PreferredBackBufferWidth = WindowWidth;
            graphics.PreferredBackBufferHeight = WindowHeight;
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // create and shuffle deck
            deck = new Deck(Content, WindowWidth / 2, WindowHeight / 2 + VerticalCardSpacing);
            deck.Shuffle();

            // first player card
            playerHand.Add(new Card(Content, deck.TakeTopCard().Rank, deck.TakeTopCard().Suit, HorizontalCardOffset, TopCardOffset));
            playerHand[0].FlipOver();

            // first dealer card
            dealerHand.Add(new Card(Content, deck.TakeTopCard().Rank, deck.TakeTopCard().Suit, WindowWidth - HorizontalCardOffset, TopCardOffset));

            // second player card
            playerHand.Add(new Card(Content, deck.TakeTopCard().Rank, deck.TakeTopCard().Suit, HorizontalCardOffset, TopCardOffset + VerticalCardSpacing));
            playerHand[1].FlipOver();


            // second dealer card
            dealerHand.Add(new Card(Content, deck.TakeTopCard().Rank, deck.TakeTopCard().Suit,
                WindowWidth - HorizontalCardOffset, TopCardOffset + VerticalCardSpacing));
            dealerHand[1].FlipOver();

            // load sprite font, create message for player score and add to list
            messageFont = Content.Load<SpriteFont>(@"fonts\Arial24");
            playerScoreMessage = new Message(ScoreMessagePrefix + GetBlockjuckScore(playerHand).ToString(),
                messageFont,
                new Vector2(HorizontalMessageOffset, ScoreMessageTopOffset));
            messages.Add(playerScoreMessage);

            // load quit button sprite for later use
            quitButtonSprite = Content.Load<Texture2D>(@"graphics\quitbutton");

            // create hit button and add to list
            MenuButton hitButton = new MenuButton(Content.Load<Texture2D>(@"graphics/hitbutton"),
                new Vector2(WindowWidth / 2, TopCardOffset), GameState.PlayerHitting);
            menuButtons.Add(hitButton);

            // create stand button and add to list
            MenuButton standButton = new MenuButton(Content.Load<Texture2D>(@"graphics/standbutton"),
                new Vector2(WindowWidth / 2, TopCardOffset + VerticalCardSpacing), GameState.PlayerStanding);
            menuButtons.Add(standButton);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            MouseState mouse = Mouse.GetState();

            // update menu buttons as appropriate
            foreach (MenuButton buttons in menuButtons)
            {
                buttons.Update(mouse);
            }

            // game state-specific processing using a switch statement
            switch (currentState)
            {
                case GameState.PlayerHitting:
                    playerHand.Add(new Card(Content, deck.TakeTopCard().Rank, deck.TakeTopCard().Suit,
                        HorizontalCardOffset, TopCardOffset + VerticalCardSpacing * playerHand.Count));

                    playerHand[playerHand.Count - 1].FlipOver();
                    ChangeState(GameState.WaitingForDealer);
                    playerHit = true;

                    CreateDealerButtons(mouse);
                    break;

                case GameState.PlayerStanding:
                    CreateDealerButtons(mouse);
                    ChangeState(GameState.WaitingForDealer);
                    break;

                case GameState.DealerHitting:
                    dealerHand.Add(new Card(Content, deck.TakeTopCard().Rank, deck.TakeTopCard().Suit,
                        WindowWidth - HorizontalCardOffset, TopCardOffset + VerticalCardSpacing * dealerHand.Count));
                    dealerHand[dealerHand.Count - 1].FlipOver();

                    ChangeState(GameState.CheckingHandOver);
                    dealerHit = true;
                    CreatePlayerButtons(mouse);
                    break;

                case GameState.DealerStanding:
                    ChangeState(GameState.CheckingHandOver);
                    break;

                case GameState.CheckingHandOver:
                    //continues the game if both are below max value and somebody had hit
                    if (playerHit == true && dealerHit == true && GetBlockjuckScore(playerHand) < MaxHandValue && GetBlockjuckScore(dealerHand) < MaxHandValue)
                    {
                        currentState = GameState.WaitingForPlayer;
                    }

                    //Following 3 statements check to see see wins if both player and dealer hit and somebody or both bust
                    if (GetBlockjuckScore(playerHand) > MaxHandValue && GetBlockjuckScore(dealerHand) > MaxHandValue)
                    {
                        DisplayResults();
                        displayWinText("Player and dealer bust! Tie!");
                    }
                    if (GetBlockjuckScore(playerHand) > MaxHandValue)
                    {
                        DisplayResults();
                        displayWinText("Player busts! Dealer wins!");
                    }
                    if (GetBlockjuckScore(dealerHand) > MaxHandValue)
                    {
                        DisplayResults();
                        displayWinText("Dealer busts! Player wins!");
                    }

                    //f both dealer and player stand, ends game and compares card values
                    if (playerHit == false && dealerHit == false)
                    {

                        if (GetBlockjuckScore(playerHand) > GetBlockjuckScore(dealerHand))
                        {
                            DisplayResults();
                            displayWinText("Player wins!");
                        }
                        if (GetBlockjuckScore(playerHand) < GetBlockjuckScore(dealerHand))
                        {
                            DisplayResults();
                            displayWinText("Dealer wins!");
                        }
                        if (GetBlockjuckScore(playerHand) == GetBlockjuckScore(dealerHand))
                        {
                            DisplayResults();
                            displayWinText("Tie!");
                        }
                    }
                    break;
                case GameState.DisplayingHandResults:
                    //Displays the quit button
                    menuButtons.Add(new MenuButton(Content.Load<Texture2D>(@"graphics/quitbutton"),
                        new Vector2(WindowWidth / 2, WindowHeight - TopMenuButtonOffset), GameState.Exiting));

                    menuButtons[0].Update(mouse);
                    break;

                case GameState.Exiting:
                    Exit();
                    break;

            }

            playerScoreMessage.Text = GetBlockjuckScore(playerHand).ToString();
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Goldenrod);

            spriteBatch.Begin();

            // draw hands
            foreach (Card card in playerHand)
            {
                card.Draw(spriteBatch);
            }
            foreach (Card card in dealerHand)
            {
                card.Draw(spriteBatch);
            }

            // draw messages
            foreach (Message message in messages)
            {
                message.Draw(spriteBatch);
            }

            // draw menu buttons
            foreach (MenuButton button in menuButtons)
            {
                button.Draw(spriteBatch);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        /// <summary>
        /// Flips dealers first card, displays their score and changes to Displaying results state
        /// </summary>
        private void DisplayResults()
        {
            ChangeState(GameState.DisplayingHandResults);
            dealerScoreMessage = new Message(ScoreMessagePrefix + GetBlockjuckScore(dealerHand).ToString(),
                messageFont,
                new Vector2(WindowWidth - HorizontalMessageOffset, ScoreMessageTopOffset));
            messages.Add(dealerScoreMessage);
            dealerHand[0].FlipOver();

            menuButtons.Clear();
        }

        /// <summary>
        /// Displays text of who won or if there was a tie
        /// </summary>
        /// <param name="message">Message to be displayed upon game ending</param>
        private void displayWinText(string message)
        {
            Message Message = new Message(message,
                messageFont,
                new Vector2(WindowWidth / 2, WindowHeight / 2));
            messages.Add(Message);
        }

        /// <summary>
        /// Creates the Dealer's buttons. Clears menu buttons of any trash beforehand.
        /// </summary>
        /// <param name="mouse">MouseState to update buttons</param>
        private void CreateDealerButtons(MouseState mouse)
        {
            menuButtons.Clear();
            MenuButton hitButton = new MenuButton(Content.Load<Texture2D>(@"graphics/hitbutton"),
                new Vector2(WindowWidth / 2, TopCardOffset), GameState.DealerHitting);
            menuButtons.Add(hitButton);

            // create stand button and add to list
            MenuButton standButton = new MenuButton(Content.Load<Texture2D>(@"graphics/standbutton"),
                new Vector2(WindowWidth / 2, TopCardOffset + VerticalCardSpacing), GameState.DealerStanding);
            menuButtons.Add(standButton);

            foreach (MenuButton button in menuButtons)
            {
                button.Update(mouse);
            }
        }

        /// <summary>
        /// Creates the Player's buttons. Clears menu buttons of any trash beforehand.
        /// </summary>
        /// <param name="mouse">MouseState to update buttons</param>
        private void CreatePlayerButtons(MouseState mouse)
        {
            menuButtons.Clear();
            MenuButton hitButton = new MenuButton(Content.Load<Texture2D>(@"graphics/hitbutton"),
                new Vector2(WindowWidth / 2, TopCardOffset), GameState.PlayerHitting);
            menuButtons.Add(hitButton);

            // create stand button and add to list
            MenuButton standButton = new MenuButton(Content.Load<Texture2D>(@"graphics/standbutton"),
                new Vector2(WindowWidth / 2, TopCardOffset + VerticalCardSpacing), GameState.PlayerStanding);
            menuButtons.Add(standButton);

            foreach (MenuButton button in menuButtons)
            {
                button.Update(mouse);
            }
        }

        /// <summary>
        /// Calculates the Blockjuck score for the given hand
        /// </summary>
        /// <param name="hand">the hand</param>
        /// <returns>the Blockjuck score for the hand</returns>
        private int GetBlockjuckScore(List<Card> hand)
        {
            // add up score excluding Aces
            int numAces = 0;
            int score = 0;
            foreach (Card card in hand)
            {
                if (card.Rank != Rank.Ace)
                {
                    score += GetBlockjuckCardValue(card);
                }
                else
                {
                    numAces++;
                }
            }

            // if more than one ace, only one should ever be counted as 11
            if (numAces > 1)
            {
                // make all but the first ace count as 1
                score += numAces - 1;
                numAces = 1;
            }

            // if there's an Ace, score it the best way possible
            if (numAces > 0)
            {
                if (score + 11 <= MaxHandValue)
                {
                    // counting Ace as 11 doesn't bust
                    score += 11;
                }
                else
                {
                    // count Ace as 1
                    score++;
                }
            }

            return score;
        }

        /// <summary>
        /// Gets the Blockjuck value for the given card
        /// </summary>
        /// <param name="card">the card</param>
        /// <returns>the Blockjuck value for the card</returns>
        private int GetBlockjuckCardValue(Card card)
        {
            switch (card.Rank)
            {
                case Rank.Ace:
                    return 11;
                case Rank.King:
                case Rank.Queen:
                case Rank.Jack:
                case Rank.Ten:
                    return 10;
                case Rank.Nine:
                    return 9;
                case Rank.Eight:
                    return 8;
                case Rank.Seven:
                    return 7;
                case Rank.Six:
                    return 6;
                case Rank.Five:
                    return 5;
                case Rank.Four:
                    return 4;
                case Rank.Three:
                    return 3;
                case Rank.Two:
                    return 2;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Changes the state of the game
        /// </summary>
        /// <param name="newState">the new game state</param>
        public static void ChangeState(GameState newState)
        {
            currentState = newState;
        }
    }
}
