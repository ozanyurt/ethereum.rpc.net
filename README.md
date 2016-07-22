# ethereum.rpc.net
Ethereum JSON RPC API samples with HttpClient

[Geth Command Line Options](https://github.com/ethereum/go-ethereum/wiki/Command-Line-Options)

[Ethereum client installation instruction](https://www.ethereum.org/cli)

`geth --dev --rpc --rpcapi="db,eth,net,web3,personal" --rpcport "8545" --rpcaddr "127.0.0.1" --ipcpath \\.\pipe\geth-dev.ipc --solc "C:\Program Files\cpp-ethereum\solc.exe" --datadir "D:\Ethereum\ethereum-dev" console 2>> D:\Ethereum\ethereum-dev.log`

**--dev:** *Developer mode: pre-configured private network with several debugging flags*

**--rpc:** *Enable the HTTP-RPC server*

**--rpcaddr:** *HTTP-RPC server listening interface*

**--rpcport:** *HTTP-RPC server listening port*

**--rpcapi:** *API's offered over the HTTP-RPC interface* 

**--solc:** *Solidity compiler command to be used*

**console:** *Geth Console: interactive JavaScript environment*


[Accessing Contracts and Transactions (with curl through JSON RPC)](http://www.ethdocs.org/en/latest/contracts-and-transactions/accessing-contracts-and-transactions.html)

[Deploying contract using web3](http://www.ethdocs.org/en/latest/contracts-and-transactions/accessing-contracts-and-transactions.html#using-web3-js)
