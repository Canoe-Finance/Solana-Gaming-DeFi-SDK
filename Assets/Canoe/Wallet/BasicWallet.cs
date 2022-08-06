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
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Canoe
{
    public class BasicWallet : MonoBehaviour
    {
        public static BasicWallet Instance;


        public Wallet Wallet { get; set; }
        public string Mnemonics { get; private set; }
        public string Password { get; private set; }
        public string PrivateKey { get; private set; }

        [HideInInspector]
        public WebSocketService webSocketService;
        private Cypher cypher;

        private string mnemonicsKey = "Mnemonics";
        private string passwordKey = "Password";
        private string encryptedMnemonicsKey = "EncryptedMnemonics";
        private string privateKeyKey = "PrivateKey";
        private void Awake()
        {
            Instance = this;
            webSocketService = new WebSocketService();
            cypher = new Cypher();
            //TokenAccount[] result = await SimpleWallet.instance.GetOwnedTokenAccounts(SimpleWallet.instance.wallet.GetAccount(0));
        }
        public Wallet CurrentWallet;

   
        /// <summary>
        /// try to login with input pwd
        /// </summary>
        /// <param name="password"></param>
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
        ///  if there is a wallet 
        /// </summary>
        /// <returns></returns>
        public bool HasWallet()
        {
            return PlayerPrefs.HasKey(encryptedMnemonicsKey);
        }

        ///// <summary>
        ///// Recreates a wallet if we have already been logged in and have mnemonics saved in memory
        ///// </summary>
        ///// <returns></returns>
        //public bool LoadSavedWallet()
        //{
        //    string mnemonicWords = string.Empty;
        //    if (PlayerPrefs.HasKey(mnemonicsKey))
        //    {
        //        try
        //        {
        //            mnemonicWords = LoadPlayerPrefs(mnemonicsKey);

        //            Wallet = new Wallet(mnemonicWords, WordList.English);
        //            webSocketService.SubscribeToWalletAccountEvents(Wallet.Account.PublicKey);
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            return false;
        //        }
        //    }
        //    return false;
        //}

        public string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        /// <summary>
        /// Restore the wallet using the mnemonic
        /// </summary>
        /// <param name="mnemonics"></param>
        /// <returns></returns>
        public Wallet RestoreWalletWithMenmonic(string mnemonics)
        {
            try
            {
                //string mnem = mnemonics;
                if (!WalletKeyPair.CheckMnemonicValidity(mnemonics))
                {
                    return null;
                    throw new Exception("Mnemonic is in incorect format");
                }
                CurrentWallet = new Wallet(mnemonics, WordList.English);
                return CurrentWallet;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return null;
            }
        }

        public void OnDestroy()
        {
            webSocketService.CloseConnection();
        }

        public void GenerateWallet()
        {
            Mnemonic newMnemonic = new Mnemonic(WordList.English, WordCount.Twelve);
            CurrentWallet = new Wallet(newMnemonic);
            Debug.Log("words:" + CurrentWallet.Mnemonic);
            Debug.Log("pubkey:" + CurrentWallet.Account.PublicKey);
            Debug.Log("privatekey:" + CurrentWallet.Account.PrivateKey);
        }

       //public  async Task<double> GetSolBananceAsync()
       // {
       //     IRpcClient rpcClient = ClientFactory.GetClient(Cluster.DevNet);
       //     double sol = await GetSolAmmount(CurrentWallet.GetAccount(0), rpcClient);
       //     Debug.Log("banalce:" + sol);
       //     return sol;
       // }
        public async Task<double> GetSolAmmount(Account account, IRpcClient rpcClient)
        {
            AccountInfo result = await AccountUtility.GetAccountData(account, rpcClient);
            if (result != null)
                return (double)result.Lamports / 1000000000;
            return 0;
        }

        public async Task<RequestResult<string>> TransferSol(string toPublicKey, ulong ammount = 100000000)
        {
            RequestResult<ResponseValue<BlockHash>> blockHash = await ClientFactory.GetClient(Cluster.DevNet).GetRecentBlockHashAsync();
            Account fromAccount = CurrentWallet.GetAccount(0);

            var transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey, new PublicKey(toPublicKey), ammount)).
                SetFeePayer(fromAccount).
                Build(fromAccount);

            return await ClientFactory.GetClient(Cluster.DevNet).SendTransactionAsync(Convert.ToBase64String(transaction));

        }

        public async Task<TokenAccount[]> GetOwnedTokenAccounts(Account account)
        {
            try
            {
                RequestResult<ResponseValue<List<TokenAccount>>> result = await ClientFactory.GetClient(Cluster.DevNet).GetTokenAccountsByOwnerAsync(account.PublicKey, null, TokenProgram.ProgramIdKey);
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

        public async Task<RequestResult<string>> TransferToken(string sourceTokenAccount, string toWalletAccount, Account sourceAccountOwner, string tokenMint, ulong amount = 1)
        {

            PublicKey associatedTokenAccountOwner = new PublicKey(toWalletAccount);
            PublicKey mint = new PublicKey(tokenMint);
            Account ownerAccount = CurrentWallet.GetAccount(0);
            PublicKey associatedTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(associatedTokenAccountOwner, new PublicKey(tokenMint));

            RequestResult<ResponseValue<BlockHash>> blockHash = await ClientFactory.GetClient(Cluster.DevNet).GetRecentBlockHashAsync();
            RequestResult<ulong> rentExemptionAmmount = await ClientFactory.GetClient(Cluster.DevNet).GetMinimumBalanceForRentExemptionAsync(TokenProgram.TokenAccountDataSize);

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

            return await ClientFactory.GetClient(Cluster.DevNet).SendTransactionAsync(transaction);
        }

        public async Task<TokenAccount[]> GetOwnedTokenAccounts(string walletPubKey, string tokenMintPubKey, string tokenProgramPublicKey)
        {
            RequestResult<ResponseValue<List<TokenAccount>>> result = await ClientFactory.GetClient(Cluster.DevNet).GetTokenAccountsByOwnerAsync(walletPubKey, tokenMintPubKey, tokenProgramPublicKey);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value.ToArray();
            }
            return null;
        }

        public async Task<AccountInfo> GetAccountData(PublicKey account)
        {
            RequestResult<ResponseValue<AccountInfo>> result = await ClientFactory.GetClient(Cluster.DevNet).GetAccountInfoAsync(account);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value;
            }
            return null;
        }
    }

}
