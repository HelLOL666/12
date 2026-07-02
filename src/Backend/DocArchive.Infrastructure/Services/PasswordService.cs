using DocArchive.Application.Interfaces;
using Isopoh.Cryptography.Argon2;

namespace DocArchive.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password) => Argon2.Hash(password);

    public bool VerifyPassword(string password, string hash) => Argon2.Verify(hash, password);
}
