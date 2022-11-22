using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Security.KeyVault.Secrets;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Net;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using AzDOEmailOnFieldChange.Classes;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.Common;

namespace AzDOEmailOnFieldChange
{
    public class Function
    {
        private readonly SecretClient _secretClient;

        public Function(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        [FunctionName(nameof(EmailOnFieldAssign))]
        public async Task<IActionResult> EmailOnFieldAssign(
         [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await GetBody<WorkItemCreateBody>(req);

            if (!string.IsNullOrEmpty(body.resource.fields.CustomDeveloperResource) || !string.IsNullOrEmpty(body.resource.fields.CustomCSLResource)
                || !string.IsNullOrEmpty(body.resource.fields.CustomCSPMResource))
            {
                using (var workItemConnection = await Connect<WorkItemTrackingHttpClient>())
                {
                    var wi = await workItemConnection.GetWorkItemAsync(body.resource.id, new List<string>() { "Custom.DeveloperResource", "Custom.CSLResource", "Custom.CSPMResource" });

                    var developerResource = body.resource.fields.CustomDeveloperResource;
                    var cslResouce = body.resource.fields.CustomCSLResource;
                    var cspmResource = body.resource.fields.CustomCSPMResource;

                    if (developerResource != null && !string.IsNullOrEmpty(developerResource))
                    {
                        var newDeveloperResource = (IdentityRef)wi.Fields["Custom.DeveloperResource"];
                        await SendEmail(newDeveloperResource.UniqueName, $"You have been assigned as the Developer Resource on Task{body.resource.id}",
                            $"Hi {newDeveloperResource.DisplayName}, {Environment.NewLine} You have been assigned as the Developer Resource on Task{body.resource.id}");

                    }

                    if (cslResouce != null && !string.IsNullOrEmpty(cslResouce))
                    {
                        var newCSLResource = (IdentityRef)wi.Fields["Custom.CSLResource"];
                        await SendEmail(newCSLResource.UniqueName, $"You have been assigned as the CSL Resource on Task{body.resource.id}",
                            $"Hi {newCSLResource.DisplayName}, {Environment.NewLine} You have been assigned as the CSL Resource on Task{body.resource.id}");

                    }

                    if (cspmResource != null && !string.IsNullOrEmpty(cspmResource))
                    {
                        var newCSPMResource = (IdentityRef)wi.Fields["Custom.CSPMResource"];
                        await SendEmail(newCSPMResource.UniqueName, $"You have been assigned as the CSPM Resource on Task{body.resource.id}",
                            $"Hi {newCSPMResource.DisplayName}, {Environment.NewLine} You have been assigned as the CSPM Resource on Task{body.resource.id}");

                    }
                }
            }

            return new OkResult();

        }

        [FunctionName(nameof(EmailOnFieldChange))]
        public async Task<IActionResult> EmailOnFieldChange(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            var body = await GetBody<WebhookBody>(req);

            if (body.resource.fields.CustomDeveloperResource != null || body.resource.fields.CustomCSLResource != null || body.resource.fields.CustomCSPMResource != null)
            {
                using (var workItemConnection = await Connect<WorkItemTrackingHttpClient>())
                {
                    var wi = await workItemConnection.GetWorkItemAsync(body.resource.workItemId, new List<string>() { "Custom.DeveloperResource", "Custom.CSLResource", "Custom.CSPMResource" });

                    var developerResource = body.resource.fields.CustomDeveloperResource;
                    var cslResouce = body.resource.fields.CustomCSLResource;
                    var cspmResource = body.resource.fields.CustomCSPMResource;

                    if (developerResource != null && !string.IsNullOrEmpty(developerResource.newValue))
                    {
                        var newDeveloperResource = (IdentityRef)wi.Fields["Custom.DeveloperResource"];
                        await SendEmail(newDeveloperResource.UniqueName, $"You have been assigned as the Developer Resource on Task{body.resource.workItemId}",
                            $"Hi {newDeveloperResource.DisplayName}, {Environment.NewLine} You have been assigned as the Developer Resource on Task{body.resource.workItemId}");

                    }

                    if (cslResouce != null && !string.IsNullOrEmpty(cslResouce.newValue))
                    {
                        var newCSLResource = (IdentityRef)wi.Fields["Custom.CSLResource"];
                        await SendEmail(newCSLResource.UniqueName, $"You have been assigned as the CSL Resource on Task{body.resource.workItemId}",
                            $"Hi {newCSLResource.DisplayName}, {Environment.NewLine} You have been assigned as the CSL Resource on Task{body.resource.workItemId}");

                    }

                    if (cspmResource != null && !string.IsNullOrEmpty(cspmResource.newValue))
                    {
                        var newCSPMResource = (IdentityRef)wi.Fields["Custom.CSPMResource"];
                        await SendEmail(newCSPMResource.UniqueName, $"You have been assigned as the CSPM Resource on Task{body.resource.workItemId}",
                            $"Hi {newCSPMResource.DisplayName}, {Environment.NewLine} You have been assigned as the CSPM Resource on Task{body.resource.workItemId}");

                    }
                }
            }


            return new OkResult();

        }

        public async Task<T> Connect<T>() where T : VssHttpClientBase
        {
            VssBasicCredential cred = new VssBasicCredential(new NetworkCredential("", (await _secretClient.GetSecretAsync("azure-devops-pat")).Value.Value));

            VssConnection connection = new VssConnection(new Uri(Environment.GetEnvironmentVariable("AzDOBaseUrl")), new VssCredentials(cred));

            return connection.GetClient<T>();
        }

        public async Task<T> GetBody<T>(HttpRequest req)
        {
            return JsonConvert.DeserializeObject<T>(await new StreamReader(req.Body).ReadToEndAsync());
        }

        public async Task SendEmail(string to, string subject, string body)
        {

            MailAddress from = new MailAddress(Environment.GetEnvironmentVariable("EmailFromAddress"));

            MailMessage message = new MailMessage()
            {
                From = from,
                Subject = subject,
                Body = body.Replace(Environment.NewLine, "<Br />"),
                IsBodyHtml = true,
                Priority = MailPriority.Normal
            };

            message.To.Add(to);

            using (SmtpClient client = new SmtpClient("smtp.office365.com", 587) { UseDefaultCredentials = false, EnableSsl = true })
            {

                client.Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("EmailFromAddress"), (await _secretClient.GetSecretAsync("email-password")).Value.Value);

                ServicePointManager.ServerCertificateValidationCallback =
                          delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                          { return true; };


                client.Send(message);

            }


        }
    }
}
