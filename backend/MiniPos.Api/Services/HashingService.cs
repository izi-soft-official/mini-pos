using Isopoh.Cryptography.Argon2;
using MiniPos.Api.Interfaces;

namespace MiniPos.Api.Services
{
    public class HashingService:IHashingService
    {
        public string Hash(string password)
        {
            return Argon2.Hash(password);
        }

        public bool VerifyHash(string hash, string password)
        {
            return Argon2.Verify(hash, password);
        }
    }
}
