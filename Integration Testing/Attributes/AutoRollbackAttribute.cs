using System;
using System.Transactions;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace NUnit.Helpers.IntegrationTesting.Attributes
{
    /// <summary>
    /// Automatically rollback changes made to a database during a test or a suite of tests.
    /// </summary>
    public class AutoRollbackAttribute : Attribute, ITestAction
    {
        private TransactionScope transaction;

        public void BeforeTest(ITest test)
        {
            transaction = new TransactionScope();
        }

        public void AfterTest(ITest test)
        {
            transaction.Dispose();
        }

        public ActionTargets Targets => ActionTargets.Test;
    }
}
