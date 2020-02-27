using System;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;
using System.Text.Json.Serialization;
using Octokit;

namespace Merge_Pull_Request
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("need atleast 3 args");
                return;
            }
            var Repo = "Repo";
            if(args[0] is string)
            {
                Repo = args[0] as string;
            }
            var Owner = "Owner";
            if(args[1] is string)
            {
                Owner = args[1] as string;
            }
            var AutoMergeLabel = "automerge";
            if(args[2] is string)
            {
                AutoMergeLabel = args[2] as string;
            }

            GitHubClient github = null;
            bool LoggedIn = false;

            // come back and make every arg beyond 3 also a valid label maybe?
            
            try{
                Console.WriteLine("Loading github...");
                string secretkey = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                github = new GitHubClient(new ProductHeaderValue("Pauliver-MergePR-By-Label"))
                {
                    Credentials = new Credentials(secretkey)
                };
                LoggedIn = true; // or maybe
                Console.WriteLine("... Loaded");
            }catch(Exception ex){
                Console.WriteLine(ex.ToString());
                Console.WriteLine("... Loading Failed");
                LoggedIn = false;
            }
            

            Console.WriteLine(" --- ");

            if(!LoggedIn)
            {
                Console.WriteLine("GitHub Login State unclear, Exiting");
                return;
            }
            
            bool shouldmerge = false;

            var prs = await github.PullRequest.GetAllForRepository(Owner,Repo);
                
            foreach(PullRequest pr in prs)
            {
                foreach(Label l in pr.Labels)
                {
                    if(l.Name == AutoMergeLabel)
                    {
                        shouldmerge = true;
                    }
                    
                    // Add your own conditions above

                    if(shouldmerge)
                    {
                        MergePullRequest mpr = new MergePullRequest();
                        mpr.CommitMessage = "Time's up, let's do this";
                        mpr.MergeMethod = PullRequestMergeMethod.Merge;
                        
                        var merge = await github.PullRequest.Merge(Owner,Repo,pr.Number,mpr);
                        if(merge.Merged)
                        {
                            Console.WriteLine("Successfully Merged");
                        }else{
                            Console.WriteLine("Merge Failed");
                        }
                    }
                }
            
                Console.WriteLine("And we are done here");
            }
        }
    }
}
