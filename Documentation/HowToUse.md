## Installing
 You can add package to your unity project via Window--Package Manager--Add package from git url, paste the address https://github.com/Canoe-Finance/Solana-Gaming-DeFi-SDK.git and click add .
If your are using an older version of unity, you can download our responsity, then select the package.json file through Window--Package Manager--Add package from disk to import.


## Step-by-step instructions
1. If you have an older version of Unity that doesn't have imported Newtonsoft.Json just import it.
2. After importing the wallet Unity will throw unity-plastic error. Just restart Unity.
3. Create new or use an existing Gameobject in you scene, selet it, click Add Component in inspector, input CanoeDeFi to serch, add it to the Gameobject. Make sure this GameObject won't be destoryed.
4. Set the envirment of solnet you prefer(MainNet/TestNet/DevNet) on CanoeDeFi script in your gameobject.

## Step-by-step functionalities description
 The CanoeDeFi script is a singleton, you can use it anywhere by calling [CanoeDeFi.Instance] easily.

###  HasWallet
- At the beginning, you can use this method to determine whether the user has logged in. If not, allow users to import or create new wallets. This method will return true if the user created or imported a wallet.
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
### Restore Wallet With Menmonic
  If the user already has an account, this method can be called with the mnemonic entered by the user. Mnemonics are 12 or 24 words separated by spaces.

  If the wallet is restored successfully, it will return true, and the user's wallet will be saved, otherwise it will return false.
```C#
bool tf = CanoeDeFi.Instance.RestoreWalletWithMenmonic(mnemonic, password);
```
###  Login With Passwprd
 If you know that the user has logged in before through the previous method, you can let the user enter the password to log in.

 If the password is correct, it will return true, otherwise it will return false.
```C#
 bool tf = CanoeDeFi.Instance.LoginWithPwd(password);
```
### Get Sol Ammount Async
Get the user's Solana account balance. It should be noted that this method is an asynchronous method.
```C#
 double count = await CanoeDeFi.Instance.GetSolAmmountAsync();
```
### Transfer Solana
Pass the address and amount entered by user to initiate a transfer request. It returns a value of RequestResult<T>, you can check it for result and more details.

It's worth noting that for solana sol, 1000000000 amouts stands for 1(UI, user input), so you need to convert it before passing in.
```C#
RequestResult<string> transferResult = await CanoeDeFi.Instance.TransferSol(toPublicKey, 
umount);
```
### Get Owned Token Accounts
Users may have several tokens in the game, you can get the list by calling this function.
The return value is a list of TokenAccount, whith contains each token's info, amount were contained ofcourse .
```C#
TokenAccount[] tokenAccounts = await CanoeDeFi.Instance.GetOwnedTokenAccounts();
```
### Transfer Token
It will be used for users' transfer and consumption in game, this is the most complex function so far, but it's not hard to understand. Overall this function transfer any kind of token to the given address.

We have a very detailed description for each parameter in the code comments.

```C#
RequestResult<string> requestResult = await 
CanoeDeFi.Instance.TransferToken(sourceTokenAccount, toWalletAccount, sourceAccountOwner, 
tokenMint, amount);
```
### Jupyter Swap Request
Swap between SOL and token, or diffent kinds of token is very useful. We implemented an aggregated dex Jupyter, it can be used by calling the method[JupyterSwapRequest()], passing parameters of inputMint, outputMint, amout, shippage, and the callback action. while jupyter's swap is done, the callback function will be called. Result should be handled in your callback function.
```C#
CanoeDeFi.Instance.JupyterSwapRequest(inputMint, outputMint, amout, shippage, callbackAction<string>);
```
## License

    This project is licensed under the MIT License - see the [LICENSE](https://github.com/bmresearch/Solnet/blob/master/LICENSE) file for details
<!--     ![Twitter Follow](https://img.shields.io/twitter/follow/) -->
