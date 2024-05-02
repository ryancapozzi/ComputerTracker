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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CompTrack
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
        }
        public Form6(string serviceTag)
        {
            InitializeComponent();
            serviceTagInput.Text = serviceTag;
            serviceTagInput2.Text = serviceTag;
        }

        private string connectionString = "Data Source=FAS-HYHGPH2; Initial Catalog=FASComputers; Integrated Security=True";
        private void updateComputerButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if comboBox value was selected
                if (string.IsNullOrWhiteSpace(featureInput.Text))
                {
                    MessageBox.Show("Please select a feature to update.");
                    return;  // Stop execution if feature selection is empty
                }

                // Check if new value is provided
                string newValue = newValueInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(newValue))
                {
                    MessageBox.Show("Please enter a new value.");
                    return;  // Stop execution if new value is empty
                }

                // Check if [Service Tag] is provided
                string serviceTag = serviceTagInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(serviceTag))
                {
                    MessageBox.Show("Please enter a [Service Tag].");
                    return;  // Stop execution if [Service Tag] is empty
                }

                // Check if UpdatedBy is provided
                string updatedBy = updatedByInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(updatedBy))
                {
                    MessageBox.Show("Please enter the updater's name.");
                    return; //Stop execution if UpdatedBy is empty
                }
                // Need to have a different logic for if it is a date they are inputting
                if (featureInput.Text == "[Warranty Expiration]")
                {
                    // Check if date is correct format
                    string format = "MM/dd/yyyy";
                    DateTime result;
                    if (!(DateTime.TryParseExact(newValueInput.Text.Trim(), format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out result)))
                    {
                        MessageBox.Show("Warranty Expiration must be a valid date. Format must be MM/dd/yyyy");
                        return;
                    }
                }

                // Logic for if the input requires a number
                if (featureInput.Text == "RAM" || featureInput.Text == "[HD Size]")
                {
                    string numericCheck = @"^\d+$";
                    if (!(Regex.IsMatch(newValueInput.Text.Trim(), numericCheck)))
                    {
                        MessageBox.Show("RAM and HD Size can only contain numbers.");
                        return;
                    }
                }
                // Logic for if the input requires letters and numbers
                if (featureInput.Text == "OS")
                {
                    string alphanumeric = "^[a-zA-Z0-9]+$";
                    if (!(Regex.IsMatch(newValueInput.Text, alphanumeric)))
                    {
                        MessageBox.Show("OS must only contain letters and numbers.");
                        return;
                    }
                }
                // Establish connection with database
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Check if service tag exists
                    string checkServiceTag = "SELECT 1 FROM laptops WHERE [Service Tag] = @ServiceTag";
                    using (SqlCommand checkServiceCommand = new SqlCommand(checkServiceTag, connection))
                    {
                        checkServiceCommand.Parameters.AddWithValue("@ServiceTag", serviceTagInput.Text.Trim());
                        object check = checkServiceCommand.ExecuteScalar();
                        if (check == null)
                        {
                            MessageBox.Show("Service tag does not exist.");
                            return;
                        }
                    }
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert a new row copying the old information
                            string insertQuery = "INSERT INTO laptops ([Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], UpdatedBy, UpdatedOn, Status) " +
                                "SELECT TOP 1 [Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], UpdatedBy, UpdatedOn, Status " +
                                "FROM laptops WHERE [Service Tag] = @ServiceTag ORDER BY laptopID DESC;";
                                // Might need to change the ORDER BY to not have DESC
                            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                                insertCommand.ExecuteNonQuery();
                            }

                            // Update the existing row with the new value and change UpdatedBy and UpdatedOn
                            string updateQuery = $"UPDATE laptops SET {featureInput.Text.Trim()} = @NewValue, UpdatedBy = @UpdatedBy, UpdatedOn = GETDATE() " +
                                                 $"WHERE LaptopID IN (SELECT TOP 1 LaptopID FROM laptops WHERE [Service Tag] = @ServiceTag ORDER BY LaptopID DESC);";
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@NewValue", newValue);
                                updateCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                                updateCommand.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                                updateCommand.ExecuteNonQuery();
                            }
                            // Assign the new updated computer to the same user it had before
                            /*string assignQuery = "INSERT INTO assignments (LaptopID, UserID, AssignmentDate) SELECT laptops.LaptopID, users.UserID, assignments.AssignmentDate FROM laptops " +
                                "JOIN users ON users.Username = (SELECT TOP 1 Username FROM users JOIN assignments ON users.UserID = assignments.UserID " +
                                "JOIN laptops ON laptops.LaptopID = assignments.LaptopID WHERE [Service Tag] = @ServiceTag ORDER BY assignmentID DESC) " +
                                "WHERE laptops.[Service Tag] = @ServiceTag AND NOT EXISTS (SELECT 1 FROM assignments WHERE LaptopID = laptops.LaptopID AND AssignmentDate IS NOT NULL " +
                                "AND ReturnDate IS NULL)";*/
                            string assignQuery = "INSERT INTO assignments (LaptopID, Username, AssignmentDate) SELECT laptops.LaptopID, users.Username, (SELECT TOP 1 AssignmentDate" +
                                " FROM assignments JOIN laptops ON laptops.LaptopID = assignments.LaptopID WHERE laptops.[Service Tag] = @ServiceTag ORDER BY AssignmentDate DESC) AS AssignmentDate FROM laptops " +
                                "JOIN users ON users.Username = (SELECT TOP 1 users.Username FROM users JOIN assignments ON users.Username = assignments.Username " +
                                "JOIN laptops ON laptops.LaptopID = assignments.LaptopID WHERE [Service Tag] = @ServiceTag ORDER BY assignmentID DESC) " +
                                "WHERE laptops.[Service Tag] = @ServiceTag AND NOT EXISTS (SELECT 1 FROM assignments WHERE LaptopID = laptops.LaptopID AND AssignmentDate IS NOT NULL " +
                                "AND ReturnDate IS NULL)";
                            using (SqlCommand assignCommand = new SqlCommand(assignQuery, connection, transaction))
                            {
                                assignCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                                assignCommand.ExecuteNonQuery();
                            }
                            transaction.Commit();
                            MessageBox.Show("Laptop information updated!");
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction if an exception occurs
                            transaction.Rollback();
                            MessageBox.Show("Error: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void addServiceButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Check if [Service Tag] is provided
                string serviceTag = serviceTagInput2.Text.Trim();
                if (string.IsNullOrEmpty(serviceTag))
                {
                    MessageBox.Show("Please enter a [Service Tag].");
                    return;
                }

                // Check if ServiceNote is provided
                string serviceNote = serviceNoteInput.Text.Trim();
                if (string.IsNullOrEmpty(serviceNote))
                {
                    MessageBox.Show("Please enter a ServiceNote.");
                    return;
                }

                // Check if ServicedBy is provided
                string servicedBy = servicedByInput.Text.Trim();
                if (string.IsNullOrEmpty(servicedBy))
                {
                    MessageBox.Show("Please enter who serviced this computer.");
                    return;
                }
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Check if service tag exists
                    string checkServiceTag = "SELECT 1 FROM laptops WHERE [Service Tag] = @ServiceTag";
                    using (SqlCommand checkServiceCommand = new SqlCommand(checkServiceTag, connection))
                    {
                        checkServiceCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                        object result = checkServiceCommand.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("Service tag does not exist.");
                            return;
                        }
                    }
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Insert a new row copying the old information
                            string insertQuery = "INSERT INTO laptops ([Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], ServiceNote, UpdatedBy, UpdatedOn, Status) " +
                                "SELECT TOP 1 [Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], ServiceNote, UpdatedBy, UpdatedOn, Status " +
                                "FROM laptops WHERE [Service Tag] = @ServiceTag ORDER BY LaptopID DESC";
                            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                                insertCommand.ExecuteNonQuery();
                            }

                            // Update the existing row with the new sercviceNote and change UpdatedBy and UpdatedOn
                            string updateQuery = $"UPDATE laptops SET ServiceNote = @NewValue, UpdatedBy = @UpdatedBy, UpdatedOn = GETDATE() " +
                                                 $"WHERE LaptopID IN (SELECT TOP 1 LaptopID FROM laptops WHERE [Service Tag] = @ServiceTag ORDER BY LaptopID DESC);";
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@NewValue", serviceNote);
                                updateCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                                updateCommand.Parameters.AddWithValue("@UpdatedBy", servicedBy);
                                updateCommand.ExecuteNonQuery();
                            }
                            // Assign the new updated computer to the same user it had before
                            /*string assignQuery = "INSERT INTO assignments (LaptopID, UserID, AssignmentDate) SELECT laptops.LaptopID, users.UserID, assignments.AssignmentDate FROM laptops " +
                                "JOIN users ON users.Username = (SELECT TOP 1 Username FROM users JOIN assignments ON users.UserID = assignments.UserID " +
                                "JOIN laptops ON laptops.LaptopID = assignments.LaptopID WHERE [Service Tag] = @ServiceTag ORDER BY assignmentID DESC) " +
                                "WHERE laptops.[Service Tag] = @ServiceTag AND NOT EXISTS (SELECT 1 FROM assignments WHERE LaptopID = laptops.LaptopID AND AssignmentDate IS NOT NULL " +
                                "AND ReturnDate IS NULL)";*/
                              string assignQuery = "INSERT INTO assignments (LaptopID, Username, AssignmentDate) SELECT laptops.LaptopID, users.Username, (SELECT TOP 1 AssignmentDate" +
                                " FROM assignments JOIN laptops ON laptops.LaptopID = assignments.LaptopID WHERE laptops.[Service Tag] = @ServiceTag ORDER BY AssignmentDate DESC) AS AssignmentDate FROM laptops " +
                                "JOIN users ON users.Username = (SELECT TOP 1 users.Username FROM users JOIN assignments ON users.Username = assignments.Username " +
                                "JOIN laptops ON laptops.LaptopID = assignments.LaptopID WHERE [Service Tag] = @ServiceTag ORDER BY assignmentID DESC) " +
                                "WHERE laptops.[Service Tag] = @ServiceTag AND NOT EXISTS (SELECT 1 FROM assignments WHERE LaptopID = laptops.LaptopID AND AssignmentDate IS NOT NULL " +
                                "AND ReturnDate IS NULL)";
                            using (SqlCommand assignCommand = new SqlCommand(assignQuery, connection, transaction))
                            {
                                assignCommand.Parameters.AddWithValue("@ServiceTag", serviceTag);
                                assignCommand.ExecuteNonQuery();
                            }
                            transaction.Commit();
                            MessageBox.Show("Laptop information updated!");
                        }
                        catch (Exception ex)
                        {
                            // Rollback the transaction if an exception occurs
                            transaction.Rollback();
                            MessageBox.Show("Error: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void featureInput_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (featureInput.Text == "[HD Size]" || featureInput.Text == "RAM")
            {
                newValueInput.MaxLength = 6;
            }
            else
            {
                newValueInput.MaxLength = 20;
            }
        }
    }
}
