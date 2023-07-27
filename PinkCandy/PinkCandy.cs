using InventorySystem.Items.Usables.Scp330;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TheRiptide
{
    public class PinkCandy
    {
        [PluginEntryPoint("Pink Candy", "1.0.0", "", "The Riptide")]
        public void OnEnabled()
        {
            PluginAPI.Events.EventManager.RegisterEvents(this);
        }

        [PluginEvent(ServerEventType.PlayerInteractScp330)]
        void OnInteractWithScp330(Player player, int unknown, bool flag1, bool flag2)
        {
            if (Random.value < 0.1)
            {
                Scp330Bag bag;
                if(Scp330Bag.TryGetBag(player.ReferenceHub, out bag))
                    bag.Candies[bag.Candies.Count - 1] = CandyKindID.Pink;
            }    
        }
    }
}
