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
    public class CanoeWallet : MonoBehaviour
    {
        public static CanoeWallet Instance;


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
            cypher = new Cypher();
        }
        public Wallet CurrentWallet;

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
                cypher.Decrypt(encryptedMnemonics, password);
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
        public Wallet RestoreWalletWithMenmonic(string mnemonics,string password)
        {
            try
            {
                //check if the mnemonic currect ;
                if (!WalletKeyPair.CheckMnemonicValidity(mnemonics))
                {
                    return null;
                    throw new Exception("Mnemonic is in incorect format");
                }

                CurrentWallet = new Wallet(mnemonics, WordList.English);

                //save the encryptedMnemonics, user can login with pwd next time
                string encryptedMnemonics = cypher.Encrypt(mnemonics, password);
                PlayerPrefs.SetString(encryptedMnemonicsKey, encryptedMnemonics);


                return CurrentWallet;
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
                return null;
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
            Debug.Log("words:" + CurrentWallet.Mnemonic);
            Debug.Log("pubkey:" + CurrentWallet.Account.PublicKey);
            Debug.Log("privatekey:" + CurrentWallet.Account.PrivateKey);
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
        public async Task<double> GetSolAmmountAsync(Account account, IRpcClient rpcClient)
        {
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
            RequestResult<ResponseValue<BlockHash>> blockHash = await ClientFactory.GetClient(Cluster.DevNet).GetRecentBlockHashAsync();
            Account fromAccount = CurrentWallet.GetAccount(0);

            var transaction = new TransactionBuilder().SetRecentBlockHash(blockHash.Result.Value.Blockhash).
                AddInstruction(SystemProgram.Transfer(fromAccount.PublicKey, new PublicKey(toPublicKey), ammount)).
                SetFeePayer(fromAccount).
                Build(fromAccount);

            return await ClientFactory.GetClient(Cluster.DevNet).SendTransactionAsync(Convert.ToBase64String(transaction));

        }

        /// <summary>
        /// get user's token list
        /// </summary>
        /// <param name="account">user address</param>
        /// <returns>list of all token infos</returns>
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

        private async Task<TokenAccount[]> GetOwnedTokenAccounts(string walletPubKey, string tokenMintPubKey, string tokenProgramPublicKey)
        {
            RequestResult<ResponseValue<List<TokenAccount>>> result = await ClientFactory.GetClient(Cluster.DevNet).GetTokenAccountsByOwnerAsync(walletPubKey, tokenMintPubKey, tokenProgramPublicKey);
            if (result.Result != null && result.Result.Value != null)
            {
                return result.Result.Value.ToArray();
            }
            return null;
        }

        private async Task<AccountInfo> GetAccountData(PublicKey account)
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
