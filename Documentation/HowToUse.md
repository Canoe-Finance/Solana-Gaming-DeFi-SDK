## Installing
 You can add the package to your unity project via Window--Package Manager--Add package from git URL, paste the address https://github.com/Canoe-Finance/Solana-Gaming-DeFi-SDK.git and click add.
If you are using an older version of unity, you can download our repository, then select the package.json file through Window--Package Manager--Add package from disk to import.


## Step-by-step instructions
1. If you have an older version of Unity that doesn't have imported Newtonsoft.Json just import it.
2. After importing the wallet Unity will throw unity-plastic error. Just restart Unity.
3. Create a new or use an existing GameObject in your scene, select it, click Add Component in the inspector, input CanoeDeFi to search, and add it to the Gameobject. Make sure this GameObject won't be destroyed.
4. Set the environment of solnet you prefer(MainNet/TestNet/DevNet) on the CanoeDeFi script in your GameObject.

## Step-by-step functionalities description
 The CanoeDeFi script is a singleton, you can use it anywhere by calling [CanoeDeFi.Instance] easily.

###  HasWallet
- In the beginning, you can use this method to determine whether the user has logged in. If not, allow users to import or create new wallets. This method will return true if the user created or imported a wallet.
```C#
  bool tf = CanoeDeFi.Instance.HasWallet();
```
### Generate New Wallet
- Creating a new wallet is divided into two steps: generating a mnemonic, and logging in using the password and the mnemonic just generated.
* Generate and return the mnemonic. You should display these mnemonics on the UI. And make sure the user has written it down.
```C#
  Mnemonic mnemonic = CanoeDeFi.Instance.GenerateNewWallet();
```
* After confirming that the user has written it down, use the mnemonic phrase just generated and the password entered by the user to log in.
```C#
  CanoeDeFi.Instance.LoginWithNewGeneratedWallet(mnemonic, password);
```
### Restore Wallet With Mnemonic
  If the user already has an account, this method can be called with the mnemonic entered by the user. Mnemonics are 12 or 24 words separated by spaces.

  If the wallet is restored successfully, it will return true, and the user's wallet will be saved, otherwise, it will return false.
```C#
bool tf = CanoeDeFi.Instance.RestoreWalletWithMenmonic(mnemonic, password);
```
###  Login With Passwprd
 If you know that the user has logged in before through the previous method, you can let the user enter the password to log in.

 If the password is correct, it will return true, otherwise, it will return false.
```C#
 bool tf = CanoeDeFi.Instance.LoginWithPwd(password);
```
### Get Sol Ammount Async
Get the user's Solana account balance. It should be noted that this method is asynchronous.
```C#
 double count = await CanoeDeFi.Instance.GetSolAmmountAsync();
```
### Transfer Solana
Pass the address and amount entered by the user to initiate a transfer request. It returns a value of RequestResult<T>, you can check it for the result and more details.

It's worth noting that for Solana sol, 1000000000 amounts stand for 1(UI, user input), so you need to convert it before passing in.
```C#
RequestResult<string> transferResult = await CanoeDeFi.Instance.TransferSol(toPublicKey, 
umount);
```
### Get Owned Token Accounts
Users may have several tokens in the game, you can get the list by calling this function.
The return value is a list of TokenAccount, which contains each token's info, and amount.
```C#
TokenAccount[] tokenAccounts = await CanoeDeFi.Instance.GetOwnedTokenAccounts();
```
### Transfer Token
It will be used for users' transfer and consumption in the game, this is the most complex function so far, but it's not hard to understand. Overall this function transfers any kind of token to the given address.

We have a very detailed description for each parameter in the code comments.

```C#
RequestResult<string> requestResult = await 
CanoeDeFi.Instance.TransferToken(sourceTokenAccount, toWalletAccount, sourceAccountOwner, 
tokenMint, tokenDecimals, amount);
```
### Request Jupiter Output Amount

After the user finishes inputting, you need to request Jupiter to display the number of results the user will get in this exchange.

Note that this method uses Unity Coroutines, the returned data is not processed with different token decimal, and needs to be processed before being displayed.

```C#
StartCoroutine(CanoeDeFi.Instance.RequestJupiterOutputAmount(inMint, outMint, amount, slippage, 4, WalletController.Instance.AARTCANOEADDRESS, callbackAction<string>);
```

## 

### Jupiter Swap Request

Swap between SOL and token, or different kinds of the token is very useful. We implemented an aggregated dex Jupiter, which can be used by calling the method[JupiterSwapRequest()], passing parameters of inputMint, outputMint, amount, shippage, feeBps, your feeAccount, and the callback action. while jupiter's swap is done, the callback function will be called. The result should be handled in your callback function.
```C#
CanoeDeFi.Instance.JupiterSwapRequest(inputMint, outputMint, amout, shippage, feeBps, feeAccount, callbackAction<bool>);
```
## License

    This project is licensed under the MIT License - see the [LICENSE](https://github.com/bmresearch/Solnet/blob/master/LICENSE) file for details
<!--     ![Twitter Follow](https://img.shields.io/twitter/follow/) -->
