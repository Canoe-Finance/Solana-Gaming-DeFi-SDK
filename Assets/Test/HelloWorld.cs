using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HelloWorld : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Wallet wallet = new Wallet(WordCount.TwentyFour, WordList.English);

        Debug.Log($"Mnemonic: {wallet.Mnemonic}");
        Debug.Log($"PubKey: {wallet.Account.PublicKey.Key}");
        Debug.Log($"PrivateKey: {wallet.Account.PrivateKey.Key}");


        IRpcClient rpcClient = ClientFactory.GetClient(Cluster.TestNet);

        var balance = rpcClient.GetBalance(wallet.Account.PublicKey);

        Debug.Log($"Balance: {balance.Result.Value}");

        var transactionHash = rpcClient.RequestAirdrop(wallet.Account.PublicKey, 100_000_000);

        Debug.Log($"TxHash: {transactionHash.Result}");



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
