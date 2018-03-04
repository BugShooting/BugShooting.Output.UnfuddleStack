using BS.Plugin.V3.Output;
using System;

namespace BugShooting.Output.UnfuddleStack
{

  public class Output: IOutput 
  {
    
    string name;
    string url;
    string userName;
    string password;
    string fileName;
    Guid fileFormatID;
    bool openItemInBrowser;
    int lastProjectID;
    int lastMessageID;
    int lastTicketNumber;

    public Output(string name, 
                  string url, 
                  string userName,
                  string password, 
                  string fileName,
                  Guid fileFormatID,
                  bool openItemInBrowser,
                  int lastProjectID,
                  int lastMessageID,
                  int lastTicketNumber)
    {
      this.name = name;
      this.url = url;
      this.userName = userName;
      this.password = password;
      this.fileName = fileName;
      this.fileFormatID = fileFormatID;
      this.openItemInBrowser = openItemInBrowser;
      this.lastProjectID = lastProjectID;
      this.lastMessageID = lastMessageID;
      this.lastTicketNumber = lastTicketNumber;
    }
    
    public string Name
    {
      get { return name; }
    }

    public string Information
    {
      get { return url; }
    }

    public string Url
    {
      get { return url; }
    }
       
    public string UserName
    {
      get { return userName; }
    }

    public string Password
    {
      get { return password; }
    }
          
    public string FileName
    {
      get { return fileName; }
    }

    public Guid FileFormatID
    {
      get { return fileFormatID; }
    }

    public bool OpenItemInBrowser
    {
      get { return openItemInBrowser; }
    }

    public int LastProjectID
    {
      get { return lastProjectID; }
    }

    public int LastMessageID
    {
      get { return lastMessageID; }
    }

    public int LastTicketNumber
    {
      get { return lastTicketNumber; }
    }

  }
}
