namespace EthereumWalletConsole
{
    public interface ITypeEncoder
    {
        byte[] Encode(object value);
    }
}