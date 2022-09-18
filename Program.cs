using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

// TODO: atualmente o programa só roda em windows.
// ao rodar em máquina diferente, as variáveis user-data-dir e profile-directory precisam ser ajustadas

var chatUrl = string.Empty;
var message = string.Empty;
var chatId = string.Empty;

try
{
    message = WriteAndReadLine("Insira a mensagem a ser enviada: ");

    Console.WriteLine("Exemplo de telefone: 5511999999999");
    Console.WriteLine("Exemplo de chatId: ES7ntMUvp9r39U0BrYLZg2 (rocket), IR9YkcuJaL45hcsVE8Qaa4 (anotacoes paula e lucas)");
    Console.WriteLine();

    chatId = WriteAndReadLine("Insira o telefone (somente numeros) ou chatId: ");

    if (IsDigitsOnly(chatId))
    {
        if (!ConsoleReadYesOrNo("Confirmando: o numero eh um telefone, correto? [Y/n] ") 
            || chatId.Length != 13)
        {
            throw new Exception("Erro com numero de telefone.");
        }
    }

    var scheduledToNextTimeOcurrence = ConsoleReadYesOrNo("A mensagem eh para ser enviada na proxima ocorrencia de um horário? [Y/n] ");

    var timestamp = string.Empty;

    if (scheduledToNextTimeOcurrence)
    {
        Console.WriteLine("Exemplo de timestamp parcial (hora:minuto): 9:15");
        Console.WriteLine();

        timestamp = WriteAndReadLine("Insira o timestamp parcial de envio: ");
    }
    else
    {
        Console.WriteLine("Exemplo de timestamp completo (dia-mes hora:minuto): 25-12 9:15");
        Console.WriteLine();

        timestamp = WriteAndReadLine("Insira o timestamp completo de envio: ");
        Console.WriteLine();
    }

    if (IsDigitsOnly(chatId))
    {
        chatUrl = $"https://wa.me/{chatId}";
    }
    else
    {
        chatUrl = $"https://chat.whatsapp.com/{chatId}";
    }

    var desiredTimestamp = FormatTimestamp(timestamp, !scheduledToNextTimeOcurrence);

    Console.WriteLine($"Mensagem vai ser enviada as ${JsonConvert.SerializeObject(desiredTimestamp)}.");
    var remainingTime = (DateTime.Now - desiredTimestamp).Duration();
    Console.WriteLine($"Agora sao {DateTime.Now}. Faltam {remainingTime.ToString(@"hh\hmm\mss\s")} pra mensagem ser enviada.");
    WaitForDateTimeAndLogEveryTenMinutes(desiredTimestamp);
    
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
    // TODO: cannot let chrome open before opening chromedriver
    // probably bc of profiles. i cant see a good fix for this otherwise a manual action

    var driver = new ChromeDriver(@"C:\dev", chromeOptions);

    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);
    driver.Navigate().GoToUrl(chatUrl);

    var startChatElement = driver.FindElement(By.Id("action-button"));
    startChatElement.Click();

    var goToWhatsappWebElementSelector = IsDigitsOnly(chatId) ?
        @"//a[contains(@href, 'web.whatsapp.com/send')]"
        : @"//a[contains(@href, 'web.whatsapp.com/accept')]";

    var goToWhatsappWebElement = driver.FindElement(By.XPath(goToWhatsappWebElementSelector));
    goToWhatsappWebElement.Click();

    var chatInputElement = driver.FindElement(By.XPath(@"//div[@title='Mensagem' and @role='textbox']/p"));

    chatInputElement.SendKeys(message);
    chatInputElement.SendKeys(Keys.Enter);

    Thread.Sleep(1000);

    driver.Close();
    driver.Quit();
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

DateTime FormatTimestamp(string timestamp, bool fullTimestamp)
{
    DateTime desiredTimestamp = DateTime.MinValue;

    var now = DateTime.Now;
    timestamp = timestamp.Trim();
    var randomSecond = new Random().Next(1, 50);

    if (!fullTimestamp) // short version of datetime, e.g.: 09:27
    {
        var hour = int.Parse(timestamp.Split(':')[0]); // 09
        var minute = int.Parse(timestamp.Split(':')[1]); // 27

        var isToTomorrow = minute > now.Minute && hour > now.Hour;
        var isToToday = !isToTomorrow;

        var tomorrow = now.AddDays(1);

        desiredTimestamp = isToToday ?
            new DateTime(now.Year, now.Month, now.Day, hour, minute, randomSecond, 0) :
            new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, hour, minute, randomSecond, 0);
    }
    else // long version, 25-12 9:15
    {
        var dateSlice = timestamp.Split(' ')[0];
        var timeSlice = timestamp.Split(' ')[1];

        var day = int.Parse(dateSlice.Split('-')[0]); // 25
        var month = int.Parse(dateSlice.Split('-')[1]); // 12

        var hour = int.Parse(timeSlice.Split(':')[0]); // 09
        var minute = int.Parse(timeSlice.Split(':')[1]); // 27

        desiredTimestamp = new DateTime(now.Year, month, day, hour, minute, randomSecond, 0);
    }

    if (desiredTimestamp == DateTime.MinValue)
    {
        throw new Exception("Erro na definicao de data do programa");
    }

    if (DateTime.Now >= desiredTimestamp)
    {
        throw new Exception("Data igual passada ou igual ao agora provida.");
    }

    return desiredTimestamp;
}

void WaitForDateTimeAndLogEveryTenMinutes(DateTime desiredTimestamp)
{
    while (desiredTimestamp > DateTime.Now)
    {
        if (DateTime.Now.Minute % 10 == 0 && DateTime.Now.Second == 0 && DateTime.Now.Millisecond == 0)
        {
            var remainingTime = (DateTime.Now - desiredTimestamp).Duration();

            Console.WriteLine($"Agora sao {DateTime.Now}. Faltam {remainingTime.ToString(@"hh\hmm\mss\s")} pra mensagem ser enviada.");
            Thread.Sleep(100);
        }
    }
}
public static class Extensions
{
    public static bool Between(this int number, int min, int max)
    {
        return number >= min && number <= max;
    }
}