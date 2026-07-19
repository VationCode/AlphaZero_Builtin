namespace Alpha.Player
{
    public class PlayerContext
    {
        public bool IsInCombat { get; private set; }

        public void SetCombat(bool p_isInCombat)
        {
            IsInCombat = p_isInCombat;
        }
    }
}
