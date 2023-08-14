using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Flashlight;
using InventorySystem.Items.Jailbird;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.Radio;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables;
using InventorySystem.Items.Usables.Scp1576;
using InventorySystem.Items.Usables.Scp244;
using InventorySystem.Items.Usables.Scp330;
using MEC;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Networking;

namespace TheRiptide
{
    public interface IItemSnapshot : ISnapshot
    {
        ItemBase Load(Player player);
        void Save(ItemBase item);
    }

    public static class ItemSnapshot
    {
        public static IItemSnapshot CreateSnapshot(ItemBase item)
        {
            if (item is Firearm firearm)
                return new FirearmSnapshot(firearm);
            else if (item is JailbirdItem jailbird)
                return new JailbirdSnapshot(jailbird);
            else if (item is MicroHIDItem micro)
                return new MicroHidSnapshot(micro);
            else if (item is RadioItem radio)
                return new RadioSnapshot(radio);
            else if (item is ThrowableItem throwable)
                return new ThrowableSnapShot(throwable);
            else if (item is Consumable consumable)
                return new ConsumableSnapshot(consumable);
            else if (item is Scp330Bag scp330)
                return new Scp330SnapShot(scp330);
            else if (item is Scp244Item scp244)
                return new Scp244SnapShot(scp244);
            else if (item is Scp1576Item scp1576)
                return new Scp1576Snapshot(scp1576);
            else if (item is Scp268 scp268)
                return new Scp268Snapshot(scp268);
            else if (item is UsableItem usable)
                return new UsableSnapshot(usable);
            else
                return new ItemBaseSnapshot(item);
        }
    }

    public class ItemBaseSnapshot : IItemSnapshot
    {
        protected ushort serial;
        protected ItemType type;

        public ItemBaseSnapshot(ItemBase item)
        {
            Save(item);
        }

        public virtual ItemBase Load(Player player)
        {
            return player.ReferenceHub.inventory.ServerAddItem(type, serial);
        }

        public virtual void Save(ItemBase item)
        {
            serial = item.ItemSerial;
            type = item.ItemTypeId;
        }
    }

    public class FirearmSnapshot : ItemBaseSnapshot
    {
        protected FirearmStatus status;

        public FirearmSnapshot(Firearm firearm)
            : base(firearm)
        {
            Save(firearm);
        }

        public override ItemBase Load(Player player)
        {
            Firearm firearm = base.Load(player) as Firearm;
            firearm.Status = status;
            return firearm;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            status = (item as Firearm).Status;
        }
    }

    public class FlashlightSnapshot : ItemBaseSnapshot
    {
        protected bool is_emitting;

        public FlashlightSnapshot(FlashlightItem flashlight)
            : base(flashlight)
        {
            Save(flashlight);
        }

        public override ItemBase Load(Player player)
        {
            FlashlightItem flashlight = base.Load(player) as FlashlightItem;
            flashlight.IsEmittingLight = is_emitting;
            new FlashlightNetworkHandler.FlashlightMessage(serial, is_emitting).SendToAuthenticated();
            return flashlight;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            is_emitting = (item as FlashlightItem).IsEmittingLight;
        }
    }

    public class JailbirdSnapshot:ItemBaseSnapshot
    {
        protected float damage_delt;
        protected int charges_performed;

        public JailbirdSnapshot(JailbirdItem jailbird)
            :base(jailbird)
        {
            Save(jailbird);
        }

        public override ItemBase Load(Player player)
        {
            JailbirdItem jailbird = base.Load(player) as JailbirdItem;
            jailbird._hitreg.TotalMeleeDamageDealt = damage_delt;
            jailbird.TotalChargesPerformed = charges_performed;
            jailbird._deterioration.RecheckUsage();
            return jailbird;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            JailbirdItem jailbird = item as JailbirdItem;
            damage_delt = jailbird._hitreg.TotalMeleeDamageDealt;
            charges_performed = jailbird.TotalChargesPerformed;
        }
    }

    public class MicroHidSnapshot : ItemBaseSnapshot
    {
        protected float energy;
        protected HidState state;
        protected HidUserInput input;
        //protected float elapsed;
        protected bool stopwatch_running;


        public MicroHidSnapshot(MicroHIDItem micro)
            : base(micro)
        {
            Save(micro);
        }

        public override ItemBase Load(Player player)
        {
            MicroHIDItem micro = base.Load(player) as MicroHIDItem;
            micro.RemainingEnergy = energy;
            micro.State = state;
            micro.UserInput = input;
            if (stopwatch_running)
                micro._stopwatch.Restart();
            micro.ServerSendStatus(HidStatusMessageType.EnergySync, micro.EnergyToByte);
            micro.ServerSendStatus(HidStatusMessageType.State, (byte)state);
            return micro;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            MicroHIDItem micro = item as MicroHIDItem;
            energy = micro.RemainingEnergy;
            state = micro.State;
            input = micro.UserInput;
            stopwatch_running = micro._stopwatch.IsRunning;
        }
    }

    public class RadioSnapshot : ItemBaseSnapshot
    {
        protected bool enabled;
        protected float battery;
        protected byte range;

        public RadioSnapshot(RadioItem radio)
            : base(radio)
        {
            Save(radio);
        }

        public override ItemBase Load(Player player)
        {
            RadioItem radio = base.Load(player) as RadioItem;
            radio._enabled = enabled;
            radio._battery = battery;
            radio._rangeId = range;
            radio.SendStatusMessage();
            return radio;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            RadioItem radio = item as RadioItem;
            enabled = radio._enabled;
            battery = radio._battery;
            range = radio._rangeId;
        }
    }

    public class ThrowableSnapShot:ItemBaseSnapshot
    {
        protected float destroy_time;
        protected bool already_fired;
        protected bool throw_stopwatch_running;
        protected bool cancel_stopwatch_running;

        public ThrowableSnapShot(ThrowableItem throwable)
            : base(throwable)
        {
            Save(throwable);
        }

        public override ItemBase Load(Player player)
        {
            ThrowableItem throwable = base.Load(player) as ThrowableItem;
            throwable._destroyTime = destroy_time;
            throwable._alreadyFired = already_fired;
            if (throw_stopwatch_running)
                throwable.ThrowStopwatch.Restart();
            if (cancel_stopwatch_running)
                throwable.CancelStopwatch.Restart();
            return throwable;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            ThrowableItem throwable = item as ThrowableItem;
            if (throwable._destroyTime == 0.0f)
                destroy_time = 0.0f;
            else
                destroy_time = throwable._destroyTime - Time.timeSinceLevelLoad;
            already_fired = throwable._alreadyFired;
            throw_stopwatch_running = throwable.ThrowStopwatch.IsRunning;
            cancel_stopwatch_running = throwable.CancelStopwatch.IsRunning;
        }
    }

    public class UsableSnapshot : ItemBaseSnapshot
    {
        protected float personal_cooldown;
        protected float global_cooldown;
        protected bool is_using;

        public UsableSnapshot(UsableItem usable)
            : base(usable)
        {
            Save(usable);
        }

        public override ItemBase Load(Player player)
        {
            UsableItem usable = base.Load(player) as UsableItem;
            usable.ServerSetPersonalCooldown(personal_cooldown);
            usable.ServerSetGlobalItemCooldown(global_cooldown);
            usable.IsUsing = is_using;
            return usable;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            UsableItem usable = item as UsableItem;
            global_cooldown = UsableItemsController.GlobalItemCooldowns[serial] - Time.timeSinceLevelLoad;
            personal_cooldown = UsableItemsController.GetHandler(item.Owner).PersonalCooldowns[type] - Time.timeSinceLevelLoad;
            is_using = usable.IsUsing;
        }
    }

    public class Scp268Snapshot : UsableSnapshot
    {
        protected bool worn;

        public Scp268Snapshot(Scp268 scp268)
            : base(scp268)
        {
            Save(scp268);
        }

        public override ItemBase Load(Player player)
        {
            Scp268 scp268 = base.Load(player) as Scp268;
            scp268._isWorn = worn;
            if (worn)
                scp268._stopwatch.Restart();
            return scp268;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            Scp268 scp268 = item as Scp268;
            worn = scp268._isWorn;
        }
    }

    public class Scp1576Snapshot:UsableSnapshot
    {
        protected float position;
        protected bool warning_triggered;
        protected bool stopwatch_running;

        public Scp1576Snapshot(Scp1576Item scp1576)
            : base(scp1576)
        {
            Save(scp1576);
        }

        public override ItemBase Load(Player player)
        {
            Scp1576Item scp1576 = base.Load(player) as Scp1576Item;
            scp1576._serverHornPos = position;
            scp1576._startWarningTriggered = warning_triggered;
            if (stopwatch_running)
                scp1576._useStopwatch.Restart();
            return scp1576;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            Scp1576Item scp1576 = item as Scp1576Item;
            position = scp1576._serverHornPos;
            warning_triggered = scp1576._startWarningTriggered;
            stopwatch_running = scp1576._useStopwatch.IsRunning;
        }
    }

    public class Scp244SnapShot:UsableSnapshot
    {
        protected bool primed;

        public Scp244SnapShot(Scp244Item scp244)
            : base(scp244)
        {
            Save(scp244);
        }

        public override ItemBase Load(Player player)
        {
            Scp244Item scp244 = base.Load(player) as Scp244Item;
            scp244._primed = primed;
            return scp244;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            Scp244Item scp244 = item as Scp244Item;
            primed = scp244._primed;
        }
    }


    public class Scp330SnapShot : UsableSnapshot
    {
        int selected;
        List<CandyKindID> candies;

        public Scp330SnapShot(Scp330Bag scp330)
            : base(scp330)
        {
            Save(scp330);
        }

        public override ItemBase Load(Player player)
        {
            Scp330Bag scp330 = base.Load(player) as Scp330Bag;
            scp330.SelectedCandyId = selected;
            scp330.Candies = candies.ToList();
            return scp330;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            Scp330Bag scp330 = item as Scp330Bag;
            selected = scp330.SelectedCandyId;
            candies = scp330.Candies.ToList();
        }
    }

    public class ConsumableSnapshot : UsableSnapshot
    {
        bool activated;
        bool stopwatch_running;

        public ConsumableSnapshot(Consumable consumable)
            : base(consumable)
        {
            Save(consumable);
        }

        public override ItemBase Load(Player player)
        {
            Consumable consumable = base.Load(player) as Consumable;
            consumable._alreadyActivated = activated;
            if (stopwatch_running)
                consumable._useStopwatch.Restart();
            return consumable;
        }

        public override void Save(ItemBase item)
        {
            base.Save(item);
            Consumable consumable = item as Consumable;
            activated = consumable._alreadyActivated;
            stopwatch_running = consumable._useStopwatch.IsRunning;
        }
    }
}
