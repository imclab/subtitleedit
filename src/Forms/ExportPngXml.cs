﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using Nikse.SubtitleEdit.Logic;
using System.Xml;
using System.IO;

namespace Nikse.SubtitleEdit.Forms
{
    public sealed partial class ExportPngXml : Form
    {
        Subtitle _subtitle;
        Color _subtitleColor = Color.White;        
        string _subtitleFontName = "Verdana";
        float _subtitleFontSize = 75.0f;
        Color _borderColor = Color.Black;
        float _borderWidth = 2.0f;
        bool _isLoading = true;

        public ExportPngXml()
        {
            InitializeComponent();
        }

        private static string BdnXmlTimeCode(TimeCode timecode)
        {
            int frames = timecode.Milliseconds / 40; // 40==25fps (1000/25)
            return string.Format("{0:00}:{1:00}:{2:00}:{3:00}", timecode.Hours, timecode.Minutes, timecode.Seconds, frames);
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            SetupImageParameters();
            
            if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                progressBar1.Value = 0;
                progressBar1.Maximum = _subtitle.Paragraphs.Count-1;
                progressBar1.Visible = true;
                const int height = 1080;
                const int width = 1920;
                const int border = 25;
                int imagesSavedCount = 0;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < _subtitle.Paragraphs.Count; i++)
                {                    
                    Bitmap bmp = GenerateImageFromText(_subtitle.Paragraphs[i].Text);
                    string numberString = string.Format("{0:0000}", i + 1);
                    if (bmp != null)
                    {

                        string fileName = Path.Combine(folderBrowserDialog1.SelectedPath, numberString + ".png");
                        bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                        imagesSavedCount++;

                        Paragraph p = _subtitle.Paragraphs[i];
                        //<Event InTC="00:00:24:07" OutTC="00:00:31:13" Forced="False">
                        //  <Graphic Width="696" Height="111" X="612" Y="930">subtitle_exp_0001.png</Graphic>
                        //</Event>
                        sb.AppendLine("<Event InTC=\"" + BdnXmlTimeCode(p.StartTime) + "\" OutTC=\"" + BdnXmlTimeCode(p.EndTime) + "\" Forced=\"False\">");
                        int x = (width - bmp.Width) / 2;
                        int y = height - (bmp.Height + border);
                        sb.AppendLine("  <Graphic Width=\"" + bmp.Width.ToString() + "\" Height=\"" + bmp.Height.ToString() + "\" X=\"" + x.ToString() + "\" Y=\"" + y.ToString() + "\">" + numberString + ".png</Graphic>");
                        sb.AppendLine("</Event>");

                        bmp.Dispose();
                        progressBar1.Value = i;
                    }
                }
                XmlDocument doc = new XmlDocument();
                Paragraph first = _subtitle.Paragraphs[0];
                Paragraph last = _subtitle.Paragraphs[_subtitle.Paragraphs.Count - 1];
                doc.LoadXml("<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine +
                            "<BDN Version=\"0.93\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"BD-03-006-0093b BDN File Format.xsd\">" + Environment.NewLine +
                            "<Description>" + Environment.NewLine +
                            "<Name Title=\"subtitle_exp\" Content=\"\"/>" + Environment.NewLine +
                            "<Language Code=\"eng\"/>" + Environment.NewLine +
                            "<Format VideoFormat=\"1080p\" FrameRate=\"25\" DropFrame=\"False\"/>" + Environment.NewLine +
                            "<Events Type=\"Graphic\" FirstEventInTC=\"" + BdnXmlTimeCode(first.StartTime) + "\" LastEventOutTC=\"" + BdnXmlTimeCode(last.EndTime) + "\" NumberofEvents=\"" + imagesSavedCount.ToString() + "\"/>" + Environment.NewLine +
                            "</Description>" + Environment.NewLine +
                            "<Events>" + Environment.NewLine +
                            "</Events>" + Environment.NewLine +
                            "</BDN>");
                XmlNode events = doc.DocumentElement.SelectSingleNode("Events");
                events.InnerXml = sb.ToString();

                File.WriteAllText(Path.Combine(folderBrowserDialog1.SelectedPath, "BDN_Index.xml"), doc.OuterXml);
                progressBar1.Visible = false;
                MessageBox.Show(string.Format(Configuration.Settings.Language.ExportPngXml.XImagesSavedInY, imagesSavedCount, folderBrowserDialog1.SelectedPath));
            }
        }

        private void SetupImageParameters()
        {
            if (_isLoading)
                return;

            _subtitleColor = panelColor.BackColor;
            _borderColor = panelBorderColor.BackColor;
            _subtitleFontName = comboBoxSubtitleFont.SelectedItem.ToString();
            _subtitleFontSize = float.Parse(comboBoxSubtitleFontSize.SelectedItem.ToString());
            _borderWidth = float.Parse(comboBoxBorderWidth.SelectedItem.ToString());
        }

        private Bitmap GenerateImageFromText(string text)
        {
            Font font = new System.Drawing.Font(_subtitleFontName, _subtitleFontSize);
            Bitmap bmp = new Bitmap(400, 200);
            Graphics g = Graphics.FromImage(bmp);
            SizeF textSize = g.MeasureString(text, font);
            g.Dispose();
            bmp.Dispose();
            bmp = new Bitmap((int)(textSize.Width * 0.8), (int)(textSize.Height * 0.7));
            g = Graphics.FromImage(bmp);
            if (checkBoxAntiAlias.Checked)
            {
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.SmoothingMode = SmoothingMode.AntiAlias;
            }
            StringFormat sf = new StringFormat();
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;// draw the text to a path            
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddString(text, font.FontFamily, 0, font.Size, new Point(bmp.Width / 2, bmp.Height / 2), sf);
            g.FillPath(new SolidBrush(_subtitleColor), path);
            if (_borderWidth > 0)
                g.DrawPath(new Pen(_borderColor, _borderWidth), path);
            g.Dispose();
            return bmp;
        }


        internal void Initialize(Subtitle subtitle)
        {
            this.Text = Configuration.Settings.Language.ExportPngXml.Title;
            groupBoxImageSettings.Text = Configuration.Settings.Language.ExportPngXml.ImageSettings;
            labelSubtitleFont.Text = Configuration.Settings.Language.ExportPngXml.FontFamily;
            labelSubtitleFontSize.Text = Configuration.Settings.Language.ExportPngXml.FontSize;
            buttonColor.Text = Configuration.Settings.Language.ExportPngXml.FontColor;
            checkBoxAntiAlias.Text = Configuration.Settings.Language.ExportPngXml.AntiAlias;
            buttonBorderColor.Text = Configuration.Settings.Language.ExportPngXml.BorderColor;
            labelBorderWidth.Text = Configuration.Settings.Language.ExportPngXml.BorderWidth;
            buttonExport.Text = Configuration.Settings.Language.ExportPngXml.ExportAllLines;
            buttonCancel.Text = Configuration.Settings.Language.General.Cancel;
            labelImageResolution.Text = string.Empty;
            subtitleListView1.InitializeLanguage(Configuration.Settings.Language.General, Configuration.Settings);
            Utilities.InitializeSubtitleFont(subtitleListView1);
            subtitleListView1.AutoSizeAllColumns(this);

            _subtitle = subtitle;
            panelColor.BackColor = _subtitleColor;
            panelBorderColor.BackColor = _borderColor;
            comboBoxBorderWidth.SelectedIndex = 1;

            foreach (var x in System.Drawing.FontFamily.Families)
            {
                comboBoxSubtitleFont.Items.Add(x.Name);
                if (string.Compare(x.Name, _subtitleFontName, true) == 0)
                    comboBoxSubtitleFont.SelectedIndex = comboBoxSubtitleFont.Items.Count - 1;
            }

            subtitleListView1.Fill(_subtitle);
            subtitleListView1.SelectIndexAndEnsureVisible(0);

            _isLoading = false;
            comboBoxSubtitleFontSize.SelectedIndex = 15;
        }

        private void subtitleListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetupImageParameters();
            if (subtitleListView1.SelectedItems.Count > 0)
            {
                var bmp = GenerateImageFromText(_subtitle.Paragraphs[subtitleListView1.SelectedItems[0].Index].Text);
                pictureBox1.Image = bmp;
                labelImageResolution.Text = string.Format("{0}x{1}", bmp.Width, bmp.Height);
            }
        }

        private void buttonColor_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = panelColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                panelColor.BackColor = colorDialog1.Color;
                subtitleListView1_SelectedIndexChanged(null, null);
            }
        }

        private void panelColor_MouseClick(object sender, MouseEventArgs e)
        {
            buttonColor_Click(null, null);
        }


        private void buttonBorderColor_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = panelBorderColor.BackColor;
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                panelBorderColor.BackColor = colorDialog1.Color;
                subtitleListView1_SelectedIndexChanged(null, null);
            }
        }

        private void panelBorderColor_MouseClick(object sender, MouseEventArgs e)
        {
            buttonBorderColor_Click(null, null);
        }

        private void comboBoxSubtitleFont_SelectedValueChanged(object sender, EventArgs e)
        {
            subtitleListView1_SelectedIndexChanged(null, null);
        }

        private void comboBoxSubtitleFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            subtitleListView1_SelectedIndexChanged(null, null);
        }

        private void comboBoxBorderWidth_SelectedIndexChanged(object sender, EventArgs e)
        {
            subtitleListView1_SelectedIndexChanged(null, null);
        }

        private void checkBoxAntiAlias_CheckedChanged(object sender, EventArgs e)
        {
            subtitleListView1_SelectedIndexChanged(null, null);
        }

        private void ExportPngXml_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
            }
            else if (e.KeyCode == Keys.F1)
            {
                Utilities.ShowHelp(string.Empty);
                e.SuppressKeyPress = true;
            }
        }

    }
}