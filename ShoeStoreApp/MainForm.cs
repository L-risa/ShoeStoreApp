using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ShoeStoreApp
{
    public partial class MainForm : Form
    {
        private string userRole;
        private int userId;

        public MainForm(string role, int id)
        {
            InitializeComponent();
            userRole = role;
            userId = id;

            this.Font = new Font("Calibri", 10);
            this.BackColor = Color.White;
            dgvProducts.BackgroundColor = Color.White;

            if (userRole == "Гость")
            {
                btnOrders.Visible = false;
                btnOrder.Visible = false;
                txtSearch.Visible = false;
                cmbFilter.Visible = false;
                cmbSort.Visible = false;
                btnSearch.Visible = false;
            }
            else if (userRole == "Авторизованный пользователь")
            {
                btnOrders.Visible = false; 
                                           
            }
            LoadProducts();
            LoadCategories();
        }

        private void LoadProducts(string searchText = "", string category = "")
        {
             string sortQuery = " ORDER BY p.Name"; 
    if (cmbSort != null && cmbSort.SelectedIndex == 1) sortQuery = " ORDER BY p.Price ASC";
    if (cmbSort != null && cmbSort.SelectedIndex == 2) sortQuery = " ORDER BY p.Price DESC";
    if (cmbSort != null && cmbSort.SelectedIndex == 3) sortQuery = " ORDER BY p.Name ASC";

            string query = @"
        SELECT p.ProductID,
               p.Name AS 'Наименование',
               c.CategoryName AS 'Категория',
               m.ManufacturerName AS 'Производство',
               p.Price AS 'Цена',
               p.ImagePath,
               ISNULL(SUM(s.Quantity), 0) AS 'TotalQuantity'
        FROM Products p
        JOIN Categories c ON p.CategoryID = c.CategoryID
        JOIN Manufacturers m ON p.ManufacturerID = m.ManufacturerID
        LEFT JOIN StockItems s ON p.ProductID = s.ProductID
        WHERE (@search = '' OR p.Name LIKE '%' + @search + '%' OR m.ManufacturerName LIKE '%' + @search + '%')
           AND (@category = '' OR c.CategoryName = @category)
        GROUP BY p.ProductID, p.Name, c.CategoryName, m.ManufacturerName, p.Price, p.ImagePath"
        + sortQuery;


            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);

                    da.SelectCommand.Parameters.AddWithValue("@search", searchText);
                    da.SelectCommand.Parameters.AddWithValue("@category", category);

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvProducts.DataSource = dt;
                }

                if (dgvProducts.Columns["ProductID"] != null)
                    dgvProducts.Columns["ProductID"].Visible = false;

                if (dgvProducts.Columns["ImagePath"] != null)
    dgvProducts.Columns["ImagePath"].Visible = false;

                if (dgvProducts.Columns["TotalQuantity"] != null)
                {
                    dgvProducts.Columns["TotalQuantity"].Visible = true;
                    dgvProducts.Columns["TotalQuantity"].HeaderText = "Остаток";
                }

                foreach (DataGridViewRow row in dgvProducts.Rows)
                {
                    if (row.Cells["TotalQuantity"].Value != null)
                    {
                        int total = Convert.ToInt32(row.Cells["TotalQuantity"].Value);
                        if (total <= 3)
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 128, 128); 
                        else
                            row.DefaultCellStyle.BackColor = Color.White; 
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message);
            }
        }

        private void LoadCategories()
        {
            string query = "SELECT CategoryName FROM Categories ORDER BY CategoryName";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataReader reader = cmd.ExecuteReader();

                    cmbFilter.Items.Clear();
                    cmbFilter.Items.Add("");
                    while (reader.Read())
                        cmbFilter.Items.Add(reader["CategoryName"].ToString());
                    cmbFilter.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка категорий: " + ex.Message);
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            string search = txtSearch.Text.Trim();
            string category = cmbFilter.SelectedItem != null ? cmbFilter.SelectedItem.ToString() : "";

            LoadProducts(search, category);
        }

        private void btnOrder_Click(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null)
            {
                MessageBox.Show("Выберите товар из списка!");
                return;
            }

            if (userRole == "Гость")
            {
                MessageBox.Show("Войдите в систему, чтобы оформить заказ!");
                return;
            }

            int productId = Convert.ToInt32(dgvProducts.CurrentRow.Cells["ProductID"].Value);
            string prodName = dgvProducts.CurrentRow.Cells["Наименование"].Value.ToString();

            OrderForm orderForm = new OrderForm(productId, userId, prodName);

            if (orderForm.ShowDialog() == DialogResult.OK)
            {
                LoadProducts();
            }
        }

        private void btnOrders_Click(object sender, EventArgs e)
        {
            new OrdersForm(userRole, userId).ShowDialog();
        }

        private void btnOrders_Click_1(object sender, EventArgs e)
        {
            OrdersForm ordersForm = new OrdersForm(userRole, userId);
            ordersForm.ShowDialog();
        }

        private void btnSearch_Click_1(object sender, EventArgs e)
        {
            string search = txtSearch.Text.Trim();
            string category = cmbFilter.SelectedItem != null ? cmbFilter.SelectedItem.ToString() : "";
            LoadProducts(search, category);
        }

        private void btnOrder_Click_1(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow == null) { MessageBox.Show("Выберите товар!"); return; }
            int productId = Convert.ToInt32(dgvProducts.CurrentRow.Cells["ProductID"].Value);
            string prodName = dgvProducts.CurrentRow.Cells["Наименование"].Value.ToString();

            OrderForm orderForm = new OrderForm(productId, userId, prodName);
            if (orderForm.ShowDialog() == DialogResult.OK)
                LoadProducts();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void dgvProducts_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dgvProducts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvProducts.CurrentRow != null)
            {
                try
                {
                    string imageName = dgvProducts.CurrentRow.Cells["ImagePath"].Value?.ToString();

                    if (!string.IsNullOrEmpty(imageName))
                    {
                        string fullPath = System.IO.Path.Combine(Application.StartupPath, imageName);

                        if (System.IO.File.Exists(fullPath))
                        {
                            using (var stream = new System.IO.FileStream(fullPath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                            {
                                pictureBox1.Image = Image.FromStream(stream);
                            }
                        }
                        else
                        {
                            pictureBox1.Image = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка загрузки фото: " + ex.Message);
                }
            }
        }

        private void cmbSort_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnSearch_Click(sender, e); 
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                LoginForm loginForm = new LoginForm();

                loginForm.Show();

                this.Hide();
            }
        }
    }
}
