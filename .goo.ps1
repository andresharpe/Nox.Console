<# goo.ps1 - Type less. Code more.

    Develop, build, test and run helper script built on Powershell

    Developed by Andre Sharpe on October, 24 2020.

    www.goo.dev

    1. '.\.goo' will output the comment headers for each implemented command
    
    2. Add a function with its purpose in its comment header to extend this project's goo file 

    3. 'goo <function>' will run your commands 
#>

<# --- NEW GOO INSTANCE --- #>

using module '.\.goo\goo.psm1'

$goo = [Goo]::new($args)


<# --- SET GLOBAL SCRIPT VARIABLES HERE --- #>

$script:SolutionName            = 'NoxConsole'

$script:RootFolder              = (Resolve-Path '.\').Path
$script:SolutionFolder          = $script:RootFolder
$script:SolutionFile            = "$script:SolutionFolder\NoxConsole.sln"
$script:ProjectFolder           = $script:RootFolder
$script:ProjectFile             = "$script:ProjectFolder\NoxConsole.csproj"

$script:DefaultEnvironment      = 'Development'


<# --- SET YOUR PROJECT'S ENVIRONMENT VARIABLES HERE --- #>

if($null -eq $Env:Environment)
{
    $Env:ENVIRONMENT = $script:DefaultEnvironment
}


<# --- ADD YOUR COMMAND DEFINITIONS HERE --- #>

<# 
    A good 'init' command will ensure a freshly cloned project will run first time.
    Guide the developer to do so easily. Check for required tools. Install them if needed. Set magic environment variables if needed.
    This should ideally replace your "Getting Started" section in your README.md
    Type less. Code more. (And get your team or collaboraters started quickly and productively!)
#>

# command: goo init | Run this command first, or to reset project completely. 
$goo.Command.Add( 'init', {
    $goo.Command.Run( 'clean' )
    $goo.Command.Run( 'build' )
    $goo.Command.Run( 'createdb' )
})

# command: goo clean | Removes data and build output
$goo.Command.Add( 'clean', {
    $goo.Console.WriteInfo( "Cleaning data and distribution folders..." )
    $goo.IO.EnsureRemoveFolder("$script:RootFolder\dist\")
    $goo.Command.RunExternal('dotnet','clean --verbosity:quiet --nologo',$script:SolutionFolder)
    $goo.StopIfError("Failed to clean previous builds. (Release)")
})

# command: goo build | Builds the solution and command line app. 
$goo.Command.Add( 'build', {
    $goo.Console.WriteInfo("Building solution...")
    $goo.Command.RunExternal('dotnet','build /clp:ErrorsOnly --configuration Release', $script:SolutionFolder)
    $goo.StopIfError("Failed to build solution. (Release)")
    $goo.Command.RunExternal('dotnet','publish --configuration Release --output .\dist --no-build', $script:CliProjectFolder)
    $goo.StopIfError("Failed to publish CLI project. (Release)")
})

# command: goo createdb | Creates and seeds the application database if it doesn't exist. 
$goo.Command.Add( 'createdb', {
    $goo.Console.WriteInfo("Creating and seeding database...")

    $connection = (Get-Content "$script:ProjectFolder\appsettings.json" | ConvertFrom-Json ).ConnectionStrings.ApplicationDbContext

    if (-not $connection.Split(';')[0].Split('=')[1].ToLower().StartsWith("localhost\")){
        $goo.Error("The connection string indicates that this it may not be safe to run 'createdb'. (Aborting!)")
    }

    $connectionServer = $connection.Split(';') | Where-Object {-not $_.ToUpper().StartsWith("DATABASE") } | Join-String -Separator ";"
    $server = ($connection.Split(';') | Where-Object {$_.ToUpper().StartsWith("SERVER")}).Split('=')[1]
    $database = ($connection.Split(';') | Where-Object {$_.ToUpper().StartsWith("DATABASE")}).Split('=')[1]

    $goo.Console.WriteLine( "Generating schema creation script for [$database]..." )
    $createScript = (dotnet ef dbcontext script) | Where-Object {-not $_.StartsWith("Build ")} | Out-String
    
    $dropScript = "USE tempdb;
    GO
    DECLARE @SQL nvarchar(1024);
    IF EXISTS (SELECT 1 FROM sys.databases WHERE [name] = N'$database')
    BEGIN
        SET @SQL = N'
            USE [$database];
            ALTER DATABASE $database SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            USE [tempdb];
            DROP DATABASE $database;';
        EXEC (@SQL);
    END;"
    $goo.Console.WriteLine( "Dropping database [$database] on server [$server]..." )
    Invoke-Sqlcmd -ConnectionString $connectionServer -Query $dropScript
    $goo.Console.WriteLine( "Creating database [$database]..." )
    Invoke-Sqlcmd -ConnectionString $connectionServer -Query "CREATE DATABASE $database;"
    $goo.Console.WriteLine( "Creating schema..." )
    Invoke-Sqlcmd -ConnectionString $connection -Query $createScript
    $goo.Console.WriteLine( "Seeding data..." )
    $data = Get-Content -Path .\Resources\SeedData\Hello.json | ConvertFrom-Json
    $data | ForEach-Object { Invoke-Sqlcmd -ConnectionString $connection -Query "INSERT INTO Hello (Language,HelloPhrase) VALUES (N'$($_.Language)',N'$($_.HelloPhrase)');" }
})

# command: goo env | Show all environment variables
$goo.Command.Add( 'env', { param($dbEnvironment,$dbInstance)
    $goo.Console.WriteLine( "environment variables" )
    $goo.Console.WriteLine( "=====================" )
    Get-ChildItem -Path Env: | Sort-Object -Property Name | Out-Host
})

# command: goo setenv <env> | Sets local environment to <env> environment
$goo.Command.Add( 'setenv', { param( $Environment )
    $oldEnv = $Env:ENVIRONMENT
    $Env:ENVIRONMENT = $Environment
    $Env:ASPNETCORE_ENVIRONMENT = $Environment
    $goo.Console.WriteInfo("Environment changed from [$oldEnv] to [$Env:ENVIRONMENT]")
})

# command: goo dev | Start up Visual Studio and VS Code for solution
$goo.Command.Add( 'dev', { 
    $goo.Command.StartProcess($script:SolutionFile)
    $goo.Command.StartProcess('code','.')
})

# command: goo run | Run the console application
$goo.Command.Add( 'run', {
    $goo.Command.RunExternal('dotnet','run',$script:SolutionFolder)
})

# command: goo feature <name> | Creates a new feature branch from your main git branch
$goo.Command.Add( 'feature', { param( $featureName )
    $goo.Git.CheckoutFeature($featureName)
})

# command: goo push <message> | Performs 'git add -A', 'git commit -m <message>', 'git -u push origin'
$goo.Command.Add( 'push', { param( $message )
    $current = $goo.Git.CurrentBranch()
    $head = $goo.Git.HeadBranch()
    if($head -eq $current) {
        $goo.Error("You can't push directly to the '$head' branch")
    }
    else {
        $goo.Git.AddCommitPushRemote($message)
    }
})

# command: goo pr | Performs and merges a pull request, checkout main and publish'
$goo.Command.Add( 'pr', { 
    gh pr create --fill
    if($?) { gh pr merge --merge }
    $goo.Command.Run( 'main' )
})

# command: goo main | Checks out the main branch and prunes features removed at origin
$goo.Command.Add( 'main', { param( $featureName )
    $goo.Git.CheckoutMain()
})


<# --- START GOO EXECUTION --- #>

$goo.Start()


<# --- EOF --- #>
