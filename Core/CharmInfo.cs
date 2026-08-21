namespace CloverMod.Core
{
    internal sealed class CharmInfo
    {
        public PowerupScript.Identifier Identifier { get; set; }

        public bool Unlocked { get; set; }

        public bool Equipped { get; set; }

        public bool InDrawer { get; set; }

        public bool Owned => Equipped || InDrawer;

        public PowerupScript.Modifier Modifier { get; set; }

        public int ChargesUsed { get; set; }

        public int ChargesMaximum { get; set; }
    }
}
