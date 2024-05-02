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
    public partial class Form7 : Form
    {
        // Store service tag in a variable
        private String serviceTag;
        public Form7(string serviceTag)
        {
            InitializeComponent();
            this.serviceTag = serviceTag;
        }

        private string connectionString = "Data Source=FAS-HYHGPH2; Initial Catalog=FASComputers; Integrated Security=True";
        // Fill the datagridview with all service history of a computer
        private void Form7_Load(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT [Service Tag], [Date Purchased], [Warranty Expiration], RAM, [HD Size], OS, [Created On], ServiceNote, UpdatedBy, UpdatedOn " +
                           "FROM laptops WHERE [Service Tag] = @ServiceTag";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ServiceTag", serviceTag);
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
                MessageBox.Show(ex.Message);
            }
        }
    }
}
