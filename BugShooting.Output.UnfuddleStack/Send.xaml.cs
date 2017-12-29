using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace BugShooting.Output.UnfuddleStack
{
  partial class Send : Window
  {
 
    public Send(string url, int lastProjectID, int lastMessageID, int lastTicketNumber, List<Project> projects, string fileName)
    {
      InitializeComponent();

      ProjectComboBox.ItemsSource = projects;

      Url.Text = url;
      NewMessage.IsChecked = true;
      ProjectComboBox.SelectedValue = lastProjectID;
      MessageIDTextBox.Text = lastMessageID.ToString();
      TicketNumberTextBox.Text = lastTicketNumber.ToString();
      FileNameTextBox.Text = fileName;

      ProjectComboBox.SelectionChanged += ValidateData;
      MessageTitleTextBox.TextChanged += ValidateData;
      MessageTextBox.TextChanged += ValidateData;
      MessageIDTextBox.TextChanged += ValidateData;
      TicketSummaryTextBox.TextChanged += ValidateData;
      TicketNumberTextBox.TextChanged += ValidateData;
      FileNameTextBox.TextChanged += ValidateData;
      ValidateData(null, null);

    }

    public SendItemType ItemType
    {
      get
      {
        if (NewMessage.IsChecked.Value)
        {
          return SendItemType.NewMessage;
        }
        else if (AttachToMessage.IsChecked.Value)
        {
          return SendItemType.AttachToMessage;
        }
        else if (NewTicket.IsChecked.Value)
        {
          return SendItemType.NewTicket;
        }
        else
        {
          return SendItemType.AttachToTicket;
        }
      }
    }
 
    public int ProjectID
    {
      get { return (int)ProjectComboBox.SelectedValue; }
    }

    public string MessageTitle
    {
      get { return MessageTitleTextBox.Text; }
    }

    public string Message
    {
      get { return MessageTextBox.Text; }
    }

    public int MessageID
    {
      get { return Convert.ToInt32(MessageIDTextBox.Text); }
    }

    public string TicketSummary
    {
      get { return TicketSummaryTextBox.Text; }
    }

    public string TicketDescription
    {
      get { return TicketDescriptionTextBox.Text; }
    }

    public int TicketNumber
    {
      get { return Convert.ToInt32(TicketNumberTextBox.Text); }
    }

    public string FileName
    {
      get { return FileNameTextBox.Text; }
    }
        
    private void ItemType_CheckedChanged(object sender, EventArgs e)
    {

      switch (ItemType)
      {
        case SendItemType.NewMessage:
          MessageTitleControls.Visibility = Visibility.Visible;
          MessageControls.Visibility = Visibility.Visible;
          MessageIDControls.Visibility = Visibility.Collapsed;
          TicketSummaryControls.Visibility = Visibility.Collapsed;
          TicketDescriptionControls.Visibility = Visibility.Collapsed;
          TicketNumberControls.Visibility = Visibility.Collapsed;

          MessageTitleTextBox.SelectAll();
          MessageTitleTextBox.Focus();

          break;

        case SendItemType.AttachToMessage:
          MessageTitleControls.Visibility = Visibility.Collapsed;
          MessageControls.Visibility = Visibility.Collapsed;
          MessageIDControls.Visibility = Visibility.Visible;
          TicketSummaryControls.Visibility = Visibility.Collapsed;
          TicketDescriptionControls.Visibility = Visibility.Collapsed;
          TicketNumberControls.Visibility = Visibility.Collapsed;

          MessageIDTextBox.SelectAll();
          MessageIDTextBox.Focus();

          break;

        case SendItemType.NewTicket:
          MessageTitleControls.Visibility = Visibility.Collapsed;
          MessageControls.Visibility = Visibility.Collapsed;
          MessageIDControls.Visibility = Visibility.Collapsed;
          TicketSummaryControls.Visibility = Visibility.Visible;
          TicketDescriptionControls.Visibility = Visibility.Visible;
          TicketNumberControls.Visibility = Visibility.Collapsed;

          TicketSummaryTextBox.SelectAll();
          TicketSummaryTextBox.Focus();

          break;

        case SendItemType.AttachToTicket:
          MessageTitleControls.Visibility = Visibility.Collapsed;
          MessageControls.Visibility = Visibility.Collapsed;
          MessageIDControls.Visibility = Visibility.Collapsed;
          TicketSummaryControls.Visibility = Visibility.Collapsed;
          TicketDescriptionControls.Visibility = Visibility.Collapsed;
          TicketNumberControls.Visibility = Visibility.Visible;

          TicketNumberTextBox.SelectAll();
          TicketNumberTextBox.Focus();

          break;
      }

      ValidateData(null, null);

    }

    private void MessageID_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
    }

    private void TicketNumber_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      e.Handled = Regex.IsMatch(e.Text, "[^0-9]+");
    }

    private void ValidateData(object sender, EventArgs e)
    {
      OK.IsEnabled = Validation.IsValid(ProjectComboBox) &&
                     ((ItemType==SendItemType.NewMessage && Validation.IsValid(MessageTitleTextBox) && Validation.IsValid(MessageTextBox)) ||
                      (ItemType == SendItemType.AttachToMessage && Validation.IsValid(MessageIDTextBox)) || 
                      (ItemType == SendItemType.NewTicket && Validation.IsValid(TicketSummaryTextBox)) ||
                      (ItemType == SendItemType.AttachToTicket && Validation.IsValid(TicketNumberTextBox))) &&
                     Validation.IsValid(FileNameTextBox);
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
      this.DialogResult = true;
    }

  }

  enum SendItemType
  {
    NewMessage = 0,
    AttachToMessage = 1,
    NewTicket = 2,
    AttachToTicket = 3
  }

}
