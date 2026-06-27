namespace gtas_vpp_be.Service.Helpers
{
    public interface IPasswordEncoder
    {
        string Encrypt(string plaintext);

        string Decrypt(string ciphertext);
    }
}
