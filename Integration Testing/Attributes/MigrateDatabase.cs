using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Reflection;
using DbUp;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace NUnit.Helpers.IntegrationTesting.Attributes
{
    /// <summary>
    /// Migrate a database to the latest version using DbUp.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MigrateDatabase : Attribute, ITestAction
    {
        private readonly string connectionStringName;
        private readonly bool emptyDatabaseBeforeMigration;
        private readonly string assemblyName;

        /// <param name="assemblyName">Name of the assembly where DbUp migration scripts are located.</param>
        /// <param name="connectionStringName">Name of the connection string to use for connecting to database.</param>
        /// <param name="emptyDatabaseBeforeMigration">Set if database should be emptied before migration is performed. Default is true.</param>
        public MigrateDatabase(string assemblyName, string connectionStringName = "DefaultConnection", bool emptyDatabaseBeforeMigration = true)
        {
            if (connectionStringName == null)
                throw new ArgumentNullException(nameof(connectionStringName));

            if (assemblyName == null)
                throw new ArgumentNullException(nameof(assemblyName));

            this.connectionStringName = connectionStringName;
            this.emptyDatabaseBeforeMigration = emptyDatabaseBeforeMigration;
            this.assemblyName = assemblyName;
        }

        public void BeforeTest(ITest test)
        {
            if (emptyDatabaseBeforeMigration)
                EmptyDatabase();

            PerformMigration();
        }

        public void AfterTest(ITest test)
        {
        }

        /// <summary>
        /// Perform the actual migration of database using DbUp and scripts provided in
        /// the named assembly.
        /// </summary>
        private void PerformMigration()
        {
            var connectionString = GetConnectionString();

            EnsureDatabase.For.SqlDatabase(connectionString);

            var upgrader = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(ScriptAssembly)
                .WithTransaction()
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                throw new Exception("Failed to migrate database");
            }
        }

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
        }

        private Assembly ScriptAssembly => Assembly.LoadFrom($"{AppDomain.CurrentDomain.BaseDirectory}{assemblyName}.dll");

        public ActionTargets Targets => ActionTargets.Suite;

        /// <summary>
        /// Empty database completely.
        /// </summary>
        public void EmptyDatabase()
        {
            const string dropForeignKeys = @"DECLARE @name VARCHAR(128)
                                             DECLARE @constraint VARCHAR(254)
                                             DECLARE @SQL VARCHAR(254)

                                             SELECT @name = (SELECT TOP 1 TABLE_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'FOREIGN KEY' ORDER BY TABLE_NAME)

                                             WHILE @name is not null
                                             BEGIN
                                                 SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'FOREIGN KEY' AND TABLE_NAME = @name ORDER BY CONSTRAINT_NAME)
                                                 WHILE @constraint IS NOT NULL
                                                 BEGIN
                                                     SELECT @SQL = 'ALTER TABLE [dbo].[' + RTRIM(@name) +'] DROP CONSTRAINT [' + RTRIM(@constraint) +']'
                                                     EXEC (@SQL)
                                                     SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'FOREIGN KEY' AND CONSTRAINT_NAME <> @constraint AND TABLE_NAME = @name ORDER BY CONSTRAINT_NAME)
                                                 END
                                             SELECT @name = (SELECT TOP 1 TABLE_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'FOREIGN KEY' ORDER BY TABLE_NAME)
                                             END";

            const string dropPrimaryKeys = @"DECLARE @name VARCHAR(128)
                                             DECLARE @constraint VARCHAR(254)
                                             DECLARE @SQL VARCHAR(254)

                                             SELECT @name = (SELECT TOP 1 TABLE_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'PRIMARY KEY' ORDER BY TABLE_NAME)

                                             WHILE @name IS NOT NULL
                                             BEGIN
                                                 SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'PRIMARY KEY' AND TABLE_NAME = @name ORDER BY CONSTRAINT_NAME)
                                                 WHILE @constraint is not null
                                                 BEGIN
                                                     SELECT @SQL = 'ALTER TABLE [dbo].[' + RTRIM(@name) +'] DROP CONSTRAINT [' + RTRIM(@constraint)+']'
                                                     EXEC (@SQL)
                                                     SELECT @constraint = (SELECT TOP 1 CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'PRIMARY KEY' AND CONSTRAINT_NAME <> @constraint AND TABLE_NAME = @name ORDER BY CONSTRAINT_NAME)
                                                 END
                                             SELECT @name = (SELECT TOP 1 TABLE_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE constraint_catalog=DB_NAME() AND CONSTRAINT_TYPE = 'PRIMARY KEY' ORDER BY TABLE_NAME)
                                             END";

            const string dropTables = @"DECLARE @name VARCHAR(128)
                                        DECLARE @SQL VARCHAR(254)

                                        SELECT @name = (SELECT TOP 1 [name] FROM sysobjects WHERE [type] = 'U' AND category = 0 ORDER BY [name])

                                        WHILE @name IS NOT NULL
                                        BEGIN
                                            SELECT @SQL = 'DROP TABLE [dbo].[' + RTRIM(@name) +']'
                                            EXEC (@SQL)
                                            SELECT @name = (SELECT TOP 1 [name] FROM sysobjects WHERE [type] = 'U' AND category = 0 AND [name] > @name ORDER BY [name])
                                        END";

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                SqlCommand dropForeignKeysCommand = new SqlCommand(dropForeignKeys, connection);
                dropForeignKeysCommand.ExecuteNonQuery();

                SqlCommand dropPrimaryKeysCommand = new SqlCommand(dropPrimaryKeys, connection);
                dropPrimaryKeysCommand.ExecuteNonQuery();

                SqlCommand dropTablesCommand = new SqlCommand(dropTables, connection);
                dropTablesCommand.ExecuteNonQuery();
            }
        }
    }
}
