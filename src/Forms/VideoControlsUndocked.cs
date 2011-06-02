﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Nikse.SubtitleEdit.Logic;

namespace Nikse.SubtitleEdit.Forms
{
    public partial class VideoControlsUndocked : Form
    {
        Main _mainForm = null;
        PositionsAndSizes _positionsAndSizes = null;
        Keys _redockKeys;

        public Panel PanelContainer
        {
            get
            {
                return panelContainer;
            }
        }

        public VideoControlsUndocked(Main mainForm, PositionsAndSizes positionsAndSizes)
        {
            InitializeComponent();
            _mainForm = mainForm;
            this.Icon = (Icon)mainForm.Icon.Clone();
            _positionsAndSizes = positionsAndSizes;
            _redockKeys = _mainForm.GetKeys(Configuration.Settings.Shortcuts.MainVideoReDockVideoControls);
        }

        public VideoControlsUndocked()
        {
            // TODO: Complete member initialization
        }

        private void VideoControlsUndocked_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && panelContainer.Controls.Count > 0)
            {
                ReDock();
            }
            _positionsAndSizes.SavePositionAndSize(this);
        }

        private void ReDock()
        {
            var control = panelContainer.Controls[0];
            var controlCheckBox = panelContainer.Controls[1];
            panelContainer.Controls.Clear();
            _mainForm.ReDockVideoButtons(control, controlCheckBox);
        }

        private void VideoControlsUndocked_KeyDown(object sender, KeyEventArgs e)
        {
            if (_redockKeys == e.KeyData)
                _mainForm.redockVideoControlsToolStripMenuItem_Click(null, null);
        }
    }
}
