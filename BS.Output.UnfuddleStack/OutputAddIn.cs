using System;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceModel;
using System.Web;
using System.Threading.Tasks;

namespace BS.Output.UnfuddleStack
{
  public class OutputAddIn: V3.OutputAddIn<Output>
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

    protected override OutputValueCollection SerializeOutput(Output Output)
    {

      OutputValueCollection outputValues = new OutputValueCollection();

      outputValues.Add(new OutputValue("Name", Output.Name));
      outputValues.Add(new OutputValue("Url", Output.Url));
      outputValues.Add(new OutputValue("UserName", Output.UserName));
      outputValues.Add(new OutputValue("Password",Output.Password, true));
      outputValues.Add(new OutputValue("OpenItemInBrowser", Convert.ToString(Output.OpenItemInBrowser)));
      outputValues.Add(new OutputValue("FileName", Output.FileName));
      outputValues.Add(new OutputValue("FileFormat", Output.FileFormat));
      outputValues.Add(new OutputValue("LastProjectID", Output.LastProjectID.ToString()));
      outputValues.Add(new OutputValue("LastMessageID", Output.LastMessageID.ToString()));
      outputValues.Add(new OutputValue("LastTicketNumber", Output.LastTicketNumber.ToString()));

      return outputValues;
      
    }

    protected override Output DeserializeOutput(OutputValueCollection OutputValues)
    {

      return new Output(OutputValues["Name", this.Name].Value,
                        OutputValues["Url", ""].Value, 
                        OutputValues["UserName", ""].Value,
                        OutputValues["Password", ""].Value, 
                        OutputValues["FileName", "Screenshot"].Value, 
                        OutputValues["FileFormat", ""].Value,
                        Convert.ToBoolean(OutputValues["OpenItemInBrowser", Convert.ToString(true)].Value),
                        Convert.ToInt32(OutputValues["LastProjectID", "1"].Value),
                        Convert.ToInt32(OutputValues["LastMessageID", "1"].Value),
                        Convert.ToInt32(OutputValues["LastTicketNumber", "1"].Value));

    }

    protected override async Task<V3.SendResult> Send(IWin32Window Owner, Output Output, V3.ImageData ImageData)
    {

      try
      {

        string userName = Output.UserName;
        string password = Output.Password;
        bool showLogin = string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password);
        bool rememberCredentials = false;

        string fileName = V3.FileHelper.GetFileName(Output.FileName, Output.FileFormat, ImageData);

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
              return new V3.SendResult(V3.Result.Canceled);
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
                return new V3.SendResult(V3.Result.Failed, projectsResult.FailedMessage);
            }

            // Show send window
            Send send = new Send(Output.Url, Output.LastProjectID, Output.LastMessageID, Output.LastTicketNumber, projectsResult.Projects,  fileName);

            var ownerHelper = new System.Windows.Interop.WindowInteropHelper(send);
            ownerHelper.Owner = Owner.Handle;

            if (!send.ShowDialog() == true)
            {
              return new V3.SendResult(V3.Result.Canceled);
            }

            int issueTypeID;
            string issueKey;

            // TODO
            //if (send.CreateNewIssue)
            //{

            //  issueTypeID = send.IssueTypeID;

            //  // Create issue
            //  CreateIssueResult createIssueResult = await UnfuddleStackProxy.CreateIssue(Output.Url, userName, password, send.ProjectKey, issueTypeID, send.Summary, send.Description);
            //  switch (createIssueResult.Status)
            //  {
            //    case ResultStatus.Success:
            //      break;
            //    case ResultStatus.LoginFailed:
            //      showLogin = true;
            //      continue;
            //    case ResultStatus.Failed:
            //      return new V3.SendResult(V3.Result.Failed, createIssueResult.FailedMessage);
            //  }

            //  issueKey = createIssueResult.IssueKey;

            //}
            //else
            //{
            //  issueTypeID = Output.LastIssueTypeID;
            //  issueKey = String.Format("{0}-{1}", send.ProjectKey, send.IssueID);

            //  // Add comment to issue
            //  if (!String.IsNullOrEmpty(send.Comment))
            //  {
            //    Result commentResult = await UnfuddleStackProxy.AddCommentToIssue(Output.Url, userName, password, issueKey, send.Comment);
            //    switch (commentResult.Status)
            //    {
            //      case ResultStatus.Success:
            //        break;
            //      case ResultStatus.LoginFailed:
            //        showLogin = true;
            //        continue;
            //      case ResultStatus.Failed:
            //        return new V3.SendResult(V3.Result.Failed, commentResult.FailedMessage);
            //    }
            //  }

            //}

            //string fullFileName = String.Format("{0}.{1}", send.FileName, V3.FileHelper.GetFileExtention(Output.FileFormat));
            //string fileMimeType = V3.FileHelper.GetMimeType(Output.FileFormat);
            //byte[] fileBytes = V3.FileHelper.GetFileBytes(Output.FileFormat, ImageData);

            //// Add attachment to issue
            //Result attachmentResult = await UnfuddleStackProxy.AddAttachmentToIssue(Output.Url, userName, password, issueKey, fullFileName, fileBytes, fileMimeType);
            //switch (attachmentResult.Status)
            //{
            //  case ResultStatus.Success:
            //    break;
            //  case ResultStatus.LoginFailed:
            //    showLogin = true;
            //    continue;
            //  case ResultStatus.Failed:
            //    return new V3.SendResult(V3.Result.Failed, attachmentResult.FailedMessage);
            //}


            //// Open issue in browser
            //if (Output.OpenItemInBrowser)
            //{
            //  V3.WebHelper.OpenUrl(String.Format("{0}/browse/{1}", Output.Url, issueKey));
            //}


            // TODO korrekte Last-Daten übergeben anstellen von "111111111"
            return new V3.SendResult(V3.Result.Success,
                                     new Output(Output.Name,
                                                Output.Url,
                                                (rememberCredentials) ? userName : Output.UserName,
                                                (rememberCredentials) ? password : Output.Password,
                                                Output.FileName,
                                                Output.FileFormat,
                                                Output.OpenItemInBrowser,
                                                111111111,
                                                111111111,
                                                111111111));

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
        return new V3.SendResult(V3.Result.Failed, ex.Message);
      }

    }

  }
}
