using MiiAnimator.MiiData.Domain;

namespace MiiAnimator.Editing;

public static class DefaultMii
{
    /// <summary>The Mii that is always loaded when the animator starts.</summary>
    public static Mii Create() =>
        new()
        {
            Name = new MiiName("Animator"),
            CreatorName = new MiiName("MiiAnim"),
            MiiId = 0x8A11_2026,
            SystemId = 0x4D49_4931,
            IsGirl = false,
            MiiFavoriteColor = MiiFavoriteColor.Blue,
            Height = new MiiScale(64),
            Weight = new MiiScale(64),
            MiiFacialFeatures = new MiiFacialFeatures(MiiFaceShape.Bread, MiiSkinColor.Light, MiiFacialFeature.None, false, false),
            MiiHair = new MiiHair(33, MiiHairColor.Brown, false),
            MiiEyebrows = new MiiEyebrow(1, 4, MiiHairColor.Brown, 4, 10, 2),
            MiiEyes = new MiiEye(4, 4, 12, MiiEyeColor.Black, 4, 2),
            MiiNose = new MiiNose(MiiNoseType.Default, 4, 9),
            MiiLips = new MiiLip(23, MiiLipColor.Skin, 4, 13),
        };

    /// <summary>Stand-in preview Mii for an actor (0 = the default Mii), so extra Miis look different from the main one.</summary>
    public static Mii ForActor(int actor)
    {
        if (actor <= 0)
            return Create();
        var colors = new[] { MiiFavoriteColor.Red, MiiFavoriteColor.Green, MiiFavoriteColor.Orange, MiiFavoriteColor.Purple };
        var hairColors = new[] { MiiHairColor.Black, MiiHairColor.Blonde, MiiHairColor.Red, MiiHairColor.Grey };
        var i = (actor - 1) % colors.Length;
        var mii = Create();
        mii.Name = new MiiName($"Actor {actor}");
        mii.MiiId = 0x8A11_2026u + (uint)actor;
        mii.MiiFavoriteColor = colors[i];
        mii.MiiHair = new MiiHair(i % 2 == 0 ? 12 : 45, hairColors[i], false);
        mii.MiiEyebrows = new MiiEyebrow(1, 4, hairColors[i], 4, 10, 2);
        mii.MiiFacialFeatures = new MiiFacialFeatures(MiiFaceShape.Bread, i % 2 == 0 ? MiiSkinColor.Yellow : MiiSkinColor.Light, MiiFacialFeature.None, false, false);
        return mii;
    }
}
