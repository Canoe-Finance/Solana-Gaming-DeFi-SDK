using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK.Utility;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Canoe
{
    public class SimpleWallet : MonoBehaviour
    {
        public static SimpleWallet Instance;
        private void Awake()
        {
            Instance = this;
        }
        public Wallet CurrentWallet;
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
    }
}
