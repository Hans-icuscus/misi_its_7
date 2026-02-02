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
using System.Windows.Forms.DataVisualization.Charting;

namespace AplToko
{
    public partial class FormLaporan : Form
    {
        public FormLaporan()
        {
            InitializeComponent();
        }
        private void HitungTotalMingguan()
        {
            using (SqlConnection conn = Koneksi.GetConnection())
            {
                conn.Open();
                string query = "SELECT SUM(TotalHarga) FROM Penjualan WHERE Tanggal >= DATEADD(day, -7, GETDATE())";
                SqlCommand cmd = new SqlCommand(query, conn);
                var hasil = cmd.ExecuteScalar();

                decimal total = hasil != DBNull.Value ? Convert.ToDecimal(hasil) : 0;
                lblTotalSeminggu.Text = $"Total penjualan seminggu terakhir: Rp {total:N0}";
            }
        }
        private void TampilkanPenjualanPerHari()
        {
            chartPenjualan.Series.Clear();
            chartPenjualan.ChartAreas.Clear();
            ChartArea area = new ChartArea("AreaUtama");
            chartPenjualan.ChartAreas.Add(area);
            Series series = new Series("Penjualan Per Hari");
            series.ChartType = SeriesChartType.Column;
            series.XValueType = ChartValueType.Date;
            chartPenjualan.Series.Add(series);
            using (SqlConnection conn = Koneksi.GetConnection())
            {
                conn.Open();
                string query = @"SELECT CAST(Tanggal AS DATE) AS Tgl, SUM(TotalHarga) AS Total 
                         FROM Penjualan 
                         WHERE Tanggal BETWEEN @tglAwal AND @tglAkhir
                         GROUP BY CAST(Tanggal AS DATE) ORDER BY Tgl";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tglAwal", dtpDari.Value.Date);
                cmd.Parameters.AddWithValue("@tglAkhir", dtpSampai.Value.Date);

                SqlDataReader reader = cmd.ExecuteReader();
                decimal totalSeluruh = 0;

                while (reader.Read())
                {
                    DateTime tanggal = Convert.ToDateTime(reader["Tgl"]);
                    decimal total = Convert.ToDecimal(reader["Total"]);
                    series.Points.AddXY(tanggal.ToString("dd-MM"), total);
                }
                reader.Close();
            }
            chartPenjualan.Titles.Clear();
            chartPenjualan.Titles.Add("Grafik Penjualan per Hari");
        }


        private void TampilkanPenjualanPerKategori()
        {
            chartPenjualan.Series.Clear();
            chartPenjualan.ChartAreas.Clear();
            ChartArea area = new ChartArea("AreaKategori");
            chartPenjualan.ChartAreas.Add(area);
            Series series = new Series("Penjualan per Kategori");
            series.ChartType = SeriesChartType.Pie;
            chartPenjualan.Series.Add(series);
            using (SqlConnection conn = Koneksi.GetConnection())
            {
                conn.Open();
                string query = @"SELECT k.NamaKategori, SUM(pd.Subtotal) AS Total FROM PenjualanDetail pd JOIN Produk p ON p.Id = pd.ProdukId JOIN Kategori k ON k.Id = p.KategoriId GROUP BY k.NamaKategori";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string kategori = reader["NamaKategori"].ToString();
                    decimal total = Convert.ToDecimal(reader["Total"]);
                    series.Points.AddXY(kategori, total);
                }
                reader.Close();
            }
            chartPenjualan.Titles.Clear();
            chartPenjualan.Titles.Add("Grafik Penjualan per Kategori");
        }

        private void FormLaporan_Load(object sender, EventArgs e)
        {
            HitungTotalMingguan();
            TampilkanPenjualanPerKategori();
        }


        private void cmbTipeLaporan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTipeLaporan.SelectedItem.ToString() == "Harian")
                TampilkanPenjualanPerHari();
            else
                TampilkanPenjualanPerKategori();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (cmbTipeLaporan.SelectedItem != null)
            {
                string pilihan = cmbTipeLaporan.SelectedItem.ToString();

                if (pilihan == "Harian")
                {
                    TampilkanPenjualanPerHari();
                }
                else if (pilihan == "Kategori")
                {
                    TampilkanPenjualanPerKategori();
                }
            }

        }

        private void btnCetak_Click(object sender, EventArgs e)
        {
            printPreviewDialog1.Document = printDocument1;
            printPreviewDialog1.ShowDialog();
        }

        private void dtpDariTanggal_ValueChanged(object sender, EventArgs e)
        {

        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Bitmap bmp = new Bitmap(this.chartPenjualan.Width, this.chartPenjualan.Height);
            chartPenjualan.DrawToBitmap(bmp, new Rectangle(0, 0, chartPenjualan.Width, chartPenjualan.Height));

            e.Graphics.DrawString("LAPORAN PENJUALAN", new Font("Arial", 18, FontStyle.Bold), Brushes.Black, new Point(100, 50));

            e.Graphics.DrawImage(bmp, new Point(100, 100));
        }
    }
}
