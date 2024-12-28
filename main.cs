public enum UserRole
{
    Customer,
    Admin
}

public class User
{
    public string Username { get; private set; }
    public decimal Balance { get; private set; }
    public UserRole Role { get; private set; }

    private string _password;
    private List<StockHolding> _portfolio;

    public User(string username, string password, decimal initialBalance = 1000, UserRole role = UserRole.Customer)
    {
        Username = username;
        Balance = initialBalance;
        Role = role;

        _password = password;
        _portfolio = new List<StockHolding>();
    }

    public bool ValidatePassword(string password)
    {
        return password == _password;
    }

    public bool IsAdmin() => Role == UserRole.Admin;

    public bool BuyStock(Stock stock, int quantity)
    {
        decimal cost = stock.Price * quantity;
        if (Balance >= cost)
        {
            Balance -= cost;
            AddToPortfolio(stock, quantity);
            return true;
        }
        return false;
    }

    public bool SellStock(Stock stock, int quantity)
    {
        var holding = _portfolio.FirstOrDefault(item => item.SymbolIndex == stock.Symbol);
        if (holding != null && holding.Quantity >= quantity)
        {
            holding.Quantity -= quantity;
            decimal revenue = stock.Price * quantity;
            Balance += revenue;
            return true;
        }
        return false;
    }

    private void AddToPortfolio(Stock stock, int quantity)
    {
        var existingHolding = _portfolio.FirstOrDefault(item => item.SymbolIndex == stock.Symbol);
        if (existingHolding != null)
        {
            existingHolding.Quantity += quantity;
        }
        else
        {
            _portfolio.Add(new StockHolding(stock.Symbol, quantity));
        }
    }

    public List<StockHolding> GetPortfolio() => _portfolio;
    public override string ToString() => $"{Username} (Role: {Role}, Balance: {Balance})";
}

public class StockHolding
{
    public string SymbolIndex { get; }
    public int Quantity { get; set; }

    public StockHolding(string symbol, int quantity)
    {
        SymbolIndex = symbol;
        Quantity = quantity;
    }

    public decimal GetTotalValue(Stock stock) => stock.Price * Quantity;
}

public class Stock
{
    public string Symbol { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }

    public Stock(string symbol, string name, decimal price)
    {
        Symbol = symbol;
        Name = name;
        Price = price;
    }
}

public class StockExchange
{
    public List<User> Users { get; private set; }
    public List<Stock> Stocks { get; private set; }

    public StockExchange()
    {
        Users = new List<User>();
        Stocks = new List<Stock>();
    }

    // Create a new user and add them to the system
    public void CreateUser(string username, string password, UserRole role = UserRole.Customer)
    {
        User newUser = new User(username, password, role: role);
        Users.Add(newUser);
    }

    // Login: find user by username and validate the password
    public User Login(string username, string password)
    {
        User user = Users.FirstOrDefault(u => u.Username == username);
        if (user != null && user.ValidatePassword(password))
        {
            return user;
        }
        return null;
    }

    // Add stock to exchange
    public void AddStock(Stock stock)
    {
        Stocks.Add(stock);
    }

    // Remove stock from exchange
    public void RemoveStock(string symbol)
    {
        var stock = Stocks.FirstOrDefault(s => s.Symbol == symbol);
        if (stock != null)
        {
            Stocks.Remove(stock);
        }
    }

    // Edit stock price
    public void EditStockPrice(string symbol, decimal newPrice)
    {
        var stock = Stocks.FirstOrDefault(s => s.Symbol == symbol);
        if (stock != null)
        {
            stock.Price = newPrice;
        }
    }
}

class Program
{
    // Admin Menu
    static void AdminMenu(StockExchange stockExchange)
    {
        Console.WriteLine("Admin Menu:");
        Console.WriteLine("1. Add Stock");
        Console.WriteLine("2. Remove Stock");
        Console.WriteLine("3. Edit Stock Price");
        Console.WriteLine("4. View All Users");
        Console.WriteLine("5. Logout");

        string adminChoice = Console.ReadLine();

        switch (adminChoice)
        {
            case "1":
                AddStock(stockExchange);
                break;
            case "2":
                RemoveStock(stockExchange);
                break;
            case "3":
                EditStockPrice(stockExchange);
                break;
            case "4":
                ViewAllUsers(stockExchange);
                break;
            case "5":
                Console.WriteLine("Logging out...");
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }

    static void AddStock(StockExchange stockExchange)
    {
        Console.WriteLine("Enter Stock Symbol:");
        string symbol = Console.ReadLine();
        Console.WriteLine("Enter Stock Name:");
        string name = Console.ReadLine();
        Console.WriteLine("Enter Stock Price:");
        decimal price = Convert.ToDecimal(Console.ReadLine());

        stockExchange.AddStock(new Stock(symbol, name, price));
        Console.WriteLine("Stock added successfully.");
    }

    static void RemoveStock(StockExchange stockExchange)
    {
        Console.WriteLine("Enter Stock Symbol to Remove:");
        string symbol = Console.ReadLine();

        stockExchange.RemoveStock(symbol);
        Console.WriteLine("Stock removed successfully.");
    }

    static void EditStockPrice(StockExchange stockExchange)
    {
        Console.WriteLine("Enter Stock Symbol to Edit:");
        string symbol = Console.ReadLine();
        Console.WriteLine("Enter New Stock Price:");
        decimal newPrice = Convert.ToDecimal(Console.ReadLine());

        stockExchange.EditStockPrice(symbol, newPrice);
        Console.WriteLine("Stock price updated successfully.");
    }

    static void ViewAllUsers(StockExchange stockExchange)
    {
        Console.Clear();
        Console.WriteLine("List of All Users:");
        foreach (var user in stockExchange.Users)
        {
            Console.WriteLine(user);  // This will use the ToString() method to display the user info
        }
    }

    // User Menu
    static void UserMenu(User user, StockExchange stockExchange)
    {
        Console.WriteLine("User Menu:");
        Console.WriteLine("1. Buy Stock");
        Console.WriteLine("2. Sell Stock");
        Console.WriteLine("3. View Portfolio");
        Console.WriteLine("4. View Balance");
        Console.WriteLine("5. Logout");

        string userChoice = Console.ReadLine();

        switch (userChoice)
        {
            case "1":
                BuyStock(user, stockExchange);
                break;
            case "2":
                SellStock(user, stockExchange);
                break;
            case "3":
                ViewPortfolio(user);
                break;
            case "4":
                ViewBalance(user);
                break;
            case "5":
                Console.WriteLine("Logging out...");
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }
    }

    static void BuyStock(User user, StockExchange stockExchange)
    {
        Console.Clear();
        Console.WriteLine("Enter Stock Symbol to Buy:");
        string symbol = Console.ReadLine();
        Console.WriteLine("Enter Number of Shares:");
        int quantity = Convert.ToInt32(Console.ReadLine());

        var stock = stockExchange.Stocks.FirstOrDefault(s => s.Symbol == symbol);
        if (stock != null && user.BuyStock(stock, quantity))
        {
            Console.WriteLine($"Successfully bought {quantity} shares of {stock.Symbol}.");
        }
        else
        {
            Console.WriteLine("Not enough balance or stock not found.");
        }
    }

    static void SellStock(User user, StockExchange stockExchange)
    {
        Console.Clear();
        Console.WriteLine("Enter Stock Symbol to Sell:");
        string symbol = Console.ReadLine();
        Console.WriteLine("Enter Number of Shares:");
        int quantity = Convert.ToInt32(Console.ReadLine());

        var stock = stockExchange.Stocks.FirstOrDefault(s => s.Symbol == symbol);
        if (stock != null && user.SellStock(stock, quantity))
        {
            Console.WriteLine($"Successfully sold {quantity} shares of {stock.Symbol}.");
        }
        else
        {
            Console.WriteLine("Not enough shares or stock not found.");
        }
    }

    static void ViewPortfolio(User user)
    {
        Console.Clear();
        Console.WriteLine("Your Portfolio:");
        foreach (var holding in user.GetPortfolio())
        {
            Console.WriteLine($"Stock: {holding.SymbolIndex}, Quantity: {holding.Quantity}");
        }
    }

    static void ViewBalance(User user)
    {
        Console.WriteLine($"Your current balance: ${user.Balance}");
    }

    static void Main(string[] args)
    {
        StockExchange stockExchange = new StockExchange();

        // Örnek Stoklar Ekle
        stockExchange.AddStock(new Stock("AAPL", "Apple Inc.", 150.00m));
        stockExchange.AddStock(new Stock("TSLA", "Tesla Inc.", 300.00m));

        // Yönetici Hesabı Oluştur
        stockExchange.CreateUser("admin", "admin", UserRole.Admin);

        while (true)
        {
            Console.WriteLine("Welcome to the Stock Exchange!");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Create New User");
            Console.WriteLine("3. Exit");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                Console.WriteLine("Enter Username:");
                string username = Console.ReadLine();
                Console.WriteLine("Enter Password:");
                string password = Console.ReadLine();

                User loggedInUser = stockExchange.Login(username, password);

                if (loggedInUser != null)
                {
                    Console.WriteLine($"Welcome {loggedInUser.Username}!");

                    if (loggedInUser.IsAdmin())
                    {
                        AdminMenu(stockExchange);  // Show admin menu
                    }
                    else
                    {
                        UserMenu(loggedInUser, stockExchange);  // Show user menu
                    }
                }
                else
                {
                    Console.WriteLine("Invalid username or password.");
                }
            }
            else if (choice == "2")
            {
                // Yeni Hesap Oluştur

                Console.Write("Enter New Username: ");
                string newUsername = Console.ReadLine();
                Console.Write("Enter New Password: ");
                string newPassword = Console.ReadLine();

                Console.Write("Role: ");
                string roleChoice = Console.ReadLine();
                UserRole role = roleChoice == "2" ? UserRole.Admin : UserRole.Customer;

                stockExchange.CreateUser(newUsername, newPassword, role);

                Console.WriteLine("New user created successfully!");
            }
            else if (choice == "3")
            {
                break;
            }
        }
    }
}
