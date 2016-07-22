using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;


namespace EthereumWalletConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: Document running the application with the options
            // PS C:\Users\Ozan > geth--dev--rpc--rpcapi = "db,eth,net,web3,personal"--rpcport "8545"--rpcaddr "0.0.0.0"--ipcpath \\.\pipe\geth - dev.ipc--solc "C:\Program Files\cpp-ethereum\solc.exe"--datadir "D:\Ethereum\ethereum-dev" console 2 >> D:\Ethereum\ethereum - dev.log
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

            // TODO: New externally owned account generation. 
            // TODO: How to import account
            // TODO: Invoke contract method. From JS or C#

            var httpClient = new HttpClient();
            var json = new JsonObj
            {
                jsonrpc = "2.0",
                method = "web3_clientVersion",
                @params = new List<object>(),
                id = "1"
            };

            var jsonToSend = JsonConvert.SerializeObject(json, Formatting.None);

            var result1 = await PostRpc(jsonToSend, httpClient);

            Console.WriteLine(result1);

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

            // TODO: Document unlock time

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
    
                uint balance = 0;

                function DepositAddress(){
                    coldStorageAddress = {{ColdStorageAddress}};
                    userId = '{{UserId}}';
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

                
                function sendToAdress(address destinationAddress, uint amount) {
                    balance = balance - amount;
                }

                function kill() { if (msg.sender == owner) selfdestruct(owner); }
            }"; // TODO: Spending from contract, add method?

            json = new JsonObj
            {
                jsonrpc = "2.0",
                method = "eth_compileSolidity",
                id = "1",
                @params = new List<object> { contract.Replace(Environment.NewLine, " ").Replace("{{UserId}}", "1").Replace("{{ColdStorageAddress}}", dynAccounts.result[1].ToString()) }

            };

            jsonToSend = JsonConvert.SerializeObject(json, Formatting.None);
            var compilerResults = await PostRpc(jsonToSend, httpClient);

            var dynResult = JsonConvert.DeserializeObject<dynamic>(compilerResults);
            var code = dynResult.result.DepositAddress.code;
            var abi = dynResult.result.DepositAddress.info.abiDefinition.ToString();
            
            //var abiDefinition = JsonConvert.DeserializeObject<List<AbiObject>>(abi);
            Console.WriteLine(compilerResults);

            var jsonWithParamData = new JsonObj()
            {
                jsonrpc = "2.0",
                method = "eth_estimateGas",
                id = "1",
                @params = new List<object> { new ParamData { data = code, from = firstAccount } } // TODO: bu neden from adresi istiyor?

            };

            jsonToSend = JsonConvert.SerializeObject(jsonWithParamData, Formatting.None);

            //var estimateGasResult = await PostRpc("{\"jsonrpc\":\"2.0\",\"method\": \"eth_estimateGas\", \"params\": [{\"from\": \"0xeb85a5557e5bdc18ee1934a89d8bb402398ee26a\", \"data\": \"0x6060604052605f8060106000396000f3606060405260e060020a6000350463c6888fa18114601a575b005b60586004356007810260609081526000907f24abdb5865df5079dcc5ac590ff6f01d5c16edbc5fab4e195d9febd1114503da90602090a15060070290565b5060206060f3\"}], \"id\": 5}", httpClient);
            var estimateGasResult = await PostRpc(jsonToSend, httpClient);
            var estimatedGas = JsonConvert.DeserializeObject<JsonObj>(estimateGasResult).result;
            Console.WriteLine(estimateGasResult);

            //var stringEncoder = new StringTypeEncoder();
            //var userId = stringEncoder.Encode("1");
            //var addressEncoder = new AddressTypeEncoder();
            //var coldStorageAddress = addressEncoder.Encode(dynAccounts.result[1].ToString());

            jsonWithParamData = new JsonObj()
            {
                jsonrpc = "2.0",
                method = "eth_sendTransaction",
                id = "1",
                @params = new List<object> { new ParamData { data = code, from = firstAccount, gas = estimatedGas, } }
            };

            jsonToSend = JsonConvert.SerializeObject(jsonWithParamData, Formatting.None);

            var sendTransactionResult = await PostRpc(jsonToSend, httpClient);
            var txHash = JsonConvert.DeserializeObject<JsonObj>(sendTransactionResult).result;
            Console.WriteLine(sendTransactionResult);

            jsonWithParamData = new JsonObj
            {
                jsonrpc = "2.0",
                method = "eth_getTransactionReceipt",
                id = "1",
                @params = new List<object> { txHash }
            };

            jsonToSend = JsonConvert.SerializeObject(jsonWithParamData, Formatting.None);

            dynamic txReciept = new ExpandoObject();
            txReciept.result = null;
            while (txReciept.result == null)
            {
                await Task.Delay(500);
                var getTransactionReceiptResult = await PostRpc(jsonToSend, httpClient);
                var convertor = new ExpandoObjectConverter();
                txReciept = JsonConvert.DeserializeObject<ExpandoObject>(getTransactionReceiptResult, convertor);
            }

            Console.WriteLine(txReciept.result.contractAddress);

            var unitConversion = new UnitConversion();
            jsonWithParamData = new JsonObj()
            {
                jsonrpc = "2.0",
                method = "eth_sendTransaction",
                id = "1",
                @params = new List<object> { new { from = firstAccount, to = txReciept.result.contractAddress, value = unitConversion.ToWei(10, UnitConversion.EthUnit.Ether) } }
            };

            jsonToSend = JsonConvert.SerializeObject(jsonWithParamData, Formatting.None);

            var sendEtherResult = await PostRpc(jsonToSend, httpClient);
            txHash = JsonConvert.DeserializeObject<JsonObj>(sendEtherResult).result;

            Console.WriteLine(sendEtherResult);
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
}
