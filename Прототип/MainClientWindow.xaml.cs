using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Collections.ObjectModel;
namespace Прототип
{
    /// <summary>
    /// Логика взаимодействия для MainClientWindow.xaml
    /// </summary>
    public partial class MainClientWindow : Window
    {
        string connectionString = "Data Source=DESKTOP-7IEK9VH;Initial Catalog=dm2;Integrated Security=True;";
        private List<Tovar> allTovars = new List<Tovar>();
        private int userId;
        
        private string userRole = "";
        private ObservableCollection<Tovar> products = new ObservableCollection<Tovar>();
        public MainClientWindow(int userId)
        {
            InitializeComponent();
            
            this.userId = userId;
            LoadUserInfo();
            LoadTovarsFromDatabase();
            ConfigureAccess();

        }
        private void MainClientWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadTovarsFromDatabase();

            LoadCategories();
            ConfigureAccess();

            if (userRole == "Админ") // ← замени на переменную, где у тебя хранится роль
            {
                AddProductButton.Visibility = Visibility.Visible;
            }
        }



        private void ConfigureAccess()
        {
            if (userRole == "Гость")
            {
               
                searchBox.IsEnabled = false;      
                sortBox.IsEnabled = false;     
                manufacturerFilterBox.IsEnabled = false;   
                categoryFilterBox.IsEnabled = false;      

                
            }
            else
            {
                
                searchBox.IsEnabled = true;
                sortBox.IsEnabled = true;
                manufacturerFilterBox.IsEnabled = true;
                categoryFilterBox.IsEnabled = true;
            }
        }

        

        

        private void LoadUserInfo()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT LastName, FirstName, Patronymic, Role FROM Users WHERE UserID = @id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", userId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string lastName = reader["LastName"]?.ToString() ?? "";
                        string firstName = reader["FirstName"]?.ToString() ?? "";
                        string patronymic = reader["Patronymic"]?.ToString() ?? "";

                        userRole = reader["Role"]?.ToString() ?? "";

                        switch (userRole)
                        {
                            case "Клиент":
                                clientRoleBlock.Text = "Вы вошли как клиент";
                                clientNameBlock.Text = $"{lastName} {firstName} {patronymic}";
                                break;

                            case "Гость":
                                clientRoleBlock.Text = "Для полного доступа войдите в систему";
                                clientNameBlock.Text = "Вы вошли как гость";
                                break;

                            case "Админ":
                                clientRoleBlock.Text = "Вы вошли как администратор";
                                clientNameBlock.Text = $"{lastName} {firstName} {patronymic}";
                                break;

                            default:
                                clientRoleBlock.Text = "Роль не определена";
                                clientNameBlock.Text = "Пользователь";
                                break;
                        }
                    }
                    else
                    {
                        clientNameBlock.Text = "Пользователь";
                        clientRoleBlock.Text = "";
                        userRole = ""; 
                    }
                }
            }
        }

        public class Tovar
        {
            public int ProductID { get; set; }
            public string ImagePath { get; set; }
            public BitmapImage Image { get; set; }  
            public string Name { get; set; }             
            public string Category { get; set; }        
            public string Description { get; set; }      
            public string Manufacturer { get; set; }    
            public decimal Price { get; set; }           
            public int Stock { get; set; }
            public string Unit { get; set; }

            public decimal Discount { get; set; }

            public decimal DiscountedPrice => Discount > 0 ? Price - (Price * Discount / 100) : Price;
            // Размер скидки (например, в %)
        }

        private void LoadTovarsFromDatabase()
        {
            allTovars.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"
            SELECT 
    p.ProductID,                   
    p.ImagePath,
    p.Name AS ProductName,
    c.Name AS CategoryName,
    p.Description,
    m.Name AS ManufacturerName,
    u.Name AS UnitName,
    p.Price,
    p.Stock,
    p.Discount
FROM Products p
LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
LEFT JOIN Manufacturers m ON p.ManufacturerID = m.ManufacturerID
LEFT JOIN Unit u ON p.UnitID = u.UnitID 
";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var tovar = new Tovar
                        {
                            ProductID = Convert.ToInt32(reader["ProductID"]), 
                            ImagePath = reader["ImagePath"]?.ToString(),
                            Name = reader["ProductName"].ToString(),
                            Category = reader["CategoryName"]?.ToString(),
                            Description = reader["Description"]?.ToString(),
                            Manufacturer = reader["ManufacturerName"]?.ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),
                            Stock = Convert.ToInt32(reader["Stock"]),

                            Unit = reader["UnitName"]?.ToString(),
                            Discount = Convert.ToDecimal(reader["Discount"])
                        };


                        // Обработка Image:
                        string imagePath = tovar.ImagePath;
                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            if (imagePath.StartsWith("data:image"))
                                tovar.Image = LoadImageFromBase64(imagePath);
                            else
                            {
                                string fullImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);

                                if (File.Exists(fullImagePath))
                                {
                                    BitmapImage bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.UriSource = new Uri(fullImagePath, UriKind.Absolute);
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    bitmap.Freeze();

                                    tovar.Image = bitmap;
                                }
                                else
                                {
                                    tovar.Image = new BitmapImage(new Uri("pack://application:,,,/Resources/picture.png"));
                                }
                            }

                        }
                        else
                        {
                            tovar.Image = new BitmapImage(new Uri("pack://application:,,,/Resources/picture.png"));
                        }


                        allTovars.Add(tovar);
                    }
                }
            }
            tovarDataGrid.ItemsSource = null;  
            tovarDataGrid.ItemsSource = allTovars;
            countTextBlock.Text = $"Найдено: {allTovars.Count} из {allTovars.Count}";
            PopulateManufacturerFilter();
        }
        public BitmapImage LoadImageFromBase64(string base64String)
        {
            if (string.IsNullOrEmpty(base64String))
                return null;

            // Обрезаем префикс "data:image/jpeg;base64," и т.п.
            var base64Data = base64String.Substring(base64String.IndexOf(',') + 1);
            byte[] binaryData = Convert.FromBase64String(base64Data);

            BitmapImage bitmap = new BitmapImage();
            using (MemoryStream ms = new MemoryStream(binaryData))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
            }
            return bitmap;
        }

        private void ApplyFilters()
        {
            if (searchBox == null || tovarDataGrid == null || countTextBlock == null || allTovars == null)
                return;

            string searchText = searchBox.Text.Trim().ToLower();
            string selectedManufacturer = manufacturerFilterBox.SelectedItem?.ToString();
            string selectedCategory = categoryFilterBox.SelectedItem?.ToString();

            var filtered = allTovars.Where(t =>
    MatchesSearch(t, searchText) &&
    (selectedManufacturer == "Все производители" || t.Manufacturer == selectedManufacturer) &&
    (selectedCategory == "Все категории" || t.Category == selectedCategory)
);

            // сортировка по стоимости
            string sortOption = (sortBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            switch (sortOption)
            {
                case "Сначала дешёвые":
                    filtered = filtered.OrderBy(t => t.Price);
                    break;
                case "Сначала дорогие":
                    filtered = filtered.OrderByDescending(t => t.Price);
                    break;
            }
            var filteredList = filtered.ToList();
            tovarDataGrid.ItemsSource = filtered.ToList();
            countTextBlock.Text = $"Найдено: {filtered.Count()} из {allTovars.Count}";
            noResultsTextBlock.Visibility = filteredList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        private void tovarDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Обработка удаления по двойному правому клику
            if (e.ChangedButton == MouseButton.Right)
            {
                if (userRole != "Админ")
                    return;

                var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
                if (row != null)
                {
                    Tovar selectedTovar = (Tovar)row.Item;

                    var result = MessageBox.Show("Вы точно хотите удалить строку?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        DeleteTovarFromDatabase(selectedTovar.ProductID);
                        LoadTovarsFromDatabase(); // обновить таблицу после удаления
                    }
                }
            }
            // Обработка редактирования по двойному левому клику
            else if (e.ChangedButton == MouseButton.Left)
            {
                if (userRole != "Админ")
                    return;

                var row = FindParent<DataGridRow>((DependencyObject)e.OriginalSource);
                if (row != null)
                {
                    Tovar selectedTovar = (Tovar)row.Item;

                    AdminWindow editWindow = new AdminWindow(selectedTovar);
                    editWindow.Owner = this;

                    if (editWindow.ShowDialog() == true)
                    {
                        LoadTovarsFromDatabase(); // обновить таблицу после редактирования
                    }
                }
            }
        }


        private void DeleteTovarFromDatabase(int productId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM Products WHERE ProductID = @ProductID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductID", productId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            MessageBox.Show(" Запись не найдена в базе данных.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            
                            var tovarToRemove = allTovars.FirstOrDefault(t => t.ProductID == productId);
                            if (tovarToRemove != null)
                                allTovars.Remove(tovarToRemove);

                            tovarDataGrid.ItemsSource = null;
                            tovarDataGrid.ItemsSource = allTovars;

                            countTextBlock.Text = $"Найдено: {allTovars.Count} из {allTovars.Count}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении из базы:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as T;
        }

        private void tovarDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            
            if (e.Row.Item is Tovar tovar)
            {
                if (tovar.Stock == 0)
                {
                    
                    e.Row.Background = Brushes.Gray;
                }
                else if (tovar.Discount > 10)
                {
                    
                    e.Row.Background = (Brush)(new BrushConverter().ConvertFrom("#7fff00"));
                }
                else
                {
                    
                    e.Row.Background = Brushes.White;
                }
            }
        }
        private bool MatchesSearch(Tovar t, string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text == "поиск...")
                return true;

            text = text.ToLower();

            bool textInName = t.Name?.ToLower().Contains(text) == true;
            bool textInDescription = t.Description?.ToLower().Contains(text) == true;
            bool textInCategory = t.Category?.ToLower().Contains(text) == true;
            bool textInManufacturer = t.Manufacturer?.ToLower().Contains(text) == true;
            bool textInUnit = t.Unit?.ToLower().Contains(text) == true;

            
            bool textInPrice = t.Price.ToString().ToLower().Contains(text);
            bool textInStock = t.Stock.ToString().ToLower().Contains(text);
            bool textInDiscount = t.Discount.ToString().ToLower().Contains(text);

            return textInName
                || textInDescription
                || textInCategory
                || textInManufacturer
                || textInUnit
                || textInPrice
                || textInStock
                || textInDiscount;
        }

        private bool MatchesCategory(Tovar t, string category)
        {
            return string.IsNullOrEmpty(category) || t.Category == category;
        }

        private bool MatchesManufacturer(Tovar t, string manufacturer)
        {
            return string.IsNullOrEmpty(manufacturer) || t.Manufacturer == manufacturer;
        }


        private void SortBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }


        private void SearchFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void PopulateManufacturerFilter()
        {
            manufacturerFilterBox.Items.Clear();
            manufacturerFilterBox.Items.Add("Все производители");

            var manufacturers = allTovars
                .Select(t => t.Manufacturer)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .OrderBy(m => m);

            foreach (var man in manufacturers)
            {
                manufacturerFilterBox.Items.Add(man);
            }

            manufacturerFilterBox.SelectedIndex = 0; 
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void manufacturerFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void LoadCategories()
        {
            categoryFilterBox.Items.Clear();
            categoryFilterBox.Items.Add("Все категории");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT DISTINCT c.Name FROM Categories c JOIN Products p ON p.CategoryID = c.CategoryID";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string category = reader["Name"].ToString();
                    categoryFilterBox.Items.Add(category);
                }
            }

            categoryFilterBox.SelectedIndex = 0;
        }




        private void searchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (searchBox.Text == "Поиск...")
            {
                searchBox.Text = "";
                searchBox.Foreground = Brushes.Black;
            }
        }

        private void searchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.Text = "Поиск...";
                searchBox.Foreground = Brushes.Gray;
            }
        }
        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            AdminWindow adminWindow = new AdminWindow(); 
            bool? result = adminWindow.ShowDialog();
            if (result == true)
            {
                LoadTovarsFromDatabase(); // метод, который перезагружает данные из базы в DataGrid
            }
        }
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
            
        }

    }

}
