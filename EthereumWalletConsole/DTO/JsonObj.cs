using System.Collections.Generic;

namespace EthereumWalletConsole
{
    public class JsonObj
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }

        public List<object> @params { get; set; }

        public string id { get; set; }
        public string result { get; set; }
    }
}