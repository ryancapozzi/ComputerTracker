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

namespace CompTrack
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            InitializeComponent();
            populateComboBox();
        }

        private string connectionString = "Data Source=FAS-HYHGPH2; Initial Catalog=FASComputers; Integrated Security=True";
        // populate the combo box with users
        private void populateComboBox()
        {
            usernameInput.Items.Clear();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT CONCAT(FirstName, LastName, SUBSTRING(Username, 3, LEN(Username))) AS DisplayName FROM users";
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
        private void searchUserButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Check user inputs for correct input format
                    if (string.IsNullOrEmpty(usernameInput.Text))
                    {
                        MessageBox.Show("Username field cannot be null.");
                        return;
                    }
                    string username = "00" + usernameInput.Text.Substring(usernameInput.Text.Length - 4, 4);

                    string query = "WITH RankedAssignments AS (SELECT laptops.[Service Tag], users.Username, assignments.AssignmentDate, " +
                        "assignments.ReturnDate, ROW_NUMBER() OVER (PARTITION BY laptops.[Service Tag], users.Username " +
                        "ORDER BY laptops.UpdatedOn DESC) AS RowNum FROM assignments JOIN laptops ON laptops.LaptopID = assignments.LaptopID " +
                        "JOIN users ON assignments.Username = users.Username WHERE users.Username = @Username) " +
                        "SELECT [Service Tag], SUBSTRING(Username, 3, LEN(Username)) AS 'Username', AssignmentDate, ReturnDate FROM RankedAssignments WHERE RowNum = 1;";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username); ;
                        // Use SqlDataReader to read the data
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Check if there are rows
                            if (reader.HasRows)
                            {
                                // Create a DataTable to hold the data
                                DataTable dataTable = new DataTable();

                                // Load the data into the DataTable
                                dataTable.Load(reader);

                                // Bind the DataTable to the DataGridView
                                assignmentDisplayData.DataSource = dataTable;
                            }
                            else
                            {
                                // No rows found, you might want to handle this case
                                MessageBox.Show("No data found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
