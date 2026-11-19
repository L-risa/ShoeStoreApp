using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ShoeStoreApp
{
    public partial class OrderDetailsForm : Form
    {
        private int orderId;

        public OrderDetailsForm(int id)
        {
            InitializeComponent();
            orderId = id;

            this.Font = new Font("Calibri", 10);
            this.Text = $"Состав заказа №{orderId}"; 

            LoadDetails();
        }

        private void LoadDetails()
        {
            string query = @"
                SELECT p.Name AS 'Товар', 
                       s.SizeValue AS 'Размер', 
                       oi.Quantity AS 'Кол-во', 
                       oi.PriceAtPurchase AS 'Цена'
                FROM OrderItems oi
                JOIN Products p ON oi.ProductID = p.ProductID
                JOIN Sizes s ON oi.SizeID = s.SizeID
                WHERE oi.OrderID = @oid";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    da.SelectCommand.Parameters.AddWithValue("@oid", orderId);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvDetails.DataSource = dt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки состава: " + ex.Message);
            }
        }

        private void OrderDetailsForm_Load(object sender, EventArgs e)
        {

        }
    }
}