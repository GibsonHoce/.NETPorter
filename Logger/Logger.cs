using Avalonia.Controls;

namespace Logger
{
    public class Logger
    {
        private string outPath;
        private static List<string> logs = new List<string>();

        public TextBox LogTextBox { get; set; }

        public enum MessageType
        {
            Message,
            Error
        }

        // Basic constructer
        public Logger()
        {

        }

        // Constructs the logger by taking in the path to where the output text file should be written
        public Logger(string outPath)
        {
            this.outPath = outPath;
        }

        // Appends the message to the next index in "logs"
        public void appendMessage(string message, MessageType messageType)
        {
            if (messageType == MessageType.Error)
            {
                message = message.ToUpper();
            }

            logs.Add(message);

            if (LogTextBox != null)
            {
                LogTextBox.Text += message + Environment.NewLine;
            }
        }

        public List<string> getLog()
        {
            return logs;
        }

        public void writeOut()
        {
            string path = outPath + "\\output.txt";
            File.WriteAllLines(path, logs);
        }
    }
}
