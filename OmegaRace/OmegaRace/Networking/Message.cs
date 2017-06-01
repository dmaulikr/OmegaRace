using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace OmegaRace
{
    abstract class Message
    {
        public abstract void execute();

        public abstract Queue_type getQueueType();
    }

    abstract class Ship_Message : Message
    {
        // data -------------
        public CollisionManager.PlayerID id;

        public Ship_Message(CollisionManager.Player p)
        {
            this.id = p.id;
        }


        public Ship_Message(CollisionManager.PlayerID _id)
        {
            this.id = _id;
        }



    }

    class PhysicsBuffer_Message : Message
    {

        //data ----------------------

        public int count;
        public PhysicsBuffer[] pBuff;

        public static PhysicsBuffer_Message pBuffGlobal = null;
        //---------------------------------------------

        public PhysicsBuffer_Message(PhysicsBuffer_Message msg)
        {
            this.count = msg.count;
            this.pBuff = msg.pBuff;
        }

        public PhysicsBuffer_Message(ref PhysicsBuffer[] physicsBuff)
        {
            this.count = physicsBuff.Length;
            this.pBuff = physicsBuff;
        }

        public override Queue_type getQueueType()
        {
            return Queue_type.QUEUE_PHYSICS_BUFFER;
        }

        public override void execute()
        {
            
            pBuffGlobal = this;
           // Debug.WriteLine("pBuffGlobal.count = " + pBuffGlobal.count);
        }
    }

    public struct PhysicsBuffer
    {
        public int id;
        public Vector2 position;
        public float rotation;
    }

    class Ship_Impulse_Message : Ship_Message
    {
        //data-----------------
        public Vector2 impulse;

        public Ship_Impulse_Message(Ship_Impulse_Message msg)
            : base(msg.id)
        {
            this.impulse = msg.impulse;
        }

        public Ship_Impulse_Message(CollisionManager.Player p, Vector2 Impulse)
            : base(p)
        {
            this.impulse.X = Impulse.X;
            this.impulse.Y = Impulse.Y;
        }

        override public Queue_type getQueueType()
        {
            return Queue_type.QUEUE_SHIP_IMPULSE;
        }

        override public void execute()
        {
            //PlayerManager pm = new PlayerManager
            CollisionManager.Player player = PlayerManager.Instance().getPlayer(this.id);
            player.playerShip.physicsObj.body.ApplyLinearImpulse(this.impulse, player.playerShip.physicsObj.body.Position);
        }
    }

    class Ship_Rot_Message : Ship_Message
    {
        // data ----------------
        public float rotation;

        public Ship_Rot_Message(Ship_Rot_Message msg)
            : base(msg.id)
        {
            this.rotation = msg.rotation;
        }

        public Ship_Rot_Message(CollisionManager.Player player, float f)
           :base(player)
        {
            this.rotation = f;
        }

        public override Queue_type getQueueType()
        {
            return Queue_type.QUEUE_SHIP_ROT;
        }

        public override void execute()
        {
            CollisionManager.Player player = PlayerManager.Instance().getPlayer(this.id);
            player.playerShip.physicsObj.body.Rotation += this.rotation;
        }
    }

    class Ship_Create_Bomb_Message : Ship_Message
    {
        public Ship_Create_Bomb_Message(Ship_Create_Bomb_Message msg)
            : base(msg.id)
        {

        }

        public Ship_Create_Bomb_Message(CollisionManager.Player p)
            : base(p)
        {

        }

        public override Queue_type getQueueType()
        {
            return Queue_type.QUEUE_SHIP_BOMB;
        }

        public override void execute()
        {
            CollisionManager.GameObjManager.Instance().createBomb(this.id);
        }
    }

    class Ship_Create_Missile_Message : Ship_Message
    {
        // data-------------

        public Ship_Create_Missile_Message(Ship_Create_Missile_Message msg)
            : base(msg.id)
        {

        }

        public Ship_Create_Missile_Message(CollisionManager.Player p)
            : base(p)
        {

        }

        public override Queue_type getQueueType()
        {
            return Queue_type.QUEUE_SHIP_MISSILE;
        }

        public override void execute()
        {
            CollisionManager.Player player = PlayerManager.Instance().getPlayer(this.id);
            player.createMissile();
        }
    }

    class Event_Message : Message
    {
        //data--------------
        public Vector2 collision_pt;
        public int GameID_A;
        public int GameID_B;
        //CollisionManager.GameObject.GameID A;
        //CollisionManager.GameObject B;

        //GameObject A = (GameObject)contact.GetFixtureA().GetUserData();
        //    GameObject B = (GameObject)contact.GetFixtureB().GetUserData();

        public Event_Message(int _idA, int _idB, Vector2 ptA)
        {
            this.GameID_A = _idA;
            this.GameID_B = _idB;
            this.collision_pt = ptA;
        }

        public Event_Message(Event_Message msg)
        {
            this.GameID_A = msg.GameID_A;
            this.GameID_B = msg.GameID_B;
            this.collision_pt = msg.collision_pt;
        }

        public override Queue_type getQueueType()
        {
            return Queue_type.QUEUE_EVENT;
        }

        public override void execute()
        {
            CollisionManager.GameObject A = CollisionManager.GameObjManager.Instance().FindByID(this.GameID_A);
            CollisionManager.GameObject B = CollisionManager.GameObjManager.Instance().FindByID(this.GameID_B);
            Vector2 ptA = this.collision_pt;

            if (A != null && B != null)
            {
                Debug.Assert(A != null);
                Debug.Assert(B != null);

                System.Console.Write("event --> send mc point A:{0} B:{1} {2}\n", A.GameID, B.GameID, ptA);
                if (A.CollideAvailable == true && B.CollideAvailable == true)
                {
                    if (A.type < B.type)
                    {
                        A.Accept(B, ptA);
                    }
                    else
                    {
                        B.Accept(A, ptA);
                    }
                }

                if (A.type == CollisionManager.GameObjType.p1missiles || A.type == CollisionManager.GameObjType.p2missiles)
                {
                    A.CollideAvailable = false;
                }

                if (B.type == CollisionManager.GameObjType.p1missiles || B.type == CollisionManager.GameObjType.p2missiles)
                {
                    B.CollideAvailable = false;
                }
            }


        }

    }
}
