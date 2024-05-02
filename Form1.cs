using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CompTrack
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            populateComboBox();
        }

        private string connectionString = "Data Source=FAS-HYHGPH2; Initial Catalog=FASComputers; Integrated Security=True";
        // Populate the combo box choices using sql query
        private void populateComboBox()
        {
            usernameInput.Items.Clear();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT CONCAT(FirstName, LastName, SUBSTRING(Username, 3, LEN(Username))) AS DisplayName FROM users WHERE status = 'A'";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            usernameInput.Items.Add(reader["DisplayName"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // Button to create a new laptop in the database
        private void createLaptopButton_Click(object sender, EventArgs e)
        {
            SqlTransaction transaction = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    object result;
                    connection.Open();
                    //string dateBought = dateBoughtInput.Text.Trim();
                    DateTime selectedDateBought = dateBoughtSelect.Value;
                    string dateBought = selectedDateBought.ToString("MM/dd/yyyy");

                    //string warrantyExpiration = warrantyExpirationInput.Text.Trim();
                    DateTime selectedWarrantyDate = dateWarrantySelect.Value;
                    string warrantyExpiration = selectedWarrantyDate.ToString("MM/dd/yyyy");

                    string serviceCode = serviceCodeInput.Text.Trim();
                    string serviceTag = serviceTagInput.Text.Trim();
                    string ram = ramInput.Text.Trim();
                    string hd = hardDriveInput.Text.Trim();
                    string os = operatingSystemInput.Text.Trim();
                    // Check if user inputs are valid
                    string pattern = @"^\d{2}/\d{2}/\d{4}$";
                    if (!(Regex.IsMatch(dateBought, pattern)))
                    {
                        MessageBox.Show("Purchase Date must be in format MM/dd/yyyy");
                        return;
                    }
                    if (!(Regex.IsMatch(warrantyExpiration, pattern)))
                    {
                        MessageBox.Show("Warranty Expiration date must be in format MM/dd/yyyy");
                        return;
                    }
                    string alphanumeric = "^[a-zA-Z0-9]+$";
                    if (!(Regex.IsMatch(serviceTag, alphanumeric)))
                    {
                        MessageBox.Show("Service tag must only contain letters and numbers.");
                        return;
                    }
                    if (!(Regex.IsMatch(os, alphanumeric)))
                    {
                        MessageBox.Show("OS must only contain letters and numbers");
                        return;
                    }
                    // Check if numeric fields only contain numbers
                    string numericCheck = @"^\d+$";
                    if (!(Regex.IsMatch(serviceCode, numericCheck)))
                    {
                        MessageBox.Show("Service code can only contain numbers.");
                        return;
                    }
                    if (!(Regex.IsMatch(ram, numericCheck)))
                    {
                        MessageBox.Show("RAM can only contain numbers.");
                        return;
                    }
                    if (!(Regex.IsMatch(hd, numericCheck)))
                    {
                        MessageBox.Show("HD Size can only contain numbers.");
                        return;
                    }
                    transaction = connection.BeginTransaction();

                    // Query to check if username exists
                    string checkQuery = "SELECT 1 FROM users WHERE Username = @Username";
                    // Query to check if laptop with service tag already exists
                    // Prevents duplicates
                    string checkTagQuery = "SELECT 1 FROM laptops WHERE [Service Tag] = @ServiceTag";
                    // Insert into laptops table
                    string insertQuery = "INSERT INTO laptops ([Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], Status) " +
                                   "VALUES (@ServiceTag, @ServiceCode, @DatePurchased, @WarrantyExpiration, @RAM, @HDSize, @OS, GETDATE(), @Status); " +
                                   "INSERT INTO assignments (LaptopID, Username, AssignmentDate) SELECT laptops.LaptopID, users.Username, GETDATE() " +
                                   "FROM laptops JOIN users ON users.Username = @Username WHERE laptops.[Service Tag] = @ServiceTag";

                    using (SqlCommand checkTagCommand = new SqlCommand(checkTagQuery, connection, transaction))
                    {
                        checkTagCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                        result = checkTagCommand.ExecuteScalar();
                        if (result != null)
                        {
                            MessageBox.Show("A laptop with this service tag already exists...");
                            transaction.Rollback();
                            return;
                        }
                    }
                    using (SqlCommand command = new SqlCommand(insertQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@ServiceTag", serviceTag);
                        command.Parameters.AddWithValue("@ServiceCode", serviceCode);
                        command.Parameters.AddWithValue("@DatePurchased", dateBought);
                        command.Parameters.AddWithValue("@WarrantyExpiration", warrantyExpiration);
                        command.Parameters.AddWithValue("@RAM", ramInput.Text.Trim());
                        command.Parameters.AddWithValue("@HDSize", hardDriveInput.Text.Trim());
                        command.Parameters.AddWithValue("@OS", operatingSystemInput.Text.Trim());
                        // Check user inputs for blank
                        if (string.IsNullOrEmpty(usernameInput.Text))
                        {
                            command.Parameters.AddWithValue("@Username" , "");
                        } else
                        {
                            command.Parameters.AddWithValue("@Username", "00" + usernameInput.Text.Substring(usernameInput.Text.Length - 4, 4));
                        }
                        // Check if username exists
                        if (!(string.IsNullOrEmpty(usernameInput.Text)))
                        {
                            using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@Username", "00" + usernameInput.Text.Substring(usernameInput.Text.Length - 4, 4));
                                result = checkCommand.ExecuteScalar();
                                if (result != null)
                                {
                                    command.Parameters.AddWithValue("@Status", "Assigned");
                                }
                                else
                                {
                                    MessageBox.Show("Username does not exist.");
                                    transaction.Rollback();
                                    return;
                                }
                            }
                        }
                        else
                        {
                            command.Parameters.AddWithValue("@Status", "Available");
                        }
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit();
                }
                MessageBox.Show("Laptop Created Successfully!");
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
