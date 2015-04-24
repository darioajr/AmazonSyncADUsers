using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;

namespace AmazonSyncADUsers
{
    class ActiveDirectoryHelper
    {
        private static string LDAPConnectionString = ConfigurationManager.AppSettings["LDAPConnectionString"];
        private static string LDAPServer = ConfigurationManager.AppSettings["LDAPServer"];
        private static string LDAPFind = ConfigurationManager.AppSettings["LDAPFind"];
        private static string LDAPUser = ConfigurationManager.AppSettings["LDAPUser"];
        private static string LDAPPassword = ConfigurationManager.AppSettings["LDAPPassword"];

        public static bool GetAllUsersOnGroup(List<string> users)
        {
            try
            {
                DirectoryEntry entry = new DirectoryEntry(LDAPConnectionString);
                DirectorySearcher searcher = new DirectorySearcher(entry);
                searcher.SizeLimit = 30000;
                searcher.PageSize = 1000;
                searcher.ClientTimeout = new TimeSpan(0, 0, 120);
                searcher.SearchScope = SearchScope.Subtree;
                searcher.Filter = LDAPFind;

                string userName;

                foreach (SearchResult sResultSet in searcher.FindAll())
                {
                    userName = "";
                    userName = GetProperty(sResultSet, "sAMAccountName");

                    if (userName == null)
                        continue;

                    users.Add(userName);
                }
                entry.Close();

                return true;
            }
            catch (Exception e) 
            {
                //TODO: Implement log
                throw; 
            }
        }

        public static Hashtable GetUserAttributes(string user)
        {
            var table = new Hashtable();
            try
            {
                DirectoryEntry entry = new DirectoryEntry(LDAPConnectionString, LDAPUser, LDAPPassword);
                DirectorySearcher searcher = new DirectorySearcher(entry);
                searcher.Filter = String.Format(LDAPFind, user);
                SearchResult result = searcher.FindOne();
                if (result != null)
                {
                    System.DirectoryServices.ResultPropertyCollection prop = result.Properties;
                    ICollection coll = prop.PropertyNames;
                    IEnumerator enu = coll.GetEnumerator();
                    while (enu.MoveNext())
                    {
                        table.Add(enu.Current, result.Properties[enu.Current.ToString()][0]);
                    }
                }
            }
            catch (Exception e)
            {
                //TODO: Implement log
                throw;
            }
            return table;
        }

        public static UserPrincipal GetUserPrincipal(string user)
        {
            try
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, LDAPServer))
                {
                    UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(pc, user);
                    return userPrincipal;
                }
            }
            catch (Exception e)
            {
                //TODO: Implement log
                throw;
            }
        }
        
        private static string GetProperty(SearchResult searchResult, string PropertyName)
        {
            if (searchResult.Properties.Contains(PropertyName))
            {
                return searchResult.Properties[PropertyName][0].ToString();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
