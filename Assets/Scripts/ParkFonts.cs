using UnityEngine;

namespace PeacePark
{
    public static class ParkFonts
    {
        public static Font Regular=>Resources.Load<Font>(ParkLanguage.IsKorean ? "Fonts/BaskinRobbins-Regular" : "Fonts/ComicRelief-Regular");
        public static Font Bold=>BoldFor(ParkLanguage.IsKorean);
        internal static Font BoldFor(bool korean)=>Resources.Load<Font>(korean ? "Fonts/BaskinRobbins-Bold" : "Fonts/ComicRelief-Bold");
    }
}
