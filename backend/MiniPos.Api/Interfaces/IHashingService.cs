namespace MiniPos.Api.Interfaces
{
    public interface IHashingService
    {
        string Hash(string password);
        bool VerifyHash(string hash, string password);
    }
}
