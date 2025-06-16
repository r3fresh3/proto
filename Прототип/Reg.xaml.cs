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
using System.Security.Cryptography;

namespace Прототип
{
    /// <summary>
    /// Логика взаимодействия для Reg.xaml
    /// </summary>
    public partial class Reg : Window
    {
        string connectionString = "Data Source=DESKTOP-7IEK9VH;Initial Catalog=dm2;Integrated Security=True;";

        public Reg()
        {
            InitializeComponent();
        }
        private bool isPasswordVisible = false;
        

        private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                visiblePasswordBox.Text = passwordBox.Password;
                visiblePasswordBox.Visibility = Visibility.Visible;
                passwordBox.Visibility = Visibility.Collapsed;
                eyeIcon.Text = "🙈";
            }
            else
            {
                passwordBox.Password = visiblePasswordBox.Text;
                passwordBox.Visibility = Visibility.Visible;
                visiblePasswordBox.Visibility = Visibility.Collapsed;
                eyeIcon.Text = "👁";
            }

            UpdatePasswordPlaceholder();
        }

        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void visiblePasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void VisiblePasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void VisiblePasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            if (isPasswordVisible)
                passwordPlaceholder.Visibility = string.IsNullOrEmpty(visiblePasswordBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            else
                passwordPlaceholder.Visibility = string.IsNullOrEmpty(passwordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void logBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                if (tb.Foreground == Brushes.Gray)
                {
                    tb.Text = "";
                    tb.FontStyle = FontStyles.Normal;
                    tb.Foreground = Brushes.Black;
                }
            }
        }

        
        private void logBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    switch (tb.Name)
                    {
                        case "logBox":
                            tb.Text = "Введите логин";
                            break;
                        case "lastNameBox":
                            tb.Text = "Фамилия";
                            break;
                        case "firstNameBox":
                            tb.Text = "Имя";
                            break;
                        case "middleNameBox":
                            tb.Text = "Отчество";
                            break;
                    }
                    tb.FontStyle = FontStyles.Italic;
                    tb.Foreground = Brushes.Gray;
                }
            }
        }

        
        

        
        

        private void confirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            confirmPlaceholder.Visibility = string.IsNullOrEmpty(confirmPasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ToggleConfirmPasswordVisibility(object sender, RoutedEventArgs e)
        {
            if (visibleConfirmPasswordBox.Visibility == Visibility.Visible)
            {
                
                visibleConfirmPasswordBox.Visibility = Visibility.Collapsed;
                confirmPasswordBox.Visibility = Visibility.Visible;
                confirmPasswordBox.Password = visibleConfirmPasswordBox.Text;
                confirmEyeIcon.Text = "👁";
            }
            else
            {
                
                visibleConfirmPasswordBox.Visibility = Visibility.Visible;
                confirmPasswordBox.Visibility = Visibility.Collapsed;
                visibleConfirmPasswordBox.Text = confirmPasswordBox.Password;
                confirmEyeIcon.Text = "🙈";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {

            var main = new MainWindow();
            main.Show();

            this.Close();

            
            
        }



        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string login = logBox.Text.Trim();
            string password = passwordBox.Password.Trim();
            string confirmPassword = confirmPasswordBox.Password.Trim();
            string firstName = firstNameBox.Text.Trim();
            string lastName = lastNameBox.Text.Trim();
            string patronymic = middleNameBox.Text.Trim();

            
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) || string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            
            if (password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    
                    string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Login = @Login";
                    using (SqlCommand checkCmd = new SqlCommand(checkUserQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Login", login);
                        int userExists = (int)checkCmd.ExecuteScalar();

                        if (userExists > 0)
                        {
                            MessageBox.Show("Пользователь с таким логином уже существует.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    
                    string insertQuery = @"
                INSERT INTO Users (Login, PasswordHash, FirstName, LastName, Patronymic, Role)
                VALUES (@Login, @PasswordHash, @FirstName, @LastName, @Patronymic, @Role)";

                    using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@Login", login);
                        insertCmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password)); 
                        insertCmd.Parameters.AddWithValue("@FirstName", firstName);
                        insertCmd.Parameters.AddWithValue("@LastName", lastName);
                        insertCmd.Parameters.AddWithValue("@Patronymic", patronymic);
                        insertCmd.Parameters.AddWithValue("@Role", "Клиент");

                        insertCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Регистрация прошла успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    MainWindow mainWindow = new MainWindow(); // Заменить, если твоя форма называется иначе
                    mainWindow.Show();

                    
                    this.Close();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при регистрации: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

    }
}

