using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using System.Data.SqlClient;

namespace Прототип
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string connectionString = "Data Source=DESKTOP-7IEK9VH;Initial Catalog=dm2;Integrated Security=True;";

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string deleteGuestsQuery = "DELETE FROM Users WHERE Role = 'Гость'";
                    SqlCommand cmd = new SqlCommand(deleteGuestsQuery, conn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении гостей из базы: " + ex.Message);
            }
        }

    }
}
