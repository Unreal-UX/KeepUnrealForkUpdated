using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;
using System.Text.Json.Serialization;
using Octokit;
//using OKW;


namespace KeepUE4Updated
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var Owner = "Owner";
            var Repo = "Repo";
            var AutoMerge = true;
            var SourceOwner = "EpicGames";
            var SourceRepo = "UnrealEngine";
            var SourceBranch = "release";
            var PRNAME = "UE4-Auto-Merge";
            var TargetBranch = "release";
            if(args.Length < 2)
            {
                Console.WriteLine("need atleast 2 args");
                return;
            }
            if(args[0] is string)
            {
                Owner = args[0] as string;
            }
            if(args[1] is string)
            {
                Repo = args[1] as string;
            }
            if(args[2] is string)
            {
                TargetBranch = args[2] as string;
            }
            if(args[3] is string)
            {
                try{
                    AutoMerge = Boolean.Parse( args[3] as string);
                }catch{
                    Console.WriteLine("Arg 3 was not a boolean");
                }
            }
            if(args[4] is string)
            {
                SourceOwner = args[4];
            }
            if(args[5] is string)
            {
                SourceRepo = args[5];
            }
            if(args[6] is string)
            {
                SourceBranch = args[6];
            }

            Console.WriteLine("");
            Console.WriteLine("Creating GitHub Client");
            OKW.OctoKitWrapper github = new OKW.OctoKitWrapper(false);
            
            Console.WriteLine("");
            Console.WriteLine("Setting Owner and Repo");
            github.SetOwnerAndRepo(Owner,Repo);

            Console.WriteLine("");
            Console.WriteLine("Attempting Login");
            github.AttemptLogin("KeepUEForkUpdated");
   
            if(!github.CleanlyLoggedIn)
            {
                Console.WriteLine("GitHub Login Failed");
            }

            Console.WriteLine("");
            Console.WriteLine("Closing Stale Pull Request");
            await github.CloseStalePullRequests(PRNAME);

            Console.WriteLine("");
            Console.WriteLine("Creating cross branch PR");
            await github.CrossBranchPR(PRNAME,SourceOwner,SourceRepo,SourceBranch,Owner,Repo,TargetBranch);

            Console.WriteLine("");
            Console.WriteLine("Merging Pull requests");            
            await github.MergePullRequest("Another UE4 Branch Updated");


            Console.WriteLine("");
            Console.WriteLine("Exiting");
        }
    }
}
