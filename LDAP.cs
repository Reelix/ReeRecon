﻿using LdapForNet;
using LdapForNet.Native;
using System;
using System.Collections.Generic;
using static LdapForNet.Native.Native;

namespace Reecon
{
    class LDAP // Port 389
    {
        public static string GetInfo(string ip)
        {
            string returnInfo = "";
            returnInfo += LDAP.GetDefaultNamingContext(ip).Trim(Environment.NewLine.ToCharArray()) + Environment.NewLine;
            returnInfo += LDAP.GetAccountInfo(ip);
            return returnInfo;
        }
        public static string GetDefaultNamingContext(string ip, bool raw = false)
        {
            string ldapInfo = string.Empty;
            using (LdapConnection ldapConnection = new LdapConnection())
            {
                ldapConnection.Connect(ip, 389);
                LdapCredential anonymousCredential = new LdapCredential();
                try
                {
                    ldapConnection.Bind(LdapAuthType.Simple, anonymousCredential);
                }
                catch (LdapException le)
                {
                    return Environment.NewLine + "- Connection Error: " + le.Message;
                }
                catch (Exception ex)
                {
                    return Environment.NewLine + "- Unknown Error: " + ex.Message;
                }
                // ldapConnection.AuthType = AuthType.Anonymous;

                var searchEntries = ldapConnection.Search(null, "(objectclass=*)", scope: LdapSearchScope.LDAP_SCOPE_BASE);
                // SearchRequest request = new SearchRequest(null, "(objectclass=*)", System.DirectoryServices.Protocols.SearchScope.Base, "defaultNamingContext");

                // SearchResponse result = (SearchResponse)ldapConnection.SendRequest(request);

                if (searchEntries.Count == 1)
                {
                    string defaultNamingContext = searchEntries[0].Attributes["defaultNamingContext"][0].ToString();
                    // ldapServiceName / dnsHostName
                    if (!raw)
                    {
                        ldapInfo = "- defaultNamingContext: ";
                    }
                    ldapInfo += defaultNamingContext;
                }
            }
            return ldapInfo;
        }

        public static string GetAccountInfo(string ip)
        {
            string ldapInfo = string.Empty;

            using (LdapConnection ldapConnection = new LdapConnection())
            {
                ldapConnection.Connect(ip);
                LdapCredential anonymousCredential = new LdapCredential();
                ldapConnection.Bind(Native.LdapAuthType.Simple, anonymousCredential);
                var rootDse = ldapConnection.GetRootDse();
                IList<LdapEntry> searchEntries;
                try
                {
                    searchEntries = ldapConnection.Search(rootDse.Attributes["defaultNamingContext"][0], "(objectclass=user)", scope: LdapSearchScope.LDAP_SCOPE_SUB);
                }
                catch (LdapException)
                {
                    return "- No Anonymous Access Allowed";
                }
                catch (Exception)
                {
                    return ":<";
                }
                foreach (var result in searchEntries)
                {
                    string accountName = result.Attributes["sAMAccountName"].Count > 0 ? result.Attributes["sAMAccountName"][0].ToString() : string.Empty;
                    if (accountName.Trim() == "System.Byte[]")
                    {
                        Console.WriteLine("Woof - Contact Reelix - Something broke in the LDAP Migration!!!");
                        //accountName = Encoding.UTF8.GetString((byte[])result.Attributes["samaccountname"][0]);
                    }
                    ldapInfo += Environment.NewLine + " - Account Name: " + accountName;
                    string commonName = result.Attributes["cn"].Count > 0 ? (string)result.Attributes["cn"][0] : string.Empty;
                    ldapInfo += Environment.NewLine + " -- Common Name: " + commonName;
                    if (result.Attributes.ContainsKey("userPrincipalName"))
                    {
                        ldapInfo += " -- userPrincipalName: " + result.Attributes["userPrincipalName"][0];
                    }
                    if (result.Attributes["lastLogon"].Count == 1)
                    {
                        string lastLoggedIn = result.Attributes["lastLogon"][0];
                        if (lastLoggedIn != "0")
                        {
                            ldapInfo += Environment.NewLine + " -- lastLogon: " + DateTime.FromFileTime(long.Parse(lastLoggedIn));
                        }
                    }
                    else
                    {
                        Console.WriteLine("Something broke with lastLogon :<");
                    }
                    if (result.Attributes.ContainsKey("description"))
                    {
                        string description = (string)result.Attributes["description"][0];
                        ldapInfo += Environment.NewLine + " -- Description: " + result.Attributes["description"][0];
                    }
                }
            }
            /*
                DirectoryEntry rootEntry = new DirectoryEntry("LDAP://" + ip)
            {
                AuthenticationType = AuthenticationTypes.Anonymous
            };
            DirectorySearcher searcher = new DirectorySearcher(rootEntry);
            var queryFormat = "(&(objectClass=user))";
            searcher.Filter = queryFormat;
            foreach (SearchResult result in searcher.FindAll())
            {
                string accountName = result.Properties["samaccountname"].Count > 0 ? result.Properties["samaccountname"][0].ToString() : string.Empty;
                if (accountName.Trim() == "System.Byte[]")
                {
                    accountName = Encoding.UTF8.GetString((byte[])result.Properties["samaccountname"][0]);
                }
                ldapInfo += Environment.NewLine + " - Account Name: " + accountName;
                string commonName = result.Properties["cn"].Count > 0 ? (string)result.Properties["cn"][0] : string.Empty;
                ldapInfo += Environment.NewLine + " -- Common Name: " + commonName;
                if (result.Properties["userPrincipleName"].Count > 0)
                {
                    ldapInfo += " -- userPrincipleName: " + result.Properties["userPrincipleName"][0];
                }
                if (result.Properties["lastLogon"].Count > 0)
                {
                    string lastLoggedIn = Encoding.UTF8.GetString((byte[])result.Properties["lastLogon"][0]);
                    if (lastLoggedIn != "0")
                    {
                        ldapInfo += Environment.NewLine + " -- lastLogon: " + lastLoggedIn;
                    }
                }
                if (result.Properties["description"].Count > 0)
                {
                    string description = (string)result.Properties["description"][0];
                    ldapInfo += Environment.NewLine + " -- Description: " + result.Properties["description"][0];
                }
                
            }
            */
            return ldapInfo;
        }
    }
}
