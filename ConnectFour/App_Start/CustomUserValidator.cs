using ConnectFour.Models;
using Microsoft.AspNet.Identity;
using Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ConnectFour.App_Start
{
    /// <summary>
    ///     Validates users before they are saved
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    public class CustomUserValidator<TUser> : CustomUserValidator<TUser, string> where TUser : class, IUser<string>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="manager"></param>
        public CustomUserValidator(UserManager<TUser, string> manager)
            : base(manager)
        {
        }
    }

    /// <summary>
    ///     Validates users before they are saved
    /// </summary>
    /// <typeparam name="TUser"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class CustomUserValidator<TUser, TKey> : UserValidator<TUser, TKey>
        where TUser : class, IUser<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="manager"></param>
        public CustomUserValidator(UserManager<TUser, TKey> manager) : base(manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }
            Manager = manager;
        }

        private UserManager<TUser, TKey> Manager { get; set; }

        public override async Task<IdentityResult> ValidateAsync(TUser item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            var errors = new List<string>();

            await ValidateUserName(item, errors);
            bool isGuest = (bool?)HttpContext.Current.Session["isGuest"] ?? false;
            if (RequireUniqueEmail && !isGuest)
            {
                await ValidateEmail(item, errors);
            }
            if (errors.Count > 0)
            {
                return IdentityResult.Failed(errors.ToArray());
            }
            return IdentityResult.Success;
        }

        private async Task ValidateUserName(TUser user, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, CustomResource.PropertyTooShort, "Name"));
            }
            else if (AllowOnlyAlphanumericUserNames && !Regex.IsMatch(user.UserName, @"^[A-Za-z0-9@_\.]+$"))
            {
                // If any characters are not letters or digits, its an illegal user name
                errors.Add(String.Format(CultureInfo.CurrentCulture, CustomResource.InvalidUserName, user.UserName));
            }
            else
            {
                var owner = await Manager.FindByNameAsync(user.UserName);//.WithCurrentCulture();
                if (owner != null && !EqualityComparer<TKey>.Default.Equals(owner.Id, user.Id))
                {
                    errors.Add(String.Format(CultureInfo.CurrentCulture, CustomResource.DuplicateName, user.UserName));
                }
            }
        }

        // make sure email is not empty, valid, and unique
        private async Task ValidateEmail(TUser user, List<string> errors)
        {
            Type userType = user.GetType();
            PropertyInfo p = userType.GetProperty("Email");
            var email = p.GetValue(user, null).ToString();
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, CustomResource.PropertyTooShort, "Email"));
                return;
            }
            try
            {
                var m = new MailAddress(email);
            }
            catch (FormatException)
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, CustomResource.InvalidEmail, email));
                return;
            }
            var owner = await Manager.FindByEmailAsync(email);
            if (owner != null && !EqualityComparer<TKey>.Default.Equals(owner.Id, user.Id))
            {
                errors.Add(String.Format(CultureInfo.CurrentCulture, CustomResource.DuplicateEmail, email));
            }
        }
    }
}
