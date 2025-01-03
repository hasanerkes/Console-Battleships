using System.Text.Json;

// Data Handler
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

// Account Access Level Enum
public enum AccountAccessLevel
{
    Customer,
    Admin
}

// Account Class
public class Account
{
    // Static
    private static string HashPassword(string password) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

    // Data
    public AccountAccessLevel AccessLevel { get; set; }
    public string Username { get; set; }
    public decimal Balance { get; set; }
    public string Password { get; set; }
    public Dictionary<string, decimal> Portfolio { get; set; } = new();

    public bool IsAdmin() => AccessLevel == AccountAccessLevel.Admin;

    // Password
    public void SetPassword(string password)
    {
        Password = HashPassword(password);
    }

    public bool VerifyPassword(string password)
    {
        return Password == HashPassword(password);
    }

    // Portfolio
    public void AddToPortfolio(string stockSymbol, decimal amount)
    {
        if (Portfolio.ContainsKey(stockSymbol))
            Portfolio[stockSymbol] += amount;
        else
            Portfolio[stockSymbol] = amount;
    }

    public bool RemoveFromPortfolio(string stockSymbol, decimal amount)
    {
        if (Portfolio.TryGetValue(stockSymbol, out decimal currentAmount) && currentAmount >= amount)
        {
            Portfolio[stockSymbol] -= amount;

            if (Portfolio[stockSymbol] == 0)
                Portfolio.Remove(stockSymbol);
            return true;
        }

        return false;
    }
}

// Stock Exchange System
public class StockExchangeSystem
{
    // Data Management
    private List<Account> Accounts { get; set; } = new();
    private Dictionary<string, decimal> Stocks { get; set; } = new();

    public StockExchangeSystem()
    {
        // Default Admin Account
        var adminAccount = new Account
        {
            Username = "admin",
            Balance = 0,
            AccessLevel = AccountAccessLevel.Admin,
        };

        adminAccount.SetPassword("admin");

        Accounts.Add(adminAccount);

        // Load Data
        var LoadedAccounts = SaveFileSystem.DeserializeJSON<List<Account>>("accounts.json");

        if (LoadedAccounts != null && LoadedAccounts.Count > 0)
        {
            Accounts = LoadedAccounts;
            Console.WriteLine($"{LoadedAccounts.Count} Accounts loaded successfully.");
        }
        else
        {
            Console.WriteLine("Account data not found, creating new.");
        }

        var LoadedStocks = SaveFileSystem.DeserializeJSON<Dictionary<string, decimal>>("stocks.json");

        if (LoadedStocks != null && LoadedStocks.Count > 0)
        {
            Stocks = LoadedStocks;
            Console.WriteLine($"{LoadedStocks.Count} Stocks loaded successfully.");
        }
        else
        {
            Console.WriteLine("Stock data not found, creating new.");
        }

        Thread.Sleep(3000);
        Console.Clear();
    }

    public void SaveData()
    {
        SaveFileSystem.SerializeJSON("accounts.json", Accounts);
        SaveFileSystem.SerializeJSON("stocks.json", Stocks);
    }

    // Account Management
    public Account GetAccountFromUsername(string username)
    {
        return Accounts.FirstOrDefault(account => account.Username.Equals(username.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
    }

    public void AddAccount(string username, string password, decimal balance, AccountAccessLevel accessLevel)
    {
        var newAccount = new Account
        {
            Username = username,
            Balance = balance,
            AccessLevel = accessLevel
        };

        newAccount.SetPassword(password);
        Accounts.Add(newAccount);
        SaveData();
    }

    public void EditAccount(Account account, string newPassword, decimal newBalance)
    {
        account.SetPassword(newPassword);
        account.Balance = newBalance;
        SaveData();
    }

    public bool DeleteAccountPrompt(Account account)
    {
        if (account.AccessLevel == AccountAccessLevel.Admin)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Administrator accounts cannot be deleted.");
            Console.ForegroundColor = ConsoleColor.White;

            return false;
        }

        Console.Write("Write \"Y\" to confirm: ");
        string response = Console.ReadLine() ?? "";

        if (response.ToLowerInvariant() == "y")
        {
            Accounts.Remove(account);
            Console.WriteLine("Account deleted.");
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
            Console.WriteLine($"\nWelcome, {account.Username}!\n");
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
            Console.WriteLine("Invalid username or password.\n");
        }
    }

    public void ListUsers()
    {
        Console.WriteLine("Users List:");

        foreach (var account in Accounts)
        {
            Console.WriteLine($"- Username: {account.Username}, Balance: {account.Balance:C}, Type: {account.AccessLevel}");
        }
    }

    // Stock Management
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
            Console.WriteLine("No stocks available.");
            return;
        }

        foreach (var stock in Stocks)
        {
            Console.WriteLine($"Stock: {stock.Key}, Price: {stock.Value:C}");
        }
    }

    // Customer Methods
    public void BuyStock(Account account, string symbol, decimal amount)
    {
        symbol = symbol.ToUpperInvariant();

        if (Stocks.TryGetValue(symbol, out decimal price))
        {
            decimal totalCost = price * amount;
            if (account.Balance >= totalCost)
            {
                account.Balance -= totalCost;
                account.AddToPortfolio(symbol, amount);
                SaveData();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Bought {amount} units of {symbol}.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Insufficient balance.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock not found.");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public void SellStock(Account account, string symbol, decimal amount)
    {
        symbol = symbol.ToUpperInvariant();

        if (Stocks.TryGetValue(symbol, out decimal price))
        {
            if (account.RemoveFromPortfolio(symbol, amount))
            {
                decimal totalRevenue = price * amount;
                account.Balance += totalRevenue;
                SaveData();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Sold {amount} units of {symbol}.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Not enough stock to sell.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Stock not found.");
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public void ViewPortfolio(Account account)
    {
        Console.Write("Your portfolio:\n");

        if (account.Portfolio.Count == 0)
        {
            Console.Write("- You have no stocks.\n");
        }
        else
        {
            decimal totalValue = account.Balance;

            foreach (var stock in account.Portfolio)
            {
                decimal valuation = (Stocks[stock.Key] * stock.Value);
                totalValue += valuation;

                Console.Write($"- {stock.Key}: {stock.Value} Units ( {valuation:C} )\n");
            }

            Console.Write($"\nAccount Balance: {account.Balance:C}");

            Console.Write($"\nTotal: {totalValue:C}\n");
        }
    }
}

// Auxiliary Systems
public static class SafeFormatSystem
{
    public static string ValidateNewUsername(StockExchangeSystem system, string message)
    {
        while (true)
        {
            Console.Write(message);

            string input = Console.ReadLine().ToLowerInvariant();

            if (system.GetAccountFromUsername(input) != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("This username is taken. Please try again.");
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Username cannot contain whitespace. Please try again.", input);
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            if (input.Length < 3 || input.Length > 20)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Username length must be within 3 and 20. Please try again.");
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
                Console.WriteLine("Username must be alphanumeric. Please try again.");
                Console.ForegroundColor = ConsoleColor.White;
                continue;
            }

            return input;
        }
    }

    public static decimal NewDecimal(string message)
    {
        while (true)
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
                    Console.WriteLine("Decimal value cannot be negative. Please try again.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid decimal format. Please try again.");
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}

public static class MenuSystem
{
    public static void AdminMenu(StockExchangeSystem system)
    {
        while (true)
        {
            Console.Write("\n[ Admin Menu ]:\n\n1. Edit Stocks\n2. Remove Stock\n3. List Stocks\n4. Edit User\n5. Remove User\n6. List Users\n7. Logout\n\nChoose an option: ");

            string choice = Console.ReadLine();

            Console.Clear();

            switch (choice)
            {
                case "1":
                    Console.Write("Stock Symbol To Edit: ");
                    string stockSymbol = Console.ReadLine();

                    Console.Write("New Stock Price: ");
                    if (decimal.TryParse(Console.ReadLine(), out decimal newPrice))
                    {
                        system.EditStock(stockSymbol, newPrice);
                        Console.WriteLine("Stock edited successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Invalid price format.");
                    }
                    break;

                case "2":
                    Console.Write("Stock Symbol To Remove: ");
                    string stockSymbolToRemove = Console.ReadLine();
                    system.RemoveStock(stockSymbolToRemove);
                    Console.WriteLine("Stock removed successfully!");
                    break;

                case "3":
                    system.ListStocks();
                    break;

                case "4":
                    {
                        Console.Write("\nUsername To Edit: ");
                        Account account = system.GetAccountFromUsername(Console.ReadLine());

                        if (account != null)
                        {
                            Console.Write("\nNew Password: ");
                            string newPassword = Console.ReadLine();

                            decimal newBalance = SafeFormatSystem.NewDecimal("\nNew Balance: ");

                            system.EditAccount(account, newPassword, newBalance);

                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("\nAccount created successfully!\n");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else
                        {
                            Console.WriteLine("User not found.");
                        }
                        break;
                    }

                case "5":
                    {
                        Console.Write("Username To Delete: ");
                        Account account = system.GetAccountFromUsername(Console.ReadLine());

                        if (account != null)
                        {
                            system.DeleteAccountPrompt(account);
                        }
                        else
                        {
                            Console.WriteLine("Account not found.");
                        }
                        break;
                    }

                case "6":
                    system.ListUsers();
                    break;

                case "7":
                    return;

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    public static void CustomerMenu(StockExchangeSystem system, Account account)
    {
        while (true)
        {
            Console.Write("\n[ Customer Menu ]:\n\n1. Buy Stock\n2. Sell Stock\n3. List Stocks\n4. View Portfolio\n5. Logout\n6. Delete Account\n\nChoose an option: ");

            string choice = Console.ReadLine();

            Console.Clear();

            switch (choice)
            {
                case "1":
                    {
                        Console.Write("Stock Symbol: ");
                        string symbol = Console.ReadLine();

                        decimal amount = SafeFormatSystem.NewDecimal("Amount: ");

                        system.BuyStock(account, symbol, amount);
                        break;
                    }

                case "2":
                    {
                        Console.Write("Stock Symbol: ");
                        string symbol = Console.ReadLine();

                        decimal amount = SafeFormatSystem.NewDecimal("Amount: ");

                        system.SellStock(account, symbol, amount);
                        break;
                    }

                case "3":
                    system.ListStocks();
                    break;

                case "4":
                    system.ViewPortfolio(account);
                    break;

                case "5":
                    return;

                case "6":
                    if (system.DeleteAccountPrompt(account))
                    {
                        return; // Log Out
                    }
                    else
                    {
                        break;
                    }

                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }
}

// Main Program
public class Program
{
    static void Main(string[] args)
    {
        StockExchangeSystem system = new StockExchangeSystem();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Welcome to the ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Stock Exchange System\n\n");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[ Main Menu ]\n\n1. Login\n2. Sign Up\n3. Exit\n\nChoose an option: ");

            string choice = Console.ReadLine();
            Console.Clear();

            switch (choice)
            {
                case "1": // Login
                    Console.Write("\nUsername: ");
                    string username = Console.ReadLine();

                    Console.Write("\nPassword: ");
                    string password = Console.ReadLine();

                    system.Login(username, password);
                    break;

                case "2": // Sign Up
                    string newUsername = SafeFormatSystem.ValidateNewUsername(system, "\nUsername: ");

                    Console.Write("\nPassword: ");
                    string newPassword = Console.ReadLine();

                    decimal newBalance = SafeFormatSystem.NewDecimal("\nBalance: ");

                    system.AddAccount(newUsername, newPassword, newBalance, AccountAccessLevel.Customer);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("\nAccount created successfully!\n");
                    Console.ForegroundColor = ConsoleColor.White;

                    system.Login(newUsername, newPassword);
                    break;

                case "3": // Exit
                    Console.WriteLine("Exiting...\n");
                    return;

                default:
                    Console.WriteLine("Invalid option.\n");
                    break;
            }
        }
    }
}
