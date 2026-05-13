using System.Security.Cryptography;
using Tessera.Channels;

namespace Tessera.Channels.Tests;

public class PepperProviderTests
{
    [Fact]
    public async Task StaticPepper_RoundTrips()
    {
        var pepper = RandomNumberGenerator.GetBytes(32);
        var provider = new StaticPepperProvider(pepper);

        var read = await provider.GetPepperAsync();
        Assert.Equal(pepper, read.ToArray());
    }

    [Fact]
    public void StaticPepper_ShortPepper_Throws()
    {
        Assert.Throws<ArgumentException>(() => new StaticPepperProvider(new byte[16]));
        Assert.Throws<ArgumentException>(() => new StaticPepperProvider(new byte[31]));
    }

    [Fact]
    public void EnvironmentPepper_MissingVariable_Throws()
    {
        var name = "ZKP_TEST_PEPPER_" + Guid.NewGuid().ToString("N");
        var provider = new EnvironmentPepperProvider(name);
        Assert.Throws<InvalidOperationException>(() => provider.GetPepperAsync().AsTask().GetAwaiter().GetResult());
    }

    [Fact]
    public void EnvironmentPepper_InvalidBase64_Throws()
    {
        var name = "ZKP_TEST_PEPPER_" + Guid.NewGuid().ToString("N");
        try
        {
            Environment.SetEnvironmentVariable(name, "not!valid!base64!");
            var provider = new EnvironmentPepperProvider(name);
            Assert.Throws<InvalidOperationException>(() =>
                provider.GetPepperAsync().AsTask().GetAwaiter().GetResult());
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public async Task EnvironmentPepper_ValidBase64_Decodes()
    {
        var name = "ZKP_TEST_PEPPER_" + Guid.NewGuid().ToString("N");
        var raw = RandomNumberGenerator.GetBytes(32);
        try
        {
            Environment.SetEnvironmentVariable(name, Convert.ToBase64String(raw));
            var provider = new EnvironmentPepperProvider(name);
            var read = await provider.GetPepperAsync();
            Assert.Equal(raw, read.ToArray());
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Fact]
    public void EnvironmentPepper_ShortPepper_Throws()
    {
        var name = "ZKP_TEST_PEPPER_" + Guid.NewGuid().ToString("N");
        try
        {
            Environment.SetEnvironmentVariable(name, Convert.ToBase64String(new byte[16]));
            var provider = new EnvironmentPepperProvider(name);
            Assert.Throws<InvalidOperationException>(() =>
                provider.GetPepperAsync().AsTask().GetAwaiter().GetResult());
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }
}
