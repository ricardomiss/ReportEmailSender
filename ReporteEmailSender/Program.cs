using ReporteEmailSender.Models;
using ReporteEmailSender.Utilities;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

var basePath = AppDomain.CurrentDomain.BaseDirectory;
var configFile = Path.Combine(basePath, "config.json");
bool running = true;

var config = VerifyConfig();

while (running)
{
    Console.Clear();
    Console.WriteLine("Reporte Email Sender");
    Console.WriteLine("---MENU---");
    Console.WriteLine("1. Enviar reporte");
    Console.WriteLine("2. Configurar");
    Console.WriteLine("0. Salir");

    var option = InputUtility.PromptForInput<int>(
    "Selecciona una opción:",
    input => int.TryParse(input, out var res) && res >= 0 && res <= 2,
    "Opción inválida",
    int.Parse
    );

    Console.Clear();

    switch (option)
    {
        case 1:
            
            await SendEmail();
            break;
        case 2:
            await ModifyConfig();
            break;
        case 0:
            running = false;
            Environment.Exit(0);
            break;
    }
}

async Task SendEmail()
{
    var client = new SmtpClient
    {
        Host = config.smtp.Host,
        Port = config.smtp.Port,
        Credentials = new NetworkCredential(config.smtp.User, config.smtp.Password)
    };

    var mail = new MailMessage
    {
        From = new MailAddress(config.correo.from),
        To = { config.correo.to },
        Subject = GetSubject(),
        Body = GetBody(),
    };
    var ccs = GetCCs();
    var att = SetAttachments();

    if(!string.IsNullOrEmpty(ccs))
        mail.CC.Add(ccs);

    if(att != null)
        mail.Attachments.Add(att);

    bool isSent = await SMTPsendEmail(client, mail);

    if(isSent)
        File.WriteAllText(configFile, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));

    Console.ReadKey();
}

async Task ModifyConfig()
{
    Console.Clear();
    Console.WriteLine("Configuración");
    Console.WriteLine("---MENU---");
    Console.WriteLine("1. Modificar correos");
    Console.WriteLine("2. Modificar SMTP");
    Console.WriteLine("3. Modificar contenido");
    Console.WriteLine("4. Regresar");

    var option = InputUtility.PromptForInput<int>(
        "Selecciona una opción:",
        input => int.TryParse(input, out var res) && res >= 1 && res <= 4,
        "Opción inválida",
        int.Parse
    );

    Console.Clear();

    switch (option)
    {
        case 1:
            ModifyCorreos();
            break;
        case 2:
            ModifySMTP();
            break;
        case 3:
            ModifyContent();
            break;
        case 4:
            break;
    }

}
async Task<bool> SMTPsendEmail(SmtpClient client, MailMessage mail)
{
    try
    {
        Task sendTask = client.SendMailAsync(mail);

        var time = await Animation("Se esta enviando el correo", sendTask);
        await sendTask;

        Console.Clear();
        Console.WriteLine($"Correo enviado en {time.ToString(@"mm\:ss")}");
        return true;
    }
    catch(Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine("¿Reintentar? (y/n)");
        if (InputUtility.PromptForBool())
        {
            Console.Clear();
            await SendEmail();
        }
        Console.Clear();
        return false;
    }
}

Configuration VerifyConfig()
{
    try
    {
        if (!File.Exists(configFile))
        {
            Console.WriteLine("No se encontró el archivo de configuración. ¿Deseas crear uno? y/n");
            if (InputUtility.PromptForBool())
            {
                Console.Clear();
                return CreateConfigFile();
            }
            else
            {
                Console.WriteLine("No se creó el archivo de configuración.");
                Environment.Exit(1);
            }
        }
        return JsonSerializer.Deserialize<Configuration>(File.ReadAllText(configFile));
    }
    catch(Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        Console.WriteLine("No se pudo leer el archivo de configuración. ¿Desea generar uno nuevo?(y/n)");
        if (InputUtility.PromptForBool())
        {
            Console.Clear();
            return CreateConfigFile();
        }
        Environment.Exit(1);
        return null;
    }
}

Configuration CreateConfigFile()
{
    var config = new Configuration();

    config.correo.from = InputUtility.PromptForInput<string>(
        "Ingresa tu correo electrónico:",
        input => input.Contains("@"),
        "Correo inválido"
    );

    config.correo.to = InputUtility.PromptForInput<string>(
        "Ingresa el correo del destinatario:",
        input => input.Contains("@"),
        "Correo inválido"
    );

    config.correo.cc = InputUtility.PromptForInputList(
        "Ingresa los correos de los destinatarios en copia:(Enter para saltar)",
        input => input.Contains("@"),
        "Correo inválido"
    );

    config.smtp.Host = InputUtility.PromptForInput<string>(
        "Ingresa el host del servidor SMTP:",
        input => !string.IsNullOrEmpty(input),
        "No se ingreso un host");

    config.smtp.Port = InputUtility.PromptForInput<int>(
        "Ingresa el puerto del servidor SMTP:",
        input => int.TryParse(input, out _),
        "No se ingreso un puerto",
        int.Parse);

    config.smtp.User = config.correo.from;

    config.smtp.Password = InputUtility.PromptForInput<string>(
        $"Ingresa la contraseña del usuario: {config.smtp.User}",
        input => !string.IsNullOrEmpty(input),
        "No se ingreso una contraseña");

    File.WriteAllText(configFile, JsonSerializer.Serialize(config, new JsonSerializerOptions {WriteIndented = true}));
    Console.Clear();
    return config;
}

string GetSubject()
{
    Console.WriteLine("Subject");
    config.content.SavedSubject.Select((x, i) => new { Subject = x, Index = i + 1 }).ToList()
        .ForEach(item => Console.WriteLine($"{item.Index}. {item.Subject}"));
    Console.WriteLine("0. Nuevo");
    var subjectOption = InputUtility.PromptForInput<int>(
        "Selecciona una subject:",
        input => int.TryParse(input, out var res) && res >= 0 && res <= config.content.SavedSubject.Count,
        "Opción inválida",
        int.Parse
    );

    if (subjectOption == 0)
    {
        var newSubject = InputUtility.PromptForInput<string>(
            "Ingresa el nuevo subject:",
            input => !string.IsNullOrEmpty(input),
            "Subject inválido"
        );
        config.content.SavedSubject.Add(newSubject);
        Console.Clear();
        return newSubject;
    }
    Console.Clear();
    return config.content.SavedSubject[subjectOption - 1];
}

string GetBody()
{
    Console.WriteLine("Body");
    config.content.SavedBodies.Select((x, i) => new { Body = x, Index = i + 1 }).ToList()
        .ForEach(item => Console.WriteLine($"{item.Index}. {item.Body}"));
    Console.WriteLine("0. Nuevo");
    var bodyOption = InputUtility.PromptForInput<int>(
        "Selecciona un body:",
        input => int.TryParse(input, out var res) && res >= 0 && res <= config.content.SavedBodies.Count,
        "Opción inválida",
        int.Parse
    );
    if (bodyOption == 0) {
        var newBody = InputUtility.PromptForBody();
        config.content.SavedBodies.Add(newBody);
        Console.Clear();
        return newBody;
    }
    Console.Clear();
    return config.content.SavedBodies[bodyOption - 1];
}

string GetCCs()
{
    Console.WriteLine("CCs");
    Console.WriteLine("¿Desea agregar CCS? y/n");
    if (InputUtility.PromptForBool())
    {
        Console.WriteLine("Agregando CCs de Configuracion");
        Console.Clear();
        return string.Join(",", config.correo.cc);
    }
    Console.Clear();
    return "";
}

Attachment SetAttachments()
{
    Console.WriteLine("Attachments");
    Console.WriteLine("¿Desea agregar archivos? y/n");
    if (InputUtility.PromptForBool())
    {
        Console.Clear();
        new DirectoryInfo(basePath).GetFiles()
            .OrderByDescending(f => f.CreationTime)
            .Take(10)
            .Select((x, i) => new { File = x, Index = i + 1 }).ToList()
            .ForEach(item => Console.WriteLine($"{item.Index}. {item.File.Name} - {item.File.CreationTime}"));
        Console.WriteLine($"0. Ultimo Archivo Guardado ({Path.GetFileName(config.content.LastPathFile)})");
        var attachmentOption = InputUtility.PromptForInput<int>(
            "Selecciona un archivo:",
            input => int.TryParse(input, out var res) && res >= 0 && res <= 10,
            "Opción inválida",
            int.Parse
        );
        if (attachmentOption == 0)
        {
            if (!string.IsNullOrEmpty(config.content.LastPathFile))
            {
                Console.Clear();
                return new Attachment(config.content.LastPathFile);
            }
            return null;
        }
        Console.Clear();
        config.content.LastPathFile = new DirectoryInfo(basePath).GetFiles().OrderByDescending(f => f.CreationTime).ElementAt(attachmentOption - 1).FullName;
        return new Attachment(config.content.LastPathFile);
    }
    return null;
}

async Task<TimeSpan>  Animation(string msg, Task task)
{
    Stopwatch stopWatch = new Stopwatch();
    string[] animation = { "|", "/", "-", "\\" };
    int index = 0;
    int delay = 200;

    stopWatch.Start();

    while (!task.IsCompleted)
    {
        Console.Write($"\r{msg} {animation[index]} {stopWatch.Elapsed.ToString(@"mm\:ss")}");
        index = (index + 1) % animation.Length;
        await Task.Delay(delay);
    }
    stopWatch.Stop();
    return stopWatch.Elapsed;
}

void ModifyCorreos()
{

    while (true)
    {
        Console.Clear();
        Console.WriteLine("Correos");
        Console.WriteLine("---MENU---");
        Console.WriteLine("1. From");
        Console.WriteLine("2. To");
        Console.WriteLine("3. CC");
        Console.WriteLine("4. Regresar");

        var option = InputUtility.PromptForInput<int>(
            "Selecciona una opción:",
            input => int.TryParse(input, out var res) && res >= 1 && res <= 4,
            "Opción inválida",
            int.Parse
        );

        switch(option)
        {
            case 1:
                config.correo.from = InputUtility.PromptForInput<string>(
                    "Ingresa tu correo electrónico:",
                    input => input.Contains("@"),
                    "Correo inválido"
                );
                break;
            case 2:
                config.correo.to = InputUtility.PromptForInput<string>(
                    "Ingresa el correo del destinatario:",
                    input => input.Contains("@"),
                    "Correo inválido"
                );
                break;
            case 3:
                config.correo.cc = InputUtility.PromptForInputList(
                    "Ingresa los correos de los destinatarios en copia:(Enter para saltar)",
                    input => input.Contains("@"),
                    "Correo inválido"
                );
                break;
            case 4:
                return;
        }
    }
}

void ModifySMTP()
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("SMTP");
        Console.WriteLine("---MENU---");
        Console.WriteLine("1. Host");
        Console.WriteLine("2. Port");
        Console.WriteLine("3. User");
        Console.WriteLine("4. Password");
        Console.WriteLine("5. Regresar");

        var option = InputUtility.PromptForInput<int>(
            "Selecciona una opción:",
            input => int.TryParse(input, out var res) && res >= 1 && res <= 5,
            "Opción inválida",
            int.Parse
        );

        switch(option)
        {
            case 1:
                config.smtp.Host = InputUtility.PromptForInput<string>(
                    "Ingresa el host del servidor SMTP:",
                    input => !string.IsNullOrEmpty(input),
                    "No se ingreso un host");
                break;
            case 2:
                config.smtp.Port = InputUtility.PromptForInput<int>(
                    "Ingresa el puerto del servidor SMTP:",
                    input => int.TryParse(input, out _),
                    "No se ingreso un puerto",
                    int.Parse);
                break;
            case 3:
                config.smtp.User = InputUtility.PromptForInput<string>(
                    "Ingresa el usuario del servidor SMTP:",
                    input => input.Contains("@"),
                    "No se ingreso un usuario"
                    );
                break;
            case 4:
                config.smtp.Password = InputUtility.PromptForInput<string>(
                    $"Ingresa la contraseña del usuario: {config.smtp.User}",
                    input => !string.IsNullOrEmpty(input),
                    "No se ingreso una contraseña");
                break;
            case 5:
                return;
        }

    }
}

void ModifyContent()
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("Content");
        Console.WriteLine("---MENU---");
        Console.WriteLine("1. SavedSubject");
        Console.WriteLine("2. SavedBodies");
        Console.WriteLine("3. Regresar");

        var option = InputUtility.PromptForInput<int>(
            "Selecciona una opción:",
            input => int.TryParse(input, out var res) && res >= 1 && res <= 3,
            "Opción inválida",
            int.Parse
        );

        switch (option)
        {
            case 1:
                config.content.SavedSubject = InputUtility.SelectOptions<string>(config.content.SavedSubject);
                break;
            case 2:
                config.content.SavedBodies = InputUtility.SelectOptions<string>(config.content.SavedBodies);
                break;
            case 3:
                return;
        }

    }
}
