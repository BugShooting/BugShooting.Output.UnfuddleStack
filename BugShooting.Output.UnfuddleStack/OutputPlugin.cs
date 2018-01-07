using System;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceModel;
using System.Threading.Tasks;
using BS.Plugin.V3.Output;
using BS.Plugin.V3.Common;
using BS.Plugin.V3.Utilities;


namespace BugShooting.Output.UnfuddleStack
{
  public class OutputPlugin: OutputPlugin<Output>
  {

    protected override string Name
    {
      get { return "Unfuddle STACK"; }
    }

    protected override Image Image64
    {
      get  { return Properties.Resources.logo_64; }
    }

    protected override Image Image16
    {
      get { return Properties.Resources.logo_16 ; }
    }

    protected override bool Editable
    {
      get { return true; }
    }

    protected override string Description
    {
      get { return "Attach screenshots to Unfuddle STACK messages or tickets."; }
    }
    
    protected override Output CreateOutput(IWin32Window Owner)
    {
      
      Output output = new Output(Name, 
                                 String.Empty, 
                                 String.Empty, 
                                 String.Empty, 
                                 "Screenshot",
                                 String.Empty, 
                                 true,
                                 1,
                                 1,
                                 1);

      return EditOutput(Owner, output);

    }

    protected override Output EditOutput(IWin32Window Owner, Output Output)
    {

      Edit edit = new Edit(Output);

      var ownerHelper = new System.Windows.Interop.WindowInteropHelper(edit);
      ownerHelper.Owner = Owner.Handle;
      
      if (edit.ShowDialog() == true) {

        return new Output(edit.OutputName,
                          edit.Url,
                          edit.UserName,
                          edit.Password,
                          edit.FileName,
                          edit.FileFormat,
                          edit.OpenItemInBrowser,
                          Output.LastProjectID,
                          Output.LastMessageID,
                          Output.LastTicketNumber);
      }
      else
      {
        return null; 
      }

    }

    protected override OutputValues SerializeOutput(Output Output)
    {

      OutputValues outputValues = new OutputValues();

      outputValues.Add("Name", Output.Name);
      outputValues.Add("Url", Output.Url);
      outputValues.Add("UserName", Output.UserName);
      outputValues.Add("Password",Output.Password, true);
      outputValues.Add("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser));
      outputValues.Add("FileName", Output.FileName);
      outputValues.Add("FileFormat", Output.FileFormat);
      outputValues.Add("LastProjectID", Output.LastProjectID.ToString());
      outputValues.Add("LastMessageID", Output.LastMessageID.ToString());
      outputValues.Add("LastTicketNumber", Output.LastTicketNumber.ToString());

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValues OutputValues)
    {

      return new Output(OutputValues["Name", this.Name],
                        OutputValues["Url", ""], 
                        OutputValues["UserName", ""],
                        OutputValues["Password", ""], 
                        OutputValues["FileName", "Screenshot"], 
                        OutputValues["FileFormat", ""],
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)]),
                        Convert.ToInt32(OutputValues["LastProjectID", "1"]),
                        Convert.ToInt32(OutputValues["LastMessageID", "1"]),
                        Convert.ToInt32(OutputValues["LastTicketNumber", "1"]));

    }

    protected override async Task<SendResult> Send(IWin32Window Owner, Output Output, ImageData ImageData)
    {

      try
      {

        string userName = Output.UserName;
        string password = Output.Password;
        bool showLogin = string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password);
        bool rememberCredentials = false;

        string fileName = AttributeHelper.ReplaceAttributes(Output.FileName, ImageData);

        while (true)
        {

          if (showLogin)
          {

            // Show credentials window
            Credentials credentials = new Credentials(Output.Url, userName, password, rememberCredentials);

            var ownerHelper = new System.Windows.Interop.WindowInteropHelper(credentials);
            ownerHelper.Owner = Owner.Handle;

            if (credentials.ShowDialog() != true)
            {
              return new SendResult(Result.Canceled);
            }

            userName = credentials.UserName;
            password = credentials.Password;
            rememberCredentials = credentials.Remember;

          }

          try
          {

            GetProjectsResult projectsResult = await UnfuddleStackProxy.GetProjects(Output.Url, userName, password);
            switch (projectsResult.Status)
            {
              case ResultStatus.Success:
                break;
              case ResultStatus.LoginFailed:
                showLogin = true;
                continue;
              case ResultStatus.Failed:
                return new SendResult(Result.Failed, projectsResult.FailedMessage);
            }

            // Show send window
            Send send = new Send(Output.Url, Output.LastProjectID, Output.LastMessageID, Output.LastTicketNumber, projectsResult.Projects,  fileName);

            var ownerHelper = new System.Windows.Interop.WindowInteropHelper(send);
            ownerHelper.Owner = Owner.Handle;

            if (!send.ShowDialog() == true)
            {
              return new SendResult(Result.Canceled);
            }


            int messageID = Output.LastMessageID; ;
            int ticketNumber = Output.LastTicketNumber;
            
            switch (send.ItemType)
            {
              case SendItemType.NewMessage:

                CreateMessageResult createMessageResult = await UnfuddleStackProxy.CreateMessage(Output.Url, userName, password, send.ProjectID, send.MessageTitle, send.Message);
                switch (createMessageResult.Status)
                {
                  case ResultStatus.Success:
                    messageID = createMessageResult.MessageID;
                    break;
                  case ResultStatus.LoginFailed:
                    showLogin = true;
                    continue;
                  case ResultStatus.Failed:
                    return new SendResult(Result.Failed, createMessageResult.FailedMessage);
                }

                break;

              case SendItemType.AttachToMessage:
                messageID = send.MessageID;
                break;

              case SendItemType.NewTicket:

                CreateTicketResult createTicketResult = await UnfuddleStackProxy.CreateTicket(Output.Url, userName, password, send.ProjectID, send.TicketSummary, send.TicketDescription);
                switch (createTicketResult.Status)
                {
                  case ResultStatus.Success:
                    ticketNumber = createTicketResult.TicketNumber;
                    break;
                  case ResultStatus.LoginFailed:
                    showLogin = true;
                    continue;
                  case ResultStatus.Failed:
                    return new SendResult(Result.Failed, createTicketResult.FailedMessage);
                }

                break;

              case SendItemType.AttachToTicket:
                ticketNumber = send.TicketNumber;
                break;
            }


            string fullFileName = String.Format("{0}.{1}", send.FileName, FileHelper.GetFileExtension(Output.FileFormat));
            string fileMimeType = FileHelper.GetMimeType(Output.FileFormat);
            byte[] fileBytes = FileHelper.GetFileBytes(Output.FileFormat, ImageData);
            string itemUrl = string.Empty;

            switch (send.ItemType)
            {
              case SendItemType.NewMessage:
              case SendItemType.AttachToMessage:

                AttachmentResult addAttachmentToMessage = await UnfuddleStackProxy.AddAttachmentToMessage(Output.Url, userName, password, send.ProjectID, messageID, fullFileName, fileBytes, fileMimeType);
                switch (addAttachmentToMessage.Status)
                {
                  case ResultStatus.Success:
                    itemUrl = String.Format("{0}/projects/{1}/messages/{2}", Output.Url, send.ProjectID, messageID);
                    break;
                  case ResultStatus.LoginFailed:
                    showLogin = true;
                    continue;
                  case ResultStatus.Failed:
                    return new SendResult(Result.Failed, addAttachmentToMessage.FailedMessage);
                }

                break;

              case SendItemType.NewTicket:
              case SendItemType.AttachToTicket:

                AttachmentResult addAttachmentToTicket = await UnfuddleStackProxy.AddAttachmentToTicket(Output.Url, userName, password, send.ProjectID, ticketNumber, fullFileName, fileBytes, fileMimeType);
                switch (addAttachmentToTicket.Status)
                {
                  case ResultStatus.Success:
                    itemUrl = String.Format("{0}/projects/{1}/tickets/by_number/{2}", Output.Url, send.ProjectID, ticketNumber);
                    break;
                  case ResultStatus.LoginFailed:
                    showLogin = true;
                    continue;
                  case ResultStatus.Failed:
                    return new SendResult(Result.Failed, addAttachmentToTicket.FailedMessage);
                }

                break;
            }


            // Open issue in browser
            if (Output.OpenItemInBrowser)
            {
              WebHelper.OpenUrl(itemUrl);
            }


            return new SendResult(Result.Success,
                                  new Output(Output.Name,
                                             Output.Url,
                                             (rememberCredentials) ? userName : Output.UserName,
                                             (rememberCredentials) ? password : Output.Password,
                                             Output.FileName,
                                             Output.FileFormat,
                                             Output.OpenItemInBrowser,
                                             send.ProjectID,
                                             messageID,
                                             ticketNumber));

          }
          catch (FaultException ex) when (ex.Reason.ToString() == "Access denied")
          {
            // Login failed
            showLogin = true;
          }

        }

      }
      catch (Exception ex)
      {
        return new SendResult(Result.Failed, ex.Message);
      }

    }

  }
}
