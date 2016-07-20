using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace EthereumWalletConsole
{
    class Program
    {

        static void Main(string[] args)
        {

            try
            {
                Program.Run().Wait();
            }
            catch (AggregateException aEx)
            {
                foreach (Exception exception in aEx.InnerExceptions)
                {
                    Console.WriteLine(exception.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();


        }

        public static async Task Run()
        {
            // https://github.com/ethereum/wiki/wiki/JSON-RPC

            var httpClient = new HttpClient();
            var json = new JsonObj
            {
                jsonrpc = "2.0",
                method = "web3_clientVersion",
                @params = new List<object>(),
                id = "1"
            };
            var jsonToSend = JsonConvert.SerializeObject(json, Formatting.None);
            Console.WriteLine(await PostRpc(jsonToSend, httpClient));

            /*
             eth_accounts
             Returns a list of addresses owned by client.
             
            */
            json = new JsonObj
            {
                jsonrpc = "2.0",
                method = "eth_accounts",
                @params = new List<object>(),
                id = "1"
            };
            jsonToSend = JsonConvert.SerializeObject(json, Formatting.None);
            var accounts = await PostRpc(jsonToSend, httpClient);

            var dynAccounts = JsonConvert.DeserializeObject<dynamic>(accounts);
            var firstAccount = dynAccounts.result[0];
            Console.WriteLine(accounts);


            //Returns the balance of the account of given address.
            json = new JsonObj
            {
                jsonrpc = "2.0",
                method = "eth_getBalance",
                @params = new List<object> { firstAccount.ToString(), "latest" },
                id = "1"
            };
            jsonToSend = JsonConvert.SerializeObject(json, Formatting.None);
            //var json2 = "{jsonrpc = \"2.0\", method = \"eth_getBalance\", @params = \"[0x36bed893e7f4b75852f518d4422ef40b60c7b058, \"latest\"]\", id = \"1\"}";
            Console.WriteLine(await PostRpc(jsonToSend, httpClient));

            json = new JsonObj
            {
                jsonrpc = "2.0",
                method = "personal_unlockAccount",
                id = "1",
                @params = new List<object> { firstAccount.ToString(), "1234567890" }

            };
            jsonToSend = JsonConvert.SerializeObject(json, Formatting.None);
            Console.WriteLine(await PostRpc(jsonToSend, httpClient));

            var contract = @"
contract DepositAddress{
    string userId;
    address coldStorageAddress;
    address owner;
    event ReceivedEther(uint amount, uint time, string userId);

    function DepositAddress(string _userId, address _coldStorageAddress){
        coldStorageAddress =_coldStorageAddress;
        userId = _userId;
        owner = msg.sender;
    }
    /* fallback function
       this function is executed whenever the contract receives plain Ether (without data)
       https://solidity.readthedocs.io/en/latest/contracts.html#fallback-function
    */
    function(){
        ReceivedEther(msg.value,block.timestamp,userId);
        coldStorageAddress.send(msg.value);
    }

    function kill() { if (msg.sender == owner) selfdestruct(owner); }
}
";

            json = new JsonObj
            {
                jsonrpc = "2.0",
                method = "eth_compileSolidity",
                id = "1",
                @params = new List<object> { contract.Replace(Environment.NewLine, " ") }

            };
            jsonToSend = JsonConvert.SerializeObject(json, Formatting.None);
            var compilerResults = await PostRpc(jsonToSend, httpClient);

            var dynResult = JsonConvert.DeserializeObject<dynamic>(compilerResults);
            var code = dynResult.result.DepositAddress.code;
            var abi = dynResult.result.DepositAddress.info.abiDefinition.ToString();
            var abiDefinition = JsonConvert.DeserializeObject<List<AbiObject>>(abi);

            Console.WriteLine(compilerResults);


            var jsonWithParamData = new JsonObj()
            {
                jsonrpc = "2.0",
                method = "eth_estimateGas",
                id = "1",
                @params = new List<object> { new ParamData { data = code, from = firstAccount } }

            };

            jsonToSend = JsonConvert.SerializeObject(jsonWithParamData, Formatting.None);

            //var estimateGasResult = await PostRpc("{\"jsonrpc\":\"2.0\",\"method\": \"eth_estimateGas\", \"params\": [{\"from\": \"0xeb85a5557e5bdc18ee1934a89d8bb402398ee26a\", \"data\": \"0x6060604052605f8060106000396000f3606060405260e060020a6000350463c6888fa18114601a575b005b60586004356007810260609081526000907f24abdb5865df5079dcc5ac590ff6f01d5c16edbc5fab4e195d9febd1114503da90602090a15060070290565b5060206060f3\"}], \"id\": 5}", httpClient);
            var estimateGasResult = await PostRpc(jsonToSend, httpClient);
            var estimatedGas = JsonConvert.DeserializeObject<JsonObj>(estimateGasResult).result;
            Console.WriteLine(estimateGasResult);
            var userId = "1";
            var coldStorageAddress = dynAccounts.result[1];
            jsonWithParamData = new JsonObj()
            {
                jsonrpc = "2.0",
                method = "eth_sendTransaction",
                id = "1",
                @params = new List<object> { userId, coldStorageAddress, new ParamData { data = code, from = firstAccount, gas = estimatedGas } }
            };

            jsonToSend = JsonConvert.SerializeObject(jsonWithParamData, Formatting.None);

            var sendTransactionResult = await PostRpc(jsonToSend, httpClient);
            var txHash = JsonConvert.DeserializeObject<JsonObj>(sendTransactionResult).result;
            Console.WriteLine(sendTransactionResult);
        }

        private static async Task<string> PostRpc(string json, HttpClient httpClient)
        {


            var content = new StringContent(json);

            content.Headers.ContentType = new MediaTypeHeaderValue("text/json");
            var response = await httpClient.PostAsync("http://localhost:8545", content);
            var returnValue = await response.Content.ReadAsStringAsync();

            return returnValue;
        }
    }
    public class AbiObject
    {
        public bool constant { get; set; }
        public List<object> inputs { get; set; }
        public string name { get; set; }
        public List<object> outputs { get; set; }
        public string type { get; set; }
    }

    //public class JsonObj
    //{
    //    public string jsonrpc { get; set; }
    //    public string method { get; set; }

    //    public List<string> @params { get; set; }

    //    public string id { get; set; }
    //    public string result { get; set; }
    //}

    public class JsonObj
    {
        public string jsonrpc { get; set; }
        public string method { get; set; }

        public List<object> @params { get; set; }

        public string id { get; set; }
        public string result { get; set; }
    }


    public class ParamData
    {
        public string from { get; set; }
        public string data { get; set; }
        public string gas { get; set; }

    }
}
