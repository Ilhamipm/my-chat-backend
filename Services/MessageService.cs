using System.Text.Json;

namespace ChatBackend.Services;

public class MessageService
{
    private readonly string _basePath;

    public MessageService()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "ChatData");
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public void SaveUnreadMessage(string targetId, string senderId, string message)
    {
        string unreadPath = Path.Combine(_basePath, targetId, "unread");
        if (!Directory.Exists(unreadPath))
        {
            Directory.CreateDirectory(unreadPath);
        }

        var messageData = new
        {
            SenderId = senderId,
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        string fileName = $"{DateTime.UtcNow.Ticks}.json";
        string filePath = Path.Combine(unreadPath, fileName);
        File.WriteAllText(filePath, JsonSerializer.Serialize(messageData));
    }

    public List<dynamic> RetrieveAndMoveUnreadMessages(string userId)
    {
        string unreadPath = Path.Combine(_basePath, userId, "unread");
        string readPath = Path.Combine(_basePath, userId, "messages");
        var messages = new List<dynamic>();

        if (!Directory.Exists(unreadPath))
        {
            return messages;
        }

        if (!Directory.Exists(readPath))
        {
            Directory.CreateDirectory(readPath);
        }

        string[] files = Directory.GetFiles(unreadPath, "*.json");
        foreach (string filePath in files)
        {
            string content = File.ReadAllText(filePath);
            var msg = JsonSerializer.Deserialize<dynamic>(content);
            if (msg != null)
            {
                messages.Add(msg);
            }

            // Move to read folder
            string fileName = Path.GetFileName(filePath);
            string destinationPath = Path.Combine(readPath, fileName);
            File.Move(filePath, destinationPath);
        }

        return messages;
    }
}

public class ChatMessage
{
    public string SenderId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
