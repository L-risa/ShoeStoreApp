using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShoeStoreApp
{
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
            this.Font = new Font("Calibri", 10);
            this.BackColor = Color.White;
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            string lastName = txtLastName.Text.Trim();
            string firstName = txtFirstName.Text.Trim();
            string middleName = txtMiddleName.Text.Trim();
            string login = txtLogin.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(firstName) ||
                string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните Фамилию, Имя, Логин и Пароль!", "Внимание");
                return;
            }

            if (IsLoginExists(login))
            {
                MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка");
                return;
            }

            string query = @"
                INSERT INTO Users (LastName, FirstName, MiddleName, Login, Password, RoleID) 
                VALUES (@lastName, @firstName, @middleName, @login, @password, 3)";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@lastName", lastName);
                    cmd.Parameters.AddWithValue("@firstName", firstName);
                    cmd.Parameters.AddWithValue("@middleName", string.IsNullOrEmpty(middleName) ? (object)DBNull.Value : middleName);
                    cmd.Parameters.AddWithValue("@login", login);
                    cmd.Parameters.AddWithValue("@password", password);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Регистрация прошла успешно! Теперь вы можете войти.", "Успех");
                this.Close(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при регистрации: " + ex.Message);
            }
        }
        private bool IsLoginExists(string login)
        {
            string query = "SELECT COUNT(*) FROM Users WHERE Login = @login";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@login", login);
                int count = (int)cmd.ExecuteScalar();
                return count > 0; 
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close(); 
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}