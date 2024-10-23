using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Eğitim
{
    public partial class Form1 : Form
    {
        private int polygonSides = 3; // Başlangıçta 3 kenarlı bir çokgen (üçgen)
        private float lineWidth = 2;  // Çizgi kalınlığı
        private float rotationAngle = 0; // Döndürme açısı
        private float scaleFactor = 1; // Ölçeklendirme faktörü
        private bool isDragging = false; // Sürükleme durumu
        private Point dragStart; // Sürükleme başlangıç noktası
        private List<PointF> polygonPoints;
        private Color[] edgeColors; // Her bir kenar için renk dizisi
        private Color fillColor = Color.LightGray; // Çokgenin içine uygulanacak dolgu rengi

        public Form1()
        {
            InitializeComponent();
            polygonPoints = new List<PointF>();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // PictureBox özellikleri
            pictureBox1.BackColor = Color.White;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;

            // Varsayılan çokgenin çizilmesi
            UpdatePolygonPoints();
            pictureBox1.Paint += new PaintEventHandler(pictureBox1_Paint);

            edgeColors = new Color[polygonSides]; // Kenar sayısı kadar renk tutacak
            for (int i = 0; i < polygonSides; i++)
            {
                edgeColors[i] = Color.Black; // Varsayılan olarak tüm kenarlar siyah
            }

            // TrackBar olayını bağlayalım
            trackBar.Scroll += new EventHandler(trackBar_Scroll);
            comboBoxMode.SelectedIndexChanged += new EventHandler(comboBoxMode_SelectedIndexChanged);

            // NumericUpDown için minimum değeri belirleyelim
            numericUpDownSides.Minimum = 3;
            numericUpDownSides.Value = 3; // Başlangıç değeri 3 olsun

            // NumericUpDown için olay işleyiciyi ekleyelim (eksik olan bu kısmı ekliyoruz)
            numericUpDownSides.ValueChanged += new EventHandler(numericUpDownSides_ValueChanged);

            // PictureBox sürükleme olayları
            pictureBox1.MouseDown += new MouseEventHandler(pictureBox1_MouseDown);
            pictureBox1.MouseMove += new MouseEventHandler(pictureBox1_MouseMove);
            pictureBox1.MouseUp += new MouseEventHandler(pictureBox1_MouseUp);

            // Renk seçimi için bir buton ya da ComboBox ekleyebiliriz
            buttonSelectColor.Click += new EventHandler(buttonSelectColor_Click); // Renk seçimi
        }
        // Renk seçme işlemi
        private void buttonSelectColor_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                // Seçilen rengi tüm kenarlara uygula
                for (int i = 0; i < edgeColors.Length; i++)
                {
                    edgeColors[i] = colorDialog.Color;
                }

                // Çokgeni yeniden çiz
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (polygonPoints.Count > 2)
            {
                Graphics g = e.Graphics;

                // 1. Adım: Çokgenin içini dolgu rengi ile doldur
                Brush fillBrush = new SolidBrush(fillColor);  // Dolgu rengi seçiliyor
                g.FillPolygon(fillBrush, polygonPoints.ToArray());

                // 2. Adım: Kenarların çizilmesi (her kenar farklı renklerle)
                for (int i = 0; i < polygonSides; i++)
                {
                    // Her kenar için bir renk belirlenmiş
                    Pen edgePen = new Pen(edgeColors[i], lineWidth);  // Kenar rengi ve çizgi kalınlığı
                    PointF p1 = polygonPoints[i];
                    PointF p2 = polygonPoints[(i + 1) % polygonSides]; // Son kenarı ilk noktaya bağla
                    g.DrawLine(edgePen, p1, p2);
                }

                // 3. Adım: Köşe noktalarının çizilmesi (mevcut işlev)
                foreach (PointF p in polygonPoints)
                {
                    g.FillEllipse(Brushes.Red, p.X - 3, p.Y - 3, 6, 6); // Noktalar küçük kırmızı dairelerle gösteriliyor
                }

                // 4. Adım: Alan ve çevre gibi bilgilerin ekrana yazdırılması (mevcut işlev)
                double area = CalculatePolygonArea(polygonPoints.ToArray());
                double perimeter = CalculatePolygonPerimeter(polygonPoints.ToArray());
                double interiorAngle = CalculateInteriorAngle(polygonSides);

                // Alanı ekranda göster
                g.DrawString($"Alan: {area:F2} piksel^2", new Font("Arial", 12), Brushes.Blue, new Point(10, 10));

                // Çevreyi ekranda göster
                g.DrawString($"Çevre: {perimeter:F2} piksel", new Font("Arial", 12), Brushes.Blue, new Point(10, 30));

                // İç açıyı ekranda göster
                g.DrawString($"İç Açı: {interiorAngle:F2} derece", new Font("Arial", 12), Brushes.Blue, new Point(10, 50));

                // 5. Adım: Ağırlık merkezini çizdirme (mevcut işlev)
                PointF centroid = CalculateCentroid(polygonPoints.ToArray());
                g.FillEllipse(Brushes.Green, centroid.X - 3, centroid.Y - 3, 6, 6); // Ağırlık merkezini küçük yeşil bir daire ile göster
                g.DrawString("Ağırlık Merkezi", new Font("Arial", 10), Brushes.Green, new PointF(centroid.X + 5, centroid.Y + 5));
            }
        }


        private void buttonSelectFillColor_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                fillColor = colorDialog.Color;
                pictureBox1.Invalidate(); // Çokgeni yeniden çizdir
            }
        }




        // Çokgen noktalarının güncellenmesi
        // Çokgen noktalarını güncelle
        private void UpdatePolygonPoints()
        {
            polygonPoints.Clear();
            float centerX = pictureBox1.Width / 2;
            float centerY = pictureBox1.Height / 2;
            float radius = Math.Min(centerX, centerY) - 10; // Kenarları ekrandan taşmasın diye

            // Kenar sayısına göre noktaları hesapla
            for (int i = 0; i < polygonSides; i++)
            {
                float angle = (float)(i * 2 * Math.PI / polygonSides);
                float x = centerX + radius * (float)Math.Cos(angle);
                float y = centerY + radius * (float)Math.Sin(angle);
                polygonPoints.Add(new PointF(x, y));
            }

            // Çokgeni yeniden çiz
            pictureBox1.Invalidate();
        }

        // numericUpDownSides'ın değeri değiştiğinde çokgeni güncelle
        private void numericUpDownSides_ValueChanged(object sender, EventArgs e)
        {
            // Yeni kenar sayısını al
            polygonSides = (int)numericUpDownSides.Value;

            // edgeColors dizisini yeniden boyutlandır
            edgeColors = new Color[polygonSides];
            for (int i = 0; i < polygonSides; i++)
            {
                edgeColors[i] = Color.Black; // Varsayılan olarak tüm kenarlar siyah
            }

            // Çokgen köşe noktalarını güncelle
            UpdatePolygonPoints();

            // Çokgeni yeniden çiz
            pictureBox1.Invalidate();
            pictureBox1.Update();
        }



        // Kullanıcı ComboBox ile mod seçtiğinde TrackBar ayarlarını güncelle
        private void comboBoxMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxMode.SelectedItem.ToString() == "Çizgi Kalınlığı")
            {
                trackBar.Minimum = 1;
                trackBar.Maximum = 10;
                trackBar.Value = (int)lineWidth; // Mevcut çizgi kalınlığına ayarla
            }
            else if (comboBoxMode.SelectedItem.ToString() == "Döndürme")
            {
                trackBar.Minimum = 0;
                trackBar.Maximum = 360;
                // Açıyı derecelere çevirip kontrol edelim ki değer aralık dışında olmasın
                int rotationDegrees = (int)(rotationAngle * 180 / Math.PI);
                if (rotationDegrees < trackBar.Minimum) rotationDegrees = trackBar.Minimum;
                if (rotationDegrees > trackBar.Maximum) rotationDegrees = trackBar.Maximum;
                trackBar.Value = rotationDegrees; // Mevcut açıyı dereceye çevir
            }
            else if (comboBoxMode.SelectedItem.ToString() == "Ölçeklendirme")
            {
                trackBar.Minimum = 1;
                trackBar.Maximum = 20;
                trackBar.Value = (int)(scaleFactor * 10); // Ölçeklendirme faktörünü TrackBar'a yansıt
            }
        }


        // TrackBar hareket ettiğinde ilgili işlemi yap
        private void trackBar_Scroll(object sender, EventArgs e)
        {
            if (comboBoxMode.SelectedItem != null)
            {
                if (comboBoxMode.SelectedItem.ToString() == "Çizgi Kalınlığı")
                {
                    lineWidth = trackBar.Value;
                    pictureBox1.Invalidate();  // Çizimi yeniden oluştur
                    pictureBox1.Update();
                }
                else if (comboBoxMode.SelectedItem.ToString() == "Döndürme")
                {
                    rotationAngle = (float)(trackBar.Value * Math.PI / 180); // Dereceyi radian'a çevir
                    RotatePolygon(rotationAngle); // Çokgeni döndür
                    pictureBox1.Invalidate();  // Çizimi yeniden oluştur
                    pictureBox1.Update();
                }
                else if (comboBoxMode.SelectedItem.ToString() == "Ölçeklendirme")
                {
                    scaleFactor = (float)trackBar.Value / 10.0f; // TrackBar'dan gelen değeri ölçeklendirme faktörü yap
                    ScalePolygon(scaleFactor); // Çokgeni ölçeklendir
                    pictureBox1.Invalidate();  // Çizimi yeniden oluştur
                    pictureBox1.Update();
                }
            }
        }



        // Fare ile sürüklemeye başlama
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            dragStart = e.Location; // İlk tıklama noktasını kaydet
            isDragging = true; // Sürükleme moduna geç
        }

        // Fare hareket ettikçe çokgeni sürükleme
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging) // Sürükleme işlemi aktifse
            {
                float dx = e.X - dragStart.X; // Hareket mesafesi x ekseni boyunca
                float dy = e.Y - dragStart.Y; // Hareket mesafesi y ekseni boyunca

                for (int i = 0; i < polygonPoints.Count; i++)
                {
                    // Tüm noktaları hareket mesafesi kadar taşı
                    polygonPoints[i] = new PointF(polygonPoints[i].X + dx, polygonPoints[i].Y + dy);
                }

                dragStart = e.Location; // Yeni başlangıç noktasını güncelle
                pictureBox1.Invalidate(); // Yeniden çizdir
            }
        }

        // Fareyi bıraktığında sürüklemeyi bitir
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false; // Sürükleme işlemini durdur
        }

        

        // Çokgenin alanını hesaplayan fonksiyon (Gauss Alan Formülü)
        private double CalculatePolygonArea(PointF[] points)
        {
            int numPoints = points.Length;
            double area = 0;

            for (int i = 0; i < numPoints; i++)
            {
                PointF current = points[i];
                PointF next = points[(i + 1) % numPoints]; // Son nokta ile ilk nokta arasındaki çizgi için mod kullanılıyor

                area += (current.X * next.Y) - (next.X * current.Y); // Doğru sıralama
            }

            area = Math.Abs(area) / 2.0;
            return area;
        }

        // Çokgenin çevresini hesaplayan fonksiyon
        private double CalculatePolygonPerimeter(PointF[] points)
        {
            int numPoints = points.Length;
            double perimeter = 0;

            for (int i = 0; i < numPoints; i++)
            {
                PointF current = points[i];
                PointF next = points[(i + 1) % numPoints]; // Son noktayı ilk noktaya bağlayarak çevreyi tamamla
                double distance = Math.Sqrt(Math.Pow(next.X - current.X, 2) + Math.Pow(next.Y - current.Y, 2));
                perimeter += distance;
            }

            return perimeter;
        }

        // Çokgenin iç açılarını hesaplayan fonksiyon
        private double CalculateInteriorAngle(int sides)
        {
            if (sides < 3) return 0; // En az 3 kenarlı olmalı
            return (double)(180 * (sides - 2)) / sides;
        }

        // Çokgenin ağırlık merkezini hesaplayan fonksiyon
        private PointF CalculateCentroid(PointF[] points)
        {
            int numPoints = points.Length;
            float centroidX = 0, centroidY = 0;

            for (int i = 0; i < numPoints; i++)
            {
                centroidX += points[i].X;
                centroidY += points[i].Y;
            }

            centroidX /= numPoints; // Tüm noktaların ortalaması doğru hesaplanıyor
            centroidY /= numPoints;

            return new PointF(centroidX, centroidY);
        }

        // Verilen açı ile çokgeni döndürme
        private void RotatePolygon(float angle)
        {
            float centerX = pictureBox1.Width / 2;
            float centerY = pictureBox1.Height / 2;

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                float x = polygonPoints[i].X - centerX; // Merkeze göre normalize et
                float y = polygonPoints[i].Y - centerY;

                // Noktayı döndür
                float rotatedX = (float)(x * Math.Cos(angle) - y * Math.Sin(angle));
                float rotatedY = (float)(x * Math.Sin(angle) + y * Math.Cos(angle));

                // Yeni pozisyonu merkez etrafında döndürülmüş şekilde güncelle
                polygonPoints[i] = new PointF(rotatedX + centerX, rotatedY + centerY);
            }

            pictureBox1.Invalidate();
        }

        // Çokgeni ölçeklendirme (büyütme/küçültme)
        private void ScalePolygon(float scaleFactor)
        {
            float centerX = pictureBox1.Width / 2;
            float centerY = pictureBox1.Height / 2;

            for (int i = 0; i < polygonPoints.Count; i++)
            {
                float x = polygonPoints[i].X - centerX;
                float y = polygonPoints[i].Y - centerY;

                polygonPoints[i] = new PointF(x * scaleFactor + centerX, y * scaleFactor + centerY);
            }

            pictureBox1.Invalidate();
        }
    }
}
