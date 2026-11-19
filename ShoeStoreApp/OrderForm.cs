using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ShoeStoreApp
{
    public partial class OrderForm : Form
    {
        private int productId;
        private int userId;

        private decimal productPrice = 0;

        public OrderForm(int prodId, int usrId, string prodName)
        {
            InitializeComponent();
            productId = prodId;
            userId = usrId;

            this.Font = new Font("Calibri", 10);
            this.BackColor = Color.White;

            
            LoadProductPrice(); 
            
            LoadSizes(); 
        }

        private void LoadProductPrice()
        {
            string query = "SELECT Price FROM Products WHERE ProductID = @pid";
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@pid", productId);

                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        productPrice = Convert.ToDecimal(result);
                        lblPrice.Text = $"Цена: {productPrice} руб.";
                        UpdateTotal(); 
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки цены: " + ex.Message);
            }
        }

        private void UpdateTotal()
        {
            int qty = (int)numQuantity.Value;
            decimal total = productPrice * qty;
            lblTotal.Text = $"Итого: {total} руб.";
        }

        private void LoadSizes()
        {
            string query = @"
                SELECT s.SizeID, s.SizeValue, st.Quantity 
                FROM StockItems st
                JOIN Sizes s ON st.SizeID = s.SizeID
                WHERE st.ProductID = @pid AND st.Quantity > 0
                ORDER BY s.SizeValue";

            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@pid", productId);
                    SqlDataReader reader = cmd.ExecuteReader();

                    cmbSize.Items.Clear();
                    while (reader.Read())
                    {
                        cmbSize.Items.Add(new SizeItem
                        {
                            SizeID = (int)reader["SizeID"],
                            SizeValue = reader["SizeValue"].ToString(),
                            MaxQuantity = (int)reader["Quantity"]
                        });
                    }
                }
                if (cmbSize.Items.Count > 0)
                    cmbSize.SelectedIndex = 0;
                else
                    MessageBox.Show("Этого товара нет в наличии!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки размеров: " + ex.Message);
            }
        }

        private void btnOrder_Click(object sender, EventArgs e)
        {
            if (cmbSize.SelectedItem == null)
            {
                MessageBox.Show("Выберите размер!");
                return;
            }

            var selectedSize = (SizeItem)cmbSize.SelectedItem;
            int qty = (int)numQuantity.Value;

            if (qty > selectedSize.MaxQuantity)
            {
                MessageBox.Show($"Максимально доступно: {selectedSize.MaxQuantity} шт.");
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string insertOrder = @"
                        INSERT INTO Orders (UserID, StatusID) 
                        OUTPUT INSERTED.OrderID 
                        VALUES (@uid, 1)";
                    SqlCommand cmdOrder = new SqlCommand(insertOrder, conn, transaction);
                    cmdOrder.Parameters.AddWithValue("@uid", userId);
                    int newOrderId = (int)cmdOrder.ExecuteScalar();

                    string insertItem = @"
                        INSERT INTO OrderItems (OrderID, ProductID, SizeID, Quantity, PriceAtPurchase) 
                        VALUES (@oid, @pid, @sid, @qty, (SELECT Price FROM Products WHERE ProductID = @pid))";
                    SqlCommand cmdItem = new SqlCommand(insertItem, conn, transaction);
                    cmdItem.Parameters.AddWithValue("@oid", newOrderId);
                    cmdItem.Parameters.AddWithValue("@pid", productId);
                    cmdItem.Parameters.AddWithValue("@sid", selectedSize.SizeID);
                    cmdItem.Parameters.AddWithValue("@qty", qty);
                    cmdItem.ExecuteNonQuery();

                    string updateStock = @"
                        UPDATE StockItems 
                        SET Quantity = Quantity - @qty 
                        WHERE ProductID = @pid AND SizeID = @sid";
                    SqlCommand cmdStock = new SqlCommand(updateStock, conn, transaction);
                    cmdStock.Parameters.AddWithValue("@qty", qty);
                    cmdStock.Parameters.AddWithValue("@pid", productId);
                    cmdStock.Parameters.AddWithValue("@sid", selectedSize.SizeID);
                    cmdStock.ExecuteNonQuery();

                    transaction.Commit();
                    MessageBox.Show("Заказ успешно оформлен!", "Успех");

                    this.DialogResult = DialogResult.OK; 
                    this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); 
                    MessageBox.Show("Ошибка оформления: " + ex.Message);
                }
            }
        }

        private void btnOrder_Click_1(object sender, EventArgs e)
        {
            if (cmbSize.SelectedItem == null)
            {
                MessageBox.Show("Выберите размер!");
                return;
            }

            var selectedSize = (SizeItem)cmbSize.SelectedItem;
            int qty = (int)numQuantity.Value;

            if (qty > selectedSize.MaxQuantity)
            {
                MessageBox.Show($"Максимально доступно: {selectedSize.MaxQuantity} шт.");
                return;
            }

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // Создает заказ
                    string insertOrder = @"INSERT INTO Orders (UserID, StatusID) OUTPUT INSERTED.OrderID VALUES (@uid, 1)";
                    SqlCommand cmdOrder = new SqlCommand(insertOrder, conn, transaction);
                    cmdOrder.Parameters.AddWithValue("@uid", userId);
                    int newOrderId = (int)cmdOrder.ExecuteScalar();

                    // Добавляет товар
                    string insertItem = @"INSERT INTO OrderItems (OrderID, ProductID, SizeID, Quantity, PriceAtPurchase) 
                                  VALUES (@oid, @pid, @sid, @qty, (SELECT Price FROM Products WHERE ProductID = @pid))";
                    SqlCommand cmdItem = new SqlCommand(insertItem, conn, transaction);
                    cmdItem.Parameters.AddWithValue("@oid", newOrderId);
                    cmdItem.Parameters.AddWithValue("@pid", productId);
                    cmdItem.Parameters.AddWithValue("@sid", selectedSize.SizeID);
                    cmdItem.Parameters.AddWithValue("@qty", qty);
                    cmdItem.ExecuteNonQuery();

                    // Уменьшает остаток
                    string updateStock = @"UPDATE StockItems SET Quantity = Quantity - @qty 
                                   WHERE ProductID = @pid AND SizeID = @sid";
                    SqlCommand cmdStock = new SqlCommand(updateStock, conn, transaction);
                    cmdStock.Parameters.AddWithValue("@qty", qty);
                    cmdStock.Parameters.AddWithValue("@pid", productId);
                    cmdStock.Parameters.AddWithValue("@sid", selectedSize.SizeID);
                    cmdStock.ExecuteNonQuery();

                    transaction.Commit();
                    MessageBox.Show("Заказ успешно оформлен!");

                    this.DialogResult = DialogResult.OK; 
                    this.Close();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        private void numQuantity_ValueChanged(object sender, EventArgs e)
        {
            UpdateTotal();
        }
    }


    public class SizeItem
    {
        public int SizeID { get; set; }
        public string SizeValue { get; set; }
        public int MaxQuantity { get; set; }
        public override string ToString() => SizeValue;
    }
}