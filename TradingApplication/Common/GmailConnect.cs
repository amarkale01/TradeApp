using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Timers;
using Label = Google.Apis.Gmail.v1.Data.Label;
using System.Threading;
using Timer = System.Timers.Timer;
using System.Linq;

namespace Trading_App.Common
{
    public class GmailConnect
    {
        UsersResource.MessagesResource.ListRequest ListRequest { get; set; }
        GmailService GmailService { get; set; }
        Timer timer = new Timer();
        public event EventHandler OnUniversalCondition;
        bool isExited = false;
        public GmailConnect()
        {
            InitializeSetting();
        }

        private void InitializeSetting()
        {
            string[] Scopes = { GmailService.Scope.GmailReadonly };
            string ApplicationName = "Gmail API .NET Quickstart";

            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }
            // Create Gmail API service.
            GmailService = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
            // Define parameters of request.
            ListRequest = GmailService.Users.Messages.List("me");
            ListRequest.LabelIds = "INBOX";
            ListRequest.IncludeSpamTrash = false;
            ListRequest.Q = "universal exit condition";
            ListRequest.MaxResults = 1;
            
            //
            timer.Interval = 2000;
            timer.Elapsed += new ElapsedEventHandler(Timer_Elapsed);
            timer.Start();
        }
        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (ListRequest != null)
                {
                    IList<Message> messages = ListRequest.Execute().Messages;

                    if (messages != null && messages.Count > 0)
                    {
                        foreach (var msgItem in messages)
                        {

                            var emailInfoRequest = GmailService.Users.Messages.Get("me", msgItem.Id);
                            var emailInfoResponse = emailInfoRequest.Execute();

                            if (emailInfoResponse != null)
                            {
                                string date = emailInfoResponse.Payload.Headers.First(x => x.Name == "Date").Value;
                                DateTime exitDate = DateTime.Parse(date);
                                if (exitDate.Day == DateTime.Now.Day)
                                {
                                    timer.Stop();
                                    if (!isExited)
                                    {
                                        isExited = true;
                                        OnUniversalCondition(null, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
     

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();
        }
    }
}
