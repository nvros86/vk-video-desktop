using System;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace VKVideoDesktop.Application.Services;

public sealed class NotificationService
{
    public void ShowDownloadComplete(string videoTitle, string filePath)
    {
        try
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var textElements = toastXml.GetElementsByTagName("text");
            textElements[0].AppendChild(toastXml.CreateTextNode("Загрузка завершена"));
            textElements[1].AppendChild(toastXml.CreateTextNode(videoTitle));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier("VK Video Desktop").Show(toast);
        }
        catch
        {
        }
    }

    public void ShowError(string title, string message)
    {
        try
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var textElements = toastXml.GetElementsByTagName("text");
            textElements[0].AppendChild(toastXml.CreateTextNode(title));
            textElements[1].AppendChild(toastXml.CreateTextNode(message));

            var toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier("VK Video Desktop").Show(toast);
        }
        catch
        {
        }
    }
}
