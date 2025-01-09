using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

// Enumlar
public enum EnumAccountType
{
    Musteri,
    Yonetici
}

// Hesap Soyut Sınıfı
public abstract class Account
{
    // Şifreleme Metodu
    private static string HashPassword(string password) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

    // Hesap Özellikleri
    public EnumAccountType AccountType { get; set; }
    public string Username { get; set; }
    private string _passwordHash;

    // Hesap Metodları
    public void SetPassword(string password) => _passwordHash = HashPassword(password);
    public bool VerifyPassword(string password) => HashPassword(password) == _passwordHash;
    public bool DeleteAccountPrompt()
    {
        if (AccountType == EnumAccountType.Yonetici)
        {
            ConsoleHandler.PrimaryMessage("\nYönetici hesapları silinemez.\n", ConsoleColor.DarkRed, true);
            return false;
        }

        ConsoleHandler.StockListVisible(false);
        Console.Write("Hesap silme işlemini onaylamak için \"evet\" yazın: ");
        string response = Console.ReadLine() ?? "";

        ConsoleHandler.CustomClear();

        if (response.ToLowerInvariant() == "evet")
        {
            AccountHandler.RemoveAccount(this);
            ConsoleHandler.PrimaryMessage("\nHesap başarıyla silindi.\n", true);

            return true;
        }

        return false;
    }

    // Hesap Seçenekleri Soyut Metodu
    public abstract void InitialiseMenu();

    // Kurucu
    public Account()
    {
        Username = "";
        _passwordHash = "";
    }
}

// Yönetici Hesabı Sınıfı
public class Admin : Account
{
    // Yönetici Seçenekleri
    public override void InitialiseMenu()
    {
        ConsoleHandler.PrimaryMessage("\nMerhaba, ");
        ConsoleHandler.PrimaryMessage($"{Username}!\n", ConsoleColor.Green, true);

        while (true)
        {
            ConsoleHandler.PrimaryMessage("\n[ Yönetici Seçenekleri ]\n\n1. Hisse Düzenle\n2. Hisse Kaldır\n3. Mevcut Kullanıcı Listesi\n4. Kullanıcı Kaldır\n5. Çıkış Yap\n\nSeçeneğiniz: ");
            
            string choice = Console.ReadLine() ?? "";

            ConsoleHandler.CustomClear();

            switch (choice)
            {
                case "1": // Hisse Düzenle
                    {
                        ConsoleHandler.StockListVisible(false);

                        Console.Write("Hisse Sembolü: ");
                        string symbol = Console.ReadLine() ?? "";

                        Console.Write("\nHisse Fiyatı: ");
                        string amount = Console.ReadLine() ?? "";
                        var result = StockExchangeHandler.ValidateDecimal(amount);

                        if (result is not null)
                        {
                            StockExchangeHandler.EditStock(symbol, result.Value);
                            ConsoleHandler.PrimaryMessage("\nHisse başarıyla düzenlendi.\n", ConsoleColor.Green, true);
                        }

                        break;
                    }
                case "2": // Hisse Kaldır
                    {
                        ConsoleHandler.StockListVisible(false);

                        Console.Write("Silinecek Hisse Sembolü: ");
                        string symbol = Console.ReadLine() ?? "";

                        if (StockExchangeHandler.RemoveStock(symbol))
                        {
                            ConsoleHandler.PrimaryMessage("\nHisse başarıyla silindi.\n", true);
                        }
                        else
                        {
                            ConsoleHandler.PrimaryMessage("\nHisse bulunamadı.\n", ConsoleColor.DarkRed, true);
                        }

                        break;
                    }

                case "3": // Mevcut Kullanıcı Listesi
                    AccountHandler.ListUsers(this);

                    break;

                case "4": // Kullanıcı Kaldır
                    {
                        ConsoleHandler.StockListVisible(false);

                        Console.Write("Silinecek Kullanıcının Adı: ");
                        Account account = AccountHandler.GetAccountFromUsername(Console.ReadLine() ?? "");

                        if (account != null)
                        {
                            account.DeleteAccountPrompt();
                        }
                        else
                        {
                            ConsoleHandler.PrimaryMessage("\nKullanıcı bulunamadı.\n", ConsoleColor.DarkRed, true);
                        }
                        break;
                    }

                case "5": // Çıkış Yap
                    return;

                default:
                    ConsoleHandler.PrimaryMessage("\nGeçersiz seçenek.\n", ConsoleColor.DarkRed, true);

                    break;
            }
        }
    }

    // Kurucu
    public Admin()
    {
        AccountType = EnumAccountType.Yonetici;
    }
}

// Müşteri Hesabı Sınıfı
public class Customer : Account
{
    // Müşteri Hesabı Özellikleri
    public decimal Balance { get; private set; }
    public Dictionary<string, decimal> Portfolio { get; set; } = [];

    // Müşteri Hesabı Metodları
    public bool EditBalance(decimal amount)
    {
        decimal newValue = Balance + amount;

        if (newValue >= 0)
        {
            Balance = newValue;
            return true; // Bakiye Değiştirildi
        }
        else
        {
            return false; // Bakiye Eksiye Düşemez
        }
    }
    public bool EditPortfolio(string stockSymbol, decimal amount)
    {
        if (amount > 0)
        {
            if (Portfolio.ContainsKey(stockSymbol))
                Portfolio[stockSymbol] += amount;
            else
                Portfolio[stockSymbol] = amount;

            return true; // Hisse Eklendi
        }
        else
        {
            amount = -amount;

            if (Portfolio.TryGetValue(stockSymbol, out decimal currentAmount) && currentAmount >= amount)
            {
                Portfolio[stockSymbol] -= amount;

                if (Portfolio[stockSymbol] == 0)
                    Portfolio.Remove(stockSymbol);

                return true; // Hisse Çıkartıldı
            }

            return false; // Hisse Yok
        }
    }
    public decimal NetWorth()
    {
        decimal totalValue = Balance;

        foreach (var stock in Portfolio)
        {

            if (StockExchangeHandler.Stocks.TryGetValue(stock.Key, out decimal price))
            {
                totalValue += price * stock.Value;
            }
        }

        return totalValue;
    }

    public void BuyStock(string symbol, decimal amount)
    {
        symbol = symbol.ToUpperInvariant();

        if (StockExchangeHandler.Stocks.TryGetValue(symbol, out decimal price))
        {
            decimal totalCost = price * amount;

            if (EditBalance(-totalCost))
            {
                EditPortfolio(symbol, amount);
                ConsoleHandler.PrimaryMessage($"\n{amount} tane {symbol} hissesi satın alındı.\n", ConsoleColor.Green, true);
            }
            else
            {
                ConsoleHandler.PrimaryMessage("\nYetersiz bakiye.\n", ConsoleColor.DarkRed, true);
            }
        }
        else
        {
            ConsoleHandler.PrimaryMessage("\nHisse bulunamadı.\n", ConsoleColor.DarkRed, true);
        }
    }
    public void SellStock(string symbol, decimal amount)
    {
        symbol = symbol.ToUpperInvariant();

        if (StockExchangeHandler.Stocks.TryGetValue(symbol, out decimal price))
        {
            if (EditPortfolio(symbol, -amount))
            {
                decimal totalRevenue = price * amount;

                EditBalance(totalRevenue);

                ConsoleHandler.PrimaryMessage($"\n{amount} tane {symbol} hissesi satıldı.\n", ConsoleColor.Green, true);
            }
            else
            {
                ConsoleHandler.PrimaryMessage("\nYetersiz hisse miktarı.\n", ConsoleColor.DarkRed, true);
            }
        }
        else
        {
            ConsoleHandler.PrimaryMessage("\nHisse bulunamadı.\n", ConsoleColor.DarkRed, true);
        }
    }
    public void ViewPortfolio()
    {
        ConsoleHandler.PrimaryMessage("\nPortföyünüz:\n\n");

        decimal totalValue = Balance;

        if (Portfolio.Count == 0)
        {
            ConsoleHandler.PrimaryMessage("- Hiç hisseniz yok.\n");
        }
        else
        {
            foreach (var stock in Portfolio)
            {

                if (StockExchangeHandler.Stocks.TryGetValue(stock.Key, out decimal price))
                {
                    decimal valuation = (price * stock.Value);
                    totalValue += valuation;

                    ConsoleHandler.PrimaryMessage($"- {stock.Key}", ConsoleColor.Yellow);
                    ConsoleHandler.PrimaryMessage($": {stock.Value} (");
                    ConsoleHandler.PrimaryMessage($"{valuation:C}", ConsoleColor.DarkGreen);
                    ConsoleHandler.PrimaryMessage(")\n");
                }
                else
                {
                    ConsoleHandler.PrimaryMessage($"- {stock.Key}: {stock.Value} (???)\n"); // Hisse fiyatı bulunamadı
                }
            }
        }

        ConsoleHandler.PrimaryMessage("\nBakiye: ");
        ConsoleHandler.PrimaryMessage($"{Balance:C}", ConsoleColor.DarkGreen);
        ConsoleHandler.PrimaryMessage("\nToplam: ");
        ConsoleHandler.PrimaryMessage($"{totalValue:C}\n", ConsoleColor.DarkGreen, true);
    }

    // Müşteri Seçenekleri
    public override void InitialiseMenu()
    {
        ConsoleHandler.PrimaryMessage("\nMerhaba, ");
        ConsoleHandler.PrimaryMessage($"{Username}!\n", ConsoleColor.Green, true);

        while (true)
        {
            ConsoleHandler.PrimaryMessage("\n[ Müşteri Seçenekleri ]\n\n1. Hisse Satın Al\n2. Hisse Sat\n3. Bakiye Ekle\n4. Portföy Görüntüle\n5. Çıkış Yap\n6. Hesabı Sil\n\nSeçeneğiniz: ");

            string choice = Console.ReadLine() ?? "";

            ConsoleHandler.CustomClear();

            switch (choice)
            {
                case "1": // Hisse Satın Al
                    {
                        ConsoleHandler.StockListVisible(false);

                        Console.Write("Hisse Sembolü: ");
                        string symbol = Console.ReadLine() ?? "";

                        Console.Write("\nMiktar: ");
                        string amount = Console.ReadLine() ?? "";
                        var result = StockExchangeHandler.ValidateDecimal(amount);

                        if (result is not null)
                        {
                            BuyStock(symbol, result.Value);
                        }

                        break;
                    }

                case "2": // Hisse Sat
                    {
                        ConsoleHandler.StockListVisible(false);

                        Console.Write("Hisse Sembolü: ");
                        string symbol = Console.ReadLine() ?? "";

                        Console.Write("\nMiktar: ");
                        string amount = Console.ReadLine() ?? "";
                        var result = StockExchangeHandler.ValidateDecimal(amount);

                        if (result is not null)
                        {
                            SellStock(symbol, result.Value);
                        }

                        break;
                    }

                case "3": // Bakiye Ekle
                    {
                        ConsoleHandler.StockListVisible(false);

                        Console.Write("Yüklenecek Tutar: ");
                        string amount = Console.ReadLine() ?? "";
                        var result = StockExchangeHandler.ValidateDecimal(amount);

                        if (result is not null)
                        {
                            EditBalance(result.Value);
                            ConsoleHandler.PrimaryMessage($"\n{result.Value:C}", ConsoleColor.DarkGreen);
                            ConsoleHandler.PrimaryMessage(" yüklendi.\n", true);
                        }

                        break;
                    }

                case "4": // Portföy Görüntüle
                    ViewPortfolio();

                    break;

                case "5": // Çıkış Yap
                    ConsoleHandler.PrimaryMessage("\nÇıkış yapıldı.\n", true);

                    return;

                case "6":
                    if (DeleteAccountPrompt())
                    {
                        return; // Çıkış Yap
                    }
                    else
                    {
                        break;
                    }

                default:
                    ConsoleHandler.PrimaryMessage("\nGeçersiz seçenek.\n", ConsoleColor.DarkRed, true);

                    break;
            }
        }
    }

    // Kurucu
    public Customer()
    {
        AccountType = EnumAccountType.Musteri;
        Balance = 0;
    }
}

// Hesap Yönetim Sistemi
public static class AccountHandler
{
    // Hesap Listesi
    private static List<Account> Accounts { get; set; } = [];

    // Hesap Yönetim Metodları
    public static Account GetAccountFromUsername(string username) => Accounts.FirstOrDefault(account => account.Username.Equals(username.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
    public static bool ValidateUsername(string newUsername)
    {
        if (GetAccountFromUsername(newUsername) is not null)
        {
            ConsoleHandler.PrimaryMessage("\nKullanıcı adı zaten mevcut.\n", ConsoleColor.DarkRed, true);
            return false;
        }
        else if (newUsername.Any(char.IsWhiteSpace))
        {
            ConsoleHandler.PrimaryMessage("\nKullanıcı adı boşluk içeremez.\n", ConsoleColor.DarkRed, true);
            return false;
        }
        else if (newUsername.Length < 3)
        {
            ConsoleHandler.PrimaryMessage("\nKullanıcı adı en az 3 karakter uzunluğunda olmalıdır.\n", ConsoleColor.DarkRed, true);
            return false;
        }
        else if (newUsername.Length > 20)
        {
            ConsoleHandler.PrimaryMessage("\nKullanıcı adı en fazla 20 karakter uzunluğunda olmalıdır.\n", ConsoleColor.DarkRed, true);
            return false;
        }

        return true;
    }
    public static void Login(string username, string password)
    {
        var account = GetAccountFromUsername(username);

        if (account is not null && account.VerifyPassword(password))
        {
            account.InitialiseMenu();
        }
        else
        {
            ConsoleHandler.PrimaryMessage("\nGeçersiz kullanıcı adı veya şifre.\n", ConsoleColor.DarkRed, true);
        }
    }
    public static void AddAccount(EnumAccountType accountType, string username, string password)
    {
        Account newAccount;

        if (accountType == EnumAccountType.Musteri)
        {
            newAccount = new Customer();
        }
        else
        {
            newAccount = new Admin();
        }

        newAccount.Username = username;
        newAccount.SetPassword(password);
        Accounts.Add(newAccount);

        ConsoleHandler.PrimaryMessage("\nHesap başarıyla oluşturuldu!\n", ConsoleColor.Green);
    }
    public static void RemoveAccount(Account account)
    {
        Accounts.Remove(account);
    }
    public static void ListUsers(Account authorisedAccount)
    {
        if (authorisedAccount.AccountType == EnumAccountType.Yonetici)
        {
            ConsoleHandler.PrimaryMessage("Yönetici Listesi:\n");

            foreach (var account in Accounts)
            {
                if (account is Admin admin)
                {
                    ConsoleHandler.PrimaryMessage("- Kullanıcı Adı: ");
                    ConsoleHandler.PrimaryMessage($"{admin.Username}\n", ConsoleColor.Magenta);
                }
            }

            ConsoleHandler.PrimaryMessage("\nMüşteri Listesi:\n");

            foreach (var account in Accounts)
            {
                if (account is Customer customer)
                {
                    ConsoleHandler.PrimaryMessage($"- Kullanıcı Adı: {customer.Username} | Özsermaye: {customer.NetWorth():C}\n");
                }
            }

            ConsoleHandler.PrimaryMessage("", true);
        }
    }
}

// Borsa Yönetim Sistemi
public static class StockExchangeHandler
{
    // Borsa Verileri
    public static Dictionary<string, decimal> Stocks { get; private set; } = [];
    private static DateTime _lastUpdate = DateTime.Now;

    // Borsa Yönetim Metodları
    public static decimal? ValidateDecimal(string value)
    {
        if (decimal.TryParse(value, out decimal result))
        {
            if (result >= 0)
            {
                return result;
            }
            else
            {
                ConsoleHandler.PrimaryMessage("\nDeğer negatif olamaz.\n", ConsoleColor.DarkRed, true);
                return null;
            }
        }
        else
        {
            ConsoleHandler.PrimaryMessage("\nGeçersiz decimal formatı.\n", ConsoleColor.DarkRed, true);
            return null;
        }
    }
    public static void EditStock(string symbol, decimal price)
    {
        Stocks[symbol.ToUpperInvariant()] = price;
    }
    public static bool RemoveStock(string symbol)
    {
        if (Stocks.Remove(symbol.ToUpperInvariant()))
        {
            return true;
        }

        return false;
    }
    public static void ListStocks()
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("-----------------------------------------\nBorsa | Son Güncelleme: " + _lastUpdate.GetDateTimeFormats('T')[0] + "\n-----------------------------------------\n");

        if (Stocks.Count == 0)
        {
            Console.Write("Şu anda borsada hisse yok.\n");
            return;
        }

        foreach (var stock in Stocks)
        {
            Console.Write("Hisse: ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{stock.Key}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" | Fiyat: ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write($"{stock.Value:C}\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("-----------------------------------------\n\n");
    }

    // Hisse Fiyatını Rastgele Değiştirme Fonksiyonu
    private static void StockPriceRandomisation(CancellationToken token)
    {
        Random random = new();

        while (!token.IsCancellationRequested)
        {
            Thread.Sleep(5000);

            lock (StockExchangeHandler.Stocks)

            {
                foreach (var stockKey in StockExchangeHandler.Stocks.Keys.ToList())
                {
                    decimal newPrice = StockExchangeHandler.Stocks[stockKey] + ((decimal)(random.NextDouble() - 0.5) * 0.2m * StockExchangeHandler.Stocks[stockKey]);

                    EditStock(stockKey, newPrice);
                }
            }

            _lastUpdate = DateTime.Now;

            ConsoleHandler.WriteStockList();
        }
    }
    private static CancellationTokenSource cancellationTokenSource = new();

    // Paralel Task
    public static void Initialise()
    {
        Task.Run(() => StockPriceRandomisation(cancellationTokenSource.Token));
    }
    public static void Terminate()
    {
        cancellationTokenSource.Cancel();
    }
}

// Konsol Yönetim Sistemi
public static class ConsoleHandler
{
    // Genel Mesaj Metodu
    public static void PrimaryMessage(string message, ConsoleColor color = ConsoleColor.White, bool bottomLine = false)
    {
        if (!_stockListVisible) // Birincil mesajlar her zaman borsanın altında gözükür
        {
            StockListVisible(true);
        }
        
        Console.ForegroundColor = color;

        try
        {
            Console.Write(message);
        }
        finally
        {
            Console.ForegroundColor = ConsoleColor.White;
        }

        if (bottomLine)
        {
            Console.Write("\n-----------------------------------------\n");
        }
    }
    public static void PrimaryMessage(string message, bool bottomLine)
    {
        PrimaryMessage(message, ConsoleColor.White, bottomLine);
    }

    // Borsa Yazdırma
    private static bool _stockListVisible = false;
    public static void StockListVisible(bool visible)
    {
        _stockListVisible = visible;
        CustomClear();
    }
    public static void WriteStockList()
    {
        if (_stockListVisible)
        {
            int minTop = StockExchangeHandler.Stocks.Count + 4;

            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            Console.CursorLeft = 0;
            Console.CursorTop = 0;

            StockExchangeHandler.ListStocks();

            Console.CursorLeft = originalLeft;
            Console.CursorTop = originalTop < minTop ? minTop : originalTop;
        }
    }
    public static void CustomClear()
    {
        Console.Clear();

        if (_stockListVisible)
        {
            WriteStockList(); // Görülmeli
        }
    }
}

// Ana Program
public static class Program
{
    static void Main()
    {
        // Varsayılan Hisseler
        StockExchangeHandler.Stocks["AAPL"] = 245.53m;
        StockExchangeHandler.Stocks["GOOG"] = 197.96m;
        StockExchangeHandler.Stocks["AMZN"] = 227.61m;
        StockExchangeHandler.Stocks["TSLA"] = 411.05m;

        // Varsayılan Yönetici Hesabı
        AccountHandler.AddAccount(EnumAccountType.Yonetici, "admin", "admin");
        ConsoleHandler.CustomClear();

        // Program Döngüsü
        StockExchangeHandler.Initialise();

        ConsoleHandler.PrimaryMessage("\nBorsa Sistemi'ne Hoşgeldiniz\n", ConsoleColor.Cyan, true);

        while (true)
        {
            ConsoleHandler.PrimaryMessage("\n[ Ana Menü ]\n\n1. Giriş Yap\n2. Kayıt Ol\n3. Çıkış Yap\n\nSeçeneğiniz: ");
            
            string choice = Console.ReadLine() ?? "";

            ConsoleHandler.CustomClear();

            switch (choice)
            {
                case "1": // Giriş Yap
                    ConsoleHandler.StockListVisible(false);

                    Console.Write("Kullanıcı Adı: ");
                    string username = Console.ReadLine() ?? "";

                    Console.Write("\nŞifre: ");
                    string password = Console.ReadLine() ?? "";

                    AccountHandler.Login(username, password);

                    break;

                case "2": // Kayıt Ol
                    ConsoleHandler.StockListVisible(false);

                    Console.Write("Kullanıcı Adı: ");

                    string newUsername = Console.ReadLine() ?? "";

                    if (AccountHandler.ValidateUsername(newUsername))
                    {
                        Console.Write("\nŞifre: ");
                        string newPassword = Console.ReadLine() ?? "";

                        AccountHandler.AddAccount(EnumAccountType.Musteri, newUsername, newPassword);

                        AccountHandler.Login(newUsername, newPassword);
                    }

                    break;

                case "3": // Çıkış
                    ConsoleHandler.StockListVisible(false);

                    Console.Write("Çıkılıyor...\n\n\n");

                    StockExchangeHandler.Terminate();

                    return;

                default:
                    ConsoleHandler.PrimaryMessage("\nGeçersiz seçenek.\n", ConsoleColor.DarkRed, true);

                    break;
            }
        }
    }
}
