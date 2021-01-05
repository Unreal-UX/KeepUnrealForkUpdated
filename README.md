# Keep Unreal Fork Updated

The github.com/EpicGames/UnrealEngine repo has over 30k forks. As a result using the GitHub UI to update them can be a real struggle. 

This Action (when completed) will create PR's to update your branches as needed


```yml
name: Sync Fork

on:
  schedule:
    - cron: '00 03 * * *' # run daily
      
  workflow_dispatch: # on button click

env:
    Owner: Pauliver 
    Repo: UE 
    Branch: "4.26" 
    ForceMerge: true 
    Source_Owner: EpicGames 
    Source_Repo: UnrealEngine 
    Source_Branch: "4.26"

jobs:
  sync_with_upstream:
    runs-on: ubuntu-latest
    name: sync fork

    steps:
    # Step 1: run a standard checkout action, provided by github
    - name: Checkout Image Tools
      uses: actions/checkout@v2
      with:
        repository: Unreal-UX/KeepUnrealForkUpdated
        path: UE4Tools

    - name: Setup dotnet
      uses: actions/setup-dotnet@v1

    - name: Restore Dependancies
      run: dotnet restore UE4Tools/

    - name: Build Image Tools
      run: dotnet build UE4Tools/ --configuration Release
      
    - name: Attempting to create and merge PR
      run: dotnet  ${{github.workspace}}/UE4Tools/bin/Release/netcoreapp3.1/KeepUE4Updated.dll ${{ env.Owner}} ${{env.Repo}} ${{env.Branch}} ${{env.ForceMerge}} ${{env.Source_Owner}} ${{env.Source_Repo}} ${{env.Source_Branch}}
      env:
        GITHUB_TOKEN: ${{ secrets.PAT }}

    # Step 4: Print a helpful timestamp for your records (not required, just nice)
    - name: Timestamp
      run: date
```
