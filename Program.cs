using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

var chatUrl = string.Empty;
var message = string.Empty;

try
{
    message = WriteAndReadLine("Insira a mensagem a ser enviada: ");


    Console.WriteLine("Exemplo de telefone: 5511999999999");
    Console.WriteLine("Exemplo de chatId: ES7ntMUvp9r39U0BrYLZg2");
    Console.WriteLine();

    var chatId = WriteAndReadLine("Insira o telefone (somente numeros) ou chatId: ");

    if (IsDigitsOnly(chatId))
    {
        if (!ConsoleReadYesOrNo("Confirmando: o numero eh um telefone, correto? [Y/n] ") 
            || chatId.Length != 13)
        {
            throw new Exception("Erro com numero de telefone.");
        }
    }

    Console.WriteLine("Amanha só significa amanhã depois que voce dormir.");
    Console.WriteLine("Mensagens agendadas de madrugada são tratadas como do dia anterior.");
    Console.WriteLine();

    var isToTomorrow = ConsoleReadYesOrNo("A mensagem eh para ser enviada amanha? [Y/n] ");

    var timestamp = string.Empty;

    if (isToTomorrow)
    {
        Console.WriteLine("Exemplo de timestamp completo (dia-mes hora:minuto): 25-12 9:15");
        Console.WriteLine();

        timestamp = WriteAndReadLine("Insira o timestamp completo de envio: ");
        Console.WriteLine();
    }
    else
    {
        Console.WriteLine("Exemplo de timestamp parcial (hora:minuto): 9:15");
        Console.WriteLine();

        timestamp = WriteAndReadLine("Insira o timestamp parcial de envio: ");
    }

    if (IsDigitsOnly(chatId))
    {
        chatUrl = $"https://wa.me/{chatId}";
    }
    else
    {
        chatUrl = $"https://chat.whatsapp.com/{chatId}";
    }

    DateTime desiredTimestamp = DateTime.MinValue;
    timestamp = timestamp.Trim();
    var now = DateTime.Now;
    var seconds = new Random().Next(0, 45);

    var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
    var isLastDayOfMonth = daysInMonth == now.Day;


    if (!isToTomorrow)
    {
        // when the message is not to tomorrow (today or some other day, full timestamp)
        // 25-12 9:15
        const int timeToSleep = 20;
        const int timeToWakeUp = 7;

        var day = now.Hour >= timeToSleep ? now.Day + 1 : now.Day;
        var hour = int.Parse(timestamp.Split(':')[0]);
        var minute = int.Parse(timestamp.Split(':')[1]);

        var month = isLastDayOfMonth ? now.Month + 1 : now.Month;
        if (now.Hour > timeToSleep)
        {
            desiredTimestamp = new DateTime(now.Year, month, day, hour, minute, seconds);
        }

        if (now.Hour < timeToWakeUp)
        {
            desiredTimestamp = new DateTime(now.Year, month, day, hour, minute, seconds);
        }
    }
    else 
    {
        // 09:15
        var date = timestamp.Split(' ')[0];
        var month = int.Parse(timestamp.Split('-')[1]);
        month = isLastDayOfMonth ? month + 1 : month;
        var day = int.Parse(timestamp.Split('-')[0]);

        var time = timestamp.Split(' ')[1];
        var hour = int.Parse(time.Split(':')[0]);
        var minute = int.Parse(time.Split(':')[1]);

        desiredTimestamp = new DateTime(now.Year, month, day, hour, minute, seconds);
    }

    if (desiredTimestamp == DateTime.MinValue)
    {
        throw new Exception("Erro na definicao de data do programa");
    }

    if (DateTime.Now >= desiredTimestamp)
    {
        throw new Exception("Data igual passada ou igual ao agora provida.");
    }

    Console.WriteLine($"Mensagem vai ser enviada as ${JsonConvert.SerializeObject(desiredTimestamp)}.");
    while (DateTime.Now != desiredTimestamp)
    {
        var remainingMinutesUntilMessageSend = (desiredTimestamp - DateTime.Now).Duration().Minutes;
        var logAttempts = remainingMinutesUntilMessageSend > 15 ? remainingMinutesUntilMessageSend / 15 : 1;

        
        for (int i = 0; i < logAttempts; i++)
        {
            Console.WriteLine($"{DateTime.Now}: Esperando a hora de mandar a mensagem...");
            Thread.Sleep(1000 * 60 * (remainingMinutesUntilMessageSend / logAttempts));
        }
    }
}
catch (Exception ex)
{
    throw new Exception($"Houve um erro com os parametros de entrada.\nStacktrace: {ex.Message}");
}

try
{
    var chromeOptions = new ChromeOptions();
    chromeOptions.AddArgument(@"--user-data-dir=C:\\Users\\lucas.frois\\AppData\\Local\\Google\\Chrome\\User Data");
    chromeOptions.AddArgument(@"--profile-directory=Default");
    // TODO: cannot let chrome open during this process

    var driver = new ChromeDriver(@"C:\dev", chromeOptions);

    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);
    driver.Navigate().GoToUrl(chatUrl);

    var startChatElement = driver.FindElement(By.Id("action-button"));
    startChatElement.Click();

    var currentUrl = driver.Url;
    var desiredUrl = currentUrl.Replace("api", "web");
    driver.Navigate().GoToUrl(desiredUrl);

    var chatInputElement = driver.FindElements(By.ClassName(@"selectable-text")).Last();
    chatInputElement.SendKeys(message);
    chatInputElement.SendKeys(Keys.Enter);
}
catch (Exception ex)
{
    throw new Exception($"Aconteceu um erro relacionado ao programa.\nStacktrace: {ex.Message}");
}

bool IsDigitsOnly(string str)
{
    foreach (char c in str)
    {
        if (c < '0' || c > '9')
            return false;
    }

    return true;
}

bool ConsoleReadYesOrNo(string stdout)
{   
    Console.Write(stdout);

    var input = Console.ReadLine();

    Console.WriteLine();

    if (input == null) return false;

    if (input.ToLower().StartsWith('y') && input.Length.Between(1, 3))
    {
        return true;
    }

    return false;
}

string WriteAndReadLine(string stdout)
{   
    Console.Write(stdout);
    
    var stdin = Console.ReadLine() ?? string.Empty;

    Console.WriteLine();

    return stdin;
}

public static class Extensions
{
    public static bool Between(this int number, int min, int max)
    {
        return number >= min && number <= max;
    }
}