using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmegaRace
{

    public enum Queue_type
    {
        QUEUE_PHYSICSBODY,
        QUEUE_GAMEOBJ,
        QUEUE_SCREENTEXTKILLS,
        QUEUE_USER_INPUT,
        QUEUE_TANK,
        QUEUE_SHIP_IMPULSE,
        QUEUE_SHIP_ROT,
        QUEUE_SHIP_BOMB,
        QUEUE_SHIP_MISSILE,
        QUEUE_PHYSICS_BUFFER,
        QUEUE_EVENT,
        QUEUE_TWILIGHT_ZONE

    }

    public struct QueueHdr
    {
        public int inSeqNum;
        public int outSeqNum;
        public Queue_type type;
        public object data;
    }

    class InputQueue
    {
        //data---------------------
        static public System.Collections.Generic.Queue<QueueHdr> inQ = new System.Collections.Generic.Queue<QueueHdr>();
        static public int seqNumGlobal = 3111;
        static public CollisionManager.Ship[] pShip = new CollisionManager.Ship[2];

        public static void Process3()
        {
            InputQueue pInputQueue = new InputQueue();
            int count = inQ.Count;

            for (int i = 0; i < count; i++)
            {
                //read header
                QueueHdr qH = inQ.Dequeue();

                Message msg = null;

                switch (qH.type)
                {
                    case Queue_type.QUEUE_SHIP_BOMB:
                        msg = new Ship_Create_Bomb_Message((Ship_Create_Bomb_Message)qH.data);
                        break;

                    case Queue_type.QUEUE_SHIP_MISSILE:
                        msg = new Ship_Create_Missile_Message((Ship_Create_Missile_Message)qH.data);
                        break;

                    case Queue_type.QUEUE_SHIP_IMPULSE:
                        msg = new Ship_Impulse_Message((Ship_Impulse_Message)qH.data);
                        break;
                    case Queue_type.QUEUE_SHIP_ROT:
                        msg = new Ship_Rot_Message((Ship_Rot_Message)qH.data);
                        break;

                    case Queue_type.QUEUE_PHYSICS_BUFFER:
                        msg = new PhysicsBuffer_Message((PhysicsBuffer_Message)qH.data);
                        break;

                    case Queue_type.QUEUE_EVENT:
                        msg = new Event_Message((Event_Message)qH.data);
                        break;
                        
                }

                msg.execute();
            }
        }

        public static void add(QueueHdr hdr)
        {
            QueueHdr qH;
            qH.type = hdr.type;
            qH.outSeqNum = InputQueue.seqNumGlobal;
            qH.inSeqNum = -1;
            qH.data = hdr.data;

            InputQueue.seqNumGlobal++;

            // add the to input Queue
            InputQueue.inQ.Enqueue(qH);
        }
    }

    
}
