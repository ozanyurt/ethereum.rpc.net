using System.Collections.Generic;

namespace EthereumWalletConsole.DTO
{
    public class AbiObject
    {
        public bool constant { get; set; }
        public List<object> inputs { get; set; }
        public string name { get; set; }
        public List<object> outputs { get; set; }
        public string type { get; set; }
    }
}