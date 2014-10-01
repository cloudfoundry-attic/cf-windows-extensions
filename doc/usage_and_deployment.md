Usage and Deployment of the Windows Extensions for HP ALS
=========================================================


##Deployment

###Changes to the HP ALS installation

- [*optional*] Add the IIS8 buildpack to your ALS installation

      wget https://github.com/UhuruSoftware/uhuru-buildpack-iis8/archive/master.zip -O dotNet.zip
      stackato create-buildpack dotNet dotNet.zip


###Adding a Windows DEA to the cluster

####The NATS URL

####Troubleshooting

#####Log files

##Usage

###IIS8 Buildpack

###Sample Applications

####Vanilla sample app with a MySQL Membership Provider

- Create the app yourself
	- From Visual Studio 2013 create an ASP.NET application, .Net Framework 4.5, when asked choose Web Forms
	<img src="create_app_with_web_forms.png"/>
	
	- Install the MySQL connector nuget packages: MySql.ConnectorNET.Entity, MySql.ConnectorNET.Data, MySql.ConnectorNET.Web. It will update Web.config file
	<img src="install_nuget_packages.png"/>
	
	- In Web.config change membership section
	```
	<membership defaultProvider="MySQLMembershipProvider">
	  <providers>
	    <clear />
	    <add name="MySQLMembershipProvider"
	         type="MySql.Web.Security.MySQLMembershipProvider, MySql.Web, Version=6.8.3.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d"
	         applicationName="/" description="MySQL default application"
	         connectionStringName="DefaultConnection" writeExceptionsToEventLog="False"
	         autogenerateschema="True" enableExpireCallback="False" enablePasswordRetrieval="False"
	         enablePasswordReset="True" requiresQuestionAndAnswer="True" requiresUniqueEmail="False" passwordFormat="Clear"
	         maxInvalidPasswordAttempts="5" minRequiredPasswordLength="7" minRequiredNonalphanumericCharacters="1"
	         passwordAttemptWindow="10" passwordStrengthRegularExpression="" />
	  </providers>
	</membership>
	```

	- In Web.config change profile section
	```
	<profile enabled="true" defaultProvider="MySQLProfileProvider">
	  <providers>
	    <clear />
	    <add name="MySQLProfileProvider" type="MySql.Web.Profile.MySQLProfileProvider, MySql.Web, Version=6.8.3.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d"
	         applicationName="/" description="" connectionStringName="DefaultConnection" writeExceptionsToEventLog="False"
	         autogenerateschema="True" enableExpireCallback="False" />
	  </providers>
	</profile>
	```

	- In Web.config change role manager section
	```
	<roleManager enabled="true" defaultProvider="MySQLRoleProvider">
	  <providers>
	    <clear />
	    <add name="MySQLRoleProvider" type="MySql.Web.Security.MySQLRoleProvider, MySql.Web, Version=6.8.3.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d"
	         applicationName="/" description="" connectionStringName="DefaultConnection" writeExceptionsToEventLog="False" 
	         autogenerateschema="True" enableExpireCallback="False" />
	  </providers>
	</roleManager>
	```

	- In Web.config change connectionStrings section. In this case "sampleaspmysqlapp" is the name of the mysql service that will be bind to the application in the stackato micro cloud. Choose whatever name you like and replace it in connectionStrings section.	
	```
	<connectionStrings>
	  <add name="DefaultConnection" connectionString="Server={sampleaspmysqlapp#host};Database={sampleaspmysqlapp#name};Uid={sampleaspmysqlapp#user};Pwd={sampleaspmysqlapp#password};Port={sampleaspmysqlapp#port};pooling=false;" providerName="MySql.Data.MySqlClient" />
	</connectionStrings>
	```

	- In Web.config change Entity Framework and system data sections 
	```
	<entityFramework >
	  <providers>
	    <provider invariantName="MySql.Data.MySqlClient" type="MySql.Data.MySqlClient.MySqlProviderServices, MySql.Data.Entity.EF6">
	    </provider>
	  </providers>
	</entityFramework>
	<system.data>
	  <DbProviderFactories>
	    <remove invariant="MySql.Data.MySqlClient" />
	    <add name="MySQL Data Provider" invariant="MySql.Data.MySqlClient" description=".Net Framework Data Provider for MySQL" type="MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data, Version=6.8.3.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d" />
	  </DbProviderFactories>
	</system.data>
	```

	- Enable migrations: go to menu item View -> Other Windows -> Click on "Package Manager Console". Type `enable-migrations` in the console prompt and press enter. A new folder, Migrations containing a new file named "Configuration.cs" will be placed in the solution.
	<img src="enable_migrations.png"/>
  
	- Under the Migration folder, add a new class named `MySqlHistoryContext.cs` and add the following code:

	```
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.Migrations.History;
    
    
    namespace SampleASPMySQLApp.Migrations
    {
    	public class MySqlHistoryContext : HistoryContext
    	{
    		public MySqlHistoryContext(DbConnection connection, string defaultSchema)
    			: base(connection, defaultSchema)
    		{
    
    		}
    
    		protected override void OnModelCreating(DbModelBuilder modelBuilder)
    		{
    			base.OnModelCreating(modelBuilder);
    			modelBuilder.Entity<HistoryRow>().Property(h => h.MigrationId).HasMaxLength(100).IsRequired();
    			modelBuilder.Entity<HistoryRow>().Property(h => h.ContextKey).HasMaxLength(200).IsRequired();
    		}
    	}
    }
	```

	- In the same migration folder, change Configuration.cs file to look like this:
	
	```
	namespace SampleASPMySQLApp.Migrations
	{
	    using System;
	    using System.Data.Entity;
	    using System.Data.Entity.Migrations;
	    using System.Linq;
	
	    internal sealed class Configuration : DbMigrationsConfiguration<SampleASPMySQLApp.Models.ApplicationDbContext>
	    {
	        public Configuration()
	        {
	            AutomaticMigrationsEnabled = false;
	
	            // register mysql code generator
	            SetSqlGenerator("MySql.Data.MySqlClient", new MySql.Data.Entity.MySqlMigrationSqlGenerator());
	
	            SetHistoryContextFactory("MySql.Data.MySqlClient", (conn, schema) => new MySqlHistoryContext(conn, schema));
	        }
	
	        protected override void Seed(SampleASPMySQLApp.Models.ApplicationDbContext context)
	        {
	
	        }
	    }
	}
	```
	- To create a new custom database initializer, add a new class file named `MySqlInitializer.cs` to the project, and change its code to this:
	```
	using SampleASPMySQLApp.Models;
	using System.Data.Entity;
	using System.Data.Entity.Infrastructure;
	using System.Linq;
	
	namespace SampleASPMySQLApp
	{
	    public class MySqlInitializer : IDatabaseInitializer<ApplicationDbContext>
	    {
	        public void InitializeDatabase(ApplicationDbContext context)
	        {
	            if (!context.Database.Exists())
	            {
	                // if database did not exist before - create it
	                context.Database.CreateIfNotExists();
	            }
	            else
	            {
	                string connString = WebConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                	string db = "Database=";
                	string dbName = connString.Substring(connString.LastIndexOf(db) + db.Length, 33);

                	// query to check if MigrationHistory table is present in the database
                	var migrationHistoryTableExists = ((IObjectContextAdapter)context).ObjectContext.ExecuteStoreQuery<int>(
                	string.Format("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '{0}' AND table_name = '__MigrationHistory'", dbName));
	
	                // if MigrationHistory table is not there (which is the case first time we run) - create it
	                if (migrationHistoryTableExists.FirstOrDefault() == 0)
	                {
	                    context.Database.Delete();
	                    context.Database.Create();
	                }
	            }
	        }
	    }
	}
	```
	- In Models folder, change the `IdentityModel.cs` file code to look like this, the Helpers section remains the same:
	```
	using System;
	using System.Security.Claims;
	using System.Threading.Tasks;
	using System.Web;
	using Microsoft.AspNet.Identity;
	using Microsoft.AspNet.Identity.EntityFramework;
	using Microsoft.AspNet.Identity.Owin;
	using Microsoft.Owin.Security;
	using SampleASPMySQLApp.Models;
	
	namespace SampleASPMySQLApp.Models
	{
	    // You can add User data for the user by adding more properties to your User class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
	    public class ApplicationUser : IdentityUser
	    {
	        public ClaimsIdentity GenerateUserIdentity(ApplicationUserManager manager)
	        {
	            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
	            var userIdentity = manager.CreateIdentity(this, DefaultAuthenticationTypes.ApplicationCookie);
	            // Add custom user claims here
	            return userIdentity;
	        }
	
	        public Task<ClaimsIdentity> GenerateUserIdentityAsync(ApplicationUserManager manager)
	        {
	            return Task.FromResult(GenerateUserIdentity(manager));
	        }
	    }
	
	    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	    {
	        public ApplicationDbContext()
	            : base("DefaultConnection", throwIfV1Schema: false)
	        {
	            System.Data.Entity.Database.SetInitializer(new MySqlInitializer());
	        }
	
	        public static ApplicationDbContext Create()
	        {
	            return new ApplicationDbContext();
	        }
	    }
	}
	```
	- Build and Start the app, go to Register page and create a new user and the message "Hello, user@example.com !" should be visible in the top right corner.
	<img src="register_user.png" /> 

- Now that the app is created, push it to stackato micro cloud:
	- Start stackato client and login to your stackato micro cloud deployment with windows DEA following the link: https://docs.stackato.com/user/quick-start/index.html
	- From command prompt go to the path where the application is, and type 
 	```
	stackato push --stack win2012
	```
when asked, choose default answer except when "Create services to bind to", choose "y". "What kind of service ?", choose 2, "Specify the name of the service" choose "sampleaspmysqlapp", or the name you specified in connection string settings in Web.config of the app. The command prompt should look something like this:
	<img src="push_app_to_stackato.png"/>
- To check that the mysql service is correctly created we open a tunnel:
	- From command prompt go to the path where mysql client is installed and type the code below where "sampleaspmysqlapp" is the name of the mysql service bound to the app
	```
	stackato tunnel sampleaspmysqlapp
	```
the tunnel application will be deployed, when asked "Which client would you like to start?" choose "2". The tunnel should be created and service connection info be displayed 
	<img src="stackato_tunnel.png"/> 

- Download the app


####Umbraco

###Debugging your app

####Resource constraints

####Streaming log files

####Configuration files

- root web.config file
- applicationHost config file

