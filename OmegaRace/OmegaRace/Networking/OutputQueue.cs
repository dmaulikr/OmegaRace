using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Net;
using System.Diagnostics;
using Microsoft.Xna.Framework;


namespace OmegaRace
{
    class OutputQueue
    {
        // data-----------------
        static public System.Collections.Generic.Queue<QueueHdr> outQ = new System.Collections.Generic.Queue<QueueHdr>();
        static public int seqNumGlobal = 9000;


       // PacketWriter packetWriter2 = new PacketWriter();

        public static void PushToNetwork()
        {

            PacketWriter packetWriter2 = new PacketWriter();

            int count = outQ.Count;
            for (int i = 0; i < count; i++)
            {
                // Read the header
                QueueHdr qH = outQ.Dequeue();

                switch (qH.type)
                {



                    case Queue_type.QUEUE_SHIP_BOMB:

                        packetWriter2.Write(qH.inSeqNum);
                                packetWriter2.Write(qH.outSeqNum);
                                packetWriter2.Write((int)qH.type);
                        if (Game1.networkSession != null)
                        {
                            if (Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                            }

                            if (Game1.networkSession.IsHost)
                            {
                                LocalNetworkGamer server = (LocalNetworkGamer)Game1.networkSession.Host;

                                server.SendData(packetWriter2, SendDataOptions.InOrder);
                            }
                        }
                        foreach (LocalNetworkGamer gamer in Game1.networkSession.LocalGamers)
                        {

                            if (!Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                                // Write our latest input state into a network packet.
                                //packetWriter2.Write(qH.inSeqNum);
                                //packetWriter2.Write(qH.outSeqNum);
                                //packetWriter2.Write((int)qH.type);
                                // packetWriter2.Write((int)qH.data);

                                // Send our input data to the server.//
                                gamer.SendData(packetWriter2,
                                               SendDataOptions.InOrder, Game1.networkSession.Host);
                            }
                        }
                        
                        break;
                    case Queue_type.QUEUE_SHIP_MISSILE:

                        packetWriter2.Write(qH.inSeqNum);
                        packetWriter2.Write(qH.outSeqNum);
                        packetWriter2.Write((int)qH.type);

                        if (Game1.networkSession != null)
                        {
                            if (Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                            }

                            if (Game1.networkSession.IsHost)
                            {
                                LocalNetworkGamer server = (LocalNetworkGamer)Game1.networkSession.Host;

                                server.SendData(packetWriter2, SendDataOptions.InOrder);
                            }
                        }
                        foreach (LocalNetworkGamer gamer in Game1.networkSession.LocalGamers)
                        {

                            if (!Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                                // Write our latest input state into a network packet.
                                //packetWriter2.Write(qH.inSeqNum);
                                //packetWriter2.Write(qH.outSeqNum);
                                //packetWriter2.Write((int)qH.type);
                               // packetWriter2.Write((int)qH.data);

                                // Send our input data to the server.
                                gamer.SendData(packetWriter2,
                                               SendDataOptions.InOrder, Game1.networkSession.Host);
                            }
                        }

                        break;
                    case Queue_type.QUEUE_SHIP_IMPULSE:

                        packetWriter2.Write(qH.inSeqNum);
                                packetWriter2.Write(qH.outSeqNum);
                                packetWriter2.Write((int)qH.type);

                                Ship_Impulse_Message sim = (Ship_Impulse_Message)qH.data;
                                
                                packetWriter2.Write((Vector2)sim.impulse);
                                Debug.WriteLine("Outputq - sim.impulse = " + sim.impulse);
                                Debug.WriteLine("Outputq - sim.X = " + sim.impulse.X);
                                Debug.WriteLine("Outputq - sim.Y = " + sim.impulse.Y);
                        
                        if (Game1.networkSession != null)
                        {
                            if (Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                            }

                            if (Game1.networkSession.IsHost)
                            {
                                LocalNetworkGamer server = (LocalNetworkGamer)Game1.networkSession.Host;

                                server.SendData(packetWriter2, SendDataOptions.InOrder);
                            }
                        }
                        foreach (LocalNetworkGamer gamer in Game1.networkSession.LocalGamers)
                        {

                            if (!Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);///just added 207pm
                                // Write our latest input state into a network packet.
                                //packetWriter2.Write(qH.inSeqNum);
                                //packetWriter2.Write(qH.outSeqNum);
                                //packetWriter2.Write((int)qH.type);
                                //Ship_Impulse_Message sim = (Ship_Impulse_Message)qH.data;
                                
                                //packetWriter2.Write((Vector2)sim.impulse);
                                
                               

                                // Send our input data to the server.
                                gamer.SendData(packetWriter2,
                                               SendDataOptions.InOrder, Game1.networkSession.Host);
                            }
                        }
                        break;
                    case Queue_type.QUEUE_SHIP_ROT:
                        //send to input queue

                        //packetWriter2.Write(qH.inSeqNum);
                        //        packetWriter2.Write(qH.outSeqNum);
                        //        packetWriter2.Write((int)qH.type);

                        //        Ship_Impulse_Message sim = (Ship_Impulse_Message)qH.data;
                                
                        //        packetWriter2.Write((Vector2)sim.impulse);

                        packetWriter2.Write(qH.inSeqNum);
                        packetWriter2.Write(qH.outSeqNum);
                        packetWriter2.Write((int)qH.type);
                        Ship_Rot_Message rotMessage = (Ship_Rot_Message)qH.data;
                        packetWriter2.Write(rotMessage.rotation);
                        Debug.WriteLine(" OutputQ - rotMessage.rotation = " + rotMessage.rotation);
                                
                        
                        if (Game1.networkSession != null)
                        {
                            if (Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                            }
                        }

                        if (Game1.networkSession.IsHost)
                        {
                            LocalNetworkGamer server = (LocalNetworkGamer)Game1.networkSession.Host;

                            server.SendData(packetWriter2, SendDataOptions.InOrder);
                        }

                        foreach (LocalNetworkGamer gamer in Game1.networkSession.LocalGamers)
                        {

                            if (!Game1.networkSession.IsHost)
                            {
                                // Write our latest input state into a network packet.
                                //packetWriter2.Write(qH.inSeqNum);
                                //packetWriter2.Write(qH.outSeqNum);
                                //packetWriter2.Write((int)qH.type);
                                //Ship_Rot_Message rotMessage = (Ship_Rot_Message)qH.data;
                                //packetWriter2.Write(rotMessage.rotation);
                              //  packetWriter2.Write((Vector2)qH.data);

                                // Send our input data to the server.
                                gamer.SendData(packetWriter2,
                                               SendDataOptions.InOrder, Game1.networkSession.Host);
                            }
                        }
                        break;
                    case Queue_type.QUEUE_PHYSICS_BUFFER:

                        if (Game1.networkSession != null)
                        {
                            if (Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                            }
                        }

                        if (Game1.networkSession != null)
                        {


                           // packetWriter2.Write(gamer.Id);
                            packetWriter2.Write(qH.inSeqNum);
                            packetWriter2.Write(qH.outSeqNum);
                            packetWriter2.Write((int)qH.type);
                            //Debug.WriteLine("qH.inSeqNum from PushToNetwork = " + qH.inSeqNum);
                            //Debug.WriteLine("qH.outSeqNum from PushToNetwork = " + qH.outSeqNum);
                            //Debug.WriteLine("qH.type from PushToNetwork = " + qH.type);
                            //PhysicsBuffer[] localPhysicsBuff = new PhysicsBuffer[];
                            PhysicsBuffer_Message PhysicsBuff_MessageInQueueHdr = new PhysicsBuffer_Message((PhysicsBuffer_Message)qH.data);

                            //get the count from the buffer
                            packetWriter2.Write(PhysicsBuff_MessageInQueueHdr.count);
                            //Debug.WriteLine("PhysicsBuff_MessageInQueueHdr.count = " + PhysicsBuff_MessageInQueueHdr.count);
                            PhysicsBuffer[] localPhysicsBuff = new PhysicsBuffer[PhysicsBuff_MessageInQueueHdr.count];
                            //get the the physics buffer struct of id, position , rotation out
                            localPhysicsBuff = PhysicsBuff_MessageInQueueHdr.pBuff;

                            for (int j = 0; j < PhysicsBuff_MessageInQueueHdr.count; j++)
                            {
                                PhysicsBuffer myPhysicsBuffer = new PhysicsBuffer();

                                myPhysicsBuffer.id = localPhysicsBuff[j].id;
                                myPhysicsBuffer.position = localPhysicsBuff[j].position;
                                myPhysicsBuffer.rotation = localPhysicsBuff[j].rotation;
                                packetWriter2.Write(myPhysicsBuffer.id);
                                packetWriter2.Write(myPhysicsBuffer.position);
                                packetWriter2.Write(myPhysicsBuffer.rotation);
                                //Debug.WriteLine("myPhysicsBuffer.id = " + myPhysicsBuffer.id);
                                //Debug.WriteLine("myPhysicsBuffer.position = " + myPhysicsBuffer.position);
                                //Debug.WriteLine("myPhysicsBuffer.rotation = " + myPhysicsBuffer.rotation);
                            }



                            if (Game1.networkSession != null)
                            {
                                if (Game1.networkSession.IsHost)
                                {
                                    LocalNetworkGamer server = (LocalNetworkGamer)Game1.networkSession.Host;

                                    server.SendData(packetWriter2, SendDataOptions.InOrder);
                                }
                            }

                            // if this is the client machine, need to send to server

                        }

                            // Send our input data to the server.
                            //if (Game1.networkSession != null)
                            //{
                            //    if (Game1.networkSession.IsHost)
                            //    {
                            //        foreach (LocalNetworkGamer gamer in Game1.networkSession.LocalGamers)
                            //        {
                            //            gamer.SendData(packetWriter2,
                            //                        SendDataOptions.InOrder, Game1.networkSession.);
                            //        }
                            //    }
                            //}
                        
                        

                        


                        break;
                    case Queue_type.QUEUE_EVENT:
                        
                        if (Game1.networkSession != null)
                        {
                            if (Game1.networkSession.IsHost)
                            {
                                InputQueue.add(qH);
                            }
                        }
                        if(Game1.networkSession != null)
                        {

                            
                                // Write our latest input state into a network packet.
                                packetWriter2.Write(qH.inSeqNum);
                                packetWriter2.Write(qH.outSeqNum);
                                packetWriter2.Write((int)qH.type);
                                // packetWriter2.Write((int)qH.data);
                                Event_Message eventMsg = new Event_Message((Event_Message)qH.data);
                                packetWriter2.Write(eventMsg.GameID_A);
                             //   Debug.WriteLine("eventMsg " + eventMsg.GameID_A);
                                packetWriter2.Write(eventMsg.GameID_B);
                              //  Debug.WriteLine("eventMsg " + eventMsg.GameID_B);
                                packetWriter2.Write(eventMsg.collision_pt);
                             //   Debug.WriteLine("eventMsg " + eventMsg.collision_pt);


                                if (Game1.networkSession != null)
                                {
                                    if (Game1.networkSession.IsHost)
                                    {
                                        LocalNetworkGamer server = (LocalNetworkGamer)Game1.networkSession.Host;

                                        server.SendData(packetWriter2, SendDataOptions.InOrder);
                                    }
                                }

                                // Send our input data to the server.
                               // gamer.SendData(packetWriter2,
                                 //              SendDataOptions.InOrder, Game1.networkSession.Host);
                            
                        }


                        //foreach (LocalNetworkGamer gamer in Game1.networkSession.LocalGamers)
                        //{

                        //    if (!Game1.networkSession.IsHost)
                        //    {
                        //        // Write our latest input state into a network packet.
                        //        packetWriter2.Write(qH.inSeqNum);
                        //        packetWriter2.Write(qH.outSeqNum);
                        //        packetWriter2.Write((int)qH.type);
                        //       // Ship_Rot_Message rotMessage = (Ship_Rot_Message)qH.data;
                        //       // packetWriter2.Write(rotMessage.rotation);
                        //      //  packetWriter2.Write((Vector2)qH.data);

                        //        // Send our input data to the server.
                        //        gamer.SendData(packetWriter2,
                        //                       SendDataOptions.InOrder, Game1.networkSession.Host);
                        //    }
                        //}
                        break;

                    default:
                        break;


                        

                        


                }
            }
        }

        public static void add(Message msg)
        {
            QueueHdr qH = new QueueHdr();
            qH.type = msg.getQueueType();
            qH.outSeqNum = OutputQueue.seqNumGlobal;
            qH.inSeqNum = -1;
            qH.data = msg;
            if (qH.type == Queue_type.QUEUE_SHIP_IMPULSE)
            {
                //Debug.WriteLine("what");
                //qH.data = (Vector2)qH.data;
                qH.data = (Ship_Impulse_Message)msg;
            }
            OutputQueue.seqNumGlobal++;
            outQ.Enqueue(qH);
        }


    }

    class PhysicsBuffer_Message_outQueue
    {
        public static void add(PhysicsBuffer_Message msg)
        {
            QueueHdr qH;
            qH.type = Queue_type.QUEUE_PHYSICS_BUFFER;
            qH.outSeqNum = OutputQueue.seqNumGlobal;
            qH.inSeqNum = -1;
            qH.data = msg;

            OutputQueue.seqNumGlobal++;

            // add the to input Queue
            OutputQueue.outQ.Enqueue(qH);
        }
    }

    class PhysicsBuffer_Message_inQueue
    {
        public static void add(PhysicsBuffer_Message msg)
        {
            QueueHdr qH;
            qH.type = Queue_type.QUEUE_PHYSICS_BUFFER;
            qH.outSeqNum = InputQueue.seqNumGlobal;
            qH.inSeqNum = -1;
            qH.data = msg;

            InputQueue.seqNumGlobal++;

            // add the to input Queue
            InputQueue.inQ.Enqueue(qH);
        }
    }
}


