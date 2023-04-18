﻿namespace KLPlugins.DataExport.Track
{
    enum TrackType
    {
        BrandsHatch = 1,
        Spa = 2,
        Monza = 3,
        Misano = 4,
        PaulRicard = 5,
        Silverstone = 6,
        Hugaroring = 7,
        Nurburgring = 8,
        Barcelona = 9,
        Zolder = 10,
        Zandvoort = 11,
        Kyalami = 12,
        Bathurst = 13,
        LagunaSeca = 14,
        Suzuka = 15,
        Snetterton = 16,
        OultonPark = 17,
        DoningtonPark = 18,
        Imola = 19,
        COTA = 21,
        Indianapolis = 22,
        WatkinsGlen = 23,
        Valencia = 24,

        Unknown = 255
    }

    static class TrackExtensions
    {

        public static double SplinePosOffset(this TrackType track)
        {
            switch (track)
            {
                case TrackType.Silverstone:
                    return 0.0209485;
                case TrackType.Spa:
                    return 0.0038425;
                default:
                    return 0;
            }
        }

    }

}
