using ConventionWebSite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace ConventionWebSite.Providers
{
    public class CustomRoleProvider : RoleProvider
    {
        private DataContext db = new DataContext();
        public override string ApplicationName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            using (var db = new DataContext())
            {
                string[] roles = new string[2];
                roles[0] = "Admin";
                roles[1] = "Student";

                return roles;
            }
        }

        public override string[] GetRolesForUser(string username)
        {
            using (var db = new DataContext())
            {
                string[] result = new string[1];
                User user = db.users.Where(u => u.Email == username).FirstOrDefault();
                if (user.IsAdmin)
                    result[0] = "Admin";
                else
                    result[0] = "Student";

                return result;
            }
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}