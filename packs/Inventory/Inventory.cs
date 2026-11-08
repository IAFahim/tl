namespace Tl.Pack.Inventory
{
    public enum Item : byte
    {
        Wood,
        Stone,
        Plank,
        Potion,
        Sword,
        Coin,
        Key,
        Gem,
    }

    [TlState]
    public partial struct Bag
    {
        public int Wood;
        public int Stone;
        public int Plank;
        public int Potion;
        public int Sword;
        public int Coin;
        public int Key;
        public int Gem;

        public void Add(Item item, int count)
        {
            switch (item)
            {
                case Item.Wood: Wood += count; break;
                case Item.Stone: Stone += count; break;
                case Item.Plank: Plank += count; break;
                case Item.Potion: Potion += count; break;
                case Item.Sword: Sword += count; break;
                case Item.Coin: Coin += count; break;
                case Item.Key: Key += count; break;
                case Item.Gem: Gem += count; break;
            }
        }
    }

    public struct ItemDelta
    {
        public Item Item;
        public int Count;
    }

    [TlClip("Inventory/Give")]
    public struct GiveClip
    {
        public Item Item;
        public int Count;
    }

    [TlClip("Inventory/Craft")]
    public struct CraftClip
    {
        public Item Input;
        public int InputCount;
        public Item Output;
        public int OutputCount;
    }

    public struct PickupMarker
    {
        public Item Item;
        public int Count;
    }

    [TlKind(Accepts = KindEntries.Pulses, Mode = KindMode.Each)]
    public struct InventoryItems
    {
        public static void Apply(in ItemDelta delta, ref Bag bag) => bag.Add(delta.Item, delta.Count);
    }

    public struct GiveTrack : ITlTrack<GiveClip>
    {
        [Pulse(typeof(InventoryItems), On = ClipEvent.Start)]
        public static ItemDelta Give(in TlFrame<GiveClip> frame) => new ItemDelta { Item = frame.Clip.Item, Count = frame.Direction * frame.Clip.Count };
    }

    public struct CraftTrack : ITlTrack<CraftClip>
    {
        [Pulse(typeof(InventoryItems), On = ClipEvent.Start)]
        public static ItemDelta Consume(in TlFrame<CraftClip> frame) => new ItemDelta { Item = frame.Clip.Input, Count = -frame.Direction * frame.Clip.InputCount };

        [Pulse(typeof(InventoryItems), On = ClipEvent.End)]
        public static ItemDelta Produce(in TlFrame<CraftClip> frame) => new ItemDelta { Item = frame.Clip.Output, Count = frame.Direction * frame.Clip.OutputCount };
    }

    public struct Pickup : IMarker<PickupMarker>
    {
        [Pulse(typeof(InventoryItems), On = ClipEvent.Marker)]
        public static ItemDelta OnPickup(in MarkerFrame<PickupMarker> marker) => new ItemDelta { Item = marker.Marker.Item, Count = marker.Direction * marker.Marker.Count };
    }
}
