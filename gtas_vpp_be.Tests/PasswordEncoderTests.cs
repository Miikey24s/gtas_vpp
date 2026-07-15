using gtas_vpp_be.Service.Helpers;
using Microsoft.Extensions.Options;
using Xunit;

namespace gtas_vpp_be.Tests;

public class PasswordEncoderTests
{
    private const string SyntheticTestKey = "unit-test-key-only";
    private const string AlternativeSyntheticTestKey = "different-unit-test-key";

    [Fact]
    public void TripleDes_SameInputAndKey_ProducesSameCiphertext()
    {
        var firstEncoder = CreateEncoder(SyntheticTestKey);
        var secondEncoder = CreateEncoder(SyntheticTestKey);

        var firstCiphertext = firstEncoder.Encrypt("synthetic-password");
        var secondCiphertext = secondEncoder.Encrypt("synthetic-password");

        Assert.Equal(firstCiphertext, secondCiphertext);
        Assert.NotEqual("synthetic-password", firstCiphertext);
    }

    [Fact]
    public void TripleDes_CustomKey_DifferentOutput()
    {
        var defaultEncoder = CreateEncoder(SyntheticTestKey);
        var customEncoder = CreateEncoder(AlternativeSyntheticTestKey);

        var defaultCiphertext = defaultEncoder.Encrypt("synthetic-password");
        var customCiphertext = customEncoder.Encrypt("synthetic-password");

        Assert.NotEqual(defaultCiphertext, customCiphertext);
    }

    [Fact]
    public void TripleDes_NullKey_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => CreateEncoder(null));
    }

    [Fact]
    public void TripleDes_RoundTrip()
    {
        var encoder = CreateEncoder(SyntheticTestKey);
        const string plaintext = "synthetic-password";

        var ciphertext = encoder.Encrypt(plaintext);
        var decrypted = encoder.Decrypt(ciphertext);

        Assert.Equal(plaintext, decrypted);
    }

    private static TripleDesPasswordEncoder CreateEncoder(string? key)
    {
        return new TripleDesPasswordEncoder(Options.Create(new PasswordEncoderOptions { Key = key }));
    }
}
