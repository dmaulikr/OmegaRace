using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using CollisionManager;
using SpriteAnimation;
using Box2D.XNA;
using System.Diagnostics;

namespace OmegaRace
{
    public enum gameState
    {
        ready, // Flashes Ready? until the timer is up
        game, // The main game mode/
        pause,
        winner // Displays the winner //
    };

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {

        GraphicsDeviceManager graphics;
        public GraphicsDeviceManager Graphics
        {
            get { return graphics; }
        }


        private static Game1 Game;
        public static Game1 GameInstance
        {
            get { return Game; }
        }

        private static Camera camera;
        public static Camera Camera
        {
            get { return camera; }
        }

        //Networking---------------------------------
        const int maxGamers = 16;
        const int maxLocalGamers = 4;

        SpriteBatch spriteBatch;
        SpriteFont font;

        KeyboardState currentKeyboardState;
        GamePadState currentGamePadState;

        public static NetworkSession networkSession;

        PacketWriter packetWriter = new PacketWriter();
        PacketReader packetReader = new PacketReader();

        string errorMessage;
        //-------------------------------------------


        // Keyboard and Xbox Controller states
        KeyboardState oldState;
        KeyboardState newState;

        GamePadState P1oldPadState;
        GamePadState P1newPadState;

        GamePadState P2oldPadState;
        GamePadState P2newPadState;


        // For flipping game states
        public static gameState state;


        // Box2D world
        World world;
        public World getWorld()
        {
            return world;
        }

        public Rectangle gameScreenSize;


        // Quick reference for Input 
        Player player1;
        Player player2;


        // Max ship speed
        int shipSpeed;

        // i put this in for networking
        


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";


            graphics.PreferredBackBufferHeight = 500;
            graphics.PreferredBackBufferWidth = 800;

            gameScreenSize = new Rectangle(0, 0, 800, 500);

            state = gameState.ready;

            world = new World(new Vector2(0, 0), false);

            shipSpeed = 200;

            Game = this;

            Components.Add(new GamerServicesComponent(this));
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
           

            camera = new Camera(GraphicsDevice.Viewport, Vector2.Zero);

            state = gameState.game;

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // TODO: use this.Content to load your game content here

                world = new World(new Vector2(0, 0), true);

                myContactListener myContactListener = new myContactListener();

                world.ContactListener = myContactListener;


                Data.Instance().createData();

                state = gameState.game;

                player1 = PlayerManager.Instance().getPlayer(PlayerID.one);
                player2 = PlayerManager.Instance().getPlayer(PlayerID.two);

                spriteBatch = new SpriteBatch(GraphicsDevice);

               // font = Content.Load<SpriteFont>("Font");
                font = Content.Load<SpriteFont>("SpriteFont1");


        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
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
            // TODO: Add your update logic here
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            GraphicsDevice.Clear(Color.Black);

            HandleInput();

            if (networkSession == null)
            {
                // If we are not in a network session, update the
                // menu screen that will let us create or join one.
                UpdateMenuScreen();
            }
            else
            {
                // If we are in a network session, update it.
                UpdateNetworkSession();
            }

            base.Update(gameTime);

            if (state == gameState.game)
            {

                //Input Queue -> msg process
                InputQueue.Process3();

                world.Step((float)gameTime.ElapsedGameTime.TotalSeconds, 5, 8);


                // Out 0) contact listener fires collisions
                    // events created in world step

                //Out 1) physics event(contact listener) msg -> Output queue

                if (Game1.networkSession != null)
                {
                    if (Game1.networkSession.IsHost)
                    {
                        // create Buffer
                        PhysicsBuffer[] physicsBuff = new PhysicsBuffer[PhysicsMan.Instance().getCount()];
                        int num = PhysicsMan.Instance().getCount();


                        // Update PhysicsObject to PhysicsBuffer for a message transmission
                        PhysicsMan.PushToBuffer(ref physicsBuff);

                        //// out 2) physics buffer msg => OutputQueue
                        PhysicsBuffer_Message msg = new PhysicsBuffer_Message(ref physicsBuff);
                        PhysicsBuffer_Message_outQueue.add(msg);
                    }
                }

                // ON remote ---------------------------------------------
                // read Physics Buffer from OutputQueue
                // no physics simulator

                // On Both----------------------------------------------

                // Out 3) input msg -> OutputQueue
                // InputQueue.Update ?? maybe this is checkinput()???

                checkInput();

                //OutputQueue -> InputQueue
                OutputQueue.PushToNetwork();

                // I put this down here to get pBuffGlobal to have the same amount of items as number of physics bodies
               // InputQueue.Process3();

                //Both----------------------------------------------
                
                //PhsicsBuffer to GameObject
                PhysicsMan.Update(ref PhysicsBuffer_Message.pBuffGlobal);

                //PhysicsMan.Instance().Update();

                ScoreManager.Instance().Update();

                GameObjManager.Instance().Update(world);

                Timer.Process(gameTime);
            }

            Game1.Camera.Update(gameTime);
        }

        /// <summary>
        /// Menu screen provides options to create or join network sessions.
        /// </summary>
        void UpdateMenuScreen()
        {
            if (IsActive)
            {
                if (Gamer.SignedInGamers.Count == 0)
                {
                    // If there are no profiles signed in, we cannot proceed.
                    // Show the Guide so the user can sign in.
                    Guide.ShowSignIn(maxLocalGamers, false);
                }
                else if (IsPressed(Keys.A, Buttons.A))
                {
                    // Create a new session?
                    CreateSession();
                }
                else if (IsPressed(Keys.B, Buttons.B))
                {
                    // Join an existing session?
                    JoinSession();
                }
            }
        }

        /// <summary>
        /// Starts hosting a new network session.
        /// </summary>
        void CreateSession()
        {
            DrawMessage("Creating session...");

            try
            {
                networkSession = NetworkSession.Create(NetworkSessionType.SystemLink,
                                                       maxLocalGamers, maxGamers);

                HookSessionEvents();
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }
        }

        void JoinSession()
        {
            DrawMessage("Joining session...");

            try
            {
                // Search for sessions.
                using (AvailableNetworkSessionCollection availableSessions =
                            NetworkSession.Find(NetworkSessionType.SystemLink,
                                                maxLocalGamers, null))
                {
                    if (availableSessions.Count == 0)
                    {
                        errorMessage = "No network sessions found.";
                        return;
                    }

                    // Join the first session we found.
                    networkSession = NetworkSession.Join(availableSessions[0]);

                    HookSessionEvents();
                }
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
            }
        }

        void HookSessionEvents()
        {
            networkSession.GamerJoined += GamerJoinedEventHandler;
            networkSession.SessionEnded += SessionEndedEventHandler;
        }

        /// <summary>
        /// This event handler will be called whenever a new gamer joins the session.
        /// We use it to allocate a Tank object, and associate it with the new gamer.
        /// </summary>
        void GamerJoinedEventHandler(object sender, GamerJoinedEventArgs e)
        {
            int gamerIndex = networkSession.AllGamers.IndexOf(e.Gamer);
            Debug.WriteLine(" gamer join: {0} \n", gamerIndex);
            e.Gamer.Tag = new Ship(gamerIndex);

            InputQueue.pShip[gamerIndex] = e.Gamer.Tag as Ship;

            //if(e.Gamer.IsHost)
            //{
            //    Ship thisShip = e.Gamer.Tag as Ship;
            //    //this.tankTextureCurrent = thisTank.tankTextureGreen;
            //}


        }


        /// <summary>
        /// Event handler notifies us when the network session has ended.
        /// </summary>
        void SessionEndedEventHandler(object sender, NetworkSessionEndedEventArgs e)
        {
            errorMessage = e.EndReason.ToString();

            networkSession.Dispose();
            networkSession = null;
        }

        void UpdateNetworkSession()
        {
            // Read inputs for locally controlled tanks, and send them to the server.
            foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
            {
                UpdateLocalGamer(gamer);
            }

            // If we are the server, update all the tanks and transmit
            // their latest positions back out over the network.
            if (networkSession.IsHost)
            {
                UpdateServer();
            }

            // Pump the underlying session object.
            networkSession.Update();

            // Make sure the session has not ended.
            if (networkSession == null)
                return;

            // Read any incoming network packets.
            foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
            {
                if (gamer.IsHost)
                {
                    ServerReadInputFromClients(gamer);
                }
                else
                {
                    ClientReadGameStateFromServer(gamer);
                }
            }
        }

        void UpdateLocalGamer(LocalNetworkGamer gamer)
        {
            // Look up what tank is associated with this local player,
            // and read the latest user inputs for it. The server will
            // later use these values to control the tank movement.
           // Tank localTank = gamer.Tag as Tank;
            Ship localShip = gamer.Tag as Ship;

           // ReadTankInputs(localTank, gamer.SignedInGamer.PlayerIndex);
           // checkInput();

            // Only send if we are not the server. There is no point sending packets
            // to ourselves, because we already know what they will contain!
            if (!networkSession.IsHost)
            {
                // Write our latest input state into a network packet.
               // packetWriter.Write(localTank.TankInput);
               // packetWriter.Write(localTank.TurretInput);

                // send messages across?
               // packetWriter.Write(

                // Send our input data to the server.
               // gamer.SendData(packetWriter,
                 //              SendDataOptions.InOrder, networkSession.Host);
            }
        }

        void UpdateServer()
        {
            // Loop over all the players in the session, not just the local ones!
            foreach (NetworkGamer gamer in networkSession.AllGamers)
            {
                // Look up what tank is associated with this player.
                //Tank tank = gamer.Tag as Tank;
                Ship ship = gamer.Tag as Ship;
                // Update the tank.
                //tank.Update();
               // ship.Update();

                // Write the tank state into the output network packet.
                packetWriter.Write(gamer.Id);
                //packetWriter.Write(tank.Position);
                //packetWriter.Write(tank.TankRotation);
                //packetWriter.Write(tank.TurretRotation);
            }

            // Send the combined data for all tanks to everyone in the session.
            LocalNetworkGamer server = (LocalNetworkGamer)networkSession.Host;

            server.SendData(packetWriter, SendDataOptions.InOrder);
        }

        void ServerReadInputFromClients(LocalNetworkGamer gamer)
        {
            // Keep reading as long as incoming packets are available.
            while (gamer.IsDataAvailable)
            {
                NetworkGamer sender;

                // Read a single packet from the network.
                gamer.ReceiveData(packetReader, out sender);

                if (!sender.IsLocal)
                {
                    // Look up the tank associated with whoever sent this packet.
                    //Tank remoteTank = sender.Tag as Tank;
                    Ship remoteShip = sender.Tag as Ship;

                    // Read the latest inputs controlling this tank.
                    // remoteTank.TankInput = packetReader.ReadVector2();
                    // remoteTank.TurretInput = packetReader.ReadVector2();

                    QueueHdr qH;

                    qH.inSeqNum = packetReader.ReadInt32();
                    qH.outSeqNum = packetReader.ReadInt32();
                    qH.type = (Queue_type)packetReader.ReadInt32();
                    //qH.data = packetReader.ReadVector2();
                    
                    

                    switch (qH.type)
                    {
                        case Queue_type.QUEUE_SHIP_BOMB:
                            Ship_Create_Bomb_Message msg = new Ship_Create_Bomb_Message(player1);
                            qH.data = msg;
                            InputQueue.add(qH);
                            break;
                        case Queue_type.QUEUE_SHIP_MISSILE:
                            Ship_Create_Missile_Message msg2 = new Ship_Create_Missile_Message(player1);
                            qH.data = msg2;
                            InputQueue.add(qH);
                            break;
                        
                        case Queue_type.QUEUE_SHIP_IMPULSE:
                            Ship_Impulse_Message sim = new Ship_Impulse_Message(player1, packetReader.ReadVector2());
                           // qH.data = packetReader.ReadVector2();
                            qH.data = sim;
                          //  Ship_Impulse_Message msg3 = new Ship_Impulse_Message((Ship_Impulse_Message)qH.data);
                           // msg = new Ship_Impulse_Message((Ship_Impulse_Message)qH.data);
                           // qH.data = msg3;
                            InputQueue.add(qH);
                           
                            break;
                        case Queue_type.QUEUE_SHIP_ROT:
                            Ship_Rot_Message rotMessage = new Ship_Rot_Message(player1, packetReader.ReadSingle());// float? ReadDouble()
                           // qH.data = packetReader.ReadVector2();
                            qH.data = rotMessage;
                           // Ship_Rot_Message msg4 = new Ship_Rot_Message((Ship_Rot_Message)qH.data);
                           // qH.data = msg4;
                            InputQueue.add(qH);
                           // msg = new Ship_Rot_Message((Ship_Rot_Message)qH.data);
                            break;
                        case Queue_type.QUEUE_EVENT:
                            break;


                    }


                }
            }
        }

        void ClientReadGameStateFromServer(LocalNetworkGamer gamer)
        {
            // Keep reading as long as incoming packets are available.
            while (gamer.IsDataAvailable)
            {
                NetworkGamer sender;

                // Read a single packet from the network.
                gamer.ReceiveData(packetReader, out sender);

                // This packet contains data about all the players in the session.
                // We keep reading from it until we have processed all the data.
                while (packetReader.Position < packetReader.Length)
                {
                    if (packetReader.Length <= 2)
                        break;
                    // Read the state of one tank from the network packet.
                   // byte gamerId = packetReader.ReadByte();
                    //Vector2 position = packetReader.ReadVector2();
                    //float tankRotation = packetReader.ReadSingle();
                    //float turretRotation = packetReader.ReadSingle();

                    QueueHdr qH;

                    qH.inSeqNum = packetReader.ReadInt32();
                    qH.outSeqNum = packetReader.ReadInt32();
                    qH.type = (Queue_type)packetReader.ReadInt32();

                   // int incomingCount = packetReader.ReadInt32();


                    //Debug.WriteLine("qH.inSeqNum  ServerRead = " + qH.inSeqNum);
                    //Debug.WriteLine("qH.outSeqNum ServerRead = " + qH.outSeqNum);
                    //Debug.WriteLine("qH.type ServerRead = " + qH.type);
                    //Debug.WriteLine("incomingCount ServerRead = " + incomingCount);


                    //////////////////////// send to input queue on client
                    //PhysicsBuffer[] physicsBuff2 = new PhysicsBuffer[incomingCount];

                    
                    switch(qH.type)
                    {
                        case Queue_type.QUEUE_PHYSICS_BUFFER:
                        if (qH.type == Queue_type.QUEUE_PHYSICS_BUFFER)
                        {
                            int incomingCount = packetReader.ReadInt32();
                            PhysicsBuffer[] physicsBuff2 = new PhysicsBuffer[incomingCount];

                            for (int y = 0; y < incomingCount; y++)
                            {
                                //PhysicsBuffer_Message localPhysicsBuffer_Message = new PhysicsBuffer_Message((PhysicsBuffer_Message)qH.data);
                                //PhysicsBuffer myPhysicsBuffer = new PhysicsBuffer();
                                //myPhysicsBuffer.id = packetReader.ReadInt32();
                                //myPhysicsBuffer.position = packetReader.ReadVector2();
                                //myPhysicsBuffer.rotation = (float)packetReader.ReadSingle();
                                //Push to buffer
                                physicsBuff2[y].id = packetReader.ReadInt32();
                                physicsBuff2[y].position = packetReader.ReadVector2();
                                physicsBuff2[y].rotation = (float)packetReader.ReadSingle();

                           

                            }

                            // send this msg to Input Queue inQ so client can read the messages
                            PhysicsBuffer_Message msg = new PhysicsBuffer_Message(ref physicsBuff2);
                            PhysicsBuffer_Message_inQueue.add(msg);


                        }
                        break;

                        case Queue_type.QUEUE_SHIP_MISSILE:
                        Ship_Create_Missile_Message msgMissile = new Ship_Create_Missile_Message(player2);
                        qH.data = msgMissile;
                        InputQueue.add(qH);
                        break;

                        case Queue_type.QUEUE_SHIP_BOMB:
                        Ship_Create_Bomb_Message msgBomb = new Ship_Create_Bomb_Message(player2);
                        qH.data = msgBomb;
                        InputQueue.add(qH);
                        break;

                        case Queue_type.QUEUE_SHIP_IMPULSE:
                        Vector2 impulse = packetReader.ReadVector2();
                        Ship_Impulse_Message msgImpulse = new Ship_Impulse_Message(player2, impulse );
                        qH.data = msgImpulse;
                        InputQueue.add(qH);
                        break;

                        case Queue_type.QUEUE_SHIP_ROT:
                            float rotation = packetReader.ReadSingle();
                            Ship_Rot_Message msgRotation = new Ship_Rot_Message(player2, rotation);
                            qH.data = msgRotation;
                            InputQueue.add(qH);
                            break;
                        
                        case Queue_type.QUEUE_EVENT:
                        //msg = new Event_Message((Event_Message)qH.data);

                            int idA = packetReader.ReadInt32();
                           // Debug.WriteLine("idA =" + idA);
                            int idB = packetReader.ReadInt32();
                           // Debug.WriteLine("idA =" + idB);
                            Vector2 collisionPt = packetReader.ReadVector2();
                           // Debug.WriteLine("idA =" + collisionPt);

                        Event_Message eventMsg = new Event_Message(idA , idB , collisionPt);
                        qH.data = eventMsg;
                        InputQueue.add(qH);


                        
                        break;

                }
                

                   
                }
            }
        }

        /// <summary>
        /// Handles input.
        /// </summary>
        private void HandleInput()
        {
            currentKeyboardState = Keyboard.GetState();
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Check for exit.
            if (IsActive && IsPressed(Keys.Escape, Buttons.Back))
            {
                Exit();
            }
        }


        /// <summary>
        /// Checks if the specified button is pressed on either keyboard or gamepad.
        /// </summary>
        bool IsPressed(Keys key, Buttons button)
        {
            return (currentKeyboardState.IsKeyDown(key) ||
                    currentGamePadState.IsButtonDown(button));
        }






        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {

            //if (networkSession == null)
            //{
            //    // If we are not in a network session, draw the
            //    // menu screen that will let us create or join one.
            //    DrawMenuScreen();
            //}
            //else
            //{
            //    // If we are in a network session, draw it.
            //    DrawNetworkSession();
            //}

            //if (state == gameState.game)
            //{
            //   // SpriteBatchManager.Instance().process();
            //}

            if (networkSession == null)
            {
                // If we are not in a network session, draw the
                // menu screen that will let us create or join one.
                DrawMenuScreen();
            }
            else
            {
                if (state == gameState.game)
                {
                    // this is where we draw the game
                     SpriteBatchManager.Instance().process();
                     DrawNetworkSession();
                }
            }

            

            // draw the game
            base.Draw(gameTime);
        }

        void DrawMenuScreen()
        {
            string message = string.Empty;
            string WelcomeMessage = string.Empty;
            string InsertCoin = string.Empty;

            if (!string.IsNullOrEmpty(errorMessage))
                message += "Error:\n" + errorMessage.Replace(". ", ".\n") + "\n\n";

            message += "Press A Button To Start\n" +
                       "Press B Button To Join the Galactic Battle";
            WelcomeMessage += "Enter The Omega Race";
            InsertCoin += "Insert Coin";

            spriteBatch.Begin();

            spriteBatch.DrawString(font, message, new Vector2(161, 161), Color.Black);
            spriteBatch.DrawString(font, message, new Vector2(160, 160), Color.White);
            spriteBatch.DrawString(font, WelcomeMessage, new Vector2(50, 50), Color.Red);
            spriteBatch.DrawString(font, InsertCoin, new Vector2(100, 100), Color.Blue);

            spriteBatch.End();
        }

        void DrawNetworkSession()
        {
            spriteBatch.Begin();

            // For each person in the session...
            foreach (NetworkGamer gamer in networkSession.AllGamers)
            {
                // Look up the tank object belonging to this network gamer.
                //Tank tank = gamer.Tag as Tank;
                Ship ship = gamer.Tag as Ship;
               

                // Draw the tank.
               // tank.Draw(spriteBatch);
               // Ship.Draw(spriteBatch);

                // Draw a gamertag label.
                string label = gamer.Gamertag;
                Color labelColor = Color.Red;
                Vector2 labelOffset = new Vector2(10, 15);

                if (gamer.IsHost)
                {
                    label += " (server)";
                    spriteBatch.DrawString(font, label, player1.playerShip.location, labelColor, 0,
                                       labelOffset, 0.6f, SpriteEffects.None, 0);
                }
                else
                {
                    label += " (client)";
                    spriteBatch.DrawString(font, label, player2.playerShip.location, labelColor, 0,
                                       labelOffset, 0.6f, SpriteEffects.None, 0);
                }
                    

                // Flash the gamertag to yellow when the player is talking.
                if (gamer.IsTalking)
                    labelColor = Color.Yellow;

                
            }

            spriteBatch.End();
        }

        public void GameOver()
        {
            state = gameState.winner;


            resetData();
        }


        private void checkInput()
        {
            newState = Keyboard.GetState();
            P1newPadState = GamePad.GetState(PlayerIndex.One);
            P2newPadState = GamePad.GetState(PlayerIndex.Two);

            if (oldState.IsKeyDown(Keys.D) || P1oldPadState.IsButtonDown(Buttons.DPadRight))
            {
                player1.playerShip.physicsObj.body.Rotation += 0.1f;
                Ship_Rot_Message msg = new Ship_Rot_Message(player1, 0.1f);
                Debug.WriteLine("D player1.playerShip.physicsObj.body.Rotation = " + player1.playerShip.physicsObj.body.Rotation);
                OutputQueue.add(msg);
            }

            if (oldState.IsKeyDown(Keys.A) || P1oldPadState.IsButtonDown(Buttons.DPadLeft))
            {

                player1.playerShip.physicsObj.body.Rotation -= 0.1f;
                Ship_Rot_Message msg = new Ship_Rot_Message(player1, -0.1f);
                Debug.WriteLine("A player1.playerShip.physicsObj.body.Rotation = " + player1.playerShip.physicsObj.body.Rotation);
                OutputQueue.add(msg);
            }

            if (oldState.IsKeyDown(Keys.W) || P1oldPadState.IsButtonDown(Buttons.DPadUp))
            {
                Ship Player1Ship = player1.playerShip;

                Vector2 direction = new Vector2((float)(Math.Cos(Player1Ship.physicsObj.body.GetAngle())), (float)(Math.Sin(Player1Ship.physicsObj.body.GetAngle())));
                Debug.WriteLine("direction - W - = " + direction);
                //Vector2 direction = new Vector2(.6f, .5f);
                direction.Normalize();
                float x = (float)(Math.Cos(Player1Ship.physicsObj.body.GetAngle()));
                float y = (float)(Math.Sin(Player1Ship.physicsObj.body.GetAngle()));
                Debug.WriteLine("x = " + x);
                Debug.WriteLine("y = " + y);
                direction *= shipSpeed;
                Debug.WriteLine("direction - W - afterNormal = " + direction);
                

                // No action, send a message thru queue
                //Player1Ship.physicsObj.body.ApplyLinearImpulse(direction, Player1Ship.physicsObj.body.GetWorldCenter());

                Ship_Impulse_Message msg = new Ship_Impulse_Message(player1, direction);
                OutputQueue.add(msg);
            }

            if ((oldState.IsKeyDown(Keys.X) && newState.IsKeyUp(Keys.X)) || (P1oldPadState.IsButtonDown(Buttons.A) && P1newPadState.IsButtonUp(Buttons.A)))
            {
                if (player1.state == PlayerState.alive && player1.missileAvailable())
                {
                   // player1.createMissile();

                    Ship_Create_Missile_Message msg = new Ship_Create_Missile_Message(player1);
                    OutputQueue.add(msg);
                }

            }

            if (oldState.IsKeyDown(Keys.C) && newState.IsKeyUp(Keys.C) || (P1oldPadState.IsButtonDown(Buttons.B) && P1newPadState.IsButtonUp(Buttons.B)))
            {
                if (player1.state == PlayerState.alive && BombManager.Instance().bombAvailable(PlayerID.one))
                {
                   // GameObjManager.Instance().createBomb(PlayerID.one);

                    Ship_Create_Bomb_Message msg = new Ship_Create_Bomb_Message(player1);
                    OutputQueue.add(msg);
                }

            }

            if (oldState.IsKeyDown(Keys.Right) || P2oldPadState.IsButtonDown(Buttons.DPadRight))
            {
               // player2.playerShip.physicsObj.body.Rotation += 0.1f;
                Ship_Rot_Message msg = new Ship_Rot_Message(player2, 0.1f);
                Debug.WriteLine("Right player2.playerShip.physicsObj.body.Rotation = " + player2.playerShip.physicsObj.body.Rotation);
                OutputQueue.add(msg);

            }

            if (oldState.IsKeyDown(Keys.Left) || P2oldPadState.IsButtonDown(Buttons.DPadLeft))
            {
                //player2.playerShip.physicsObj.body.Rotation -= 0.1f;
                Ship_Rot_Message msg = new Ship_Rot_Message(player2, -0.1f);
                Debug.WriteLine("Left player1.playerShip.physicsObj.body.Rotation = " + player2.playerShip.physicsObj.body.Rotation);
                OutputQueue.add(msg);
            }


            if (oldState.IsKeyDown(Keys.Up) || P2oldPadState.IsButtonDown(Buttons.DPadUp))
            {
                Ship Player2Ship = player2.playerShip;

                Vector2 direction = new Vector2((float)(Math.Cos(Player2Ship.physicsObj.body.GetAngle())), (float)(Math.Sin(Player2Ship.physicsObj.body.GetAngle())));
                //Vector2 direction = new Vector2(-.5f, .5f);
                Debug.WriteLine("direction - up - = " + direction);
                float x = (float)(Math.Cos(Player2Ship.physicsObj.body.GetAngle()));
                float y = (float)(Math.Sin(Player2Ship.physicsObj.body.GetAngle()));
                Debug.WriteLine("x = " + x);
                Debug.WriteLine("y = " + y);
                direction.Normalize();

                direction *= shipSpeed;

               // Player2Ship.physicsObj.body.ApplyLinearImpulse(direction, Player2Ship.physicsObj.body.GetWorldCenter());

                // No action, send a message thru queue
                //Player1Ship.physicsObj.body.ApplyLinearImpulse(direction, Player1Ship.physicsObj.body.GetWorldCenter());

                Ship_Impulse_Message msg = new Ship_Impulse_Message(player2, direction);
                OutputQueue.add(msg);

            }

            if ((oldState.IsKeyDown(Keys.OemQuestion) && newState.IsKeyUp(Keys.OemQuestion)) || (P2oldPadState.IsButtonDown(Buttons.A) && P2newPadState.IsButtonUp(Buttons.A)))
            {
                if (player2.state == PlayerState.alive && player2.missileAvailable())
                {
                   // player2.createMissile();
                    Ship_Create_Missile_Message msg = new Ship_Create_Missile_Message(player2);
                    OutputQueue.add(msg);
                }
            }

            if (oldState.IsKeyDown(Keys.OemPeriod) && newState.IsKeyUp(Keys.OemPeriod) || (P2oldPadState.IsButtonDown(Buttons.B) && P2newPadState.IsButtonUp(Buttons.B)))
            {
                if (player2.state == PlayerState.alive && BombManager.Instance().bombAvailable(PlayerID.two))
                {
                   // GameObjManager.Instance().createBomb(PlayerID.two);

                    Ship_Create_Bomb_Message msg = new Ship_Create_Bomb_Message(player2);
                    OutputQueue.add(msg);
                }
            }


            else { }



            P1oldPadState = P1newPadState;
            P2oldPadState = P2newPadState;
            oldState = newState;
        }


        //Draw --------------------------------------------------------
        void DrawMessage(string message)
        {
            if (!BeginDraw())
                return;

            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            spriteBatch.DrawString(font, message, new Vector2(161, 161), Color.Black);
            spriteBatch.DrawString(font, message, new Vector2(160, 160), Color.White);

            spriteBatch.End();

            EndDraw();
        }





        private void clearData()
        {
            TextureManager.Instance().clear();
            ImageManager.Instance().clear();
            SpriteBatchManager.Instance().clear();
            SpriteProxyManager.Instance().clear();
            DisplayManager.Instance().clear();
            AnimManager.Instance().clear();
            GameObjManager.Instance().clear();
            Timer.Clear();
            PlayerManager.Instance().clear();
            BombManager.Instance().clear();
        }

        public void resetData()
        {
            clearData();

            LoadContent();

            ScoreManager.Instance().createData();

            state = gameState.game;
        }
    }
}
