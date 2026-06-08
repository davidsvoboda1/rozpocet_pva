// Autor: David Svoboda | Třída: 3.C | Předmět: Programování a vývoj aplikací | Program: Rozpočet financí

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace RozpocetFinanci
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal class MainForm : Form
    {
        private readonly List<(int id, decimal castka, string kategorie, DateTime datum, string popis, bool prijem)> _data
            = new List<(int, decimal, string, DateTime, string, bool)>();
        private int _nextId = 1;

        private static readonly Color CMain = Color.FromArgb(30, 30, 50);
        private static readonly Color CPanel = Color.FromArgb(42, 42, 68);
        private static readonly Color CPole = Color.FromArgb(55, 55, 85);
        private static readonly Color CText = Color.FromArgb(220, 220, 240);
        private static readonly Color CZel = Color.FromArgb(39, 174, 96);
        private static readonly Color CRud = Color.FromArgb(192, 57, 43);

        private RadioButton _rbPrijem, _rbVydaj;
        private TextBox _txtCastka, _txtPopis;
        private ComboBox _cmbKategorie, _cmbFilter;
        private DateTimePicker _dtpDatum;
        private DataGridView _grid;
        private Label _lblPrijmy, _lblVydaje, _lblZustatek;

        public MainForm()
        {
            Text = "Rozpočet financí";
            Size = new Size(860, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = CMain;
            Font = new Font("Segoe UI", 9f);

            VytvorTabulku();
            VytvorStatus();
            VytvorFilter();
            VytvorVstup();

            ObnovTabulku(null);
            ObnovStatus();
        }

        private Label Lbl(string t, Point p) =>
            new Label { Text = t, Location = p, AutoSize = true, ForeColor = CText, BackColor = Color.Transparent };

        private TextBox Txt(Point p, int w) =>
            new TextBox { Location = p, Width = w, BackColor = CPole, ForeColor = CText, BorderStyle = BorderStyle.FixedSingle };

        private Button Btn(string t, Point p, int w, Color c) =>
            new Button { Text = t, Location = p, Width = w, Height = 28, BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand };

        private void VytvorVstup()
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 160, BackColor = CPanel };

            var lblN = new Label { Text = "PŘIDAT TRANSAKCI", Location = new Point(10, 8), AutoSize = true, ForeColor = Color.FromArgb(150, 180, 255), Font = new Font("Segoe UI", 8f, FontStyle.Bold), BackColor = Color.Transparent };

            _rbPrijem = new RadioButton { Text = "Příjem", Location = new Point(10, 32), Checked = true, AutoSize = true, ForeColor = CZel, BackColor = Color.Transparent };
            _rbVydaj = new RadioButton { Text = "Výdaj", Location = new Point(85, 32), AutoSize = true, ForeColor = CRud, BackColor = Color.Transparent };

            _txtCastka = Txt(new Point(10, 68), 100);
            _txtCastka.KeyPress += PriStiskuKlavesyCastka;

            _cmbKategorie = new ComboBox { Location = new Point(200, 68), Width = 140, DropDownStyle = ComboBoxStyle.DropDown, BackColor = CPole, ForeColor = CText, FlatStyle = FlatStyle.Flat };
            foreach (string k in new[] { "Jídlo", "Bydlení", "Doprava", "Zábava", "Zdraví", "Plat", "Ostatní" })
                _cmbKategorie.Items.Add(k);
            _cmbKategorie.Text = "Jídlo";

            _dtpDatum = new DateTimePicker { Location = new Point(355, 68), Width = 130, Format = DateTimePickerFormat.Short };

            _txtPopis = Txt(new Point(10, 125), 460);
            _txtPopis.KeyDown += PriStiskuKlavesyPopis;

            var btnPridat = Btn("Přidat", new Point(500, 65), 80, CZel);
            var btnSmazat = Btn("Smazat", new Point(588, 65), 80, CRud);
            var btnVymazat = Btn("Smazat vše", new Point(500, 122), 168, Color.FromArgb(80, 80, 110));
            btnPridat.Click += PriKliknutiPridat;
            btnSmazat.Click += PriKliknutiSmazat;
            btnVymazat.Click += PriKliknutiVymazatVse;

            pnl.Controls.AddRange(new Control[] {
                lblN, _rbPrijem, _rbVydaj,
                Lbl("Částka (Kč):", new Point(10, 52)),  _txtCastka,
                Lbl("Kategorie:",   new Point(200, 52)), _cmbKategorie,
                Lbl("Datum:",       new Point(355, 52)), _dtpDatum,
                Lbl("Popis:",       new Point(10, 109)), _txtPopis,
                btnPridat, btnSmazat, btnVymazat
            });
            Controls.Add(pnl);
        }

        private void VytvorFilter()
        {
            var pnl = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = Color.FromArgb(35, 35, 58) };
            var lbl = new Label { Text = "Filtr:", Location = new Point(10, 9), AutoSize = true, ForeColor = CText, BackColor = Color.Transparent };
            _cmbFilter = new ComboBox { Location = new Point(45, 5), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CPole, ForeColor = CText, FlatStyle = FlatStyle.Flat };
            _cmbFilter.Items.Add("— Vše —");
            _cmbFilter.SelectedIndex = 0;
            _cmbFilter.SelectedIndexChanged += PriZmeneFiltru;
            pnl.Controls.Add(lbl);
            pnl.Controls.Add(_cmbFilter);
            Controls.Add(pnl);
        }

        private void VytvorTabulku()
        {
            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = CMain,
                GridColor = Color.FromArgb(60, 60, 90),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = CPanel;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(150, 180, 255);
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _grid.DefaultCellStyle.BackColor = Color.FromArgb(38, 38, 60);
            _grid.DefaultCellStyle.ForeColor = CText;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 80, 140);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(44, 44, 70);
            _grid.Columns.Add("Id", "ID");
            _grid.Columns.Add("Typ", "Typ");
            _grid.Columns.Add("Castka", "Částka (Kč)");
            _grid.Columns.Add("Kat", "Kategorie");
            _grid.Columns.Add("Datum", "Datum");
            _grid.Columns.Add("Popis", "Popis");
            _grid.Columns["Id"].FillWeight = 5;
            _grid.Columns["Typ"].FillWeight = 10;
            _grid.Columns["Castka"].FillWeight = 14;
            _grid.Columns["Kat"].FillWeight = 16;
            _grid.Columns["Datum"].FillWeight = 13;
            _grid.Columns["Popis"].FillWeight = 42;
            _grid.CellFormatting += PriFormatovaniBunky;
            Controls.Add(_grid);
        }

        private void VytvorStatus()
        {
            var pnl = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = CPanel };
            _lblPrijmy = new Label { Location = new Point(10, 8), AutoSize = true, ForeColor = CZel, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _lblVydaje = new Label { Location = new Point(190, 8), AutoSize = true, ForeColor = CRud, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _lblZustatek = new Label { Location = new Point(370, 8), AutoSize = true, ForeColor = CText, BackColor = Color.Transparent, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            pnl.Controls.AddRange(new Control[] { _lblPrijmy, _lblVydaje, _lblZustatek });
            Controls.Add(pnl);
        }

        private void PriKliknutiPridat(object sender, EventArgs e)
        {
            if (!decimal.TryParse(_txtCastka.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal castka) || castka <= 0)
            {
                MessageBox.Show("Zadejte platnou kladnou částku.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(_cmbKategorie.Text))
            {
                MessageBox.Show("Kategorie nesmí být prázdná.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _data.Add((_nextId++, castka, _cmbKategorie.Text.Trim(), _dtpDatum.Value.Date, _txtPopis.Text.Trim(), _rbPrijem.Checked));
            _txtCastka.Clear();
            _txtPopis.Clear();
            AktualizujFiltr();
            ObnovTabulku(ZiskejAktualniFilter());
            ObnovStatus();
        }

        private void PriKliknutiSmazat(object sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Vyberte transakci ke smazání.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int id = Convert.ToInt32(_grid.SelectedRows[0].Cells["Id"].Value);
            _data.RemoveAll(t => t.id == id);
            AktualizujFiltr();
            ObnovTabulku(ZiskejAktualniFilter());
            ObnovStatus();
        }

        private void PriKliknutiVymazatVse(object sender, EventArgs e)
        {
            if (_data.Count == 0) return;
            if (MessageBox.Show("Smazat všechny transakce?", "Potvrzení", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _data.Clear();
            _nextId = 1;
            AktualizujFiltr();
            ObnovTabulku(null);
            ObnovStatus();
        }

        private void PriZmeneFiltru(object sender, EventArgs e)
        {
            ObnovTabulku(ZiskejAktualniFilter());
        }

        private void PriStiskuKlavesyCastka(object sender, KeyPressEventArgs e)
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.' && e.KeyChar != '\b')
                e.Handled = true;
        }

        private void PriStiskuKlavesyPopis(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { PriKliknutiPridat(sender, e); e.SuppressKeyPress = true; }
        }

        private void PriFormatovaniBunky(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (_grid.Columns[e.ColumnIndex].Name == "Typ" && e.Value != null)
            {
                e.CellStyle.ForeColor = e.Value.ToString() == "Příjem" ? CZel : CRud;
                e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
        }

        private string ZiskejAktualniFilter() =>
            _cmbFilter.SelectedIndex == 0 ? null : _cmbFilter.SelectedItem?.ToString();

        private void AktualizujFiltr()
        {
            string aktualni = ZiskejAktualniFilter();
            _cmbFilter.Items.Clear();
            _cmbFilter.Items.Add("— Vše —");
            foreach (string k in _data.Select(t => t.kategorie).Distinct().OrderBy(x => x))
                _cmbFilter.Items.Add(k);
            int idx = aktualni != null ? _cmbFilter.Items.IndexOf(aktualni) : 0;
            _cmbFilter.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void ObnovTabulku(string filtr)
        {
            _grid.Rows.Clear();
            var seznam = string.IsNullOrEmpty(filtr)
                ? _data
                : _data.Where(t => t.kategorie == filtr).ToList();
            foreach (var t in seznam)
                _grid.Rows.Add(t.id, t.prijem ? "Příjem" : "Výdaj",
                    t.castka.ToString("N2", System.Globalization.CultureInfo.CurrentCulture),
                    t.kategorie, t.datum.ToString("dd.MM.yyyy"), t.popis);
        }

        private void ObnovStatus()
        {
            decimal prijmy = _data.Where(t => t.prijem).Sum(t => t.castka);
            decimal vydaje = _data.Where(t => !t.prijem).Sum(t => t.castka);
            decimal zust = prijmy - vydaje;
            _lblPrijmy.Text = $"Příjmy: {prijmy:N2} Kč";
            _lblVydaje.Text = $"Výdaje: {vydaje:N2} Kč";
            _lblZustatek.Text = $"Zůstatek: {zust:N2} Kč";
            _lblZustatek.ForeColor = zust >= 0 ? CZel : CRud;
        }
    }
}
