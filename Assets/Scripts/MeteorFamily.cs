namespace VectorSlash
{
    /// <summary>Tracks every piece that came from one spawned meteor, for the Perfect Split bonus.</summary>
    public class MeteorFamily
    {
        /// <summary>True if the original meteor needed more than one cut.</summary>
        public bool multiCut;
        /// <summary>Meteors and fragments from this family still in play.</summary>
        public int alive;
        public int fragments;
        public int collected;
        /// <summary>Set when any piece hit the ship or flew off screen.</summary>
        public bool failed;
    }
}
