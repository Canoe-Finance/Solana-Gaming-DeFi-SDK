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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
