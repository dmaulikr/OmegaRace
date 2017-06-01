using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Box2D.XNA;
using CollisionManager;

namespace OmegaRace
{
    class PhysicsMan : Manager
    {
        private static PhysicsMan instance;
        private int count;

        private PhysicsMan()
        {

        }

        public static PhysicsMan Instance()
        {
            if (instance == null)
                instance = new PhysicsMan();
            return instance;
        }

        public void addPhysicsObj(GameObject _gameObj,Body _body)
        {
            PhysicsObj obj = new PhysicsObj(_gameObj, _body);
            _gameObj.physicsObj = obj;

            this.privActiveAddToFront((ManLink)obj, ref this.active);
            count++;
        }

        public static void PushToBuffer(ref PhysicsBuffer[] physicsBuff)
        {
            PhysicsMan pMan = PhysicsMan.Instance();

            ManLink ptr = pMan.active;
            PhysicsObj physNode = null;
            Body body = null;

            int i = 0;
            while (ptr != null)
            {
                physNode = (PhysicsObj)ptr;
                body = physNode.body;

                //Push to buffer
                physicsBuff[i].id = physNode.gameObj.GameID;
                physicsBuff[i].position = body.Position;
                physicsBuff[i].rotation = body.GetAngle();

                i++;
                ptr = ptr.next;

            }
        }

        public static void Update(ref PhysicsBuffer_Message pPBMsg)
        {
            if (pPBMsg == null)
                return;

            PhysicsMan pMan = PhysicsMan.Instance();
            //PhysicsObj physNode = null;
            GameObject gameObj = null;

            for (int i = 0; i < pPBMsg.count; i++)
            {
                //physNode = pMan.FindByID(pPBMsg.pBuff[i].id);
                gameObj = GameObjManager.Instance().FindByID(pPBMsg.pBuff[i].id);
                // We might have removed since the last update, so if Im null, do nothing.
                if (gameObj != null)
                {
                    gameObj.pushPhysics(pPBMsg.pBuff[i].rotation, pPBMsg.pBuff[i].position);
                }
                else
                {
                    // do nothing
                }
            }

        }

        public void Update()
        {
            ManLink ptr = this.active;
            PhysicsObj physNode = null;
            Body body = null;


            while (ptr != null)
            {
                physNode = (PhysicsObj)ptr;
                body = physNode.body;

                physNode.gameObj.pushPhysics(body.GetAngle(), body.Position);

                ptr = ptr.next;
            }

        }

        public int getCount()
        {
            return count;
        }

        public void removePhysicsObj(PhysicsObj _obj)
        {
            this.privActiveRemoveNode((ManLink)_obj, ref this.active);
            count--;

        }

        protected override object privGetNewObj()
        {
            throw new NotImplementedException();
        }

    }
}
