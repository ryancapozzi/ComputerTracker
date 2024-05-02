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
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
            populateComboBox();
        }
        public Form4(string serviceTag)
        {
            InitializeComponent();
            populateComboBox();
            serviceTagInput.Text = serviceTag;
        }

        private string connectionString = "Data Source=FAS-HYHGPH2; Initial Catalog=FASComputers; Integrated Security=True";
        // Populate the box with users
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
        private void returnComputerButton_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Check user inputs for correct input format
                    string checkServiceTagQuery = "SELECT 1 FROM laptops WHERE [Service Tag] = @ServiceTag";
                    using (SqlCommand checkServiceCommand = new SqlCommand(checkServiceTagQuery, connection))
                    {
                        checkServiceCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                        object result = checkServiceCommand.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("Service tag does not exist.");
                            return;
                        }
                    }
                    if (string.IsNullOrEmpty(usernameInput.Text))
                    {
                        MessageBox.Show("Username field cannot be null.");
                        return;
                    }
                    // Check if return radio button is checked
                    if (rbtnReturn.Checked)
                    {
                        string username = "00" + usernameInput.Text.Substring(usernameInput.Text.Length - 4, 4);
                        // Check if the laptop is assigned to the user before updating
                        string checkAssignmentQuery = "SELECT 1 FROM assignments " +
                                                      "WHERE Username = @Username " +
                                                      "AND LaptopID IN (SELECT LaptopID FROM laptops WHERE [Service Tag] = @ServiceTag) " +
                                                      "AND AssignmentDate IS NOT NULL AND ReturnDate IS NULL";
                        using (SqlCommand checkCommand = new SqlCommand(checkAssignmentQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@Username", username);
                            checkCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());

                            object result = checkCommand.ExecuteScalar();

                            if (result != null)
                            {
                                // Laptop is assigned, proceed with the update
                                string updateQuery = "UPDATE assignments SET ReturnDate = GETDATE() " +
                                                     "WHERE Username = @Username " +
                                                     "AND LaptopID IN (SELECT LaptopID FROM laptops WHERE [Service Tag] = @ServiceTag); " +
                                                     "UPDATE laptops SET Status = 'Available' WHERE [Service Tag] = @ServiceTag";

                                using (SqlCommand command = new SqlCommand(updateQuery, connection))
                                {
                                    command.Parameters.AddWithValue("@Username", username);
                                    command.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                                    command.ExecuteNonQuery();
                                }

                                MessageBox.Show("Computer Returned Successfully!");
                            }
                            else
                            {
                                MessageBox.Show("Laptop is not assigned to the user or has already been returned.");
                            }
                        }
                    }
                    // Else the retire radio button is checked
                    else
                    {
                        string checkServiceTag = "SELECT 1 FROM laptops WHERE [Service Tag] = @ServiceTag";
                        using (SqlCommand checkServiceCommand = new SqlCommand(checkServiceTag, connection))
                        {
                            checkServiceCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                            object result = checkServiceCommand.ExecuteScalar();
                            if (result == null)
                            {
                                MessageBox.Show("Service tag does not exist.");
                                return;
                            }
                        }
                        string checkRetiredQuery = "SELECT 1 FROM laptops WHERE [Service Tag] = @ServiceTag AND Status = 'Retired'";
                        string retireQuery = "UPDATE laptops SET Status = 'Retired' WHERE [Service Tag] = @ServiceTag";
                        using (SqlCommand checkCommand = new SqlCommand(checkRetiredQuery, connection))
                        {
                            checkCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                            object result = checkCommand.ExecuteScalar();
                            if (result == null)
                            {
                                using (SqlCommand retireCommand = new SqlCommand(retireQuery, connection))
                                {
                                    retireCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                                    retireCommand.ExecuteNonQuery();
                                }
                                MessageBox.Show("Computer permanently retired.");
                            }
                            else
                            {
                                MessageBox.Show("Computer has already been retired.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
