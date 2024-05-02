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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CompTrack
{
    public partial class Form2 : Form
    {
        private bool usersCopied = false;
        public Form2()
        {
            InitializeComponent();
            this.FormClosing += Form2_FormClosing;
        }

        private string connectionString = "Data Source=FAS-HYHGPH2; Initial Catalog=FASComputers; Integrated Security=True";
        private string fasConnectionString = "Data Source=FAS-SQL\\SQLEXPRESS; Initial Catalog=FAS; User ID=readonly; Password=readonly";

        private void Form2_Load(object sender, EventArgs e)
        {
            if (!usersCopied)
            {
                try
                {
                    using (SqlConnection fasConnection = new SqlConnection(fasConnectionString))
                    using (SqlConnection fasComputersConnection = new SqlConnection(connectionString))
                    {
                        fasConnection.Open();
                        fasComputersConnection.Open();

                        string selectQuery = "SELECT emp_id, f_name, l_name, status FROM employee";

                        using (SqlCommand selectCommand = new SqlCommand(selectQuery, fasConnection))
                        using (SqlDataReader reader = selectCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string insertQuery = "INSERT INTO users (Username, FirstName, LastName, status) VALUES (@Username, @FirstName, @LastName, @status)";
                                using (SqlCommand insertCommand = new SqlCommand(insertQuery, fasComputersConnection))
                                {
                                    insertCommand.Parameters.AddWithValue("@Username", reader["emp_id"]);
                                    insertCommand.Parameters.AddWithValue("@FirstName", reader["f_name"]);
                                    insertCommand.Parameters.AddWithValue("@LastName", reader["l_name"]);
                                    insertCommand.Parameters.AddWithValue("@status", reader["status"]);

                                    insertCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    usersCopied = true;
                }
                catch (Exception ex)
                {
                    // Handle exceptions (e.g., log, display error message)
                    MessageBox.Show("Error: " + ex.Message);
                }
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query;
                    connection.Open();
                    query = "WITH RankedEntries AS (SELECT laptops.[Service Tag], CASE WHEN laptops.Status = 'Available' THEN 'Unassigned' ELSE CONCAT(TRIM(users.FirstName), ' ', TRIM(users.LastName)) END AS 'Name', " +
                        "laptops.[Date Purchased], laptops.[Warranty Expiration], laptops.RAM, " +
                        "laptops.[HD Size], laptops.OS, laptops.[Created On], laptops.Status, ROW_NUMBER() OVER (PARTITION BY [Service Tag] ORDER BY assignments.AssignmentID DESC) AS RowNum " +
                        "FROM laptops LEFT JOIN assignments ON laptops.LaptopID = assignments.LaptopID LEFT JOIN users ON users.Username = assignments.Username WHERE laptops.Status = 'Assigned' OR laptops.Status = 'Available') " +
                        "SELECT [Service Tag], Name, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], Status FROM RankedEntries WHERE RowNum = 1;";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Use sql reader to read data from the query
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // check if data exists
                            if (reader.HasRows)
                            {
                                // Create a new data table
                                DataTable dataTable = new DataTable();
                                // Load the data into the dataTable
                                dataTable.Load(reader);
                                // Clear previous columns
                                laptopsDataView.Columns.Clear();
                                // Bind the dataTable to the dataGridView
                                laptopsDataView.DataSource = dataTable;
                            }
                            else
                            {
                                MessageBox.Show("No data found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :" + ex.Message);
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string deleteQuery = "DELETE FROM users";
                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    string resetIdentityColumn = "DBCC CHECKIDENT ('users', RESEED, 0)";
                    using (SqlCommand resetCommand = new SqlCommand(resetIdentityColumn, connection))
                    {
                        resetCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex);
            }
        }
        // Btn to take you to the add computers interface
        private void btnAddComputer_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.ShowDialog();
        }

        // Btn to refresh the laptops table
        private void refreshButton_Click(object sender, EventArgs e)
        {
            Form2_Load(sender, e);
            rbtnShowAll.Checked = true;
        }

        // Btn to take you to the assign users interface
        private void btnAssignUser_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.ShowDialog();
        }

        // Btn to take you to return laptop interface 
        private void btnReturnLaptop_Click(object sender, EventArgs e)
        {
            Form4 form4 = new Form4();
            form4.ShowDialog();
        }

        // Btn to take you to user searching interface
        private void btnSearchUser_Click(object sender, EventArgs e)
        {
            Form5 form5 = new Form5();
            form5.ShowDialog();
        }

        // Btn to update a computer's info or enter a service note
        private void btnUpdateService_Click(object sender, EventArgs e)
        {
            Form6 form6 = new Form6();
            form6.ShowDialog();
        }

        //Btn to filter dataGridView to show all
        private void rbtnShowAll_CheckedChanged(object sender, EventArgs e)
        {
            Form2_Load(sender, e);
        }

        //Btn to filter dataGridView to only show assigned computers
        private void rbtnFilterAssigned_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query;
                    connection.Open();
                    query = "WITH RankedEntries AS (SELECT laptops.[Service Tag], CASE WHEN laptops.Status = 'Available' THEN 'Unassigned' ELSE CONCAT(TRIM(users.FirstName), ' ', TRIM(users.LastName)) END AS 'Name', " +
                        "laptops.[Date Purchased], laptops.[Warranty Expiration], laptops.RAM, " +
                        "laptops.[HD Size], laptops.OS, laptops.[Created On], laptops.Status, ROW_NUMBER() OVER (PARTITION BY [Service Tag] ORDER BY assignments.AssignmentID DESC) AS RowNum " +
                        "FROM laptops LEFT JOIN assignments ON laptops.LaptopID = assignments.LaptopID LEFT JOIN users ON users.Username = assignments.Username WHERE laptops.Status = 'Assigned') " +
                        "SELECT [Service Tag], Name, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], Status FROM RankedEntries WHERE RowNum = 1;";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Use sql reader to read data from the query
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // check if data exists
                            if (reader.HasRows)
                            {
                                // Create a new data table
                                DataTable dataTable = new DataTable();
                                // Load the data into the dataTable
                                dataTable.Load(reader);
                                // Clear previous columns
                                laptopsDataView.Columns.Clear();
                                // Bind the dataTable to the dataGridView
                                laptopsDataView.DataSource = dataTable;
                            }
                            else
                            {
                                MessageBox.Show("No data found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :" + ex.Message);
            }
        }

        //Btn to filter dataGridView to show only unassigned computers
        private void rbtnFilterUnassigned_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query;
                    connection.Open();
                    //if (rbtnFilterUpdated.Checked)
                    //{
                    query = "WITH RankedEntries AS (SELECT laptops.[Service Tag], CASE WHEN laptops.Status = 'Available' THEN 'Unassigned' ELSE CONCAT(TRIM(users.FirstName), ' ', TRIM(users.LastName)) END AS 'Name', " +
                        "laptops.[Date Purchased], laptops.[Warranty Expiration], laptops.RAM, " +
                        "laptops.[HD Size], laptops.OS, laptops.[Created On], laptops.Status, ROW_NUMBER() OVER (PARTITION BY [Service Tag] ORDER BY assignments.AssignmentID DESC) AS RowNum " +
                        "FROM laptops LEFT JOIN assignments ON laptops.LaptopID = assignments.LaptopID LEFT JOIN users ON users.Username = assignments.Username WHERE laptops.Status = 'Available') " +
                        "SELECT [Service Tag], Name, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], Status FROM RankedEntries WHERE RowNum = 1;";
                    //}
                    //else
                    //{
                    //query = "SELECT [Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], ServiceNote, UpdatedBy, UpdatedOn, Status " +
                    //"FROM laptops WHERE Status = 'Available'";
                    //}
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Use sql reader to read data from the query
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // check if data exists
                            if (reader.HasRows)
                            {
                                // Create a new data table
                                DataTable dataTable = new DataTable();
                                // Load the data into the dataTable
                                dataTable.Load(reader);
                                // Clear previous columns
                                laptopsDataView.Columns.Clear();
                                // Bind the dataTable to the dataGridView
                                laptopsDataView.DataSource = dataTable;
                            }
                            else
                            {
                                MessageBox.Show("No data found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :" + ex.Message);
            }
        }
        // Btn to filter data grid view to only show retired computers
        private void rbtnFilterRetired_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string query;
                    connection.Open();
                    query = "WITH RankedUnassignedEntries AS (SELECT laptops.[Service Tag], laptops.[Date Purchased], laptops.[Warranty Expiration], laptops.RAM, " +
                    "laptops.[HD Size], laptops.OS, laptops.[Created On], laptops.Status, ROW_NUMBER() " +
                    "OVER (PARTITION BY laptops.[Service Tag], laptops.ServiceCode ORDER BY laptops.LaptopID DESC) AS RowNum FROM laptops " +
                    "WHERE laptops.Status = 'Retired') " +
                    "SELECT [Service Tag], [Date Purchased], [Warranty Expiration], " +
                    "RAM, [HD Size], OS, [Created On], Status FROM RankedUnassignedEntries WHERE RowNum = 1;";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Use sql reader to read data from the query
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // check if data exists
                            if (reader.HasRows)
                            {
                                // Create a new data table
                                DataTable dataTable = new DataTable();
                                // Load the data into the dataTable
                                dataTable.Load(reader);
                                // Clear previous columns
                                laptopsDataView.Columns.Clear();
                                // Bind the dataTable to the dataGridView
                                laptopsDataView.DataSource = dataTable;
                            }
                            else
                            {
                                MessageBox.Show("No data found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error :" + ex.Message);
            }
        }
        // Radio button to show only the most recently updated version of a computer
        /*private void rbtnFilterUpdated_CheckedChanged(object sender, EventArgs e)
        {
            if (rbtnShowAll.Checked)
            {
                rbtnShowAll_CheckedChanged(sender, e);
            }
            else if (rbtnFilterAssigned.Checked)
            {
                rbtnFilterAssigned_CheckedChanged(sender, e);
            }
            else
            {
                rbtnFilterUnassigned_CheckedChanged(sender, e);
            }
        }*/

        // Radio button to show computers including the versions before they were updated
        /*private void rbtnFilterNotUpdated_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query;
                    if (rbtnShowAll.Checked)
                    {
                        query = "SELECT [Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], ServiceNote, UpdatedBy, UpdatedOn, Status FROM laptops;";
                    }
                    else if (rbtnFilterAssigned.Checked)
                    {
                        query = "SELECT [Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], ServiceNote, UpdatedBy, UpdatedOn, Status " +
                                "FROM laptops WHERE laptops.Status = 'Assigned';";
                    }
                    else
                    {
                        query = "SELECT [Service Tag], ServiceCode, [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], ServiceNote, UpdatedBy, UpdatedOn, Status " +
                                "FROM laptops WHERE Status = 'Available'";
                    }
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            // Check if data exists
                            if (reader.HasRows)
                            {
                                // Create a new datatable
                                DataTable dataTable = new DataTable();
                                // Load the data into datatbale
                                dataTable.Load(reader);
                                // Clear the previous columns
                                laptopsDataView.Columns.Clear();
                                // Link datatable to dg
                                laptopsDataView.DataSource = dataTable;
                            }
                            else
                            {
                                MessageBox.Show("No data found.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }*/
        private int rowIndex;
        private int columnIndex;
        // Handler for right clicking a computer to bring up assignment screen
        private void laptopsDataView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            rowIndex = e.RowIndex;
            columnIndex = e.ColumnIndex;
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string columnName = "Service Tag";
                DataGridViewColumn column = laptopsDataView.Columns[columnName];
                if (column != null)
                {
                    int columnIndex = column.Index;
                    if (e.ColumnIndex == columnIndex)
                    {
                        // Create and display context menu
                        ContextMenuStrip contextMenu = new ContextMenuStrip();

                        // Add menu items
                        ToolStripMenuItem menuServiceHistory = new ToolStripMenuItem("View Service History");
                        ToolStripMenuItem menuAssign = new ToolStripMenuItem("Assign User");
                        ToolStripMenuItem menuReturn = new ToolStripMenuItem("Return/Retire Computer");
                        ToolStripMenuItem menuUpdate = new ToolStripMenuItem("Update or add service note");
                        // Attach click event handlers
                        menuServiceHistory.Click += menuServiceHistory_Click;
                        menuAssign.Click += menuAssign_Click;
                        menuReturn.Click += menuReturn_Click;
                        menuUpdate.Click += menuUpdate_Click;

                        // Add items to context menu
                        contextMenu.Items.Add(menuServiceHistory);
                        contextMenu.Items.Add(menuAssign);
                        contextMenu.Items.Add(menuReturn);
                        contextMenu.Items.Add(menuUpdate);

                        // Show context menu at mouse position
                        Point clientPoint = laptopsDataView.PointToClient(Cursor.Position);
                        contextMenu.Show(laptopsDataView, clientPoint);
                    }
                }
            }
        }
        private void menuServiceHistory_Click(object sender, EventArgs e)
        {
            string serviceTag = laptopsDataView.Rows[rowIndex].Cells[columnIndex].Value.ToString();
            Form7 form7 = new Form7(serviceTag);
            form7.ShowDialog();
        }
        private void menuAssign_Click(object sender, EventArgs e)
        {
            string serviceTag = laptopsDataView.Rows[rowIndex].Cells[columnIndex].Value.ToString();
            Form3 form3 = new Form3(serviceTag);
            form3.ShowDialog();
        }
        private void menuReturn_Click(object sender, EventArgs e)
        {
            string serviceTag = laptopsDataView.Rows[rowIndex].Cells[columnIndex].Value.ToString();
            Form4 form4 = new Form4(serviceTag);
            form4.ShowDialog();
        }
        private void menuUpdate_Click(object sender, EventArgs e)
        {
            string serviceTag = laptopsDataView.Rows[rowIndex].Cells[columnIndex].Value.ToString();
            Form6 form6 = new Form6(serviceTag);
            form6.ShowDialog();
        }
    }
}