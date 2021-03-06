using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Aiursoft.Pylon.Models;
using Microsoft.AspNetCore.Identity;
using Aiursoft.API.Models;
using Aiursoft.API.Services;
using Microsoft.Extensions.Logging;
using Aiursoft.API.Data;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Pylon.Services;
using Aiursoft.Pylon.Services.ToDeveloperServer;
using Aiursoft.Pylon.Models.API.ApiViewModels;
using Aiursoft.Pylon.Models.API.ApiAddressModels;
using Aiursoft.Pylon.Attributes;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Models.API;
using Aiursoft.Pylon.Models.API.UserAddressModels;
using Aiursoft.API.Attributes;
using Aiursoft.API.Models.UserViewModels;
using Microsoft.Extensions.Configuration;
using static Aiursoft.Pylon.Services.ExtendMethods;
using System.ComponentModel.DataAnnotations;

namespace Aiursoft.API.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<APIUser> _userManager;
        private readonly SignInManager<APIUser> _signInManager;
        private readonly ILogger _logger;
        private readonly APIDbContext _dbContext;
        private readonly IStringLocalizer<ApiController> _localizer;
        private readonly AiurEmailSender _emailSender;
        private readonly AiurSMSSender _smsSender;
        private readonly DeveloperApiService _developerApiService;
        private readonly ServiceLocation _serviceLocation;

        public UserController(
            UserManager<APIUser> userManager,
            SignInManager<APIUser> signInManager,
            ILoggerFactory loggerFactory,
            APIDbContext _context,
            IStringLocalizer<ApiController> localizer,
            AiurEmailSender emailSender,
            AiurSMSSender smsSender,
            DeveloperApiService developerApiService,
            ServiceLocation serviceLocation)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = loggerFactory.CreateLogger<ApiController>();
            _dbContext = _context;
            _localizer = localizer;
            _emailSender = emailSender;
            _smsSender = smsSender;
            _developerApiService = developerApiService;
            _serviceLocation = serviceLocation;
        }

        [ForceValidateAccessToken]
        [APIExpHandler]
        [APIModelStateChecker]
        public async Task<JsonResult> ChangeProfile(ChangeProfileAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            if (!app.App.ChangeBasicInfo)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to change users' basic info.");
            }
            if (!string.IsNullOrEmpty(model.NewNickName))
            {
                targetUser.NickName = model.NewNickName;
            }
            if (!string.IsNullOrEmpty(model.NewIconAddress))
            {
                targetUser.HeadImgUrl = model.NewIconAddress;
            }
            if (!string.Equals(model.NewBio, "Not_Mofified"))
            {
                targetUser.Bio = model.NewBio;
            }
            await _dbContext.SaveChangesAsync();
            return Json(new AiurProtocal { Code = ErrorType.Success, Message = "Successfully changed this user's nickname!" });
        }

        [ForceValidateAccessToken]
        [APIExpHandler]
        [APIModelStateChecker]
        public async Task<JsonResult> ChangePassword(ChangePasswordAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            if (!app.App.ChangePassword)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to change users' password.");
            }
            var result = await _userManager.ChangePasswordAsync(targetUser, model.OldPassword, model.NewPassword);
            await _userManager.UpdateAsync(targetUser);
            if (result.Succeeded)
            {
                return Json(new AiurProtocal { Code = ErrorType.Success, Message = "Successfully changed this user's password!" });
            }
            else
            {
                return Json(new AiurProtocal { Code = ErrorType.WrongKey, Message = result.Errors.First().Description });
            }
        }

        [ForceValidateAccessToken]
        [APIExpHandler]
        [APIModelStateChecker]
        public async Task<IActionResult> ViewPhoneNumber(ViewPhoneNumberAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            if (targetUser == null)
            {
                return this.Protocal(ErrorType.NotFound, "Could not find target user.");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            if (!app.App.ViewPhoneNumber)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to view users' phone number.");
            }
            return Json(new AiurValue<string>(targetUser.PhoneNumber)
            {
                Code = ErrorType.Success,
                Message = "Successfully get the target user's phone number."
            });
        }

        [ForceValidateAccessToken]
        [APIExpHandler]
        [APIModelStateChecker]
        public async Task<JsonResult> SetPhoneNumber(SetPhoneNumberAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            var targetUser = await _dbContext.Users.FindAsync(model.OpenId);
            if (targetUser == null)
            {
                return this.Protocal(ErrorType.NotFound, "Could not find target user.");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            if (!app.App.ChangePhoneNumber)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to set users' phone number.");
            }
            if (string.IsNullOrWhiteSpace(model.Phone))
            {
                targetUser.PhoneNumber = string.Empty;
            }
            else
            {
                targetUser.PhoneNumber = model.Phone;
            }
            await _userManager.UpdateAsync(targetUser);
            return this.Protocal(ErrorType.Success, "Successfully set the user's PhoneNumber!");
        }

        [ForceValidateAccessToken]
        [APIExpHandler]
        [APIModelStateChecker]
        public async Task<IActionResult> ViewAllEmails(ViewAllEmailsAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            var targetUser = await _dbContext.Users.Include(t => t.Emails).SingleOrDefaultAsync(t => t.Id == model.OpenId);
            if (targetUser == null)
            {
                return this.Protocal(ErrorType.NotFound, "Could not find target user.");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == targetUser.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            return Json(new AiurCollection<AiurUserEmail>(targetUser.Emails)
            {
                Code = ErrorType.Success,
                Message = "Successfully get the target user's emails."
            });
        }

        [APIExpHandler]
        [APIModelStateChecker]
        [HttpPost]
        [ForceValidateAccessToken]
        public async Task<IActionResult> BindNewEmail(BindNewEmailAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            var user = await _userManager.FindByIdAsync(model.OpenId);
            var emailexists = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.NewEmail);
            if (emailexists != null)
            {
                return this.Protocal(ErrorType.NotEnoughResources, $"An user has already bind email: {model.NewEmail}!");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == user.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            if (!app.App.ConfirmEmail)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to bind new email!");
            }
            var mail = new UserEmail
            {
                OwnerId = user.Id,
                EmailAddress = model.NewEmail,
                Validated = false
            };
            _dbContext.UserEmails.Add(mail);
            await _dbContext.SaveChangesAsync();
            return this.Protocal(ErrorType.Success, "Successfully set");
        }

        [HttpPost]
        [APIExpHandler]
        [APIModelStateChecker]
        [ForceValidateAccessToken]
        public async Task<IActionResult> DeleteEmail(DeleteEmailAddressModel model)
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            var user = await _userManager.FindByIdAsync(model.OpenId);
            var useremail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.ThatEmail.ToLower());
            if (useremail == null)
            {
                return this.Protocal(ErrorType.NotFound, $"Can not find your email:{model.ThatEmail}");
            }
            if (useremail.OwnerId != user.Id)
            {
                return this.Protocal(ErrorType.Unauthorized, $"The account you tried to authorize is not an account with id: {model.OpenId}");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == user.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            if (!app.App.ConfirmEmail)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to send confirmation email!");
            }
            _dbContext.UserEmails.Remove(useremail);
            await _dbContext.SaveChangesAsync();
            return this.Protocal(ErrorType.Success, $"Successfully deleted the email: {model.ThatEmail}!");
        }

        [HttpPost]
        [APIExpHandler]
        [APIModelStateChecker]
        [ForceValidateAccessToken]
        public async Task<IActionResult> SendConfirmationEmail(SendConfirmationEmailAddressModel model)//User Id
        {
            var accessToken = await _dbContext
                .AccessToken
                .SingleOrDefaultAsync(t => t.Value == model.AccessToken);

            var app = await _developerApiService.AppInfoAsync(accessToken.ApplyAppId);
            var user = await _userManager.FindByIdAsync(model.Id);
            var useremail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.Email.ToLower());
            if (useremail == null)
            {
                return this.Protocal(ErrorType.NotFound, $"Can not find your email:{model.Email}");
            }
            if (useremail.OwnerId != user.Id)
            {
                return this.Protocal(ErrorType.Unauthorized, $"The account you tried to authorize is not an account with id: {model.Id}");
            }
            if (useremail.Validated)
            {
                return this.Protocal(ErrorType.HasDoneAlready, $"The email :{model.Email} was already validated!");
            }
            if (!_dbContext.LocalAppGrant.Exists(t => t.AppID == accessToken.ApplyAppId && t.APIUserId == user.Id))
            {
                return Json(new AiurProtocal { Code = ErrorType.Unauthorized, Message = "This user did not grant your app!" });
            }
            if (!app.App.ConfirmEmail)
            {
                return this.Protocal(ErrorType.Unauthorized, "You app is not allowed to send confirmation email!");
            }
            //limit the sending frenquency to 3 minutes.
            if (DateTime.Now > useremail.LastSendTime + new TimeSpan(0, 3, 0))
            {
                var token = StringOperation.RandomString(30);
                useremail.ValidateToken = token;
                useremail.LastSendTime = DateTime.Now;
                await _dbContext.SaveChangesAsync();
                var callbackUrl = new AiurUrl(_serviceLocation.API, "User", nameof(EmailConfirm), new
                {
                    userId = user.Id,
                    code = token
                });
                await _emailSender.SendEmail(useremail.EmailAddress, $"{Values.ProjectName} Account Email Confirmation",
                    $"Please confirm your email by clicking <a href='{callbackUrl}'>here</a>");
            }
            return this.Protocal(ErrorType.Success, "Successfully sent the validation email.");
        }

        public async Task<IActionResult> EmailConfirm(string userId, string code)
        {
            var user = await _dbContext
                .Users
                .Include(t => t.Emails)
                .SingleOrDefaultAsync(t => t.Id == userId);
            if (user == null)
            {
                return NotFound();
            }
            var mailObject = await _dbContext
                .UserEmails
                .SingleOrDefaultAsync(t => t.ValidateToken == code);

            if (mailObject == null || mailObject.OwnerId != user.Id)
            {
                return NotFound();
            }
            if (!mailObject.Validated)
            {
                _logger.LogWarning($"The email object with address: {mailObject.EmailAddress} was already validated but the user was still trying to validate it!");
            }
            mailObject.Validated = true;
            await _dbContext.SaveChangesAsync();
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPasswordFor()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordFor(ForgotPasswordForViewModel model)
        {
            if (ModelState.IsValid)
            {
                var mail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.Email.ToLower());
                if (mail == null)
                {
                    ModelState.AddModelError(nameof(model.Email), $"The account with Email: {model.Email} was not found!");
                    return View(model);
                }
                return RedirectToAction(nameof(MethodSelection), new { id = mail.OwnerId });
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> MethodSelection(string id)//User id
        {
            var user = await _dbContext
                .Users
                .Include(t => t.Emails)
                .SingleOrDefaultAsync(t => t.Id == id);

            if (user == null)
            {
                return NotFound();
            }
            var model = new MethodSelectionViewModel
            {
                AccountName = user.Email
            };
            model.SMSResetAvaliable = user.PhoneNumberConfirmed;
            model.PhoneNumber = user.PhoneNumber?.Substring(user.PhoneNumber.Length - 4) ?? string.Empty;
            model.AvaliableEmails = user.Emails.Where(t => t.Validated);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordViaEmail(ForgotPasswordViaEmailViewModel model)
        {
            var mail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.Email.ToLower());
            if (mail == null)
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(mail.OwnerId);
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = new AiurUrl(_serviceLocation.API, "User", nameof(ResetPassword), new
            {
                Code = code,
                UserId = user.Id
            });
            await _emailSender.SendEmail(model.Email, "Reset Password",
                $"Please reset your password by clicking <a href='{callbackUrl}'>here</a>");
            return RedirectToAction(nameof(ForgotPasswordSent));
        }

        [HttpGet]
        public IActionResult ForgotPasswordSent()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPasswordViaSMS(ForgotPasswordViaEmailViewModel model)
        {
            var mail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.Email.ToLower());
            if (mail == null)
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(mail.OwnerId);
            if (user.PhoneNumberConfirmed == false)
            {
                return NotFound();
            }
            var code = StringOperation.RandomString(6);
            user.SMSPasswordResetToken = code;
            await _userManager.UpdateAsync(user);
            await _smsSender.SendAsync(user.PhoneNumber, code + " is your Aiursoft password reset code.");
            return RedirectToAction(nameof(EnterSMSCode), new { model.Email });
        }

        public async Task<IActionResult> EnterSMSCode(string Email)
        {
            var mail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == Email.ToLower());
            if (mail == null)
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(mail.OwnerId);
            if (user == null || user.PhoneNumberConfirmed == false)
            {
                return NotFound();
            }
            var phoneLast = user.PhoneNumber.Substring(user.PhoneNumber.Length - 4);
            var model = new EnterSMSCodeViewModel
            {
                Email = Email,
                PhoneLast = phoneLast
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EnterSMSCode(EnterSMSCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ModelStateValid = false;
                return View(model);
            }
            var mail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.Email.ToLower());
            if (mail == null)
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(mail.OwnerId);
            if (user.SMSPasswordResetToken.ToLower().Trim() == model.Code.ToLower().Trim())
            {
                user.SMSPasswordResetToken = string.Empty;
                await _userManager.UpdateAsync(user);
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                return RedirectToAction(nameof(ResetPassword), new { code = token });
            }
            else
            {
                model.ModelStateValid = false;
                ModelState.AddModelError("", "Your code is not correct and we can't help you reset your password!");
                return View(model);
            }
        }

        #region Reset password
        [HttpGet]
        public IActionResult ResetPassword(string code = null)
        {
            if (code == null)
            {
                return RedirectToAction(nameof(ForgotPasswordFor));
            }
            var model = new ResetPasswordViewModel
            {
                Code = code
            };
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // What if the user deleted all his emails?
            var mail = await _dbContext.UserEmails.SingleOrDefaultAsync(t => t.EmailAddress == model.Email.ToLower());
            if (mail == null)
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(mail.OwnerId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, $"Can not find target user with email '{model.Email}'.");
                return View();
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }
            AddErrors(result);
            return View();
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        #endregion

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}