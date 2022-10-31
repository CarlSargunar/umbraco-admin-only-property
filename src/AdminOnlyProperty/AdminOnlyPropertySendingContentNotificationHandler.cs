using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Extensions;

namespace Umbraco.Community.AdminOnlyProperty
{
    public sealed class AdminOnlyPropertySendingContentNotificationHandler
        : INotificationHandler<SendingContentNotification>
    {
        private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;

        public AdminOnlyPropertySendingContentNotificationHandler(IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
        {
            _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        }

        public void Handle(SendingContentNotification notification)
        {
            var user = _backOfficeSecurityAccessor?.BackOfficeSecurity?.CurrentUser;
            if (user != null)
            {
                foreach (var variant in notification.Content.Variants)
                {
                    foreach (var tab in variant.Tabs)
                    {
                        tab.Properties = tab?.Properties?.Where(prop =>
                        {
                            var cacheKey = $"__aopConfig";
                            if (prop?.PropertyEditor?.Alias.InvariantEquals(AdminOnlyPropertyDataEditor.DataEditorAlias) == true &&
                                prop?.ConfigNullable.TryGetValue(cacheKey, out var tmp1) == true &&
                                tmp1 is Dictionary<string, object> config &&
                                config.TryGetValue(AdminOnlyPropertyConfigurationEditor.UserGroups, out var tmp2) == true &&
                                tmp2 is JArray array1 &&
                                array1.Count > 0)
                            {
                                prop.ConfigNullable.Remove(cacheKey);

                                var allowedGroups = array1.ToObject<string[]>();

                                return user.Groups.Any(x => allowedGroups?.Contains(x.Alias) == true);
                            }

                            return true;

                        }).ToList();

                        if (tab?.Properties?.Any() == false)
                        {
                            tab.Type = string.Empty;
                        }
                    }
                }
            }
        }
    }
}
