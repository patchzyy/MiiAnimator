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
}
