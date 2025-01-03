using System.Text.Json;

// Kayıt Sistemi
public static class SaveFileSystem
{
    public static T DeserializeJSON<T>(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<T>(json);
        }

        return default;
    }

    public static void SerializeJSON(string filePath, object data)
    {
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}

// Erişim Seviyesi Enum
public enum AccountAccessLevel
{
    Müşteri,
    Yönetici
}

// Hesap Sınıfı
public class Account
{
    // Sabit
    private static string HashPassword(string password) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

    // Hesap Verisi
    public AccountAccessLevel AccessLevel { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public decimal Balance { get; set; }
    public Dictionary<string, decimal> Portfolio { get; set; } = new();

    public bool IsAdmin() => AccessLevel == AccountAccessLevel.Yönetici;

    // Hesap Metodları
    public void SetPassword(string password)
    {
        Password = HashPassword(password);
    }

    public bool VerifyPassword(string password)
    {
        return Password == HashPassword(password);
    }

    public bool EditBalance(decimal amount)
    {
        decimal newBalance = Balance + amount;

        if (newBalance < 0)
        {
            return false; // Bakiye Eksiye Düşemez
        }
        else
        {
            Balance = newBalance;

            return true; // Bakiye Güncellendi
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
}

// Borsa Sistemi
public class StockExchangeSystem
{
    // Veri Yönetimi
    private List<Account> Accounts { get; set; } = new();
    private Dictionary<string, decimal> Stocks { get; set; } = new();

    public StockExchangeSystem()
    {
        // Varsayılan Yönetici Hesabı
        var adminAccount = new Account
        {
            Username = "admin",
            Balance = 0,
            AccessLevel = AccountAccessLevel.Yönetici,
        };

        adminAccount.SetPassword("admin");

        Accounts.Add(adminAccount);

        // Verileri Yükle
        var LoadedAccounts = SaveFileSystem.DeserializeJSON<List<Account>>("accounts.json");

        if (LoadedAccounts != null && LoadedAccounts.Count > 0)
        {
            Accounts = LoadedAccounts;
            Console.Write($"\n{LoadedAccounts.Count} Hesap başarıyla yüklendi.");
        }
        else
        {
            Console.Write("\nKullanıcı kaydı bulunamadı, yenisi oluşturuluyor.");
        }

        var LoadedStocks = SaveFileSystem.DeserializeJSON<Dictionary<string, decimal>>("stocks.json");

        if (LoadedStocks != null && LoadedStocks.Count > 0)
        {
            Stocks = LoadedStocks;
            Console.Write($"\n{LoadedStocks.Count} Hisse başarıyla yüklendi.");
        }
        else
        {
            Console.Write("\nHisse kaydı bulunamadı, yenisi oluşturuluyor.");
        }

        Thread.Sleep(3000);
        Console.Clear();
    }

    public void SaveData()
    {
        SaveFileSystem.SerializeJSON("accounts.json", Accounts);
        SaveFileSystem.SerializeJSON("stocks.json", Stocks);
    }

    // Hesap Yönetimi
    public Account GetAccountFromUsername(string username)
    {
        return Accounts.FirstOrDefault(account => account.Username.Equals(username.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
    }

    public void AddAccount(string username, string password, AccountAccessLevel accessLevel)
    {
        var newAccount = new Account
        {
            AccessLevel = accessLevel,
            Username = username,
            Balance = 0
        };

        newAccount.SetPassword(password);
        Accounts.Add(newAccount);
        SaveData();
    }

    public bool DeleteAccountPrompt(Account account)
    {
        if (account.AccessLevel == AccountAccessLevel.Yönetici)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\nYönetici hesapları silinemez.");
            Console.ForegroundColor = ConsoleColor.White;

            return false;
        }

        Console.Write("\nOnaylamak için \"evet\" yazın: ");
        string response = Console.ReadLine() ?? "";

        if (response.ToLowerInvariant() == "evet")
        {
            Accounts.Remove(account);
            Console.Write("\nHesap başarıyla silindi.");
            SaveData();

            return true;
        }

        return false;
    }

    public void Login(string username, string password)
    {
        var account = GetAccountFromUsername(username);

        if (account != null && account.VerifyPassword(password))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"\nMerhaba, {account.Username}!\n");
            Console.ForegroundColor = ConsoleColor.White;

            if (account.IsAdmin())
            {
                MenuSystem.AdminMenu(this);
            }
            else
            {
                MenuSystem.CustomerMenu(this, account);
            }
        }
        else
        {
            Console.Write("\nGeçersiz kullanıcı adı veya şifre.");
        }
    }

    public void ListUsers()
    {
        Console.Write("\nKullanıcı Listesi:\n");

        foreach (var account in Accounts)
        {
            Console.Write($"\n- Kullanıcı Adı: {account.Username} | Bakiye: {account.Balance:C} | Tür: {account.AccessLevel}");
        }
    }

    // Hisse Yönetimi
    public void EditStock(string symbol, decimal price)
    {
        Stocks[symbol.ToUpperInvariant()] = price;
        SaveData();
    }

    public bool RemoveStock(string symbol)
    {
        if (Stocks.Remove(symbol.ToUpperInvariant()))
        {
            SaveData();
            return true;
        }

        return false;
    }

    public void ListStocks()
    {
        if (Stocks.Count == 0)
        {
            Console.Write("\nMevcut hisse yok.");
            return;
        }

        foreach (var stock in Stocks)
        {
            Console.Write($"\nHisse: {stock.Key} | Fiyat: {stock.Value:C}");
        }
    }

    // Müşteri Metodları
    public void BuyStock(Account account, string symbol, decimal amount)
    {
        symbol = symbol.ToUpperInvariant();

        if (Stocks.TryGetValue(symbol, out decimal price))
        {
            decimal totalCost = price * amount;

            if (account.EditBalance(-totalCost))
            {
                account.EditPortfolio(symbol, amount);
                SaveData();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\n{amount} tane {symbol} hissesi satın alındı.");
                Console.ForegroundColor = ConsoleColor.White;
            } else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nYetersiz bakiye.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\nHisse bulunamadı.");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public void SellStock(Account account, string symbol, decimal amount)
    {
        symbol = symbol.ToUpperInvariant();

        if (Stocks.TryGetValue(symbol, out decimal price))
        {
            if (account.EditPortfolio(symbol, -amount))
            {
                decimal totalRevenue = price * amount;

                account.EditBalance(totalRevenue);
                SaveData();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"\n{amount} tane {symbol} hissesi satıldı.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nYetersiz hisse miktarı.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\nHisse bulunamadı.");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public void ViewPortfolio(Account account)
    {
        Console.Write("\nYour portfolio:\n");

        if (account.Portfolio.Count == 0)
        {
            Console.Write("\n- You have no stocks.");
        }
        else
        {
            decimal totalValue = account.Balance;

            foreach (var stock in account.Portfolio)
            {
                decimal valuation = (Stocks[stock.Key] * stock.Value);
                totalValue += valuation;

                Console.Write($"\n- {stock.Key}: {stock.Value} ({valuation:C})");
            }

            Console.Write($"\n\nBakiye: {account.Balance:C}");

            Console.Write($"\n\nToplam: {totalValue:C}\n");
        }
    }
}

// Yardımcı Sistemler
public static class SafeFormatSystem
{
    public static string NewUsername(StockExchangeSystem system, string message)
    {
        while (true)
        {
            Console.Write(message);

            string input = Console.ReadLine().ToLowerInvariant();

            if (system.GetAccountFromUsername(input) != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Kullanıcı adı zaten mevcut.");
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Kullanıcı adında boşluk olamaz.", input);
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            if (input.Length < 3 || input.Length > 20)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Kullanıcı adı uzunluğu 3 ile 20 arasında olmalıdır.");
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            bool alphanumeric = true;

            foreach (char c in input)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    alphanumeric = false;
                    break;
                }
            }

            if (!alphanumeric)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Kullanıcı adı sadece alfanümerik harfler veya sayılardan oluşmalıdır.");
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            return input;
        }
    }

    public static decimal NewDecimal(string message)
    {
        Console.Write(message);

        string input = Console.ReadLine();

        if (decimal.TryParse(input, out decimal result))
        {
            if (result >= 0)
            {
                return result;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("\nDecimal değeri negatif olamaz.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("\nGeçersiz decimal formatı.");
            Console.ForegroundColor = ConsoleColor.White;
        }

        return -1;
    }
}

public static class MenuSystem
{
    public static void AdminMenu(StockExchangeSystem system)
    {
        while (true)
        {
            Console.Write("\n[ Yönetici Seçenekleri ]:\n\n1. Hisse Düzenle\n2. Hisse Kaldır\n3. Mevcut Hisseleri Listele\n4. Kullanıcı Kaldır\n5. Mevcut Kullanıcıları Listele\n6. Çıkış Yap\n\nSeçeneğiniz: ");

            string choice = Console.ReadLine();

            Console.Clear();

            switch (choice)
            {
                case "1":
                    Console.Write("\nHisse Sembolü: ");
                    string stockSymbol = Console.ReadLine();

                    decimal newPrice = SafeFormatSystem.NewDecimal("Hisse Fiyatı: ");

                    if (newPrice != -1)
                    {
                        system.EditStock(stockSymbol, newPrice);
                        Console.WriteLine("Hisse başarıyla düzenlendi.");
                    }
                    break;

                case "2":
                    Console.Write("\nSilinecek Hisse Sembolü: ");
                    string stockSymbolToRemove = Console.ReadLine();

                    if (system.RemoveStock(stockSymbolToRemove))
                    {
                        Console.Write("\nHisse başarıyla silindi.");
                    } else
                    {
                        Console.Write("\nHisse bulunamadı.");
                    }
                    break;

                case "3":
                    system.ListStocks();
                    break;

                case "4":
                    {
                        Console.Write("\nSilinecek Kullanıcının Adı: ");
                        Account account = system.GetAccountFromUsername(Console.ReadLine());

                        if (account != null)
                        {
                            system.DeleteAccountPrompt(account);
                        }
                        else
                        {
                            Console.WriteLine("Kullanıcı bulunamadı.");
                        }
                        break;
                    }

                case "5":
                    system.ListUsers();
                    break;

                case "6":
                    return;

                default:
                    Console.WriteLine("Geçersiz seçenek.");
                    break;
            }
        }
    }

    public static void CustomerMenu(StockExchangeSystem system, Account account)
    {
        while (true)
        {
            Console.Write("\n[ Müşteri Seçenekleri ]:\n\n1. Hisse Satın Al\n2. Hisse Sat\n3. Mevcut Hisseleri Listele\n4. Bakiye Ekle\n5. Portföy Görüntüle\n6. Çıkış Yap\n7. Hesabı Sil\n\nSeçeneğiniz: ");

            string choice = Console.ReadLine();

            Console.Clear();

            switch (choice)
            {
                case "1": // Hisse Satın Al
                    {
                        Console.Write("Hisse Sembolü: ");
                        string symbol = Console.ReadLine();

                        decimal amount = SafeFormatSystem.NewDecimal("Miktar: ");

                        if (amount != -1)
                        {
                            system.BuyStock(account, symbol, amount);
                        }

                        break;
                    }

                case "2": // Hisse Sat
                    {
                        Console.Write("Hisse Sembolü: ");
                        string symbol = Console.ReadLine();

                        decimal amount = SafeFormatSystem.NewDecimal("Miktar: ");

                        system.SellStock(account, symbol, amount);
                        break;
                    }

                case "3": // Mevcut Hisseleri Listele
                    system.ListStocks();
                    break;

                case "4": // Bakiye Ekle
                    {
                        decimal amount = SafeFormatSystem.NewDecimal("Yüklemek İstediğiniz Tutar: ");
                        account.EditBalance(amount);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"\n{amount:C} yüklendi.");
                        Console.ForegroundColor = ConsoleColor.White;

                        break;
                    }

                case "5":
                    system.ViewPortfolio(account);
                    break;

                case "6":
                    return;

                case "7":
                    if (system.DeleteAccountPrompt(account))
                    {
                        return; // Çıkış Yap
                    }
                    else
                    {
                        break;
                    }

                default:
                    Console.Write("\nGeçersiz seçenek.");
                    break;
            }
        }
    }
}

// Ana Program
public class Program
{
    static void Main(string[] args)
    {
        StockExchangeSystem system = new StockExchangeSystem();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Hisse Senedi Borsa Sistemi\n\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("'ne Hoşgeldiniz");

            Console.Write("[ Ana Menü ]\n\n1. Giriş Yap\n2. Kayıt Ol\n3. Çıkış Yap\n\nSeçeneğiniz: ");

            string choice = Console.ReadLine();
            Console.Clear();

            switch (choice)
            {
                case "1": // Giriş Yap
                    Console.Write("\nKullanıcı Adı: ");
                    string username = Console.ReadLine();

                    Console.Write("\nŞifre: ");
                    string password = Console.ReadLine();

                    system.Login(username, password);
                    break;
                    
                case "2": // Kayıt Ol
                    string newUsername = SafeFormatSystem.NewUsername(system, "\nKullanıcı Adı: ");

                    Console.Write("\nŞifre: ");
                    string newPassword = Console.ReadLine();

                    system.AddAccount(newUsername, newPassword, AccountAccessLevel.Müşteri);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\nHesap başarıyla oluşturuldu!");
                    Console.ForegroundColor = ConsoleColor.White;

                    system.Login(newUsername, newPassword);
                    break;

                case "3": // Çıkış
                    Console.Write("\nÇıkılıyor...\n\n\n");
                    return;

                default:
                    Console.Write("\nGeçersiz seçenek.");
                    break;
            }
        }
    }
}
