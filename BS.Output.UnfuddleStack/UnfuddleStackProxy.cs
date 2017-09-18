using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Linq;

namespace BS.Output.UnfuddleStack
{
  internal class UnfuddleStackProxy
  {

    static internal async Task<GetProjectsResult> GetProjects(string url, string userName, string password)
    {

      try
      {
        string requestUrl = GetApiUrl(url, "projects");
        string resultData = await GetData(requestUrl, userName, password);

        List<Project> projects = new List<Project>();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(resultData);
        
        foreach (XmlNode node in xmlDoc.GetElementsByTagName("project"))
        {

          int projectID = 0;
          string projectTitle = string.Empty;

          foreach (XmlNode childNode in node.ChildNodes)
          {
            switch (childNode.Name)
            {
              case "id":
                projectID = Convert.ToInt32(childNode.InnerText);
                break;
              case "title":
                projectTitle = childNode.InnerText;
                break;
            }
          }
          projects.Add(new Project(projectID, projectTitle));
        }

        return new GetProjectsResult(ResultStatus.Success, null, projects);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new GetProjectsResult(ResultStatus.LoginFailed, null, null);

            default:
              return new GetProjectsResult(ResultStatus.Failed, response.StatusDescription, null);
          }

        }

      }

    }

    static internal async Task<CreateMessageResult> CreateMessage(string url, string userName, string password, int projectID, string title, string message)
    {

      try
      {
        string requestUrl = GetApiUrl(url, String.Format("projects/{0}/messages", projectID));
        int messageID = await CreateItem(requestUrl, userName, password, String.Format("<message><title>{0}</title><body>{1}</body></message>", HttpUtility.HtmlEncode(title), HttpUtility.HtmlEncode(message)));
     
        return new CreateMessageResult(ResultStatus.Success, messageID, null);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new CreateMessageResult(ResultStatus.LoginFailed, 0, null);

            default:
              return new CreateMessageResult(ResultStatus.Failed, 0, response.StatusDescription);
          }

        }

      }

    }

    static internal async Task<CreateTicketResult> CreateTicket(string url, string userName, string password, int projectID, string summary, string description)
    {

      try
      {
        string requestUrl = GetApiUrl(url, String.Format("projects/{0}/tickets", projectID));
        int ticketID = await CreateItem(requestUrl, userName, password, String.Format("<ticket><summary>{0}</summary><description>{1}</description><priority>3</priority></ticket>", HttpUtility.HtmlEncode(summary), HttpUtility.HtmlEncode(description)));
   
        int ticketNumber = await GetTicketNumber(url, userName, password, projectID, ticketID);
        
        return new CreateTicketResult(ResultStatus.Success, ticketNumber, null);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new CreateTicketResult(ResultStatus.LoginFailed, 0, null);

            default:
              return new CreateTicketResult(ResultStatus.Failed, 0, response.StatusDescription);
          }

        }

      }

    }

    static internal async Task<Result> AddAttachmentToMessage(string url, string userName, string password, int projectID, int messageID, string fullFileName, byte[] fileBytes, string fileMimeType)
    {

      try
      {

        string uploadUrl = GetApiUrl(url, string.Format("projects/{0}/messages/{1}/attachments/upload", projectID, messageID));
        string fileKey = await UploadFile(uploadUrl, userName, password, fileBytes, fileMimeType);

        string attachUrl = GetApiUrl(url, string.Format("projects/{0}/messages/{1}/attachments", projectID, messageID));
        await AttachFile(attachUrl, userName, password, fileKey, fullFileName, fileMimeType);
        
        return new Result(ResultStatus.Success, null);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new Result(ResultStatus.LoginFailed, null);

            default:
              return new Result(ResultStatus.Failed, response.StatusDescription);
          }

        }

      }

    }

    static internal async Task<Result> AddAttachmentToTicket(string url, string userName, string password, int projectID, int ticketNumber, string fullFileName, byte[] fileBytes, string fileMimeType)
    {

      try
      {

        int ticketID = await GetTicketID(url, userName, password, projectID, ticketNumber); ;

        string uploadUrl = GetApiUrl(url, string.Format("projects/{0}/tickets/{1}/attachments/upload", projectID, ticketID));
        string fileKey = await UploadFile(uploadUrl, userName, password, fileBytes, fileMimeType);

        string attachUrl = GetApiUrl(url, string.Format("projects/{0}/tickets/{1}/attachments", projectID, ticketID));
        await AttachFile(attachUrl, userName, password, fileKey, fullFileName, fileMimeType);

        return new Result(ResultStatus.Success, null);

      }
      catch (WebException ex) when (ex.Response is HttpWebResponse)
      {

        using (HttpWebResponse response = (HttpWebResponse)ex.Response)
        {

          switch (response.StatusCode)
          {
            case HttpStatusCode.Unauthorized:
              return new Result(ResultStatus.LoginFailed, null);

            default:
              return new Result(ResultStatus.Failed, response.StatusDescription);
          }

        }

      }

    }


    private static async Task<string> GetData(string url, string userName, string password)
    {

      WebRequest request = WebRequest.Create(url);
      request.Method = "GET";
      request.ContentType = "application/xml";

      string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
      request.Headers.Add("Authorization", "Basic " + basicAuth);

      using (WebResponse response = await request.GetResponseAsync())
      {
        using (Stream responseStream = response.GetResponseStream())
        {
          using (StreamReader reader = new StreamReader(responseStream))
          {
            return await reader.ReadToEndAsync();
          }
        }
      }

    }

    private static async Task<int> GetTicketNumber(string url, string userName, string password, int projectID, int ticketID)
    {

      string ticketUrl = GetApiUrl(url, String.Format("projects/{0}/tickets/{1}", projectID, ticketID));
      string resultData = await GetData(ticketUrl, userName, password);

      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(resultData);

      return Convert.ToInt32(xmlDoc.GetElementsByTagName("number")[0].InnerText);

    }

    private static async Task<int> GetTicketID(string url, string userName, string password, int projectID, int ticketNumber)
    {

      string ticketUrl = GetApiUrl(url, String.Format("projects/{0}/tickets/by_number/{1}", projectID, ticketNumber));
      string resultData = await GetData(ticketUrl, userName, password);

      XmlDocument xmlDoc = new XmlDocument();
      xmlDoc.LoadXml(resultData);

      return Convert.ToInt32(xmlDoc.GetElementsByTagName("id")[0].InnerText);

    }

    private static async Task<int> CreateItem(string url, string userName, string password, string xmlData)
    {

      HttpWebRequest request = (HttpWebRequest)(WebRequest.Create(url));
      request.Method = "POST";
      request.Accept = "application/xml";
      request.ContentType = "application/xml";

      string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
      request.Headers.Add("Authorization", "Basic " + basicAuth);

      byte[] postData = Encoding.UTF8.GetBytes(xmlData);

      request.ContentLength = postData.Length;

      using (Stream requestStream = await request.GetRequestStreamAsync())
      {
        requestStream.Write(postData, 0, postData.Length);
        requestStream.Close();
      }

      using (WebResponse response = await request.GetResponseAsync())
      {
        
        foreach (string key in response.Headers.Keys)
        {
          if (key.Equals("location", StringComparison.InvariantCultureIgnoreCase))
          {
            return Convert.ToInt32(response.Headers[key].Split('/').Last());
          }
        }

        throw new InvalidOperationException();

      }

    }

    private static async Task<string> UploadFile(string url, string userName, string password, byte[] fileBytes, string fileMimeType)
    {

      WebRequest request = WebRequest.Create(url);
      request.Method = "POST";
      request.ContentType = fileMimeType;
      request.ContentLength = fileBytes.Length;

      string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
      request.Headers.Add("Authorization", "Basic " + basicAuth);
      
      using (Stream requestStream = await request.GetRequestStreamAsync())
      {
        requestStream.Write(fileBytes, 0, fileBytes.Length);
        requestStream.Close();
      }

      using (WebResponse response = await request.GetResponseAsync())
      {
        using (Stream responseStream = response.GetResponseStream())
        {
          using (StreamReader reader = new StreamReader(responseStream))
          {
            string result = await reader.ReadToEndAsync();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(result);
            return xmlDoc.InnerText;
          }
        }
      }

    }

    private static async Task AttachFile(string url, string userName, string password, string fileKey, string fullFileName, string fileMimeType)
    {

      HttpWebRequest request = (HttpWebRequest)(WebRequest.Create(url));
      request.Method = "POST";
      request.Accept = "application/xml";
      request.ContentType = "application/xml";

      string basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", userName, password)));
      request.Headers.Add("Authorization", "Basic " + basicAuth);

      byte[] postData = Encoding.UTF8.GetBytes(String.Format("<attachment><filename>{0}</filename><content-type>{1}</content-type><upload><key>{2}</key></upload></attachment>", fullFileName, fileMimeType, fileKey));

      request.ContentLength = postData.Length;

      using (Stream requestStream = await request.GetRequestStreamAsync())
      {
        requestStream.Write(postData, 0, postData.Length);
        requestStream.Close();
      }

      using (WebResponse response = await request.GetResponseAsync())
      {
        // NOP
      }

    }


    private static string GetApiUrl(string url, string method)
    {

      string apiUrl = url;

      if (!(apiUrl.LastIndexOf("/") == apiUrl.Length - 1))
      {
        apiUrl += "/";
      }

      apiUrl += "api/v1/" + method;

      return apiUrl;

    }

  }

  internal enum ResultStatus : int
  {
    Success = 1,
    LoginFailed = 2,
    Failed = 3
  }

  class Result
  {

    ResultStatus status;
    string failedMessage;

    public Result(ResultStatus status,
                  string failedMessage)
    {
      this.status = status;
      this.failedMessage = failedMessage;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

  }

  class GetProjectsResult
  {

    ResultStatus status;
    string failedMessage;
    List<Project> projects;

    public GetProjectsResult(ResultStatus status,
                             string failedMessage,
                             List<Project> projects)
    {
      this.status = status;
      this.failedMessage = failedMessage;
      this.projects = projects;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

    public List<Project> Projects
    {
      get { return projects; }
    }

  }

  class CreateMessageResult
  {

    ResultStatus status;
    int messageID;
     string failedMessage;

    public CreateMessageResult(ResultStatus status,
                               int messageID,
                               string failedMessage)
    {
      this.status = status;
      this.messageID = messageID;
      this.failedMessage = failedMessage;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public int MessageID
    {
      get { return messageID; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

  }

  class CreateTicketResult
  {

    ResultStatus status;
    int ticketNumber;
    string failedMessage;

    public CreateTicketResult(ResultStatus status,
                              int ticketNumber,
                              string failedMessage)
    {
      this.status = status;
      this.ticketNumber = ticketNumber;
      this.failedMessage = failedMessage;
    }

    public ResultStatus Status
    {
      get { return status; }
    }

    public int TicketNumber
    {
      get { return ticketNumber; }
    }

    public string FailedMessage
    {
      get { return failedMessage; }
    }

  } 

  class Project
  {

    int id;
    string title;
    
    public Project(int id,
                   string title)
    {
      this.id = id;
      this.title = title;
    }

    public int ID
    {
      get { return id; }
    }
        
    public string Title
    {
      get { return title; }
    }

  }

}
