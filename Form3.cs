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
    public partial class Form3 : Form
    {
        private string serviceTag;
        public Form3()
        {
            InitializeComponent();
            populateComboBox();
        }
        public Form3(string serviceTag)
        {
            InitializeComponent();
            populateComboBox();
            serviceTagInput.Text = serviceTag;
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
        private void assignUserButton_Click(object sender, EventArgs e)
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
                    // Check if laptop trying to be assigned is retired
                    string checkRetiredQuery = "SELECT 1 FROM laptops WHERE [Service Tag] = @ServiceTag AND Status = 'Retired'";
                    using (SqlCommand checkRetiredCommand = new SqlCommand(checkRetiredQuery, connection))
                    {
                        checkRetiredCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                        object result = checkRetiredCommand.ExecuteScalar();
                        if (result != null)
                        {
                            MessageBox.Show("Cannot assign a user to a retired computer.");
                            return;
                        }
                    }
                    // Alter usernameInput to be '00XXXX' format
                    string username = "00" + usernameInput.Text.Substring(usernameInput.Text.Length - 4, 4);
                    // Query to assign a laptop to a user
                    string query = "INSERT INTO assignments (LaptopID, Username, AssignmentDate) " +
                                   "SELECT laptops.LaptopID, users.Username, GETDATE() FROM laptops " +
                                   "JOIN users ON users.Username = @Username WHERE laptops.[Service Tag] = @ServiceTag " +
                                   "AND NOT EXISTS (SELECT 1 FROM laptops " +
                                   "WHERE [Service Tag] = @ServiceTag AND Status = 'Assigned')";
                    // Query to change status in laptops table
                    string statusQuery = "UPDATE laptops SET Status = 'Assigned' WHERE [Service Tag] = @ServiceTag";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                        int rowsAffected = command.ExecuteNonQuery();
                        // Check if new assignment was created. Use check to determine what to say
                        if (rowsAffected <= 0)
                        {
                            MessageBox.Show("Error: One of the following... \nComputer already assigned\nComputer does not exist\nUser does not exist");
                        }
                        else
                        {
                            using (SqlCommand statusCommand = new SqlCommand(statusQuery, connection))
                            {
                                statusCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                                statusCommand.ExecuteNonQuery();
                            }
                            MessageBox.Show("User assigned to laptop.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            //place holder
        }
    }
}
