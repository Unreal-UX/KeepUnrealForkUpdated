using System;
using System.Text;
using System.IO;
using Octokit;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace OKW
{
    // It's just an OK wrapper, not a great wrapper
    // we are wrappign OctoKit
    public class OctoKitWrapper
    {
        protected int APICallsRemaining = 0;

        protected string repo = "Repo";
        protected string owner = "Owner";

        bool gitHubStuff = false;
        private bool cleanlyLoggedIn = false;
        private GitHubClient github = null;
        System.IO.DirectoryInfo repoDirectory; 

        public GitHubClient GithubClient { get => github; set => github = value; }
        public bool CleanlyLoggedIn { get => cleanlyLoggedIn; }
        public DirectoryInfo RepoDirectory { get => repoDirectory; set => repoDirectory = value; }
        public bool DoGitHubStuff { get => gitHubStuff; set => gitHubStuff = value; }
        public bool ImagesWereCommtted { get => imagesWereCommtted; }

        protected string email;
        protected string username;

        public void TestCleanlyLoggedIn()
        {
            if(!cleanlyLoggedIn)
            {
                Console.WriteLine("We have not cleanlyLogged In, but we are trying to do stuff!");
                Console.WriteLine(System.Environment.StackTrace);
            }
        }

        public OctoKitWrapper(bool gitHubStuff)
        {
            this.gitHubStuff = gitHubStuff;
            this.username = "GitHub Action";
            this.email = "actions@users.noreply.github.com";
        }

        public bool SetOwnerAndRepo(string p_Owner, string p_Repo)
        {
            this.owner = p_Owner;
            this.repo = p_Repo;
            Console.WriteLine("Owner Set to : " + this.owner);
            Console.WriteLine("Repo set to : " + this.repo);
            return true;
        }
        
        public bool PrintRateLimits()
        {
            try{
                Task<MiscellaneousRateLimit> limits = github.Miscellaneous.GetRateLimits();
                var awaitlimits = limits.GetAwaiter().GetResult(); 
                APICallsRemaining = awaitlimits.Rate.Remaining;
                Console.WriteLine("Rate Limit Total: " + awaitlimits.Rate.Limit);
                Console.WriteLine("Rate Limit Remaing: " + APICallsRemaining);
                Console.WriteLine("Rate Limit Resets: " + awaitlimits.Rate.Reset.ToString("MM/dd/yyyy h:mm tt"));   
                }catch(Exception rc)
                {
                    Console.Write(rc.ToString());
                    return false;
                }   
            return true;
        }

        public bool AttemptLogin()
        {
            try{
                Console.WriteLine("Loading github...");
                string secretkey = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                github = new GitHubClient(new ProductHeaderValue("Perfect-Is-Enamy-Of-Good-Enough"))
                {
                    Credentials = new Credentials(secretkey)
                };
                cleanlyLoggedIn = true; // or maybe
                Console.WriteLine("... Loaded");

                try{
                    Task<MiscellaneousRateLimit> limits = github.Miscellaneous.GetRateLimits();
                    var awaitlimits = limits.GetAwaiter().GetResult(); 
                    APICallsRemaining = awaitlimits.Rate.Remaining;
                    Console.WriteLine("Rate Limit Total: " + awaitlimits.Rate.Limit);
                    Console.WriteLine("Rate Limit Remaing: " + APICallsRemaining);
                    Console.WriteLine("Rate Limit Resets: " + awaitlimits.Rate.Reset.ToString("MM/dd/yyyy h:mm tt"));
                    
                }catch(Exception rc)
                {
                    Console.Write(rc.ToString());
                }

                return true;
            }catch(Exception ex){
                Console.WriteLine(ex.ToString());
                Console.WriteLine("... Loading Failed");
                cleanlyLoggedIn = false;
                return false;
            }
        }

        public int DecrementAPICallsBy(int num = 1)
        {
            APICallsRemaining -= num;
            if(APICallsRemaining <= 0)
            {
                Console.WriteLine("THIS IS YOUR LAST API CALL< OR THERE ARE NON LEFT");
            }
            return APICallsRemaining;
        }

        public void RefreshRateLimits()
        {
            try{
                Task<MiscellaneousRateLimit> limits = github.Miscellaneous.GetRateLimits();
                var awaitlimits = limits.GetAwaiter().GetResult(); 
                APICallsRemaining = awaitlimits.Rate.Remaining;
                Console.WriteLine("Rate Limit Total: " + awaitlimits.Rate.Limit);
                Console.WriteLine("Rate Limit Remaing: " + APICallsRemaining);
                Console.WriteLine("Rate Limit Resets: " + awaitlimits.Rate.Reset.ToString("MM/dd/yyyy h:mm tt"));
            }
            catch(Exception rc)
            {
                Console.Write(rc.ToString());
            }
        }

        protected string CurrentBranchName;
        protected string TargetBranchName;
        protected string AutoMergeLabel;
        bool SetupForMergByLabel = false;
        public bool SetupForMergesByLabel(string p_autoMergeLabel = "automerge", string currentBranch = "master", string targetBranch = "gh-pages")
        {
            this.CurrentBranchName = currentBranch;
            this.TargetBranchName = targetBranch;
            this.AutoMergeLabel = p_autoMergeLabel;

            SetupForMergByLabel = true;

            return true;
        } 

        async public ValueTask<bool> CommitContainedImages(System.IO.DirectoryInfo GalleryPath, int commit_num = -1)
        {
            TestCleanlyLoggedIn();
            
            if(commit_num == -1)
            {
                // use latest
            }else{
                // use commit number
            }
            // https://snips-nlu.readthedocs.io/en/latest/data_model.html#builtin-entities-and-resolution


            Octokit.CommitRequest ocr = new CommitRequest();
            
            // Only checking, check-ins into the gallery folder
            ocr.Path = "/" + GalleryPath.Name + "/";

            // in the past 1 day  (@@PAULIVER - CHANGE THIS TO WEEK)
            ocr.Since = new DateTimeOffset(System.DateTime.UtcNow.AddDays(-1));
            // until now
            ocr.Until = new DateTimeOffset(System.DateTime.UtcNow);

            // there was likely one, which would have triggered this script
            try{
                
                // Get all the files modified
                // Were any thumbnails deleted, were any resized images deleted?
                // were any images added or removed
                var commits = await github.Repository.Commit.GetAll(owner, repo, ocr, ApiOptions.None); DecrementAPICallsBy();

                Console.WriteLine("There have been " + commits.Count + " Commits");

                Console.Write("   From " );
                if(ocr.Since != null)
                    Console.Write(ocr.Since.Value.ToString("MMMM dd h:mm tt"));
                Console.Write(" to ");
                if(ocr.Until != null)
                    Console.Write(ocr.Until.Value.ToString("MMMM dd h:mm tt")); 
                Console.WriteLine("  --:");

                if(commits.Count == 0)
                {
                    return false;
                }

                // go backwards through commits and see what was added since the last .json commit
                // were there any images?

                //foreach(GitHubCommit gc in commits)
                //{
                //    Console.WriteLine(gc.ToString());
                //}

            }catch(Octokit.NotFoundException nfe)
            {
                Console.WriteLine("Not Found Exception : No images changed?");
                Console.WriteLine(nfe.ToString());
                // if no images were changed, then return false and we can skip all the time it take sto process images
                return false;
            }catch(Exception ex)
            {
                this.cleanlyLoggedIn = false;
                Console.WriteLine(ex.ToString());
            }

            return true;
        }

        protected string headMasterRef;
        protected Reference masterReference;
        protected Commit latestCommit;
        protected NewTree UpdatedTree;

        async public ValueTask<bool> SetupCommit()
        {
            TestCleanlyLoggedIn();
            //https://laedit.net/2016/11/12/GitHub-commit-with-Octokit-net.html
            try
            {
                headMasterRef = "heads/master";
                // Get reference of master branch
                masterReference = await github.Git.Reference.Get(owner, repo, headMasterRef); DecrementAPICallsBy();
                // Get the laster commit of this branch
                latestCommit = await github.Git.Commit.Get(owner, repo, masterReference.Object.Sha); DecrementAPICallsBy();

                UpdatedTree = new NewTree {BaseTree = latestCommit.Tree.Sha };

            }catch(Exception ex)
            {
                cleanlyLoggedIn = false;

                Console.WriteLine(ex.ToString());
                
                return false;
            }
            return true;
        }

        const string NEWFILE = "NEW-FILE";
        const string TOOBIG = "TOO-BIG";

        const string MULTIPLERESULTS = "MULTIPLE-RESULTS";
        const string ZERORESULTS = "ZERO-RESULTS";
        private const string COMMITMESSAGE = "Time's up, let's do this";

        // Flip this to a bitmask enum return? and a string with an SHA, so we can pack the bitmask with the flags we need, and return the SHA
        async public Task<string> GetFileSHA(string filename)
        {
            // https://developer.github.com/v3/repos/contents/#get-contents
            // if this is the first time we have seen the file return
            IReadOnlyList<Octokit.RepositoryContent> content = null; 
            try{
                content = await github.Repository.Content.GetAllContents(owner,repo,filename); DecrementAPICallsBy();
            }catch(Exception ex)
            {
                if(ex is Octokit.NotFoundException)
                {
                    return ZERORESULTS;
                }
                Console.WriteLine(ex.ToString());
            }

            if(content.Count > 1)
            {
                return MULTIPLERESULTS;
            }else if(content.Count == 0)
            {
                return ZERORESULTS;
            }
            // should we consider testing for (content[0].Size) so we can use "too big" ?
            return content[0].Sha;
        }

        async public ValueTask<bool> JustOneFileExists(string filename)
        {
            Console.WriteLine("Checking if file exists: " + filename);
            IReadOnlyList<Octokit.RepositoryContent> content = null;

            try{
                content = await github.Repository.Content.GetAllContents(owner, repo, filename); DecrementAPICallsBy();
            }catch(Exception ex)
            {
                if(ex is Octokit.NotFoundException)
                {
                    return false;
                }
                Console.WriteLine(ex.ToString());
            }

            if(content.Count == 1)
            {
                return true;
            }
            return false;
        }

        protected bool imagesWereCommtted = false;
        public void SetNumberImagesCommitted(int NumCommitted)
        {
            if(NumCommitted >= 1)
            {
                imagesWereCommtted = true; 
            }else{
                imagesWereCommtted = false;
            }
        }

        async public ValueTask<bool> ImmediatlyAddorUpdateTextFile(System.IO.FileInfo fi)
        {
            // We are using an API that has a limit of 1mb files
            // so this will not work for our images
            TestCleanlyLoggedIn();
            string filename = fi.FullName.Replace(repoDirectory.FullName,"");
            string SHA = await GetFileSHA(filename); 
            
            try
            {
                string filecontnet = File.ReadAllText(fi.FullName);

                // This is one implementation of the abstract class SHA1.
                //var File_SHA = SHA1Util.SHA1HashStringForUTF8String(filecontnet);

                
    

                if(SHA == ZERORESULTS)
                {
                    Console.WriteLine("retrieved Zero results, was expecting one. Creating file instead");
                    var temp = await github.Repository.Content.CreateFile(owner,repo,filename,new CreateFileRequest("Created " + fi.Name,filecontnet)); DecrementAPICallsBy();

                }
                else if(SHA == MULTIPLERESULTS)
                {
                    Console.WriteLine("retrieved multiple results, was expecting one. can't continue");
                    return false;
                }
                else if(SHA == TOOBIG)
                {
                    Console.WriteLine("attempted to retrieve a file over 1mb from an API that limits to 1mb");
                    return false;
                }else{
                    var fur = new UpdateFileRequest("Updated " + fi.Name, filecontnet, SHA);
                    var temp = await github.Repository.Content.UpdateFile(owner,repo,filename, fur); DecrementAPICallsBy();
                }

            }catch(Exception ex)
            {
                cleanlyLoggedIn = false;

                Console.WriteLine(ex.ToString());
                
                return false;
            }
            return true;
        }
        
        public async ValueTask<bool> CloseStalePullRequests(string PRname)
        {
            return await FindStalePullRequests(PRname, false);
        }

        public async ValueTask<bool> FindStalePullRequests(string PRname, bool try_and_close = false)
        {
            TestCleanlyLoggedIn();

            bool ShouldClose = false;

            bool FoundAnyStale = false;

            var prs = await github.PullRequest.GetAllForRepository(owner,repo);
            
            foreach(PullRequest pr in prs)
            {
                foreach(Label l in pr.Labels)
                {
                    ShouldClose = false;
                    if(l.Name == AutoMergeLabel && pr.Title.Contains(PRname)) // I'm left over from a previous run
                    {
                        ShouldClose = true;
                    }
                    if(ShouldClose)
                    {
                        FoundAnyStale = true;
                        Console.WriteLine("It looks like you have an existing PR still open");
                        Console.WriteLine("This is likely to fail, unless you close : " + pr.Title);
                        if(try_and_close)
                        {
                            PullRequestUpdate pru = new PullRequestUpdate();
                            pru.State = ItemState.Closed;
                            var newpr = await github.PullRequest.Update(owner,repo,(int)pr.Id,pru); DecrementAPICallsBy();
                            Console.WriteLine("New Pr: " + newpr.Title + " is now " + newpr.State );
                        }
                    }
                    ShouldClose = false;
                }
            }
            return FoundAnyStale;
        }

        async public ValueTask<bool> CreateAndLabelPullRequest(string PRname)
        {
            TestCleanlyLoggedIn();

            Console.WriteLine("PR: " + PRname);
            Console.WriteLine("Owner: " + owner);
            Console.WriteLine("CurrentBranch: " + CurrentBranchName);
            Console.WriteLine("TargetBranch: " + TargetBranchName);

            NewPullRequest newPr = new NewPullRequest(PRname + " : " + System.DateTime.UtcNow.ToString(),CurrentBranchName,TargetBranchName);
            PullRequest pullRequest = await github.PullRequest.Create(owner,repo,newPr); DecrementAPICallsBy();
            
            Console.WriteLine("PR Created # : " + pullRequest.Number);

            Console.WriteLine("PR Created: " + pullRequest.Title);

            //var prupdate = new PullRequestUpdate();
            //var newUpdate = await github.PullRequest.Update(Owner,Repo,pullRequest.Number,prupdate);

            try{

                Console.WriteLine("Owner: " + PRname);        
                Console.WriteLine("Repo: " + repo);
                Console.WriteLine("pullRequest.Number: " + pullRequest.Number);

                if(github == null){  
                    Console.WriteLine("github == null");
                }
                if(github.Issue == null){
                    Console.WriteLine("github.Issue == null");
                }

                var issue = await github.Issue.Get(owner, repo, pullRequest.Number); DecrementAPICallsBy();
                if(issue != null) //https://octokitnet.readthedocs.io/en/latest/issues/
                {
                    var issueUpdate = issue.ToUpdate();
                    if(issueUpdate != null)
                    {
                        issueUpdate.AddLabel(AutoMergeLabel);
                        var labeladded = await github.Issue.Update(owner, repo, pullRequest.Number, issueUpdate); DecrementAPICallsBy();
                        Console.WriteLine("Label Added: " + AutoMergeLabel);
                    }
                }

            }catch(Exception ex){
                cleanlyLoggedIn = false;
                Console.WriteLine(ex.ToString());  
                return false; 
            }
        return true;
        }


        async public ValueTask<bool> AddorUpdateBinaryFile(System.IO.FileInfo fi)
        {
            TestCleanlyLoggedIn();
            try
            {
                // For image, get image content and convert it to base64
                var imgBase64 = Convert.ToBase64String(File.ReadAllBytes(fi.FullName));
                
                // Create image blob
                var imgBlob = new NewBlob { Encoding = EncodingType.Base64, Content = (imgBase64) };

                string FileName = fi.FullName.Replace(repoDirectory.FullName,"").Replace("\\","/");
                
                // Both these paths are the same, and we need fewer API calls. This will be 300 fewer API calls now

                //bool FileExists = await JustOneFileExists( FileName );
                //if(FileExists)
                //{
                //    var imgBlobRef = await github.Git.Blob.Create(owner, repo, imgBlob);
                //
                //    UpdatedTree.Tree.Add(new NewTreeItem { Path = FileName, Mode = "100644", Type = TreeType.Blob, Sha = imgBlobRef.Sha });
                //}else

                {
                    var imgBlobRef = await github.Git.Blob.Create(owner, repo, imgBlob); DecrementAPICallsBy();

                    UpdatedTree.Tree.Add(new NewTreeItem { Path = FileName, Mode = "100644", Type = TreeType.Blob, Sha = imgBlobRef.Sha });
                }

                // Is the file in the repo?
                // - if not add it
                // - if it is update it
            }catch(Exception ex)
            {
                cleanlyLoggedIn = false;

                Console.WriteLine(ex.ToString());
                
                return false;
            }
            return true;
        }

        async public ValueTask<bool> CommitBinaryFile(System.IO.FileInfo fi)
        {
            return await AddorUpdateBinaryFile(fi);
        }

        async public ValueTask<bool> CommitAndPush()
        {
            TestCleanlyLoggedIn();
            try{
                var newTree = await github.Git.Tree.Create(owner, repo, UpdatedTree); DecrementAPICallsBy();
                var newCommit = new NewCommit("Updated Images and json files", newTree.Sha, masterReference.Object.Sha);
                var commit = await github.Git.Commit.Create(owner, repo, newCommit); DecrementAPICallsBy();
                var headMasterRef = "heads/master";
                // Update HEAD with the commit
                await github.Git.Reference.Update(owner, repo, headMasterRef, new ReferenceUpdate(commit.Sha)); DecrementAPICallsBy();
            }catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        async public ValueTask<bool> MergePullRequest(string CommitMessage = COMMITMESSAGE)
        {
            Console.WriteLine("Merging pull requests For: " + owner + " \\ " + repo );
            Console.WriteLine(" - With Label : " + AutoMergeLabel );
            Console.WriteLine(" - Message will be : " + CommitMessage);
            if(CommitMessage == COMMITMESSAGE)
            {
                Console.WriteLine(" - LLLLEEEERROOOYYYY JENKINS");
            }

            bool shouldmerge = false;

            var prs = await github.PullRequest.GetAllForRepository(owner,repo); DecrementAPICallsBy();
                
            foreach(PullRequest pr in prs)
            {
                shouldmerge = false; // Reset state in a loop
                Console.WriteLine("Found PR: " + pr.Title);

                foreach(Label l in pr.Labels)
                {

                    if(l.Name == AutoMergeLabel)
                    {
                        shouldmerge = true;
                    }
                    
                    if(false)// Add your own conditions here, or perhaps a "NEVER MERGE" label?
                    {
                        shouldmerge = true;
                    }

                }
                if(shouldmerge)
                {
                    MergePullRequest mpr = new MergePullRequest();
                    mpr.CommitMessage = CommitMessage;
                    mpr.MergeMethod = PullRequestMergeMethod.Merge;
                    
                    var merge = await github.PullRequest.Merge(owner,repo,pr.Number,mpr); DecrementAPICallsBy();
                    if(merge.Merged)
                    {
                        Console.WriteLine("-> " + pr.Number + " - Successfully Merged");
                    }else{
                        Console.WriteLine("-> " + pr.Number + " - Merge Failed");
                    }
                }
                shouldmerge = false; // Reset state in a loop
            }
            return true;
        }

        
        public async ValueTask<bool> SyncPoint(bool await = true)
        {
            // It looks like the GitHub stuff gets pretty out of wack with too much going on Async? SHA's end up wrong, etc... The tree ends up in bad state. 

            // leaving this function here as a warning to future paul. DON"T GET CLEVER. You can't go Async with the commits to GitHub. 

            return true;
        }
        
    }

    public class PaulsOKSIngleton
    {

    }
}
