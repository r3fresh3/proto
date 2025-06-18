using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Прототип
{
    public partial class PasswordRecovery : Window
    {
        private string connectionString = "Data Source=DESKTOP-7IEK9VH;Initial Catalog=dm2;Integrated Security=True;";
        private int foundUserId = -1;

        public PasswordRecovery()
        {
            InitializeComponent();
        }

        private void FindAccount_Click(object sender, RoutedEventArgs e)
        {
            string email = emailBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Введите email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT UserID, Login FROM Users WHERE Email = @Email";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Email", email);

                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        foundUserId = Convert.ToInt32(reader["UserID"]);
                        string login = reader["Login"].ToString();

                        foundLoginText.Text = $"Это ваш аккаунт?\nЛогин: {login}";
                        confirmationPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        MessageBox.Show("Аккаунт с таким email не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        confirmationPanel.Visibility = Visibility.Collapsed;
                    }

                    reader.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при подключении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ConfirmAccount_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем все кроме панели ввода паролей
            emailInputPanel.Visibility = Visibility.Collapsed;
            confirmationPanel.Visibility = Visibility.Collapsed;
            newPasswordPanel.Visibility = Visibility.Visible;

            // Очистим пароли на всякий случай
            newPasswordBox.Clear();
            newPasswordTextBox.Clear();
            repeatPasswordBox.Clear();
            repeatPasswordTextBox.Clear();
        }

        private void RejectAccount_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Попробуйте ввести другую почту.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);

            emailBox.Clear();
            emailBox.Focus();

            confirmationPanel.Visibility = Visibility.Collapsed;
            newPasswordPanel.Visibility = Visibility.Collapsed;
            emailInputPanel.Visibility = Visibility.Visible;
        }

        private void ShowNewPassword(object sender, RoutedEventArgs e)
        {
            newPasswordTextBox.Text = newPasswordBox.Password;
            newPasswordTextBox.Visibility = Visibility.Visible;
            newPasswordBox.Visibility = Visibility.Collapsed;
        }

        private void HideNewPassword(object sender, RoutedEventArgs e)
        {
            newPasswordBox.Password = newPasswordTextBox.Text;
            newPasswordBox.Visibility = Visibility.Visible;
            newPasswordTextBox.Visibility = Visibility.Collapsed;
        }

        private void ShowRepeatPassword(object sender, RoutedEventArgs e)
        {
            repeatPasswordTextBox.Text = repeatPasswordBox.Password;
            repeatPasswordTextBox.Visibility = Visibility.Visible;
            repeatPasswordBox.Visibility = Visibility.Collapsed;
        }

        private void HideRepeatPassword(object sender, RoutedEventArgs e)
        {
            repeatPasswordBox.Password = repeatPasswordTextBox.Text;
            repeatPasswordBox.Visibility = Visibility.Visible;
            repeatPasswordTextBox.Visibility = Visibility.Collapsed;
        }

        private void SaveNewPassword_Click(object sender, RoutedEventArgs e)
        {
            string newPass = newPasswordBox.Visibility == Visibility.Visible
                ? newPasswordBox.Password
                : newPasswordTextBox.Text;

            string repeatPass = repeatPasswordBox.Visibility == Visibility.Visible
                ? repeatPasswordBox.Password
                : repeatPasswordTextBox.Text;

            if (string.IsNullOrWhiteSpace(newPass) || string.IsNullOrWhiteSpace(repeatPass))
            {
                MessageBox.Show("Введите новый пароль в оба поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass != repeatPass)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE UserID = @UserID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PasswordHash", newPass); 
                    cmd.Parameters.AddWithValue("@UserID", foundUserId);

                    int rows = cmd.ExecuteNonQuery();

                    if (rows > 0)
                    {
                        MessageBox.Show("Пароль успешно обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при обновлении пароля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка подключения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
