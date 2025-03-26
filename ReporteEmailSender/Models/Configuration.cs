namespace ReporteEmailSender.Models
{
    public class Configuration
    {
        public Correo correo { get; set; }
        public SMTP smtp { get; set; }
        public Content content { get; set; }

        public Configuration()
        {
            correo = new Correo();
            smtp = new SMTP();
            content = new Content();
        }
    }

    public class Correo
    {
        public string from { get; set; }
        public string to { get; set; }
        public List<string> cc { get; set; }

    }

    public class SMTP
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }

    public class Content
    {
        public string LastPathFile { get; set; }
        public List<string> SavedSubject { get; set; } = new List<string>();
        public List<string> SavedBodies { get; set; } = new List<string>();
    }

}
