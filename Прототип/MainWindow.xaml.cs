using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace Прототип
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer lockoutTimer;
        private int countdownSeconds;
        string connectionString = "Data Source=DESKTOP-7IEK9VH;Initial Catalog=dm2;Integrated Security=True;";
        private string captchaText = "";
        private bool isPasswordVisible = false;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha();
            LoadSavedCredentials();
        }
        private void registrLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var RgWindow = new Reg(); 
            RgWindow.Show();
            this.Close();
        }

        private void logBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (logBox.Text == "Введите логин")
            {
                logBox.Text = "";
                logBox.Foreground = Brushes.Black;
            }
        }

        private void logBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(logBox.Text))
            {
                logBox.Text = "Введите логин";
                logBox.Foreground = Brushes.Gray;
            }
        }
        private void captchaInputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (captchaInputBox.Text == "Введите капчу")
            {
                captchaInputBox.Text = "";
                captchaInputBox.Foreground = Brushes.Black;
            }
        }

        private void captchaInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(captchaInputBox.Text))
            {
                captchaInputBox.Text = "Введите капчу";
                captchaInputBox.Foreground = Brushes.Gray;
            }
        }
        private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            passwordPlaceholder.Visibility = string.IsNullOrEmpty(passwordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void visiblePasswordBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            passwordPlaceholder.Visibility = string.IsNullOrEmpty(visiblePasswordBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            passwordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(passwordBox.Password))
                passwordPlaceholder.Visibility = Visibility.Visible;
        }

        private void VisiblePasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            passwordPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void VisiblePasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(visiblePasswordBox.Text))
                passwordPlaceholder.Visibility = Visibility.Visible;
        }
        private void TogglePasswordVisibility(object sender, RoutedEventArgs e)
        {
            if (isPasswordVisible)
            {
                
                visiblePasswordBox.Visibility = Visibility.Collapsed;
                passwordBox.Visibility = Visibility.Visible;
                passwordBox.Password = visiblePasswordBox.Text;
                eyeIcon.Text = "👁";  
                isPasswordVisible = false;
            }
            else
            {
                
                visiblePasswordBox.Visibility = Visibility.Visible;
                visiblePasswordBox.Text = passwordBox.Password;
                passwordBox.Visibility = Visibility.Collapsed;
                eyeIcon.Text = "🙈";  
                isPasswordVisible = true;
            }
        }
        private void RefreshCaptcha(object sender, RoutedEventArgs e)
        {
            GenerateCaptcha(); 
        }

        private void GenerateCaptcha()
        {
            captchaCanvas.Children.Clear();
            Random rnd = new Random();
            captchaText = "";
            string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; 

            for (int i = 0; i < 5; i++)
            {
                captchaText += chars[rnd.Next(chars.Length)];
            }

            
            for (int i = 0; i < captchaText.Length; i++)
            {
                TextBlock letter = new TextBlock
                {
                    Text = captchaText[i].ToString(),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(20 + i * 30 + rnd.Next(-2, 2), rnd.Next(0, 10), 0, 0),
                    RenderTransform = new RotateTransform(rnd.Next(-20, 20))
                };
                captchaCanvas.Children.Add(letter);
            }

            // Добавим "шум"
            for (int i = 0; i < 15; i++)
            {
                Line noise = new Line
                {
                    X1 = rnd.Next(0, (int)captchaCanvas.Width),
                    Y1 = rnd.Next(0, (int)captchaCanvas.Height),
                    X2 = rnd.Next(0, (int)captchaCanvas.Width),
                    Y2 = rnd.Next(0, (int)captchaCanvas.Height),
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                };
                captchaCanvas.Children.Add(noise);
            }
        }
        private int failedAttempts = 0;


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string login = logBox.Text.Trim();
            string password = isPasswordVisible ? visiblePasswordBox.Text.Trim() : passwordBox.Password.Trim();

            // Сброс цвета бордера к дефолтному
            Brush defaultBrush = SystemColors.ControlDarkBrush;
            logBox.BorderBrush = defaultBrush;
            passwordBox.BorderBrush = defaultBrush;
            visiblePasswordBox.BorderBrush = defaultBrush;

            bool hasError = false;

            // Проверяем логин на пустоту и плейсхолдер
            if (string.IsNullOrWhiteSpace(login) || login == "Введите логин")
            {
                logBox.BorderBrush = Brushes.Red;
                hasError = true;
            }

            // Проверяем пароль на пустоту и плейсхолдер
            if (string.IsNullOrWhiteSpace(password) || password == "Введите пароль")
            {
                if (isPasswordVisible)
                    visiblePasswordBox.BorderBrush = Brushes.Red;
                else
                    passwordBox.BorderBrush = Brushes.Red;
                hasError = true;
            }

            if (hasError)
            {
                MessageBox.Show("Пожалуйста, заполните все обязательные поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверяем капчу при 2 и более ошибках
            if (failedAttempts >= 2)
            {
                if (captchaInputBox.Text.Trim().ToUpper() != captchaText.ToUpper())
                {
                    MessageBox.Show("Неверная CAPTCHA. Повторите попытку.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    GenerateCaptcha();
                    StartLockoutCountdown();
                    return;
                }
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT * FROM Users WHERE Login = @Login AND PasswordHash = @PasswordHash";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Login", login);
                        cmd.Parameters.AddWithValue("@PasswordHash", password);

                        SqlDataReader reader = cmd.ExecuteReader();

                        if (reader.Read())
                        {
                            int userId = Convert.ToInt32(reader["UserID"]);
                            string firstName = reader["FirstName"].ToString();
                            string lastName = reader["LastName"].ToString();
                            string patronymic = reader["Patronymic"].ToString();
                            string role = reader["Role"]?.ToString() ?? "";

                            MessageBox.Show($"Добро пожаловать, {lastName} {firstName} {patronymic}!\nРоль: {role}", "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);

                            
                            Properties.Settings.Default.SavedLogin = login;
                            Properties.Settings.Default.SavedPassword = password;
                            Properties.Settings.Default.Save();

                            if (role == "Админ")
                            {
                                var adminWindow = new MainClientWindow(userId);
                                adminWindow.Show();
                            }
                            else if (role == "Клиент")
                            {
                                var clientWindow = new MainClientWindow(userId);
                                clientWindow.Show();
                            }

                            this.Close();
                        }
                        failedAttempts++;

                        if (failedAttempts == 1)
                        {
                            MessageBox.Show("Пользователь не зарегистрирован.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else if (failedAttempts == 2)
                        {
                            captchaBlock.Visibility = Visibility.Visible;
                            this.Height = 520;
                            this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;
                            this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
                            GenerateCaptcha();

                            MessageBox.Show("Пользователь не зарегистрирован. Введите CAPTCHA.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else if (failedAttempts >= 3)
                        {
                            captchaBlock.Visibility = Visibility.Visible;
                            this.Height = 520;
                            this.Top = (SystemParameters.WorkArea.Height - this.Height) / 2;
                            this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
                            GenerateCaptcha();

                            var result = MessageBox.Show("Вы ввели неправильный пароль несколько раз.\nХотите восстановить пароль?",
                                "Восстановление пароля", MessageBoxButton.YesNo, MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                MessageBox.Show("Переход к восстановлению пароля");
                                return;
                            }
                            else
                            {
                                MessageBox.Show("Пользователь не зарегистрирован. Введите CAPTCHA.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }

                        reader.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при подключении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        // Метод для загрузки сохранённых данных (вызывать при инициализации окна, например, в Loaded событии)
        private void LoadSavedCredentials()
        {
            string savedLogin = Properties.Settings.Default.SavedLogin;
            string savedPassword = Properties.Settings.Default.SavedPassword;

            if (!string.IsNullOrWhiteSpace(savedLogin))
                logBox.Text = savedLogin;

            if (!string.IsNullOrWhiteSpace(savedPassword))
            {
                if (isPasswordVisible)
                    visiblePasswordBox.Text = savedPassword;
                else
                    passwordBox.Password = savedPassword;
            }
        }

        private void GuestLoginLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            string guestLogin = $"guest_{DateTime.Now.Ticks}";
            string fam = "Гость";
            string imya = "Системы";
            string otch = "";

            int userId = 0; // сюда получим новый UserID

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string query = @"
                INSERT INTO Users (Login, PasswordHash, LastName, FirstName, Patronymic, Role)
                OUTPUT INSERTED.UserID
                VALUES (@Login, @PasswordHash, @LastName, @FirstName, @Patronymic, @Role)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Login", guestLogin);
                    cmd.Parameters.AddWithValue("@PasswordHash", ""); // или null, если разрешено
                    cmd.Parameters.AddWithValue("@LastName", fam);
                    cmd.Parameters.AddWithValue("@FirstName", imya);
                    cmd.Parameters.AddWithValue("@Patronymic", otch);
                    cmd.Parameters.AddWithValue("@Role", "Гость");

                    // ExecuteScalar вернет первый столбец первой строки, а у нас это UserID
                    userId = (int)cmd.ExecuteScalar();
                }

                // Открываем форму с переданным UserID гостя
                MainClientWindow guestWindow = new MainClientWindow(userId);
                guestWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при входе как гость: " + ex.Message);
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
        private void LockoutTimer_Tick(object sender, EventArgs e)
        {
            countdownSeconds--;

            if (countdownSeconds > 0)
            {
                buttnlog.Content = $"Повтор через {countdownSeconds}";
            }
            else
            {
                lockoutTimer.Stop();
                buttnlog.Content = "Войти";
                buttnlog.IsEnabled = true;
            }
        }
        private void StartLockoutCountdown()
        {
            countdownSeconds = 10;
            buttnlog.IsEnabled = false;
            buttnlog.Content = $"Повтор через {countdownSeconds}";

            lockoutTimer = new DispatcherTimer();
            lockoutTimer.Interval = TimeSpan.FromSeconds(1);
            lockoutTimer.Tick += LockoutTimer_Tick;
            lockoutTimer.Start();
        }


    }
}
