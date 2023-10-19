namespace LSLibLite.LS.Enums;

public static class GameEnumExtensions
{
    public static PackageVersion PAKVersion(this Game game)
    {
        return game switch
        {
            Game.DivinityOriginalSin    => PackageVersion.V7,
            Game.DivinityOriginalSinEE  => PackageVersion.V9,
            Game.DivinityOriginalSin2   => PackageVersion.V10,
            Game.DivinityOriginalSin2DE => PackageVersion.V13,
            Game.BaldursGate3           => PackageVersion.V18,
            _                           => PackageVersion.V18
        };
    }

    public static LSFVersion LSFVersion(this Game game)
    {
        return game switch
        {
            Game.DivinityOriginalSin    => Enums.LSFVersion.VerChunkedCompress,
            Game.DivinityOriginalSinEE  => Enums.LSFVersion.VerChunkedCompress,
            Game.DivinityOriginalSin2   => Enums.LSFVersion.VerExtendedNodes,
            Game.DivinityOriginalSin2DE => Enums.LSFVersion.VerExtendedNodes,
            Game.BaldursGate3           => Enums.LSFVersion.VerBG3AdditionalBlob,
            _                           => Enums.LSFVersion.VerBG3AdditionalBlob
        };
    }

    public static LSXVersion LSXVersion(this Game game)
    {
        return game switch
        {
            Game.DivinityOriginalSin    => Enums.LSXVersion.V3,
            Game.DivinityOriginalSinEE  => Enums.LSXVersion.V3,
            Game.DivinityOriginalSin2   => Enums.LSXVersion.V3,
            Game.DivinityOriginalSin2DE => Enums.LSXVersion.V3,
            Game.BaldursGate3           => Enums.LSXVersion.V4,
            _                           => Enums.LSXVersion.V4
        };
    }
}