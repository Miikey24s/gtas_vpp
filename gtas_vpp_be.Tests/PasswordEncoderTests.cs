using gtas_vpp_be.Service.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests;

public class PasswordEncoderTests
{
    [Fact]
    public void TripleDes_GoldenVector()
    {
        var encoder = CreateEncoder("ttpsolutions");

        var ciphertext = encoder.Encrypt("abc*123@");

        Assert.Equal("wiSEc6nf/dK/Vu0E738j8Q==", ciphertext);
    }

    [Fact]
    public void TripleDes_CustomKey_DifferentOutput()
    {
        var defaultEncoder = CreateEncoder("ttpsolutions");
        var customEncoder = CreateEncoder("different-key");

        var defaultCiphertext = defaultEncoder.Encrypt("abc*123@");
        var customCiphertext = customEncoder.Encrypt("abc*123@");

        Assert.NotEqual(defaultCiphertext, customCiphertext);
    }

    [Fact]
    public void TripleDes_NullKey_UsesDefault()
    {
        var encoder = CreateEncoder(null);

        var ciphertext = encoder.Encrypt("abc*123@");

        Assert.Equal("wiSEc6nf/dK/Vu0E738j8Q==", ciphertext);
    }

    [Fact]
    public void TripleDes_RoundTrip()
    {
        var encoder = CreateEncoder("ttpsolutions");
        const string plaintext = "abc*123@";

        var ciphertext = encoder.Encrypt(plaintext);
        var decrypted = encoder.Decrypt(ciphertext);

        Assert.Equal(plaintext, decrypted);
    }

    private static TripleDesPasswordEncoder CreateEncoder(string? key)
    {
        return new TripleDesPasswordEncoder(Options.Create(new PasswordEncoderOptions { Key = key }));
    }
}
