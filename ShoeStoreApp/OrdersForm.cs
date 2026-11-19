using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ShoeStoreApp
{
    public partial class OrdersForm : Form
    {
        private string userRole;
        private int userId;

        public OrdersForm(string role, int id)
        {
            InitializeComponent();
            userRole = role;
            userId = id;

            this.Font = new Font("Calibri", 10);
            this.BackColor = Color.White;
            dgvOrders.BackgroundColor = Color.White;

            if (userRole != "Администратор")
            {
                btnChangeStatus.Visible = false;
                cmbStatus.Visible = false;

                btnDeleteOrder.Visible = false;
            }

            LoadStatuses();
            LoadOrders();
        }

        private void LoadStatuses()
        {
            string query = "SELECT StatusID, StatusName FROM OrderStatuses ORDER BY StatusID";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    cmbStatus.Items.Clear();
                    while (reader.Read())
                    {
                        cmbStatus.Items.Add(new StatusItem
                        {
                            StatusID = (int)reader["StatusID"],
                            StatusName = reader["StatusName"].ToString()
                        });
                    }
                    cmbStatus.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка статусов: " + ex.Message);
            }
        }

        private void LoadOrders()
        {
            string query;

            if (userRole == "Администратор" || userRole == "Менеджер")
            {
                query = @"
            SELECT o.OrderID AS '№',
                   o.OrderDate AS 'Дата',
                   os.StatusName AS 'Статус',
                   u.LastName + ' ' + u.FirstName + ' ' + ISNULL(u.MiddleName,'') AS 'Клиент',
                   p.ImagePath
            FROM Orders o
            JOIN Users u ON o.UserID = u.UserID
            JOIN OrderStatuses os ON o.StatusID = os.StatusID
            LEFT JOIN OrderItems oi ON o.OrderID = oi.OrderID
            LEFT JOIN Products p ON oi.ProductID = p.ProductID
            GROUP BY o.OrderID, o.OrderDate, os.StatusName, u.LastName, u.FirstName, u.MiddleName, p.ImagePath
            ORDER BY o.OrderDate DESC";
            }
            else
            {
                query = @"
            SELECT o.OrderID AS '№',
                   o.OrderDate AS 'Дата',
                   os.StatusName AS 'Статус',
                   p.ImagePath
            FROM Orders o
            JOIN OrderStatuses os ON o.StatusID = os.StatusID
            LEFT JOIN OrderItems oi ON o.OrderID = oi.OrderID
            LEFT JOIN Products p ON oi.ProductID = p.ProductID
            WHERE o.UserID = @uid
            GROUP BY o.OrderID, o.OrderDate, os.StatusName, p.ImagePath
            ORDER BY o.OrderDate DESC";
            }

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    if (userRole != "Администратор" && userRole != "Менеджер")
                        da.SelectCommand.Parameters.AddWithValue("@uid", userId);

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvOrders.DataSource = dt;
                }

                if (dgvOrders.Columns["ImagePath"] != null)
                    dgvOrders.Columns["ImagePath"].Visible = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов: " + ex.Message);
            }
        }

        private void btnChangeStatus_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Выберите заказ!");
                return;
            }

            int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["№"].Value);
            int newStatusId = ((StatusItem)cmbStatus.SelectedItem).StatusID;

            string query = "UPDATE Orders SET StatusID = @sid WHERE OrderID = @oid";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sid", newStatusId);
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Статус обновлен!");
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка обновления: " + ex.Message);
            }
        }

        private void btnChangeStatus_Click_1(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Выберите заказ в таблице!");
                return;
            }

            int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["№"].Value);
            if (cmbStatus.SelectedItem == null) return;
            int newStatusId = ((StatusItem)cmbStatus.SelectedItem).StatusID;

            string query = "UPDATE Orders SET StatusID = @sid WHERE OrderID = @oid";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@sid", newStatusId);
                    cmd.Parameters.AddWithValue("@oid", orderId);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Статус успешно изменен!");
                LoadOrders(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private void dgvOrders_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow != null)
            {
                try
                {
                    string imageName = dgvOrders.CurrentRow.Cells["ImagePath"].Value?.ToString();

                    if (!string.IsNullOrEmpty(imageName))
                    {
                        string fullPath = System.IO.Path.Combine(Application.StartupPath, imageName);

                        if (System.IO.File.Exists(fullPath))
                        {
                            using (var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                            {
                                pictureBoxOrder.Image = Image.FromStream(stream);
                            }
                        }
                        else
                        {
                            pictureBoxOrder.Image = null; 
                        }
                    }
                    else
                    {
                        pictureBoxOrder.Image = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка загрузки фото: " + ex.Message);
                }
            }
        }

        public class StatusItem
        {
            public int StatusID { get; set; }
            public string StatusName { get; set; }
            public override string ToString() => StatusName;
        }

        private void dgvOrders_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvOrders.CurrentRow != null)
            {
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["№"].Value);

                OrderDetailsForm details = new OrderDetailsForm(orderId);
                details.ShowDialog();
            }
        }

        private void btnDeleteOrder_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Выберите заказ для удаления!");
                return;
            }

            if (MessageBox.Show("Вы уверены, что хотите удалить этот заказ?", "Внимание", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                int orderId = Convert.ToInt32(dgvOrders.CurrentRow.Cells["№"].Value);
                string query = "DELETE FROM OrderItems WHERE OrderID = @oid; DELETE FROM Orders WHERE OrderID = @oid;";

                try
                {
                    using (var conn = DatabaseHelper.GetConnection())
                    {
                        conn.Open();
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@oid", orderId);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Заказ удален!");
                    LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления: " + ex.Message);
                }
            }
        }
    }
}


    