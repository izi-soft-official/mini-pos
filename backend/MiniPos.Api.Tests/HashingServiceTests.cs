using MiniPos.Api.Services;
using Xunit;

namespace MiniPos.Api.Tests
{
    public class HashingServiceTests
    {
        [Fact]
        public void Hash_And_Verify_Should_Work()
        {
            var service = new HashingService();
            string password = "P@ssw0rd!445";
            string wrongPass = "SomethingElse@dfD*";

            var hash = service.Hash(password);

            bool verified = service.VerifyHash(hash, password);
            bool wrong = service.VerifyHash(hash, wrongPass);

            Assert.False(string.IsNullOrWhiteSpace(hash));
            Assert.True(verified);
            Assert.False(wrong);
        }
    }
}
