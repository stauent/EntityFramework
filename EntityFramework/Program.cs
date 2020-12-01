using ConfigurationAssistant;
using System.Threading.Tasks;

namespace EntityFramework
{
    // To help visualize what's in your DBContext, a useful tool is EFCorePowerTools https://github.com/ErikEJ/EFCorePowerTools
    // You can get it from the visual studio marketplace https://marketplace.visualstudio.com/items?itemName=ErikEJ.EFCorePowerTools
    // To use this extension, you MUST have the DGML editor component for visual studio loaded. Open the 
    // Visual Studio Installer and in the "Individual components\Code tools" section, ensure that "DGML Editor" has been checked.
    // Next select the menu option "Extensions\ManageExtensions" and then search for "EF Core Power Tools". Select that item from
    // the list and click "Download". Restart visual studio and that extension should start to work.
    // In order for you to be able to use this tool with a DbContext, you have to ensure that the .csproj file that contains the DbContext
    // has a package reference to:
    //              <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="5.0.0">
    //
    // NOTE: YOU MUST also copy copy appconfig.json file into the project containing the DbContext you're working with. The reason for this
    //       is that power tools loads your project (.csproj) dynamically to inspect it for DbContext classes. If your main project is the 
    //       place where you have the appsettings.json file, then powertools won't load it, and you won't have any of your connection string 
    //       information. This is OK for development because your connection strings will be loaded from some other secure location when running in production.
    //       To try this out, right click on the "DataAccessLayer" project and select "Ef Core Power Tools\Add DbContext Model Diagram".

    // ------------------------ Instructions for Database First -----------------------------
    // To set up the entities in the DataAccessLayer assembly, open the PackageManagerConsole
    // and select the "Default Project" to be "DataAccessLayer". Set this assembly as the startup project.
    // Now enter the following command to scaffold all the entities:
    //
    //  XXX = Name of your Sql Server Instance
    //  YYY = Your Sql Server User ID
    //  ZZZ = Your Sql Server Password
    //
    //      Scaffold-DbContext  "Data Source = XXX; Initial Catalog = Dsuite; Persist Security Info = True; User ID = YYY; Password=ZZZ;" Microsoft.EntityFrameworkCore.SqlServer -DataAnnotations -OutputDir D:\Humber\EntityFramework\DataAccessLayer\Models\ 
    //
    //  NOTE: If you want to start using Migrations on this existing database, you need to perofrm a few steps. First, add your first migration:
    //        https://cmatskas.com/ef-core-migrations-with-existing-database-schema-and-data/  
    //
    //              Add-Migration DSuiteFirstMigration -Context DSuiteContext
    //
    //      Now, edit the XXXXXDSuiteFirstMigration.cs file and delete ALL the code inside the "Up" method, but don't delete the "Up" method.
    //      Next, update the database by running the migration:
    //
    //              Update-Database -Context DSuiteContext -verbose
    //
    //      This will generate a new table "__EFMigrationsHistory" in your database. From this point forward you can keep your models and database schema
    //      synchronized by modifying your model, creating a new migration, and then call "Update-Database" (as above).
    //
    // -------------------- Instructions for Code First  ---------------------------------
    //
    //  Setting up migrations:
    //
    //      NOTE: The connection string is retrieved using the ConfigFactory helper class. It inspects the "MyProjectSettings" section of the appsettings.json
    //            file to get configuration data. In order for "secrets.json" to properly work in this environment you must ensure that the DataAccessLayer.csproj
    //            has copied the "UserSecretsId" section from the main project .csproj file. The reason is that the secrets.json file is only supposed to be used
    //            in development. So when we run our application under visual studio in development, everything works fine. However, EF Core Migrations runs 
    //            in "Production" context. So, when migrations are run it that context, it will only read your appsettings.json file and ignore secrets.json. If however, you 
    //            ensure that the .csproj project containing your DBContext class contains the correct "UserSecretsId" section, then migrations will work properly.
    //
    //      Go to the  Package Manager Console (PMC) and enter: get-help entityframework
    //      This will show you all the powershell commands available related to entity framework.
    //      The commands you're interested in are:
    //      - Add-Migration
    //      - Update-Database
    //
    //      In the PMC select Default Project "DataAccessLayer" but in the solution explorer, set startup project
    //      to be the main application "EntityFramework"
    //      Let's create our initial migration by entering (FirstMigration will be the name of the migration): 
    //
    //        -->Syntax:     Add-Migration [-Name] <String> [-OutputDir <String>] [-Context <String>] [-Project <String>] [-StartupProject <String>] [-Environment <String>] [<CommonParameters>]
    //                       Add-Migration FirstMigration -Context SchoolContext 
    //
    //      This will generate a folder called "Migrations" and a file called XXXXX_FirstMigration.cs in that folder (where XXXX is the timestamp).
    //      Also a file called "SchoolContextModelSnapshot.cs" is created. It contains a snapshot of what the DB schema currently looks like. 
    //      EF uses this to compare changes in your model to determine what changed.
    //
    //      Now that you've created  the first migration script, we run migrations to create the database from the model.
    //      In the PMC enter this command to generate a T-SQL script that will perform the migration.
    //          script-migration
    //      This script can now be tweaked (if necessary) before running it against a PROD database.
    //
    //      If you want to run the migration directly against the database, then run the command:
    //          Update-Database  -Context SchoolContext  -verbose
    //      NOTE: Nothing happens if the database is already up to date.
    //
    //      Now, add a string property to the "Student" class called "MyFavoriteClass".
    //      Create a migration that will update the database with this new information.
    //
    //              Add-Migration SecondMigration -Context SchoolContext
    //
    //      The database has not been updated yet. To do that you need to execute the "Update-Database" command
    //              Update-Database -Context SchoolContext -verbose
    //
    //      The migrations infrastructure is smart enough to know which migrations have been applied to the database
    //      and which have not. So the previous commands will only apply migrations that have not yet been applied.
    //      At any time you can attempt to apply a specific migration by name:
    //
    //          Update-Database -Context SchoolContext -Migration SecondMigration -verbose
    //
    //      If the migration has already been applied then you'll see the message:
    //          "No migrations were applied. The database is already up to date."

    class Program
    {
        public static ConfigurationResults<MyApplication> configuredApplication = null;

        static async Task Main(string[] args)
        {
            configuredApplication = ConsoleHostBuilderHelper.CreateApp<MyApplication>(args);
            await configuredApplication.myService.Run();
        }
    }

}
