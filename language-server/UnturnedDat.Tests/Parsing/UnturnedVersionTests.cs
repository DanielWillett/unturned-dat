using UnturnedDat.Data;

namespace UnturnedDat.Tests.Parsing;

public class UnturnedVersionTests
{
    [Test]
    [TestCase("1.2.3.4", "1.2.3.4")]
    [TestCase("0.0.0.0", "0.0.0.0")]
    [TestCase("99.99.99.99", "99.99.99.99")]
    [TestCase("255.255.255.255", "255.255.255.255")]
    [TestCase(" 255.255.255.255 ", "255.255.255.255")]
    [TestCase(" 255 . 255 . 255 . 255 ", "255.255.255.255")]
    [TestCase("o:1.2.3.4", "1.2.3.4")]
    [TestCase("o:0.0.0.0", "0.0.0.0")]
    [TestCase("o:255.255.255.255", "255.255.255.255")]
    [TestCase("O:1.2.3.4", "1.2.3.4")]
    [TestCase("O:0.0.0.0", "0.0.0.0")]
    [TestCase("O:255.255.255.255", "255.255.255.255")]
    [TestCase(" O: 255 . 255 . 255 . 255 ", "255.255.255.255")]
    [TestCase(" o: 255 . 255 . 255 . 255 ", "255.255.255.255")]
    [TestCase("o:3.22.1.0", "o:3.22.1.0")]
    public void Parse(string valid, string expected)
    {
        UnturnedVersion parsed = UnturnedVersion.Parse(valid);
        Assert.That(parsed.ToString(true), Is.EqualTo(expected));
    }

    [Test]
    public void ParseInvalid([Values(
        "",
        "1",
        "1.2",
        "1.2.3",
        "1.2.3.4.5",
        "1..2.3.4",
        "1.2..3.4",
        "1.2.3..4",
        ".1.2.3.4",
        "1.2.3.4.",
        ".",
        "...",
        "o1.2.3.4",
        "O1.2.3.4",
        "a:1.2.3.4",
        ":1.2.3.4",
        "1.2.3.4:o",
        "1234"
        )] string invalid)
    {
        Assert.Throws<FormatException>(() =>
        {
            UnturnedVersion.Parse(invalid);
        });
    }

    [Test]
    public void ToString([Values(
        "3.26.5.9",
        "3.16.5.9",
        "1.2.3.4",
        "0.0.0.0",
        "10.10.10.10",
        "255.255.255.255"
        )] string version)
    {
        UnturnedVersion v = UnturnedVersion.Parse(version);
        Assert.That(v.ToString(), Is.EqualTo(version));
    }

    [Test]
    public void ToStringPrefix([Values(
        "3.8.5.9",
        "o:3.20.5.9",
        "3.20.5.9",
        "1.2.3.4"
        )] string version)
    {
        UnturnedVersion v = UnturnedVersion.Parse(version);
        Assert.That(v.ToString(true), Is.EqualTo(version));
    }

    [Test]
    public void ToString_LeavesPrefixOutForFirstModernVersion()
    {
        Assert.That(UnturnedVersion.TryPack("3.19.14.0", out uint packed));
        UnturnedVersion v = new UnturnedVersion(packed, true);
        Assert.That(v.ToString(true), Is.EqualTo("3.19.14.0"));
    }

    [Test]
    public void ToString_IncludesPrefixForAmbiguousLegacyVersion()
    {
        Assert.That(UnturnedVersion.TryPack("3.20.0.0", out uint packed));
        UnturnedVersion v = new UnturnedVersion(packed, false);
        Assert.That(v.ToString(true), Is.EqualTo("o:3.20.0.0"));
    }

    [Test]
    public void ToString_ExcludesPrefixForAmbiguousModernVersion()
    {
        Assert.That(UnturnedVersion.TryPack("3.20.0.0", out uint packed));
        UnturnedVersion v = new UnturnedVersion(packed, true);
        Assert.That(v.ToString(true), Is.EqualTo("3.20.0.0"));
    }

    [Test]
    public void ToString_ExcludesPrefixForUnambigousLegacyVersion()
    {
        Assert.That(UnturnedVersion.TryPack("3.18.0.0", out uint packed));
        UnturnedVersion v = new UnturnedVersion(packed, false);
        Assert.That(v.ToString(true), Is.EqualTo("3.18.0.0"));
    }

    [Test]
    public void ToString_ExcludesPrefixForUnambiguousModernVersion()
    {
        Assert.That(UnturnedVersion.TryPack("3.31.0.0", out uint packed));
        UnturnedVersion v = new UnturnedVersion(packed, true);
        Assert.That(v.ToString(true), Is.EqualTo("3.31.0.0"));
    }

    [Test]
    public void Compare_OldWithOld()
    {
        UnturnedVersion a = UnturnedVersion.Parse("3.17.10.1");
        UnturnedVersion b = UnturnedVersion.Parse("3.15.5.2");

        Assert.That(a > b);
        Assert.That(a >= b);
        Assert.That(b < a);
        Assert.That(b <= a);
    }

    [Test]
    public void Compare_NewWithNew()
    {
        UnturnedVersion a = UnturnedVersion.Parse("3.22.8.1");
        UnturnedVersion b = UnturnedVersion.Parse("3.22.6.3");

        Assert.That(a > b);
        Assert.That(a >= b);
        Assert.That(b < a);
        Assert.That(b <= a);
    }

    [Test]
    public void Compare_OldWithNew()
    {
        UnturnedVersion a = UnturnedVersion.Parse("3.22.8.1");
        UnturnedVersion b = UnturnedVersion.Parse("o:3.29.6.3");

        Assert.That(a > b);
        Assert.That(a >= b);
        Assert.That(b < a);
        Assert.That(b <= a);
    }
}