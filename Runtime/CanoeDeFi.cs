using LitJson;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.SDK.Utility;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Canoe
{
    public class CanoeDeFi : MonoBehaviour
    {
        public static CanoeDeFi Instance;

        public Wallet Wallet { get; set; }
        public string Mnemonics { get; private set; }
        public string Password { get; private set; }
        public string PrivateKey { get; private set; }

        [SerializeField]
        [Header("which sol net you prefer")]
        public Cluster Env = Cluster.DevNet;

        private Cypher cypher;

        #region Public members

        public Wallet CurrentWallet;
        public TokenAccount[] TokenAccounts;

        #endregion

        #region Private members

        private JsonData jupyteRoute;
        //private string routeUrl = "https://quote-api.jup.ag/v1/quote?inputMint=So11111111111111111111111111111111111111112&outputMint=EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v&amount=10000000&slippage=0.5";
        private string routeUrl = $"https://quote-api.jup.ag/v1/quote?inputMint={0}&outputMint={1}&amount={2}&slippage={3}";
        private string jupiterPostUrl = "https://quote-api.jup.ag/v1/swap";
        private string jupiterMsgBase64;
        private RequestResult<string> jupiterSwapResult;
        private Action<RequestResult<string>> jupiterSwapCallback;
        private string passwordKey = "Password";
        private string encryptedMnemonicsKey = "EncryptedMnemonics";
        private string privateKeyKey = "PrivateKey";

        #endregion
        private void Awake()
        {
            Instance = this;
            cypher = new Cypher();
            jupyteRoute = new JsonData();
        }


        /// <summary>
        ///  Is there a wallet that has already been logged in
        /// </summary>
        /// <returns></returns>
        public bool HasWallet()
        {
            return PlayerPrefs.HasKey(encryptedMnemonicsKey);
        }

        /// <summary>
        /// try to login with input pwd
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool LoginWithPwd(string password)
        {
            try
            {
                string encryptedMnemonics = PlayerPrefs.GetString(encryptedMnemonicsKey);
                var mnemonic = cypher.Decrypt(encryptedMnemonics, password);
                CurrentWallet = new Wallet(mnemonic);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        /// <summary>
        /// Restore the wallet using the mnemonic
        /// </summary>
        /// <param name="mnemonics">mnemonics form user input</param>
        /// <param name="password">password form user input</param>
        /// <returns></returns>
        public bool RestoreWalletWithMenmonic(string mnemonics, string password)
        {
            try
            {
                //check if the mnemonic currect ;
                if (!WalletKeyPair.CheckMnemonicValidity(mnemonics))
                {
                    return false;
                    throw new Exception("Mnemonic is in incorect format");
                }

                CurrentWallet = new Wallet(mnemonics, WordList.English);

                //save the encryptedMnemonics, user can login with pwd next time
                string encryptedMnemonics = cypher.Encrypt(mnemonics, password);
                PlayerPrefs.SetString(encryptedMnemonicsKey, encryptedMnemonics);


                return true;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return false;
            }
        }


        /// <summary>
        /// generate a new wallet
        /// </summary>
        /// <returns> mnemonic of new wallet, should be shown on screen</returns>
        public Mnemonic GenerateNewWallet()
        {
            Mnemonic newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            CurrentWallet = new Wallet(newMnemonic);
            Debug.Log($"words:{CurrentWallet.Mnemonic}");
            Debug.Log($"pubkey:{CurrentWallet.Account.PublicKey}");
            Debug.Log($"privatekey:{CurrentWallet.Account.PrivateKey}");
            return newMnemonic;
        }

        /// <summary>
        /// Confirm login with new wallet
        /// </summary>
        /// <param name="mnemonic">new generated mnemonic</param>
        /// <param name="password">password form user input</param>
        public void LoginWithNewGeneratedWallet(Mnemonic mnemonic, string password)
        {
            //save the encryptedMnemonics, user can login with pwd next time
            string encryptedMnemonics = cypher.Encrypt(CurrentWallet.Mnemonic.ToString(), password);
            PlayerPrefs.SetString(encryptedMnemonicsKey, encryptedMnemonics);
        }

        /// <summary>
        /// get users sol banance
        /// </summary>
        /// <param name="account"></param>
        /// <param name="rpcClient"></param>
        /// <returns></returns>
        public async Task<double> GetSolAmmountAsync()
        {
            Account account = CurrentWallet.Account;
            IRpcClient rpcClient = ClientFactory.GetClient(Env);
            AccountInfo result = await AccountUtility.GetAccountData(account, rpcClient);
            if (result != null)
                return (double)result.Lamports / 1000000000;
            return 0;
        }

        /// <summary>
        /// transfer sol to the give address
        /// </summary>
        /// <param name="toPublicKey">give address</param>
        /// <param name="ammount">ui amount</param>
        /// <returns></returns>
        public async Task<RequestResult<string>> TransferSol(string toPublicKey, ulong ammount = 100000000)
        {
            RequestResult<ResponseValue<BlockHash>> blockHash = await ClientFactory.GetClient(Env).GetRecentBlockHashAsync();
            Account fromAccount = CurrentWallet.GetAccount(0);

            var transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey, new PublicKey(toPublicKey), ammount)).
                SetFeePayer(fromAccount).
                Build(fromAccount);

            return await ClientFactory.GetClient(Env).SendTransactionAsync(Convert.ToBase64String(transaction));

        }

        /// <summary>
        /// get user's token list
        /// </summary>
        /// <param name="account">user address</param>
        /// <returns>list of all token infos</returns>
        public async Task<TokenAccount[]> GetOwnedTokenAccounts()
        {
            Account account = CurrentWallet.Account;
            try
            {
                RequestResult<ResponseValue<List<TokenAccount>>> result = await ClientFactory.GetClient(Env).GetTokenAccountsByOwnerAsync(account.PublicKey, null, TokenProgram.ProgramIdKey);
                if (result.Result != null && result.Result.Value != null)
                {
                    return result.Result.Value.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
            return null;
        }

        /// <summary>
        /// transfer any kind of token to given address
        /// </summary>
        /// <param name="sourceTokenAccount">you can get the list of TokenAccount,by calling GetOwnedTokenAccounts(), get the corresponding item of TokenAccount[], pass it's PublicKey here</param>
        /// <param name="toWalletAccount">target address</param>
        /// <param name="sourceAccountOwner">you can call GetAccount() with user's wallet, pass it in</param>
        /// <param name="tokenMint">mint of the token.get the list of TokenAccounts by calling GetOwnedTokenAccounts(), the it can be obtained through TokenAccount[i].Account.Data.Parsed.Info.Mint</param>
        /// <param name="amount">ui amount</param>
        /// <returns>transfer result</returns>
        public async Task<RequestResult<string>> TransferToken(string sourceTokenAccount, string toWalletAccount, Account sourceAccountOwner, string tokenMint, ulong amount = 1)
        {

            PublicKey associatedTokenAccountOwner = new PublicKey(toWalletAccount);
            PublicKey mint = new PublicKey(tokenMint);
            Account ownerAccount = CurrentWallet.GetAccount(0);
            PublicKey associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(associatedTokenAccountOwner, new PublicKey(tokenMint));

            RequestResult<ResponseValue<BlockHash>> blockHash = await ClientFactory.GetClient(Env).GetRecentBlockHashAsync();
            RequestResult<ulong> rentExemptionAmmount = await ClientFactory.GetClient(Env).GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize);

            TokenAccount[] lortAccounts = await GetOwnedTokenAccounts(toWalletAccount, tokenMint, TokenProgram.ProgramIdKey);
            byte[] transaction;
            //try to make sure is the account already have a token account
            var info = await GetAccountData(associatedTokenAccount);
            //already have a token account
            if (info != null)
            {
                PublicKey initialAccount =
    AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(ownerAccount, mint);

                Debug.Log($"initialAccount: {initialAccount}");
                transaction = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(ownerAccount).
                    AddInstruction(TokenProgram.TransferChecked(
                        initialAccount,
                        associatedTokenAccount,
                        amount,
                        9,//token decimals
                       ownerAccount, mint
                        )).
                    Build(new List<Account> { ownerAccount });
            }
            else
            {
                Debug.Log($"AssociatedTokenAccountOwner: {associatedTokenAccountOwner}");
                Debug.Log($"AssociatedTokenAccount: {associatedTokenAccount}");

                PublicKey initialAccount =
        AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(ownerAccount, mint);

                Debug.Log($"initialAccount: {initialAccount}");
                transaction = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(ownerAccount).
                    AddInstruction(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        ownerAccount,
                        associatedTokenAccountOwner,
                        mint)).
                    AddInstruction(TokenProgram.TransferChecked(
                        initialAccount,
                        associatedTokenAccount,
                        amount,
                        9,//token decimals
                       ownerAccount, mint
                        )).// the ownerAccount was set as the mint authority
                    Build(new List<Account> { ownerAccount });
            }

            return await ClientFactory.GetClient(Env).SendTransactionAsync(transaction);
        }

        /// <summary>
        /// make a jupiter swap
        /// </summary>
        /// <param name="inputMint"></param>
        /// <param name="outputMint"></param>
        /// <param name="amout"></param>
        /// <param name="shippage"></param>
        /// <param name="Callback"></param>
        public void JupiterSwapRequest(string inputMint, string outputMint, ulong amout, float shippage = 0.5f, Action<RequestResult<string>> callback = null)
        {
            string routUrlWithPams = string.Format(routeUrl, inputMint, outputMint, amout, shippage);
            jupiterSwapCallback = callback;
            StartCoroutine(GetJupiterTx(routUrlWithPams));
        }

        #region Private functions

        private async Task<TokenAccount[]> GetOwnedTokenAccounts(string walletPubKey, string tokenMintPubKey, string tokenProgramPublicKey)
        {
            RequestResult<ResponseValue<List<TokenAccount>>> result = await ClientFactory.GetClient(Env).GetTokenAccountsByOwnerAsync(walletPubKey, tokenMintPubKey, tokenProgramPublicKey);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value.ToArray();
            }
            return null;
        }

        private async Task<AccountInfo> GetAccountData(PublicKey account)
        {
            RequestResult<ResponseValue<AccountInfo>> result = await ClientFactory.GetClient(Env).GetAccountInfoAsync(account);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value;
            }
            return null;
        }


        private IEnumerator GetJupiterTx(string routeUrlWithPams)
        {
            //get jupiter route
            UnityWebRequest getRequest = UnityWebRequest.Get(routeUrlWithPams);
            yield return getRequest.SendWebRequest();
            if (getRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(" Failed to communicate with the server");
                yield return null;
            }
            string data = getRequest.downloadHandler.text;
            Debug.Log(data);
            JsonData jData = JsonMapper.ToObject(data);
            //choose the first
            jupyteRoute["route"] = jData["data"][0];

            //get jupiter transaction

            jupyteRoute["userPublicKey"] = CurrentWallet.Account.PublicKey.ToString();
            Debug.Log($"data:{(string)jupyteRoute.ToJson()}");
            byte[] postBytes = System.Text.Encoding.Default.GetBytes((string)jupyteRoute.ToJson());

            Debug.Log($"route: {(string)jupyteRoute.ToJson()}");
            Debug.Log($"userPublicKey:{CurrentWallet.Account.PublicKey}");
            UnityWebRequest postRequest = new UnityWebRequest(jupiterPostUrl, "POST");
            postRequest.SetRequestHeader("Content-Type", "application/json");
            postRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(postBytes);
            postRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            yield return postRequest.SendWebRequest();

            if (postRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log($"UnityWebRequest.Result.ProtocolError:{postRequest.result}");
            }
            else if (postRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log($"UnityWebRequest.Result.ConnectionError:{postRequest.result}");
            }
            else
            {
                string receiveContent = postRequest.downloadHandler.text;
                Debug.Log(receiveContent);
            }

            Debug.Log($"Status Code: {postRequest.responseCode}");
            if (postRequest.responseCode == 200)
            {
                string text = postRequest.downloadHandler.text;
            }
            JsonData resJdata = JsonMapper.ToObject(postRequest.downloadHandler.text);
            Debug.Log($"base64Data:{resJdata["swapTransaction"]}");

            jupiterMsgBase64 = resJdata["swapTransaction"].ToJson().Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\"", "");

            var task = Task.Run(SendJupiterTransaction);
            yield return new WaitUntil(() => task.IsCompleted);

        }

        private async Task SendJupiterTransaction()
        {
            Transaction decodedInstructions = Transaction.Deserialize(jupiterMsgBase64);

            RequestResult<ResponseValue<BlockHash>> blockHash = await ClientFactory.GetClient(Cluster.MainNet).GetRecentBlockHashAsync();

            var tb = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
           SetFeePayer(CurrentWallet.Account);
            for (int i = 0; i < decodedInstructions.Instructions.Count; i++)
            {
                tb.AddInstruction(decodedInstructions.Instructions[i]);
            }
            byte[] txBytes = tb.
           Build(new List<Account> { CurrentWallet.Account });


            var result = await ClientFactory.GetClient(Cluster.MainNet).SendTransactionAsync(txBytes);
            jupiterSwapResult = result;
            jupiterSwapCallback?.Invoke(result);
            Debug.Log($"done: {result.Reason}");
        }
        #endregion
    }

}
