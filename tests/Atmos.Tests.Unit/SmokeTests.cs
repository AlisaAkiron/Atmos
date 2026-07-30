namespace Atmos.Tests.Unit;

public class SmokeTests
{
    [Test]
    public async Task TUnit_Runs()
    {
        // Summed at runtime: the TUnit analyzer rejects Assert.That on a constant
        var values = new[] { 1, 1 };

        await Assert.That(values.Sum()).IsEqualTo(2);
    }
}
