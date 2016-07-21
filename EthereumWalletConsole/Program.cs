using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
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

    function kill() { if (msg.sender == owner) selfdestruct(owner); }
}
";

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
                @params = new List<object> { new ParamData { data = code, from = firstAccount } }

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

            jsonWithParamData = new JsonObj()
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
        public object inputs { get; set; }

    }

    public class AddressTypeEncoder : ITypeEncoder
    {
        private IntTypeEncoder intTypeEncoder;

        public AddressTypeEncoder()
        {
            this.intTypeEncoder = new IntTypeEncoder();
        }

        public byte[] Encode(object value)
        {
            var strValue = value as string;

            if (strValue != null
                && !strValue.StartsWith("0x", StringComparison.Ordinal))
            {
                // address is supposed to be always in hex
                value = "0x" + value;
            }

            byte[] addr = intTypeEncoder.Encode(value);

            for (int i = 0; i < 12; i++)
            {
                if (addr[i] != 0 && addr[i] != 0xFF)
                {
                    throw new Exception("Invalid address (should be 20 bytes length): " + addr.ToHex());
                }

                if (addr[i] == 0xFF) addr[i] = 0;

            }
            return addr;
        }
    }

    public interface ITypeEncoder
    {
        byte[] Encode(object value);
    }
    public class IntTypeEncoder : ITypeEncoder
    {
        private IntTypeDecoder intTypeDecoder;

        public IntTypeEncoder()
        {
            this.intTypeDecoder = new IntTypeDecoder();
        }

        public byte[] Encode(object value)
        {
            BigInteger bigInt;

            var stringValue = value as string;

            if (stringValue != null)
            {
                bigInt = intTypeDecoder.Decode<BigInteger>(stringValue);
            }
            else if (value is BigInteger)
            {
                bigInt = (BigInteger)value;
            }
            else if (value.IsNumber())
            {
                bigInt = BigInteger.Parse(value.ToString());
            }
            else
            {
                throw new Exception("Invalid value for type '" + this + "': " + value + " (" + value.GetType() + ")");
            }
            return EncodeInt(bigInt);
        }

        public byte[] EncodeInt(int i)
        {
            return EncodeInt(new BigInteger(i));
        }

        public byte[] EncodeInt(BigInteger bigInt)
        {
            byte[] ret = new byte[32];

            for (int i = 0; i < ret.Length; i++)
            {
                if (bigInt.Sign < 0)
                {
                    ret[i] = 0xFF;
                }
                else
                {
                    ret[i] = 0;
                }
            }

            byte[] bytes;

            //It should always be Big Endian.
            if (BitConverter.IsLittleEndian)
            {
                bytes = bigInt.ToByteArray().Reverse().ToArray();
            }
            else
            {
                bytes = bigInt.ToByteArray().ToArray();
            }

            Array.Copy(bytes, 0, ret, 32 - bytes.Length, bytes.Length);

            return ret;
        }
    }
    public class IntTypeDecoder : TypeDecoder
    {
        public override bool IsSupportedType(Type type)
        {
            return type == typeof(int) || type == typeof(ulong) || type == typeof(long) || type == typeof(uint) ||
                   type == typeof(BigInteger);
        }


        public override object Decode(byte[] encoded, Type type)
        {
            if (type == typeof(int))
            {
                return DecodeInt(encoded);
            }

            if (type == typeof(long))
            {
                return DecodeLong(encoded);
            }

            if (type == typeof(ulong))
            {
                return DecodeULong(encoded);
            }

            if (type == typeof(uint))
            {
                return DecodeUInt(encoded);
            }

            if (type == typeof(BigInteger))
            {
                return DecodeBigInteger(encoded);
            }

            throw new NotSupportedException(type.ToString() + " is not a supported decoding type for IntType");
        }

        public int DecodeInt(byte[] encoded)
        {
            return (int)DecodeBigInteger(encoded);
        }

        public uint DecodeUInt(byte[] encoded)
        {
            return (uint)DecodeBigInteger(encoded);
        }

        public long DecodeLong(byte[] encoded)
        {
            return (long)DecodeBigInteger(encoded);
        }

        public ulong DecodeULong(byte[] encoded)
        {
            return (ulong)DecodeBigInteger(encoded);
        }

        public BigInteger DecodeBigInteger(string hexString)
        {
            if (!hexString.StartsWith("0x"))
            {
                hexString = "0x" + hexString;
            }

            return DecodeBigInteger(hexString.HexToByteArray());
        }

        public BigInteger DecodeBigInteger(byte[] encoded)
        {
            bool paddedPrefix = true;

            bool negative = encoded.First() == 0xFF;

            if (BitConverter.IsLittleEndian)
            {
                encoded = encoded.Reverse().ToArray();
            }

            if (negative)
            {
                return new BigInteger(encoded) - new BigInteger("0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff".HexToByteArray()) - 1;
            }

            return new BigInteger(encoded);
        }



    }
    public abstract class TypeDecoder : ITypeDecoder
    {
        public abstract bool IsSupportedType(Type type);
        public abstract object Decode(byte[] encoded, Type type);

        public T Decode<T>(byte[] encoded)
        {
            return (T)Decode(encoded, typeof(T));
        }

        public object Decode(string encoded, Type type)
        {
            if (!encoded.StartsWith("0x"))
            {
                encoded = "0x" + encoded;
            }

            return Decode(encoded.HexToByteArray(), type);
        }

        public T Decode<T>(string encoded)
        {
            return (T)Decode(encoded, typeof(T));
        }
    }

    public class BytesTypeEncoder : ITypeEncoder
    {
        private IntTypeEncoder intTypeEncoder;

        public BytesTypeEncoder()
        {
            this.intTypeEncoder = new IntTypeEncoder();
        }

        public byte[] Encode(object value)
        {
            return Encode(value, true);
        }

        public byte[] Encode(object value, bool checkEndian)
        {
            if (!(value is byte[]))
            {
                throw new Exception("byte[] value expected for type 'bytes'");
            }
            byte[] bb = (byte[])value;
            byte[] ret = new byte[((bb.Length - 1) / 32 + 1) * 32]; // padding 32 bytes

            //It should always be Big Endian.
            if (BitConverter.IsLittleEndian && checkEndian)
            {
                bb = bb.Reverse().ToArray();
            }

            Array.Copy(bb, 0, ret, 0, bb.Length);

            return ByteUtil.Merge(intTypeEncoder.EncodeInt(bb.Length), ret);
        }

    }
    public class StringTypeEncoder : ITypeEncoder
    {
        private BytesTypeEncoder byteTypeEncoder;

        public StringTypeEncoder()
        {
            byteTypeEncoder = new BytesTypeEncoder();
        }

        public byte[] Encode(object value)
        {
            if (!(value is string))
            {
                throw new Exception("String value expected for type 'string'");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes((string)value);

            return byteTypeEncoder.Encode(bytes, false);
        }
    }


    public interface ITypeDecoder
    {
        bool IsSupportedType(Type type);
        object Decode(byte[] encoded, Type type);

        T Decode<T>(byte[] encoded);

        object Decode(string hexString, Type type);

        T Decode<T>(string hexString);
    }

    public class ByteUtil
    {

        public static readonly sbyte[] EMPTY_BYTE_ARRAY = new sbyte[0];
        public static readonly sbyte[] ZERO_BYTE_ARRAY = new sbyte[] { 0 };

        /// <summary>
        /// Creates a copy of bytes and appends b to the end of it
        /// </summary>
        public static byte[] AppendByte(byte[] bytes, byte b)
        {
            byte[] result = new byte[bytes.Length + 1];
            Array.Copy(bytes, result, bytes.Length);
            result[result.Length - 1] = b;
            return result;
        }


        /// <param name ="arrays"> - arrays to merge </param>
        /// <returns> - merged array </returns>
        public static byte[] Merge(params byte[][] arrays)
        {
            int arrCount = 0;
            int count = 0;
            foreach (byte[] array in arrays)
            {
                arrCount++;
                count += array.Length;
            }

            // Create new array and copy all array contents
            byte[] mergedArray = new byte[count];
            int start = 0;
            foreach (byte[] array in arrays)
            {
                Array.Copy(array, 0, mergedArray, start, array.Length);
                start += array.Length;
            }
            return mergedArray;
        }
    }

    public static class HexByteConvertorExtensions
    {
        public static string ToHex(this byte[] value)
        {
            return string.Concat(value.Select(b => b.ToString("x2")));
        }

        public static string ToHexCompact(this byte[] value)
        {
            return ToHex(value).TrimStart('0');
        }
        //From article http://blogs.msdn.com/b/heikkiri/archive/2012/07/17/hex-string-to-corresponding-byte-array.aspx

        private static readonly byte[] Empty = new byte[0];

        public static byte[] HexToByteArray(this string value)
        {
            byte[] bytes = null;
            if (String.IsNullOrEmpty(value))
                bytes = Empty;
            else
            {
                int string_length = value.Length;
                int character_index = (value.StartsWith("0x", StringComparison.Ordinal)) ? 2 : 0;
                // Does the string define leading HEX indicator '0x'. Adjust starting index accordingly.               
                int number_of_characters = string_length - character_index;

                bool add_leading_zero = false;
                if (0 != (number_of_characters % 2))
                {
                    add_leading_zero = true;

                    number_of_characters += 1; // Leading '0' has been striped from the string presentation.
                }

                bytes = new byte[number_of_characters / 2]; // Initialize our byte array to hold the converted string.

                int write_index = 0;
                if (add_leading_zero)
                {
                    bytes[write_index++] = FromCharacterToByte(value[character_index], character_index);
                    character_index += 1;
                }

                for (int read_index = character_index; read_index < value.Length; read_index += 2)
                {
                    byte upper = FromCharacterToByte(value[read_index], read_index, 4);
                    byte lower = FromCharacterToByte(value[read_index + 1], read_index + 1);

                    bytes[write_index++] = (byte)(upper | lower);
                }
            }

            return bytes;
        }

        private static byte FromCharacterToByte(char character, int index, int shift = 0)
        {
            byte value = (byte)character;
            if (((0x40 < value) && (0x47 > value)) || ((0x60 < value) && (0x67 > value)))
            {
                if (0x40 == (0x40 & value))
                {
                    if (0x20 == (0x20 & value))
                        value = (byte)(((value + 0xA) - 0x61) << shift);
                    else
                        value = (byte)(((value + 0xA) - 0x41) << shift);
                }
            }
            else if ((0x29 < value) && (0x40 > value))
                value = (byte)((value - 0x30) << shift);
            else
                throw new InvalidOperationException(String.Format("Character '{0}' at index '{1}' is not valid alphanumeric character.", character, index));

            return value;
        }


    }
    public static class NumberExtensions
    {
        public static bool IsNumber(this object value)
        {
            return value is sbyte
                   || value is byte
                   || value is short
                   || value is ushort
                   || value is int
                   || value is uint
                   || value is long
                   || value is ulong
                   || value is float
                   || value is double
                   || value is decimal;
        }
    }
}
