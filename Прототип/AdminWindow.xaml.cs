using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Linq;

using System.Windows.Controls;
using System.Windows.Media;
namespace Прототип
{
    public partial class AdminWindow : Window
    {
        private MainClientWindow.Tovar tovar;
        private string connectionString = "Data Source=DESKTOP-7IEK9VH;Initial Catalog=dm2;Integrated Security=True;";
        private string imagesFolder = "Images";
        private string currentImagePath;
        
        public class CategoryItem
        {
            public int CategoryID { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        public class ManufacturerItem
        {
            public int ManufacturerID { get; set; }
            public string Name { get; set; }
            public override string ToString() => Name;
        }

        public AdminWindow(MainClientWindow.Tovar selectedTovar)
        {
            InitializeComponent();
            tovar = selectedTovar;
            isEditMode = true;

            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            LoadCategories();
            LoadManufacturers();
            LoadTovarToForm();

            IDLabel.Visibility = Visibility.Visible;


        }
        public AdminWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            tovar = null;
            

            if (!Directory.Exists(imagesFolder))
                Directory.CreateDirectory(imagesFolder);

            LoadCategories();
            LoadManufacturers();

            IDLabel.Visibility = Visibility.Collapsed;
        }
        private void LoadTovarToForm()
        {
            // Заполнение полей из объекта tovar
            IDLabel.Content = tovar.ProductID.ToString();
            NameTextBox.Text = tovar.Name;
            CategoryComboBox.SelectedItem = CategoryComboBox.Items
                .OfType<CategoryItem>()
                .FirstOrDefault(c => c.Name == tovar.Category);
            ManufacturerComboBox.SelectedItem = ManufacturerComboBox.Items
                .OfType<ManufacturerItem>()
                .FirstOrDefault(m => m.Name == tovar.Manufacturer);

            StockTextBox.Text = tovar.Stock.ToString();
            UnitTextBox.Text = tovar.Unit;
            PriceTextBox.Text = tovar.Price.ToString("F2");
            DiscountTextBox.Text = tovar.Discount.ToString("F2");
            DescriptionTextBox.Text = tovar.Description;

            if (!string.IsNullOrEmpty(tovar.ImagePath))
            {
                currentImagePath = tovar.ImagePath;
                ImagePathTextBox.Text = currentImagePath;
                LoadImage(currentImagePath);
            }
        }

        private void LoadImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || imagePath == "NULL")
            {
                SetPlaceholderImage();
                return;
            }

            // Если изображение в формате Base64
            if (imagePath.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    int commaIndex = imagePath.IndexOf(',');
                    if (commaIndex < 0)
                    {
                        SetPlaceholderImage();
                        return;
                    }

                    string base64 = imagePath.Substring(commaIndex + 1);
                    byte[] imageBytes = Convert.FromBase64String(base64);

                    BitmapImage bitmap = new BitmapImage();
                    using (MemoryStream ms = new MemoryStream(imageBytes))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }

                    PreviewImage.Source = bitmap;
                }
                catch
                {
                    SetPlaceholderImage();
                }
            }
            else
            {
                // Путь обычного изображения с диска
                try
                {
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath);
                    if (File.Exists(fullPath))
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        PreviewImage.Source = bitmap;
                    }
                    else
                    {
                        SetPlaceholderImage();
                    }
                }
                catch
                {
                    SetPlaceholderImage();
                }
            }
        }


        private void SetPlaceholderImage()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Resources/picture.png", UriKind.Absolute);
                BitmapImage placeholder = new BitmapImage(uri);
                PreviewImage.Source = placeholder;
            }
            catch
            {
                PreviewImage.Source = null;
            }
        }





        private void BrowseImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png";
            if (dlg.ShowDialog() == true)
            {
                BitmapImage bmp = new BitmapImage(new Uri(dlg.FileName));
                if (bmp.PixelWidth > 300 || bmp.PixelHeight > 200)
                {
                    MessageBox.Show("Изображение должно быть не больше 300x200 пикселей.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                
                string namePart = NameTextBox.Text.Trim().Replace(" ", "_");
                string categoryPart = (CategoryComboBox.SelectedItem as CategoryItem)?.Name?.Trim().Replace(" ", "_") ?? "БезКатегории";
                string manufacturerPart = (ManufacturerComboBox.SelectedItem as ManufacturerItem)?.Name?.Trim().Replace(" ", "_") ?? "БезПроизводителя";
                string datePart = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                
                string extension = System.IO.Path.GetExtension(dlg.FileName);
                string newFileName = $"{namePart}-{categoryPart}-{manufacturerPart}-{datePart}{extension}";
                string destPath = System.IO.Path.Combine(imagesFolder, newFileName);

                try
                {
                    File.Copy(dlg.FileName, destPath);

                    
                    if (!string.IsNullOrEmpty(currentImagePath))
                    {
                        string oldFullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, currentImagePath);
                        if (File.Exists(oldFullPath) && !oldFullPath.Equals(destPath, StringComparison.OrdinalIgnoreCase))
                        {
                            try { File.Delete(oldFullPath); } catch {  }
                        }
                    }

                    // Обновляем поля
                    currentImagePath = System.IO.Path.Combine(imagesFolder, newFileName);
                    ImagePathTextBox.Text = currentImagePath;
                    LoadImage(currentImagePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при копировании изображения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private bool ValidateInputs(out decimal price, out decimal discount, out int stock)
        {
            price = 0;
            discount = 0;
            stock = 0;

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите наименование товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out price) || price < 0)
            {
                MessageBox.Show("Стоимость должна быть неотрицательным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(DiscountTextBox.Text, out discount) || discount < 0)
            {
                MessageBox.Show("Размер скидки должен быть неотрицательным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(StockTextBox.Text, out stock) || stock < 0)
            {
                MessageBox.Show("Остаток на складе должен быть неотрицательным целым числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
        private bool isEditMode = false;
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out decimal price, out decimal discount, out int stock))
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    if (isEditMode)
                    {
                        // Обновляем существующий товар
                        string sql = @"
                    UPDATE Products SET 
                        Name = @Name,
                        CategoryID = @CategoryID,
                        ManufacturerID = @ManufacturerID,
                        Stock = @Stock,
                        Unit = @Unit,
                        Price = @Price,
                        Discount = @Discount,
                        ImagePath = @ImagePath,
                        Description = @Description
                    WHERE ProductID = @ProductID";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", NameTextBox.Text);
                            cmd.Parameters.AddWithValue("@CategoryID", (CategoryComboBox.SelectedItem as CategoryItem)?.CategoryID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ManufacturerID", (ManufacturerComboBox.SelectedItem as ManufacturerItem)?.ManufacturerID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Stock", stock);
                            cmd.Parameters.AddWithValue("@Unit", UnitTextBox.Text);
                            cmd.Parameters.AddWithValue("@Price", price);
                            cmd.Parameters.AddWithValue("@Discount", discount);
                            cmd.Parameters.AddWithValue("@ImagePath", currentImagePath ?? "");
                            cmd.Parameters.AddWithValue("@Description", DescriptionTextBox.Text);
                            cmd.Parameters.AddWithValue("@ProductID", int.Parse(IDLabel.Content.ToString()));

                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Добавляем новый товар
                        string sql = @"
                    INSERT INTO Products
                    (Name, CategoryID, ManufacturerID, Stock, Unit, Price, Discount, ImagePath, Description)
                    VALUES
                    (@Name, @CategoryID, @ManufacturerID, @Stock, @Unit, @Price, @Discount, @ImagePath, @Description)";

                        using (SqlCommand cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Name", NameTextBox.Text);
                            cmd.Parameters.AddWithValue("@CategoryID", (CategoryComboBox.SelectedItem as CategoryItem)?.CategoryID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@ManufacturerID", (ManufacturerComboBox.SelectedItem as ManufacturerItem)?.ManufacturerID ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Stock", stock);
                            cmd.Parameters.AddWithValue("@Unit", UnitTextBox.Text);
                            cmd.Parameters.AddWithValue("@Price", price);
                            cmd.Parameters.AddWithValue("@Discount", discount);
                            cmd.Parameters.AddWithValue("@ImagePath", currentImagePath ?? "");
                            cmd.Parameters.AddWithValue("@Description", DescriptionTextBox.Text);

                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Данные успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void LoadCategories()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT CategoryID, Name FROM Categories", conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    CategoryComboBox.Items.Clear();

                    while (reader.Read())
                    {
                        CategoryComboBox.Items.Add(new CategoryItem
                        {
                            CategoryID = (int)reader["CategoryID"],
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки категорий: " + ex.Message);
            }
        }

        private void DescriptionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int currentLength = DescriptionTextBox.Text.Length;
            CharCounterTextBlock.Text = $"{currentLength}/255";

            // Цвет счётчика
            CharCounterTextBlock.Foreground = currentLength >= 255
                ? Brushes.Red
                : Brushes.Gray;

            // Показывать или скрывать плейсхолдер
            PlaceholderTextBlock.Visibility = currentLength == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }


        private void LoadManufacturers()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT ManufacturerID, Name FROM Manufacturers", conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    ManufacturerComboBox.Items.Clear();

                    while (reader.Read())
                    {
                        ManufacturerComboBox.Items.Add(new ManufacturerItem
                        {
                            ManufacturerID = (int)reader["ManufacturerID"],
                            Name = reader["Name"].ToString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки производителей: " + ex.Message);
            }
        }




    }
}
