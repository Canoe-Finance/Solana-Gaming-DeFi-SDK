using Solana.Unity.SDK.Utility;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.Wallet.Bip39;
using UnityEngine;

namespace Solana.Unity.SDK
{
    [RequireComponent(typeof(Startup))]
    [RequireComponent(typeof(MainThreadDispatcher))]
    public class WalletBaseComponent : MonoBehaviour
    {
        #region Player Prefs Keys

        private string mnemonicsKey = "Mnemonics";
        private string passwordKey = "Password";
        private string encryptedMnemonicsKey = "EncryptedMnemonics";
        private string privateKeyKey = "PrivateKey";
        #endregion
        #region Connections
        public static string devNetAdress = "https://api.devnet.solana.com";
        public static string testNetAdress = "https://api.testnet.solana.com";
        public static string mainNetAdress = "https://api.mainnet-beta.solana.com";

        public static string webSocketDevNetAdress = "ws://api.devnet.solana.com";
        public static string webSocketTestNetAdress = "ws://api.testnet.solana.com";
        public static string webSocketMainNetAdress = "ws://api.mainnet-beta.solana.com";

        public string customUrl = "http://192.168.0.22:8899";

        public enum EClientUrlSource
        {
            EDevnet,
            EMainnet,
            ETestnet,
            ECustom
        }

        public EClientUrlSource clientSource;
        public bool autoConnectOnStartup = false;

        public IRpcClient activeRpcClient { get; private set; }


        public virtual void Awake()
        {
            webSocketService = new WebSocketService();
            cypher = new Cypher();

            if (autoConnectOnStartup)
            {
                StartConnection(clientSource);
                webSocketService.StartConnection(GetWebsocketConnectionURL(clientSource));
            }
        }

        public void OnDestroy()
        {
            webSocketService.CloseConnection();
        }

        /// <summary>
        /// Returns the url of the desired client source
        /// </summary>
        /// <param name="clientUrlSource"> Desired client source</param>
        /// <returns></returns>
        public string GetConnectionURL(EClientUrlSource clientUrlSource)
        {
            string url = "";
            switch (clientUrlSource)
            {
                case EClientUrlSource.ECustom:
                    url = customUrl;
                    break;
                case EClientUrlSource.EDevnet:
                    url = devNetAdress;
                    break;
                case EClientUrlSource.EMainnet:
                    url = mainNetAdress;
                    break;
                case EClientUrlSource.ETestnet:
                    url = testNetAdress;
                    break;
            }
            return url;
        }

        /// <summary>
        /// Returns the websocket url of the desired client source
        /// </summary>
        /// <param name="clientUrlSource"> Desired client source</param>
        /// <returns></returns>
        public string GetWebsocketConnectionURL(EClientUrlSource clientUrlSource)
        {
            string url = "";
            switch (clientUrlSource)
            {
                case EClientUrlSource.ECustom:
                    url = customUrl;
                    break;
                case EClientUrlSource.EDevnet:
                    url = webSocketDevNetAdress;
                    break;
                case EClientUrlSource.EMainnet:
                    url = webSocketMainNetAdress;
                    break;
                case EClientUrlSource.ETestnet:
                    url = webSocketTestNetAdress;
                    break;
            }
            return url;
        }

        #endregion
        public Wallet.Wallet wallet { get; set; }
        public string mnemonics { get; private set; }
        public string password { get; private set; }
        public string privateKey { get; private set; }

        [HideInInspector]
        public WebSocketService webSocketService;
        private Cypher cypher;

        /// <summary>
        /// Creates private and public key with mnemonics, then starts RPC connection and creates Account
        /// </summary>
        /// <param name="account">Account to create</param>
        /// <param name="toPublicKey">Public key of Account</param>
        /// <param name="ammount">SOL amount</param>
        public async void CreateAccount(Account account, string toPublicKey = "", ulong ammount = 1000)
        {
            try
            {
                Keypair keypair = WalletKeyPair.GenerateKeyPairFromMnemonic(WalletKeyPair.GenerateNewMnemonic());

                toPublicKey = keypair.publicKey;

                RequestResult<ResponseValue<BlockHash>> blockHash = await activeRpcClient.GetRecentBlockHashAsync();

                var transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    AddInstruction(SystemProgram.CreateAccount(account.PublicKey, new PublicKey(toPublicKey), ammount,
                    (long)TokenProgram.TokenAccountDataSize, SystemProgram.ProgramIdKey))
                    .Build(new List<Account>() {
                    account,
                    new Account(keypair.privateKeyByte, keypair.publicKeyByte)
                    });

                RequestResult<string> firstSig = await activeRpcClient.SendTransactionAsync(Convert.ToBase64String(transaction));
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
        }

        /// <summary>
        /// Returns the account data for the forwarded account
        /// </summary>
        /// <param name="account">Forwarded account for which we want to return data</param>
        /// <returns></returns>
        public async Task<AccountInfo> GetAccountData(Account account)
        {
            RequestResult<ResponseValue<AccountInfo>> result = await activeRpcClient.GetAccountInfoAsync(account.PublicKey);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value;
            }
            return null;
        }

        /// <summary>
        /// Returns tokens held by the forwarded account
        /// </summary>
        /// <param name="walletPubKey">Pub key of the wallet for which we want to return tokens</param>
        /// <param name="tokenMintPubKey"></param>
        /// <param name="tokenProgramPublicKey"></param>
        /// <returns></returns>
        public async Task<TokenAccount[]> GetOwnedTokenAccounts(string walletPubKey, string tokenMintPubKey, string tokenProgramPublicKey)
        {
            RequestResult<ResponseValue<List<TokenAccount>>> result = await activeRpcClient.GetTokenAccountsByOwnerAsync(walletPubKey, tokenMintPubKey, tokenProgramPublicKey);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Returns tokens held by the forwarded account
        /// </summary>
        /// <param name="walletPubKey">Pub key of the wallet for which we want to return tokens</param>
        /// <param name="tokenMintPubKey"></param>
        /// <param name="tokenProgramPublicKey"></param>
        /// <returns></returns>
        public async Task<TokenAccount[]> GetOwnedTokenAccounts(Account account, string tokenMintPubKey, string tokenProgramPublicKey)
        {
            RequestResult<ResponseValue<List<TokenAccount>>> result = await activeRpcClient.GetTokenAccountsByOwnerAsync(
                account.PublicKey,
                tokenMintPubKey,
                tokenProgramPublicKey);

            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Returns token balance for forwarded token public key
        /// </summary>
        /// <param name="tokenPubKey"> Public key token for which we want to return balance</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<TokenBalance> GetTokenBalance(string tokenPubKey)
        {
            RequestResult<ResponseValue<TokenBalance>> result = await activeRpcClient.GetTokenAccountBalanceAsync(tokenPubKey);
            if (result.Result != null)
                return result.Result.Value;
            else
            {
                return null;
                throw new Exception("No balance for this token reveived");
            }
        }

        /// <summary>
        /// Returns token supply for forwarded token public key
        /// </summary>
        /// <param name="tokenPubKey"> Public key token for which we want to return supply</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<RequestResult<ResponseValue<TokenBalance>>> GetTokenSupply(string key)
        {
            RequestResult<ResponseValue<TokenBalance>> supply = await activeRpcClient.GetTokenSupplyAsync(key);
            return supply;
        }

        /// <summary>
        /// Start RPC connection and return new RPC Client 
        /// </summary>
        /// <param name="clientUrlSource">Choosed client source</param>
        /// <param name="customUrl">Custom url for rpc connection</param>
        /// <returns></returns>
        public IRpcClient StartConnection(EClientUrlSource clientUrlSource, string customUrl = "")
        {
            if (!string.IsNullOrEmpty(customUrl))
                this.customUrl = customUrl;

            try
            {
                if (activeRpcClient == null)
                {
                    activeRpcClient = ClientFactory.GetClient(GetConnectionURL(clientUrlSource), true);
                }

                return activeRpcClient;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a wallet of forwarded mnemonics. Decrypts them with a passed password and stores them in memory, then start Websocket connection to Wallet
        /// </summary>
        /// <param name="mnemonics">Mnemonics by which we generate a wallet</param>
        /// <returns></returns>
        public Wallet.Wallet GenerateWalletWithMenmonic(string mnemonics)
        {
            password = LoadPlayerPrefs(passwordKey);
            try
            {
                string mnem = mnemonics;
                if (!WalletKeyPair.CheckMnemonicValidity(mnem))
                {
                    return null;
                    throw new Exception("Mnemonic is in incorect format");
                }

                this.mnemonics = mnemonics;
                string encryptedMnemonics = cypher.Encrypt(this.mnemonics, password);

                wallet = new Wallet.Wallet(this.mnemonics, WordList.English);
                privateKey = wallet.Account.PrivateKey;

                webSocketService.SubscribeToWalletAccountEvents(wallet.Account.PublicKey);

                SavePlayerPrefs(mnemonicsKey, this.mnemonics);
                SavePlayerPrefs(encryptedMnemonicsKey, encryptedMnemonics);

                return wallet;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return null;
            }
        }

        /// <summary>
        /// Recreates a wallet if we have already been logged in and have mnemonics saved in memory
        /// </summary>
        /// <returns></returns>
        public bool LoadSavedWallet()
        {
            string mnemonicWords = string.Empty;
            if (PlayerPrefs.HasKey(mnemonicsKey))
            {
                try
                {
                    mnemonicWords = LoadPlayerPrefs(mnemonicsKey);

                    wallet = new Wallet.Wallet(mnemonicWords,  WordList.English);
                    webSocketService.SubscribeToWalletAccountEvents(wallet.Account.PublicKey);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// At each login tries to decrypt encrypted mnemonics with the entered password
        /// </summary>
        /// <param name="password"> Password by which we will try to decrypt the mnemonics</param>
        /// <returns></returns>
        public bool LoginCheckMnemonicAndPassword(string password)
        {
            try
            {
                string encryptedMnemonics = LoadPlayerPrefs(encryptedMnemonicsKey);
                cypher.Decrypt(encryptedMnemonics, password);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Returns amount of SOL for Account
        /// </summary>
        /// <param name="account">Account for which we want to check the SOL balance</param>
        /// <returns></returns>
        public async Task<double> GetSolAmmount(Account account)
        {
            AccountInfo result = await AccountUtility.GetAccountData(account, activeRpcClient);
            if (result != null)
                return (double)result.Lamports / 1000000000;
            return 0;
        }

        /// <summary>
        /// Executes a SOL transaction from one account to another
        /// </summary>
        /// <param name="fromAccount">The Account from which we perform the transaction</param>
        /// <param name="toPublicKey">The Account on which we perform the transaction</param>
        /// <param name="ammount">Ammount of sol</param>
        public async void TransferSol(Account fromAccount, string toPublicKey, ulong ammount = 10000000)
        {
            RequestResult<ResponseValue<BlockHash>> blockHash = await activeRpcClient.GetRecentBlockHashAsync();

            var transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey, new PublicKey(toPublicKey), ammount)).Build(fromAccount);

            RequestResult<string> firstSig = await activeRpcClient.SendTransactionAsync(Convert.ToBase64String(transaction));
        }

        /// <summary>
        /// Executes a token transaction on the desired wallet
        /// </summary>
        /// <param name="sourceTokenAccount">Pub Key of the wallet from which we make the transaction</param>
        /// <param name="toWalletAccount">The Pub Key of the wallet to which we want to make a transaction</param>
        /// <param name="sourceAccountOwner">The Account from which we send tokens</param>
        /// <param name="tokenMint"></param>
        /// <param name="ammount">Ammount of tokens we want to send</param>
        /// <returns></returns>
        public async Task<RequestResult<string>> TransferToken(string sourceTokenAccount, string toWalletAccount, Account sourceAccountOwner, string tokenMint, ulong amount = 1)
        {
            
            PublicKey associatedTokenAccountOwner = new PublicKey(toWalletAccount);
            PublicKey mint = new PublicKey(tokenMint);
            Account ownerAccount = wallet.GetAccount(0);
            PublicKey associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(associatedTokenAccountOwner, new PublicKey(tokenMint));
            
            RequestResult<ResponseValue<BlockHash>> blockHash = await activeRpcClient.GetRecentBlockHashAsync();
            RequestResult<ulong> rentExemptionAmmount = await activeRpcClient.GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize);
            TokenAccount[] lortAccounts = await GetOwnedTokenAccounts(toWalletAccount, tokenMint, "");
            byte[] transaction;
            if (lortAccounts != null && lortAccounts.Length > 0)
            {
                transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    AddInstruction(TokenProgram.Transfer(new PublicKey(sourceTokenAccount),
                    new PublicKey(lortAccounts[0].PublicKey),
                    amount,
                    sourceAccountOwner.PublicKey))
                    .Build(sourceAccountOwner);
            }
            else
            {
                Console.WriteLine($"AssociatedTokenAccountOwner: {associatedTokenAccountOwner}");
                Console.WriteLine($"AssociatedTokenAccount: {associatedTokenAccount}");

                transaction = new TransactionBuilder().
                    SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                    SetFeePayer(ownerAccount).
                    AddInstruction(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        ownerAccount,
                        associatedTokenAccountOwner,
                        mint)).
                    AddInstruction(TokenProgram.TransferChecked(
                        ownerAccount,
                        associatedTokenAccount,
                        amount,
                        0,
                        mint,
                        ownerAccount)).// the ownerAccount was set as the mint authority
                    Build(new List<Account> { ownerAccount });
            }

            return await activeRpcClient.SendTransactionAsync(transaction);
        }

        /// <summary>
        /// Initializes a transaction to mint tokens to a destination account.
        /// </summary>
        /// <param name="mint">The token mint.</param>
        /// <param name="destination">The account to mint tokens to.</param>
        /// <param name="amount">The amount of tokens.</param>
        /// <returns></returns>
        public async Task<RequestResult<string>> MintTo(string mint, string destination, ulong amount = 1)
        {
            RequestResult<ResponseValue<BlockHash>> blockHash = await activeRpcClient.GetRecentBlockHashAsync();
            Account fromAccount = wallet.GetAccount(0);

            var transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                AddInstruction(TokenProgram.MintTo(new PublicKey(mint), new PublicKey(destination), amount, fromAccount.PublicKey)).Build(fromAccount);

            return await activeRpcClient.SendTransactionAsync(Convert.ToBase64String(transaction));
        }

        /// <summary>
        /// The key of the account on which we want to execute the transaction
        /// </summary>
        /// <param name="toPublicKey"> Public key of wallet on which we want to execute the transaction </param>
        /// <param name="ammount"> Ammount of sol we want to send</param>
        /// <returns></returns>
        public async Task<RequestResult<string>> TransferSol(string toPublicKey, ulong ammount = 10000000)
        {
            RequestResult<ResponseValue<BlockHash>> blockHash = await activeRpcClient.GetRecentBlockHashAsync();
            Account fromAccount = wallet.GetAccount(0);

            var transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey, new PublicKey(toPublicKey), ammount)).Build(fromAccount);

            return await activeRpcClient.SendTransactionAsync(Convert.ToBase64String(transaction));
        }

        /// <summary>
        /// Airdrop sol on wallet
        /// </summary>
        /// <param name="account">Account to which send sol</param>
        /// <param name="ammount">Amount of sol</param>
        /// <returns>Amount of sol</returns>
        public async Task<string> RequestAirdrop(Account account, ulong ammount = 1000000000)
        {
            var result = await activeRpcClient.RequestAirdropAsync(account.PublicKey, ammount);
            return result.Result;
        }
        

        /// <summary>
        /// Returns an array of tokens on the account
        /// </summary>
        /// <param name="account">The account for which we are requesting tokens</param>
        /// <returns>Array of tokens</returns>
        public async Task<TokenAccount[]> GetOwnedTokenAccounts(Account account)
        {
            try
            {
                RequestResult<ResponseValue<List<TokenAccount>>> result = await activeRpcClient.GetTokenAccountsByOwnerAsync(account.PublicKey, null, TokenProgram.ProgramIdKey);
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
        /// It disconnects the websocket connection and deletes the wallet we were logged into
        /// </summary>
        public void DeleteWalletAndClearKey()
        {
            webSocketService.UnSubscribeToWalletAccountEvents();
            wallet = null;
        }

        /// <summary>
        /// A function that automatically initiates a websocket connection to the wallet when we log in
        /// </summary>
        public void StartWebSocketConnection()
        {
            if (webSocketService.Socket != null) return;

            webSocketService.StartConnection(GetWebsocketConnectionURL(clientSource));
        }

        #region Data Functions
        public void SavePlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
#if UNITY_WEBGL
            PlayerPrefs.Save();
#endif
        }

        public string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        #endregion

        #region Getters And Setters
        public string MnemonicsKey => mnemonicsKey;
        public string EncryptedMnemonicsKey => encryptedMnemonicsKey;
        public string PasswordKey => passwordKey;
        public string PrivateKeyKey => privateKeyKey;
        #endregion
    }
}
