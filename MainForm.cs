using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TourAgencyApp
{
    public class MainForm : Form
    {
        private List<Tour> _allTours = new();
        private Image? _placeholder;

        private PictureBox _logoPicture;
        private TextBox _searchBox;
        private ComboBox _typeCombo;
        private ComboBox _sortCombo;
        private CheckBox _actualCheck;
        private Label _totalLabel;
        private FlowLayoutPanel _toursPanel;

        public MainForm()
        {
            InitializeComponents();
            LoadData();
        }

        private void InitializeComponents()
        {
            Text = "Туристическое агентство";
            MinimumSize = new Size(1000, 700);
            Size = new Size(1200, 800);
            BackColor = Color.White;
            StartPosition = FormStartPosition.CenterScreen;

            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(240, 240, 245),
                Padding = new Padding(10, 10, 10, 10)
            };

            _logoPicture = new PictureBox
            {
                Size = new Size(160, 55),
                Location = new Point(10, 12),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");
            if (File.Exists(logoPath))
                _logoPicture.Image = Image.FromFile(logoPath);

            string placeholderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "picture.png");
            if (File.Exists(placeholderPath))
                _placeholder = Image.FromFile(placeholderPath);

            _searchBox = new TextBox
            {
                Location = new Point(185, 27),
                Size = new Size(220, 25),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Поиск..."
            };
            _searchBox.TextChanged += (s, e) => ApplyFilters();

            var typeLabel = new Label
            {
                Text = "Тип:",
                Location = new Point(420, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            _typeCombo = new ComboBox
            {
                Location = new Point(455, 26),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _typeCombo.SelectedIndexChanged += (s, e) => ApplyFilters();

            var sortLabel = new Label
            {
                Text = "Сортировка:",
                Location = new Point(668, 30),
                AutoSize = true,
                Font = new Font("Segoe UI", 10)
            };

            _sortCombo = new ComboBox
            {
                Location = new Point(760, 26),
                Size = new Size(180, 25),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _sortCombo.Items.AddRange(new object[] { "По цене (возр.)", "По цене (убыв.)" });
            _sortCombo.SelectedIndex = 0;
            _sortCombo.SelectedIndexChanged += (s, e) => ApplyFilters();

            _actualCheck = new CheckBox
            {
                Text = "Только актуальные",
                Location = new Point(955, 27),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Checked = false
            };
            _actualCheck.CheckedChanged += (s, e) => ApplyFilters();

            _totalLabel = new Label
            {
                Text = "Итого: 0 руб.",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 120),
                AutoSize = true
            };
            _totalLabel.Location = new Point(topPanel.Width - 220, 30);

            topPanel.SizeChanged += (s, e) =>
            {
                _totalLabel.Location = new Point(topPanel.Width - _totalLabel.Width - 15, 30);
            };

            topPanel.Controls.AddRange(new Control[] {
                _logoPicture, _searchBox, typeLabel, _typeCombo, sortLabel, _sortCombo, _actualCheck, _totalLabel
            });

            _toursPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 248, 252),
                Padding = new Padding(10)
            };

            Controls.Add(_toursPanel);
            Controls.Add(topPanel);
        }

        private void LoadData()
        {
            try
            {
                _allTours = DbHelper.LoadTours();
                DbHelper.LoadTypes(_allTours);

                _typeCombo.Items.Clear();
                _typeCombo.Items.Add("Все типы");
                var typeNames = DbHelper.LoadTypeNames();
                foreach (var t in typeNames)
                    _typeCombo.Items.Add(t);
                _typeCombo.SelectedIndex = 0;

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyFilters()
        {
            string search = _searchBox.Text.Trim().ToLower();
            string selectedType = _typeCombo.SelectedItem?.ToString() ?? "Все типы";
            bool onlyActual = _actualCheck.Checked;
            int sortIndex = _sortCombo.SelectedIndex;

            var filtered = _allTours.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
                filtered = filtered.Where(t =>
                    t.Name.ToLower().Contains(search) ||
                    t.Description.ToLower().Contains(search));

            if (selectedType != "Все типы")
                filtered = filtered.Where(t => t.Types.Contains(selectedType));

            if (onlyActual)
                filtered = filtered.Where(t => t.IsActual);

            if (sortIndex == 0)
                filtered = filtered.OrderBy(t => t.Price);
            else
                filtered = filtered.OrderByDescending(t => t.Price);

            var result = filtered.ToList();

            decimal total = result.Sum(t => t.Price * t.TicketCount);
            _totalLabel.Text = $"Итого: {total:N0} руб.";
            _totalLabel.Location = new Point(this.ClientSize.Width - _totalLabel.Width - 15, 30);

            RenderCards(result);
        }

        private void RenderCards(List<Tour> tours)
        {
            _toursPanel.SuspendLayout();
            foreach (Control c in _toursPanel.Controls)
                c.Dispose();
            _toursPanel.Controls.Clear();

            foreach (var tour in tours)
            {
                var card = CreateCard(tour);
                _toursPanel.Controls.Add(card);
            }

            _toursPanel.ResumeLayout();
        }

        private Panel CreateCard(Tour tour)
        {
            var card = new Panel
            {
                Size = new Size(230, 320),
                Margin = new Padding(8),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var pen = new Pen(Color.FromArgb(200, 200, 215), 1);
                g.DrawRectangle(pen, rect);
            };

            var pic = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(230, 150),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(240, 240, 248)
            };

            if (tour.ImagePreview != null && tour.ImagePreview.Length > 0)
            {
                try
                {
                    using var ms = new MemoryStream(tour.ImagePreview);
                    pic.Image = Image.FromStream(ms);
                }
                catch
                {
                    pic.Image = _placeholder;
                }
            }
            else
            {
                pic.Image = _placeholder;
            }

            var nameLabel = new Label
            {
                Text = tour.Name,
                Location = new Point(5, 155),
                Size = new Size(220, 50),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 60)
            };

            var priceLabel = new Label
            {
                Text = $"{tour.Price:N0} РУБ",
                Location = new Point(5, 205),
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 100, 180)
            };

            var ticketsLabel = new Label
            {
                Text = $"Билетов: {tour.TicketCount}",
                Location = new Point(5, 230),
                AutoSize = true,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            bool isActual = tour.IsActual;
            var statusLabel = new Label
            {
                Text = isActual ? "Актуален" : "Не актуален",
                Location = new Point(5, 255),
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = isActual ? Color.FromArgb(0, 140, 70) : Color.FromArgb(180, 50, 50)
            };

            if (!isActual)
                card.BackColor = Color.FromArgb(248, 245, 245);

            card.Controls.AddRange(new Control[] { pic, nameLabel, priceLabel, ticketsLabel, statusLabel });
            return card;
        }
    }
}
