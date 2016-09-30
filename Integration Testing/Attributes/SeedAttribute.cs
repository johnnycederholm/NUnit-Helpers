using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace NUnit.Helpers.IntegrationTesting.Attributes
{
    /// <summary>
    /// Seed a database with test data from a .sql-file named after the test method
    /// and located as a embedded resource in test assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SeedAttribute : Attribute, ITestAction
    {
        private readonly string connectionStringName;

        public SeedAttribute()
        {
            NameSpace = "Seeds";
            connectionStringName = "DefaultConnection";
        }

        /// <param name="connectionStringName">Name of the connection string to use when connecting to database for running seed queries.</param>
        public SeedAttribute(string connectionStringName)
        {
            if (connectionStringName == null)
                throw new ArgumentNullException(nameof(connectionStringName));

            this.connectionStringName = connectionStringName;
        }

        public void BeforeTest(ITest test)
        {
            using (SqlConnection connection = new SqlConnection(GetConnectionString()))
            {
                connection.Open();

                SqlCommand command = connection.CreateCommand();
                command.CommandText = GetSqlQuery(test);
                command.ExecuteNonQuery();
            }
        }

        public void AfterTest(ITest test)
        {
        }

        public ActionTargets Targets => ActionTargets.Test;

        /// <summary>
        /// Get or set the name space where embedded scripts are located inside assembly.
        /// </summary>
        /// <example>Seeds.Database.PersonRepository</example>
        public string NameSpace { get; set; }

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
        }

        /// <summary>
        /// Get SQL located in embedded resource.
        /// </summary>
        private string GetSqlQuery(ITest test)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{assembly.GetName().Name}.{NameSpace}.{test.MethodName}.sql";

            Stream stream = null;

            try
            {
                stream = assembly.GetManifestResourceStream(resourceName);

                using (StreamReader reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (ArgumentNullException)
            {
                throw new FileNotFoundException($"Failed to load embedded resource file {resourceName}");
            }
            finally
            {
                stream?.Dispose();
            }
        }
    }
}
