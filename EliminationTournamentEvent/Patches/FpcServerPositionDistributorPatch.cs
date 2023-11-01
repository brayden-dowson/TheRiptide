using HarmonyLib;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.Visibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheRiptide.Patches
{
    [HarmonyPatch(typeof(FpcServerPositionDistributor))]
    class FpcServerPositionDistributorPatch
    {
        [HarmonyPatch(nameof(FpcServerPositionDistributor.WriteAll))]
        public static bool Prefix(ReferenceHub receiver, NetworkWriter writer)
        {
            ushort index1 = 0;
            bool flag;
            VisibilityController visibilityController;
            if (receiver.roleManager.CurrentRole is ICustomVisibilityRole currentRole1)
            {
                flag = true;
                visibilityController = currentRole1.VisibilityController;
            }
            else
            {
                flag = false;
                visibilityController = null;
            }
            //bool is_human = receiver.roleManager.CurrentRole.RoleTypeId.IsHuman();
            //bool is_tutorial = receiver.roleManager.CurrentRole.RoleTypeId != RoleTypeId.Tutorial;
            //bool in_match = SpectatorVisibility.InMatch(receiver);
            foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
            {
                if ((int)allHub.netId != (int)receiver.netId && allHub.roleManager.CurrentRole is IFpcRole currentRole2)
                {
                    bool isInvisible = flag && !visibilityController.ValidateVisibility(allHub);
                    if (!flag && !SpectatorVisibility.AllowSpectating(receiver, allHub))
                        isInvisible = true;
                    if (receiver.roleManager.CurrentRole.RoleTypeId != RoleTypeId.Overwatch && currentRole2.FpcModule.Role.RoleTypeId == RoleTypeId.Tutorial && !SpectatorVisibility.InMatch(allHub))
                        isInvisible = true;
                    FpcSyncData newSyncData = FpcServerPositionDistributor.GetNewSyncData(receiver, allHub, currentRole2.FpcModule, isInvisible);
                    if (!isInvisible)
                    {
                        FpcServerPositionDistributor._bufferPlayerIDs[index1] = allHub.PlayerId;
                        FpcServerPositionDistributor._bufferSyncData[index1] = newSyncData;
                        ++index1;
                    }
                }
            }
            writer.WriteUShort(index1);
            for (int index2 = 0; index2 < index1; ++index2)
            {
                writer.WriteRecyclablePlayerId(new RecyclablePlayerId(FpcServerPositionDistributor._bufferPlayerIDs[index2]));
                FpcServerPositionDistributor._bufferSyncData[index2].Write(writer);
            }
            return false;
        }
    }
}
