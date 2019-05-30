﻿using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Shipping.CanadaPost.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.CanadaPost.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class ShippingCanadaPostController : BasePluginController
    {
        #region Fields

        private readonly CanadaPostSettings _canadaPostSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;

        #endregion

        #region Ctor

        public ShippingCanadaPostController(CanadaPostSettings canadaPostSettings,
            ILocalizationService localizationService,
            IPermissionService permissionService,
            INotificationService notificationService,
            ISettingService settingService)
        {
            this._canadaPostSettings = canadaPostSettings;
            this._localizationService = localizationService;
            this._permissionService = permissionService;
            this._settingService = settingService;
            this._notificationService = notificationService;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            var model = new CanadaPostShippingModel()
            {
                CustomerNumber = _canadaPostSettings.CustomerNumber,
                ContractId = _canadaPostSettings.ContractId,
                ApiKey = _canadaPostSettings.ApiKey,
                UseSandbox = _canadaPostSettings.UseSandbox,
                SelectedServicesCodes = _canadaPostSettings.SelectedServicesCodes
            };

            //set available services
            var availableServices = CanadaPostHelper.GetServices(null, _canadaPostSettings.ApiKey, _canadaPostSettings.UseSandbox, out string errors);
            if (availableServices != null)
            {
                model.AvailableServices = availableServices.service.Select(service => new SelectListItem
                {
                    Value = service.servicecode,
                    Text = service.servicename,
                    Selected = _canadaPostSettings.SelectedServicesCodes?.Contains(service.servicecode) ?? false
                }).ToList();
            }

            return View("~/Plugins/Shipping.CanadaPost/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAntiForgery]
        public IActionResult Configure(CanadaPostShippingModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //Canada Post page provides the API key with extra spaces
            model.ApiKey = model.ApiKey?.Replace(" : ", ":");

            //save settings
            _canadaPostSettings.CustomerNumber = model.CustomerNumber;
            _canadaPostSettings.ContractId = model.ContractId;
            _canadaPostSettings.ApiKey = model.ApiKey;
            _canadaPostSettings.UseSandbox = model.UseSandbox;
            _canadaPostSettings.SelectedServicesCodes = model.SelectedServicesCodes.ToList();
            _settingService.SaveSetting(_canadaPostSettings);

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}
