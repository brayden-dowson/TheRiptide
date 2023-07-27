using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide
{
    class JesterFirearmActionModule : IActionModule
    {
        public float CyclicRate => 0.0f;

        public bool IsTriggerHeld => false;

        public FirearmStatus PredictedStatus => new FirearmStatus(0, FirearmStatusFlags.None, 0);

        public bool Standby => true;

        public ActionModuleResponse DoClientsideAction(bool isTriggerPressed)
        {
            return ActionModuleResponse.Idle;
        }

        public bool ServerAuthorizeDryFire()
        {
            return false;
        }

        public bool ServerAuthorizeShot()
        {
            return false;
        }
    }
}
